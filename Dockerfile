# syntax=docker.io/docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/aspnet:9.0

# Add the MS repo to install `libmsquic` to support DNS-over-QUIC:
ADD --link https://packages.microsoft.com/config/debian/12/packages-microsoft-prod.deb /
RUN <<HEREDOC
  dpkg -i packages-microsoft-prod.deb && rm packages-microsoft-prod.deb
  apt-get update && apt-get install -y libmsquic
  apt-get clean -y && rm -rf /var/lib/apt/lists/*
HEREDOC

# Project is built outside of Docker, copy over the build directory:
WORKDIR /opt/technitium/dnsclient
COPY --link ./net.dnsclient/bin/Release/publish /opt/technitium/dnsclient

ENTRYPOINT ["/usr/bin/dotnet", "/opt/technitium/dnsclient/DnsClientApp.dll"]
CMD ["--urls", "http://0.0.0.0:8001/"]


## Only append image metadata below this line:
EXPOSE 8001/tcp

# https://specs.opencontainers.org/image-spec/annotations/
# https://github.com/opencontainers/image-spec/blob/main/annotations.md
LABEL org.opencontainers.image.title="Technitium DNS Client"
LABEL org.opencontainers.image.vendor="Technitium"
LABEL org.opencontainers.image.source="https://github.com/TechnitiumSoftware/net.dnsclient"
LABEL org.opencontainers.image.url="https://dnsclient.net/"
LABEL org.opencontainers.image.authors="support@technitium.com"
