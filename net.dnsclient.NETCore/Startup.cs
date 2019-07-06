/*
Technitium dnsclient.net
Copyright (C) 2019  Shreyas Zare (shreyas@technitium.com)

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
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Net;
using System.Threading.Tasks;
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

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
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
                await Task.Run(() =>
                {
                    HttpRequest Request = context.Request;
                    HttpResponse Response = context.Response;

                    if (Request.Path == "/api/dnsclient/")
                    {
                        try
                        {
                            string server = Request.Query["server"];
                            string domain = Request.Query["domain"];
                            DnsResourceRecordType type = (DnsResourceRecordType)Enum.Parse(typeof(DnsResourceRecordType), Request.Query["type"], true);

                            if (domain.EndsWith("."))
                                domain = domain.Substring(0, domain.Length - 1);

                            bool preferIpv6 = Configuration.GetValue<bool>("PreferIpv6");
                            int retries = Configuration.GetValue<int>("Retries");
                            int timeout = Configuration.GetValue<int>("Timeout");

                            DnsDatagram dnsResponse;

                            if (server == "recursive-resolver")
                            {
                                bool useTcp = Configuration.GetValue<bool>("UseTcpForRecursion");
                                DnsQuestionRecord question;

                                if (type == DnsResourceRecordType.PTR)
                                    question = new DnsQuestionRecord(IPAddress.Parse(domain), DnsClass.IN);
                                else
                                    question = new DnsQuestionRecord(domain, type, DnsClass.IN);

                                dnsResponse = DnsClient.RecursiveResolve(question, null, null, null, preferIpv6, retries, timeout, useTcp);
                            }
                            else
                            {
                                DnsTransportProtocol protocol = (DnsTransportProtocol)Enum.Parse(typeof(DnsTransportProtocol), Request.Query["protocol"], true);
                                NameServerAddress nameServer = new NameServerAddress(server);

                                if (nameServer.IPEndPoint == null)
                                {
                                    nameServer.ResolveIPAddress(null, null, preferIpv6);
                                }
                                else if (nameServer.DomainEndPoint == null)
                                {
                                    try
                                    {
                                        nameServer.ResolveDomainName(null, null, preferIpv6);
                                    }
                                    catch
                                    { }
                                }

                                DnsClient dnsClient = new DnsClient(nameServer);

                                dnsClient.PreferIPv6 = preferIpv6;
                                dnsClient.Protocol = protocol;
                                dnsClient.Retries = retries;
                                dnsClient.Timeout = timeout;

                                dnsResponse = dnsClient.Resolve(domain, type);

                                if (dnsResponse.Header.Truncation && (dnsClient.Protocol == DnsTransportProtocol.Udp))
                                {
                                    dnsClient.Protocol = DnsTransportProtocol.Tcp;
                                    dnsResponse = dnsClient.Resolve(domain, type);
                                }
                            }

                            string jsonResponse = JsonConvert.SerializeObject(dnsResponse, new StringEnumConverter());

                            Response.Headers.Add("Content-Type", "application/json; charset=utf-8");
                            Response.WriteAsync("{\"status\":\"ok\", \"response\":" + jsonResponse + "}");
                        }
                        catch (Exception ex)
                        {
                            string jsonResponse = JsonConvert.SerializeObject(ex);

                            Response.Headers.Add("Content-Type", "application/json; charset=utf-8");
                            Response.WriteAsync("{\"status\":\"error\", \"response\":" + jsonResponse + "}");
                        }
                    }
                });
            });
        }
    }
}
