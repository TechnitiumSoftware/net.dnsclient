/*
Technitium DNS Client
Copyright (C) 2018  Shreyas Zare (shreyas@technitium.com)

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

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Net;
using System.Web.UI;
using TechnitiumLibrary.Net.Dns;

namespace net.dnsclient.api.dnsclient
{
    public partial class _default : Page
    {
        const bool PREFER_IPv6 = false;
        const DnsClientProtocol PROTOCOL = DnsClientProtocol.Tcp;
        const int RETRIES = 2;
        const int MAX_STACK_COUNT = 10;

        static _default()
        {
            DnsClient.RecursiveResolveDefaultProtocol = DnsClientProtocol.Tcp;
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            try
            {
                string server = Request.QueryString["server"];
                string domain = Request.QueryString["domain"];
                DnsResourceRecordType type = (DnsResourceRecordType)Enum.Parse(typeof(DnsResourceRecordType), Request.QueryString["type"]);

                DnsDatagram dnsResponse;

                if (server == "root-servers")
                {
                    dnsResponse = DnsClient.ResolveViaRootNameServers(domain, type, new SimpleDnsCache(), null, PREFER_IPv6, PROTOCOL, RETRIES, MAX_STACK_COUNT);
                }
                else
                {
                    NameServerAddress[] nameServers;

                    if (IPAddress.TryParse(server, out IPAddress serverIP))
                    {
                        string serverDomain = null;

                        try
                        {
                            serverDomain = (new DnsClient() { PreferIPv6 = PREFER_IPv6, Protocol = PROTOCOL, Retries = RETRIES }).ResolvePTR(serverIP);
                        }
                        catch
                        { }

                        nameServers = new NameServerAddress[] { new NameServerAddress(serverDomain, serverIP) };
                    }
                    else
                    {
                        IPAddress[] serverIPs = (new DnsClient() { PreferIPv6 = PREFER_IPv6, Protocol = PROTOCOL, Retries = RETRIES }).ResolveIP(server, PREFER_IPv6);

                        nameServers = new NameServerAddress[serverIPs.Length];

                        for (int i = 0; i < serverIPs.Length; i++)
                            nameServers[i] = new NameServerAddress(server, serverIPs[i]);
                    }

                    dnsResponse = (new DnsClient(nameServers) { PreferIPv6 = PREFER_IPv6, Protocol = PROTOCOL, Retries = RETRIES }).Resolve(domain, type);
                }

                string jsonResponse = JsonConvert.SerializeObject(dnsResponse, new StringEnumConverter());

                Response.AddHeader("Content-Type", "application/json; charset=utf-8");
                Response.Write("{\"status\":\"ok\", \"response\":" + jsonResponse + "}");
            }
            catch (Exception ex)
            {
                string jsonResponse = JsonConvert.SerializeObject(ex);

                Response.AddHeader("Content-Type", "application/json; charset=utf-8");
                Response.Write("{\"status\":\"error\", \"response\":" + jsonResponse + "}");
            }
        }
    }
}