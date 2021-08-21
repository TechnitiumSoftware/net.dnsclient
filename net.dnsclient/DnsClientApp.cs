/*
Technitium dnsclient.net
Copyright (C) 2021  Shreyas Zare (shreyas@technitium.com)

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <http://www.gnu.org/licenses/>.

*/

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Net;
using System.Reflection;
using TechnitiumLibrary.Net.Dns;

namespace net.dnsclient
{
    public class DnsClientApp
    {
        public IConfiguration Configuration { get; set; }

        public DnsClientApp(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public void Configure(IApplicationBuilder app, IHostEnvironment env)
        {
            int staticFilesCachePeriod;

            if (env.IsDevelopment())
            {
                staticFilesCachePeriod = 60;
                app.UseDeveloperExceptionPage();
            }
            else
            {
                staticFilesCachePeriod = 14400;
            }

            app.UseDefaultFiles();
            app.UseStaticFiles(new StaticFileOptions()
            {
                OnPrepareResponse = delegate (StaticFileResponseContext ctx)
                {
                    ctx.Context.Response.Headers.Append("Cache-Control", $"public, max-age={staticFilesCachePeriod}");
                }
            });

            app.Run(async (context) =>
            {
                HttpRequest request = context.Request;
                HttpResponse response = context.Response;

                switch (request.Path)
                {
                    case "/api/dnsclient/":
                        try
                        {
                            string server = request.Query["server"];
                            string domain = request.Query["domain"];
                            DnsResourceRecordType type = Enum.Parse<DnsResourceRecordType>(request.Query["type"], true);

                            domain = domain.Trim(new char[] { '\t', ' ', '.' });

                            bool preferIpv6 = Configuration.GetValue<bool>("PreferIpv6");
                            bool randomizeName = false;
                            bool qnameMinimization = false;
                            int retries = Configuration.GetValue<int>("Retries");
                            int timeout = Configuration.GetValue<int>("Timeout");

                            DnsDatagram dnsResponse;

                            if (server == "recursive-resolver")
                            {
                                DnsQuestionRecord question;

                                if ((type == DnsResourceRecordType.PTR) && IPAddress.TryParse(domain, out IPAddress address))
                                    question = new DnsQuestionRecord(address, DnsClass.IN);
                                else
                                    question = new DnsQuestionRecord(domain, type, DnsClass.IN);

                                dnsResponse = await DnsClient.RecursiveResolveAsync(question, null, null, preferIpv6, randomizeName, qnameMinimization, false, retries, timeout);
                            }
                            else
                            {
                                DnsTransportProtocol protocol = Enum.Parse<DnsTransportProtocol>(request.Query["protocol"], true);
                                NameServerAddress nameServer = new NameServerAddress(server);

                                if (nameServer.Protocol != protocol)
                                    nameServer = nameServer.ChangeProtocol(protocol);

                                if (nameServer.IPEndPoint == null)
                                {
                                    await nameServer.ResolveIPAddressAsync(new DnsClient() { PreferIPv6 = preferIpv6, RandomizeName = randomizeName, Retries = retries, Timeout = timeout }, preferIpv6);
                                }
                                else if (nameServer.DomainEndPoint == null)
                                {
                                    try
                                    {
                                        await nameServer.ResolveDomainNameAsync(new DnsClient() { PreferIPv6 = preferIpv6, RandomizeName = randomizeName, Retries = retries, Timeout = timeout });
                                    }
                                    catch
                                    { }
                                }

                                DnsClient dnsClient = new DnsClient(nameServer);

                                dnsClient.PreferIPv6 = preferIpv6;
                                dnsClient.RandomizeName = randomizeName;
                                dnsClient.Retries = retries;
                                dnsClient.Timeout = timeout;

                                dnsResponse = await dnsClient.ResolveAsync(domain, type);
                            }

                            string jsonResponse = JsonConvert.SerializeObject(dnsResponse, new StringEnumConverter());

                            response.Headers.Add("Content-Type", "application/json; charset=utf-8");
                            await response.WriteAsync("{\"status\":\"ok\", \"response\":" + jsonResponse + "}");
                        }
                        catch (Exception ex)
                        {
                            string jsonResponse = JsonConvert.SerializeObject(ex);

                            response.Headers.Add("Content-Type", "application/json; charset=utf-8");
                            await response.WriteAsync("{\"status\":\"error\", \"response\":" + jsonResponse + "}");
                        }
                        break;

                    case "/api/version":
                        response.Headers.Add("Content-Type", "application/json; charset=utf-8");
                        await response.WriteAsync("{\"status\":\"ok\", \"response\": {\"version\": \"" + GetCleanVersion(Assembly.GetExecutingAssembly().GetName().Version) + "\"}}");
                        break;
                }
            });
        }

        private static string GetCleanVersion(Version version)
        {
            string strVersion = version.Major + "." + version.Minor;

            if (version.Build > 0)
                strVersion += "." + version.Build;

            if (version.Revision > 0)
                strVersion += "." + version.Revision;

            return strVersion;
        }
    }
}
