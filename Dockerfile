FROM mcr.microsoft.com/dotnet/aspnet:6.0
LABEL product="Technitium DNS Client"
LABEL vendor="Technitium"
LABEL email="support@technitium.com"
LABEL project_url="https://dnsclient.net/"
LABEL github_url="https://github.com/TechnitiumSoftware/net.dnsclient"

WORKDIR /opt/dnsclient/

COPY ./net.dnsclient/bin/Release/publish/ .

EXPOSE 8001/tcp

CMD ["/usr/bin/dotnet", "/opt/dnsclient/DnsClientApp.dll", "--urls", "http://0.0.0.0:8001/"]
