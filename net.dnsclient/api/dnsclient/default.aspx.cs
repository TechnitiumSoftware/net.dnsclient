using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Web.UI;
using TechnitiumLibrary.Net;

namespace net.dnsclient.api.dnsclient
{
    public partial class _default : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            try
            {
                string server = Request.QueryString["server"];
                string domain = Request.QueryString["domain"];
                DnsRecordType type = (DnsRecordType)Enum.Parse(typeof(DnsRecordType), Request.QueryString["type"]);

                DnsDatagram dnsResponse;

                if (server == "root-servers")
                {
                    dnsResponse = DnsClient.ResolveViaRootNameServers(domain, type);
                }
                else
                {
                    using (DnsClient client = new DnsClient(server))
                    {
                        dnsResponse = client.Resolve(domain, type);
                    }
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