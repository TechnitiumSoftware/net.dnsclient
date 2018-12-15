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
using System.Web.UI;
using TechnitiumLibrary.Net.Dns;

namespace net.dnsclient.api.dnsclient
{
    public partial class _default : Page
    {
        const bool PREFER_IPv6 = false;
        const DnsClientProtocol PROTOCOL = DnsClientProtocol.Tcp;
        const DnsClientProtocol RECURSIVE_RESOLVE_PROTOCOL = DnsClientProtocol.Tcp;
        const int RETRIES = 2;
        const int TIMEOUT = 2000;

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
                    dnsResponse = DnsClient.ResolveViaRootNameServers(domain, type, new SimpleDnsCache(), null, PREFER_IPv6, PROTOCOL, RETRIES, TIMEOUT, RECURSIVE_RESOLVE_PROTOCOL);
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