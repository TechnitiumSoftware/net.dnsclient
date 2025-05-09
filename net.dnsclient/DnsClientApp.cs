﻿/*
Technitium dnsclient.net
Copyright (C) 2025  Shreyas Zare (shreyas@technitium.com)

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
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using TechnitiumLibrary.Net;
using TechnitiumLibrary.Net.Dns;
using TechnitiumLibrary.Net.Dns.ResourceRecords;

namespace net.dnsclient
{
    public class DnsClientApp
    {
        static readonly char[] domainTrimChars = new char[] { '\t', ' ', '.' };

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

                            NetworkAddress eDnsClientSubnet = null;
                            string strEDnsClientSubnet = request.Query["eDnsClientSubnet"];
                            if (!string.IsNullOrEmpty(strEDnsClientSubnet))
                            {
                                eDnsClientSubnet = NetworkAddress.Parse(strEDnsClientSubnet);
                                switch (eDnsClientSubnet.AddressFamily)
                                {
                                    case AddressFamily.InterNetwork:
                                        if (eDnsClientSubnet.PrefixLength == 32)
                                            eDnsClientSubnet = new NetworkAddress(eDnsClientSubnet.Address, 24);

                                        break;

                                    case AddressFamily.InterNetworkV6:
                                        if (eDnsClientSubnet.PrefixLength == 128)
                                            eDnsClientSubnet = new NetworkAddress(eDnsClientSubnet.Address, 56);

                                        break;
                                }
                            }

                            bool dnssecValidation = false;
                            string strDnssecValidation = request.Query["dnssec"];
                            if (!string.IsNullOrEmpty(strDnssecValidation))
                                dnssecValidation = bool.Parse(strDnssecValidation);

                            domain = domain.Trim(domainTrimChars);

                            bool preferIpv6 = Configuration.GetValue<bool>("PreferIpv6");
                            ushort udpPayloadSize = Configuration.GetValue<ushort>("UdpPayloadSize");
                            bool randomizeName = false;
                            bool qnameMinimization = false;
                            int retries = Configuration.GetValue<int>("Retries");
                            int timeout = Configuration.GetValue<int>("Timeout");

                            DnsDatagram dnsResponse;
                            List<DnsDatagram> rawResponses = new List<DnsDatagram>();
                            string dnssecErrorMessage = null;

                            if (server.Equals("recursive-resolver", StringComparison.OrdinalIgnoreCase))
                            {
                                DnsQuestionRecord question;

                                if ((type == DnsResourceRecordType.PTR) && IPAddress.TryParse(domain, out IPAddress address))
                                    question = new DnsQuestionRecord(address, DnsClass.IN);
                                else
                                    question = new DnsQuestionRecord(domain, type, DnsClass.IN);

                                DnsCache dnsCache = new DnsCache();
                                dnsCache.MinimumRecordTtl = 0;
                                dnsCache.MaximumRecordTtl = 7 * 24 * 60 * 60;

                                try
                                {
                                    dnsResponse = await TechnitiumLibrary.TaskExtensions.TimeoutAsync(async delegate (CancellationToken cancellationToken1)
                                    {
                                        return await DnsClient.RecursiveResolveAsync(question, dnsCache, null, preferIpv6, udpPayloadSize, randomizeName, qnameMinimization, dnssecValidation, eDnsClientSubnet, retries, timeout, rawResponses: rawResponses, cancellationToken: cancellationToken1);
                                    }, 60000);
                                }
                                catch (DnsClientResponseDnssecValidationException ex)
                                {
                                    if (ex.InnerException is DnsClientResponseDnssecValidationException ex1)
                                        ex = ex1;

                                    dnsResponse = ex.Response;
                                    dnssecErrorMessage = ex.Message;
                                }
                            }
                            else if (server.Equals("system-dns", StringComparison.OrdinalIgnoreCase))
                            {
                                DnsClient dnsClient = new DnsClient();

                                dnsClient.PreferIPv6 = preferIpv6;
                                dnsClient.RandomizeName = randomizeName;
                                dnsClient.Retries = retries;
                                dnsClient.Timeout = timeout;
                                dnsClient.UdpPayloadSize = udpPayloadSize;
                                dnsClient.DnssecValidation = dnssecValidation;
                                dnsClient.EDnsClientSubnet = eDnsClientSubnet;

                                try
                                {
                                    dnsResponse = await dnsClient.ResolveAsync(domain, type);
                                }
                                catch (DnsClientResponseDnssecValidationException ex)
                                {
                                    if (ex.InnerException is DnsClientResponseDnssecValidationException ex1)
                                        ex = ex1;

                                    dnsResponse = ex.Response;
                                    dnssecErrorMessage = ex.Message;
                                }
                            }
                            else
                            {
                                DnsTransportProtocol protocol = Enum.Parse<DnsTransportProtocol>(request.Query["protocol"], true);
                                NameServerAddress nameServer = NameServerAddress.Parse(server);

                                if (nameServer.Protocol != protocol)
                                    nameServer = nameServer.ChangeProtocol(protocol);

                                if (nameServer.IsIPEndPointStale)
                                {
                                    await nameServer.ResolveIPAddressAsync(new DnsClient() { PreferIPv6 = preferIpv6, RandomizeName = randomizeName, Retries = retries, Timeout = timeout }, preferIpv6);
                                }
                                else if ((nameServer.DomainEndPoint is null) && ((protocol == DnsTransportProtocol.Udp) || (protocol == DnsTransportProtocol.Tcp)))
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
                                dnsClient.UdpPayloadSize = udpPayloadSize;
                                dnsClient.DnssecValidation = dnssecValidation;
                                dnsClient.EDnsClientSubnet = eDnsClientSubnet;

                                try
                                {
                                    dnsResponse = await dnsClient.ResolveAsync(domain, type);
                                }
                                catch (DnsClientResponseDnssecValidationException ex)
                                {
                                    if (ex.InnerException is DnsClientResponseDnssecValidationException ex1)
                                        ex = ex1;

                                    dnsResponse = ex.Response;
                                    dnssecErrorMessage = ex.Message;
                                }
                            }

                            using (MemoryStream mS = new MemoryStream(4096))
                            {
                                Utf8JsonWriter jsonWriter = new Utf8JsonWriter(mS);
                                jsonWriter.WriteStartObject();

                                if (dnssecErrorMessage is null)
                                {
                                    jsonWriter.WriteString("status", "ok");
                                }
                                else
                                {
                                    jsonWriter.WriteString("status", "warning");
                                    jsonWriter.WriteString("warningMessage", dnssecErrorMessage);
                                }

                                jsonWriter.WritePropertyName("response");
                                dnsResponse.SerializeTo(jsonWriter);

                                jsonWriter.WritePropertyName("rawResponses");
                                jsonWriter.WriteStartArray();

                                for (int i = 0; i < rawResponses.Count; i++)
                                    rawResponses[i].SerializeTo(jsonWriter);

                                jsonWriter.WriteEndArray();

                                jsonWriter.WriteEndObject();
                                jsonWriter.Flush();

                                response.ContentType = "application/json; charset=utf-8";
                                response.ContentLength = mS.Length;

                                mS.Position = 0;
                                using (Stream stream = response.Body)
                                {
                                    await mS.CopyToAsync(stream);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            using (MemoryStream mS = new MemoryStream(4096))
                            {
                                Utf8JsonWriter jsonWriter = new Utf8JsonWriter(mS);
                                jsonWriter.WriteStartObject();

                                jsonWriter.WriteString("status", "error");
                                jsonWriter.WriteString("errorMessage", ex.Message);
                                jsonWriter.WriteString("stackTrace", ex.StackTrace);

                                if (ex.InnerException != null)
                                    jsonWriter.WriteString("innerErrorMessage", ex.InnerException.Message);

                                jsonWriter.WriteEndObject();
                                jsonWriter.Flush();

                                response.ContentType = "application/json; charset=utf-8";
                                response.ContentLength = mS.Length;

                                mS.Position = 0;
                                using (Stream stream = response.Body)
                                {
                                    await mS.CopyToAsync(stream);
                                }
                            }
                        }
                        break;

                    case "/api/version":
                        response.Headers.ContentType = "application/json; charset=utf-8";
                        await response.WriteAsync("{\"status\":\"ok\", \"response\": {\"version\": \"" + GetCleanVersion(Assembly.GetExecutingAssembly().GetName().Version) + "\"}}");
                        break;

                    default:
                        response.StatusCode = (int)HttpStatusCode.NotFound;
                        response.ContentLength = 0;
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
