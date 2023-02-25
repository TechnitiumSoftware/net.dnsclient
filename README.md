# DNS Client
DNS Client is an ASP.NET Core web application hosted on https://dnsclient.net/. It can also be downloaded as a portable web app and run locally.

# Features
- Standalone portable web app available for Windows, Linux and macOS.
- Allows querying any DNS server.
- Supports DNSSEC validation with RSA and ECDSA algorithm for all DNS transport protocols.
- Supports DNS-over-HTTPS, DNS-over-TLS, and DNS-over-QUIC protocols.
- Built-in recursive resolver module to automatically query authoritative name servers.
- Supports IPv6.
- Supports HTTP REST API that returns JSON response.
- Open source cross-platform .NET implementation hosted on GitHub.

# System Requirements
- Requires [ASP.NET Core 7](https://dotnet.microsoft.com/download) installed.
- Windows, Linux and macOS supported.
- Web app interface works with any modern web browser like Chrome, FireFox or Edge.

# Download
- [DnsClientPortable.tar.gz](https://go.technitium.com/?id=26)

# Usage Instructions
- Install [ASP.NET Core 7](https://dotnet.microsoft.com/download) runtime.
- Extract the downloaded DNS Client tar archive.
- Run start.bat on Windows or start.sh on Linux to start the web app.
- Open http://localhost:8001/ in any web browser to use the web app.
- Edit the *appsettings.json* file for changing advance options like enabling IPv6 preference.

# Docker
Pull the official image from [Docker Hub](https://hub.docker.com/r/technitium/dns-client). Use the [docker-compose.yml](https://github.com/TechnitiumSoftware/net.dnsclient/blob/master/docker-compose.yml) example to create a new container and edit it as required for your deployments.

# Support
For support, send an email to support@technitium.com. For any issues, feedback, or feature request, create an issue on [GitHub](https://github.com/TechnitiumSoftware/net.dnsclient/issues).

# Become A Patron
Make contribution to Technitium and help making new software, updates, and features possible.

[Donate Now!](https://www.patreon.com/technitium)
