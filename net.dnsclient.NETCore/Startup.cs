/*
Technitium dnsclient.net
Copyright (C) 2020  Shreyas Zare (shreyas@technitium.com)

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
using TechnitiumLibrary.Net.Dns;

namespace net.dnsclient.NETCore
{
    public class Startup
    {
        public IConfiguration Configuration { get; set; }

        public Startup(IConfiguration configuration)
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

                if (request.Path == "/api/dnsclient/")
                {
                    try
                    {
                        string server = request.Query["server"];
                        string domain = request.Query["domain"];
                        DnsResourceRecordType type = (DnsResourceRecordType)Enum.Parse(typeof(DnsResourceRecordType), request.Query["type"], true);

                        domain = domain.Trim();

                        if (domain.EndsWith("."))
                            domain = domain.Substring(0, domain.Length - 1);

                        bool preferIpv6 = Configuration.GetValue<bool>("PreferIpv6");
                        bool randomizeName = false;
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

                            dnsResponse = await DnsClient.RecursiveResolveAsync(question, null, null, null, preferIpv6, randomizeName, retries, timeout);
                        }
                        else
                        {
                            DnsTransportProtocol protocol = (DnsTransportProtocol)Enum.Parse(typeof(DnsTransportProtocol), request.Query["protocol"], true);

                            if ((protocol == DnsTransportProtocol.Tls) && !server.Contains(":853"))
                                server += ":853";

                            NameServerAddress nameServer = new NameServerAddress(server, protocol);

                            if (nameServer.IPEndPoint == null)
                            {
                                await nameServer.ResolveIPAddressAsync(null, null, preferIpv6);
                            }
                            else if (nameServer.DomainEndPoint == null)
                            {
                                try
                                {
                                    await nameServer.ResolveDomainNameAsync(null, null, preferIpv6);
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
                }
            });
        }
    }
}
