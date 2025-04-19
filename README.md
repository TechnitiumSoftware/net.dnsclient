# DNS Client
DNS Client is an ASP.NET Core web application hosted on https://dnsclient.net/.

# Features
- Works on Windows, Linux, macOS and Raspberry Pi.
- Docker image available on [Docker Hub](https://hub.docker.com/r/technitium/dns-client).
- Web app interface works with any modern web browser like Chrome, FireFox or Edge.
- Allows querying any DNS server.
- Supports DNSSEC validation with RSA, ECDSA and EdDSA algorithms for all DNS transport protocols.
- Supports DNS-over-HTTPS, DNS-over-TLS and DNS-over-QUIC protocols.
- Built-in recursive resolver to automatically query authoritative name servers.
- Supports IPv6.
- Open source cross-platform .NET implementation hosted on GitHub.

# Linux / Raspberry Pi Automated Installer And Updater
```
curl -sSL https://download.technitium.com/dnsclient/install.sh | sudo bash
```
Run the above command in Terminal or using SSH to install or update the DNS Client.

Note! Raspberry Pi with an arm7 CPU is supported and thus both Raspberry Pi 1 and Raspberry Pi Zero which have arm6 CPU are not supported.

# Docker
```
docker pull technitium/dns-client:latest
```
Pull the official image from [Docker Hub](https://hub.docker.com/r/technitium/dns-client). Use the [docker-compose.yml](https://github.com/TechnitiumSoftware/net.dnsclient/blob/master/docker-compose.yml) example to create a new container and edit it as required for your deployments.

# Manual Installation

## System Requirements
- Requires [ASP.NET Core 8](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) installed.

## Download
- [DnsClientPortable.tar.gz](https://go.technitium.com/?id=26)

## Manual Install Instructions
- Install [ASP.NET Core 8](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) runtime.
- Extract the downloaded DNS Client tar archive.
- Run start.bat on Windows or start.sh on Linux to start the web app.
- Open http://localhost:8001/ in any web browser to use the web app.
- Edit the *appsettings.json* file for changing advanced options like enabling IPv6 preference.

# Support
For support, send an email to support@technitium.com. For any issues, feedback, or feature request, create an issue on [GitHub](https://github.com/TechnitiumSoftware/net.dnsclient/issues).

# Become A Patron
Make contribution to Technitium and help making new software, updates, and features possible.

[Donate Now!](https://www.patreon.com/technitium)
