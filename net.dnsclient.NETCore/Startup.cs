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
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using TechnitiumLibrary.Net.Dns;

namespace net.dnsclient.NETCore
{
    public class Startup
    {
        const bool PREFER_IPv6 = false;
        const DnsTransportProtocol PROTOCOL = DnsTransportProtocol.Udp;
        const DnsTransportProtocol RECURSIVE_RESOLVE_PROTOCOL = DnsTransportProtocol.Udp;
        const int RETRIES = 2;
        const int TIMEOUT = 2000;

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseDefaultFiles();
            app.UseStaticFiles();

            app.Run(async (context) =>
            {
                HttpRequest Request = context.Request;
                HttpResponse Response = context.Response;

                if (Request.Path == "/api/dnsclient/")
                {
                    try
                    {
                        string server = Request.Query["server"];
                        string domain = Request.Query["domain"];
                        DnsResourceRecordType type = (DnsResourceRecordType)Enum.Parse(typeof(DnsResourceRecordType), Request.Query["type"]);

                        if (domain.EndsWith("."))
                            domain = domain.Substring(0, domain.Length - 1);

                        DnsDatagram dnsResponse;

                        if (server == "recursive-resolver")
                        {
                            dnsResponse = DnsClient.RecursiveResolve(domain, type, new SimpleDnsCache(), null, PREFER_IPv6, PROTOCOL, RETRIES, TIMEOUT, RECURSIVE_RESOLVE_PROTOCOL);
                        }
                        else
                        {
                            NameServerAddress nameServer = new NameServerAddress(server);

                            if (nameServer.IPEndPoint == null)
                            {
                                nameServer.ResolveIPAddress(null, null, PREFER_IPv6, PROTOCOL, RETRIES, TIMEOUT);
                            }
                            else if (nameServer.DomainEndPoint == null)
                            {
                                try
                                {
                                    nameServer.ResolveDomainName(null, null, PREFER_IPv6, PROTOCOL, RETRIES, TIMEOUT);
                                }
                                catch
                                { }
                            }

                            dnsResponse = (new DnsClient(nameServer) { PreferIPv6 = PREFER_IPv6, Protocol = PROTOCOL, Retries = RETRIES, Timeout = TIMEOUT, RecursiveResolveProtocol = RECURSIVE_RESOLVE_PROTOCOL }).Resolve(domain, type);
                        }

                        string jsonResponse = JsonConvert.SerializeObject(dnsResponse, new StringEnumConverter());

                        Response.Headers.Add("Content-Type", "application/json; charset=utf-8");
                        await Response.WriteAsync("{\"status\":\"ok\", \"response\":" + jsonResponse + "}");
                    }
                    catch (Exception ex)
                    {
                        string jsonResponse = JsonConvert.SerializeObject(ex);

                        Response.Headers.Add("Content-Type", "application/json; charset=utf-8");
                        await Response.WriteAsync("{\"status\":\"error\", \"response\":" + jsonResponse + "}");
                    }
                }
            });
        }
    }
}
