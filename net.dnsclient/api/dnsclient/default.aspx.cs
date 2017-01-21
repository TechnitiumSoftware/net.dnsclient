using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Net;
using System.Web.UI;
using TechnitiumLibrary.Net;

namespace net.dnsclient.api.dnsclient
{
    public partial class _default : Page
    {
        const bool IPv6 = false;
        const bool TCP = true;
        const int RETRIES = 2;

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
                    dnsResponse = DnsClient.ResolveViaRootNameServers(domain, type, IPv6, TCP, RETRIES);
                }
                else
                {
                    IPAddress serverIP;
                    NameServerAddress nameServer;

                    if (IPAddress.TryParse(server, out serverIP))
                    {
                        nameServer = new NameServerAddress(serverIP);
                    }
                    else
                    {
                        serverIP = (new DnsClient(new IPAddress[] { IPAddress.Parse("8.8.8.8"), IPAddress.Parse("8.8.4.4") }, IPv6, TCP, RETRIES)).ResolveIP(server);
                        nameServer = new NameServerAddress(server, serverIP);
                    }

                    dnsResponse = (new DnsClient(nameServer, IPv6, TCP, RETRIES)).Resolve(domain, type);
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