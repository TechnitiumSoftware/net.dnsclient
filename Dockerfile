FROM mcr.microsoft.com/dotnet/aspnet:7.0
LABEL product="Technitium DNS Client"
LABEL vendor="Technitium"
LABEL email="support@technitium.com"
LABEL project_url="https://dnsclient.net/"
LABEL github_url="https://github.com/TechnitiumSoftware/net.dnsclient"

WORKDIR /opt/technitium/dnsclient/

RUN apt update; apt install curl -y; \
curl https://packages.microsoft.com/config/debian/11/packages-microsoft-prod.deb --output packages-microsoft-prod.deb; \
dpkg -i packages-microsoft-prod.deb; \
rm packages-microsoft-prod.deb

RUN apt update; apt install libmsquic=2.1.8 -y; apt clean -y;

COPY ./net.dnsclient/bin/Release/publish/ .

EXPOSE 8001/tcp

ENTRYPOINT ["/usr/bin/dotnet", "/opt/technitium/dnsclient/DnsClientApp.dll"]
CMD ["--urls", "http://0.0.0.0:8001/"]
