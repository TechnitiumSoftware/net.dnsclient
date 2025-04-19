#!/bin/sh

dotnetDir="/opt/dotnet"

dnsClientDir="/opt/technitium/dnsclient"

echo ""
echo "================================="
echo "Technitium DNS Client Uninstaller"
echo "================================="
echo ""
echo "Uninstalling Technitium DNS Client..."

if [ -d $dnsClientDir ]
then
	if [ "$(ps --no-headers -o comm 1 | tr -d '\n')" = "systemd" ] 
	then
		sudo systemctl disable dnsclient.service >/dev/null 2>&1
		sudo systemctl stop dnsclient.service >/dev/null 2>&1
		rm /etc/systemd/system/dnsclient.service >/dev/null 2>&1
	fi

	rm -rf $dnsClientDir >/dev/null 2>&1

	if [ -d $dotnetDir ]
	then
		echo "Uninstalling .NET Runtime..."
		rm /usr/bin/dotnet >/dev/null 2>&1
		rm -rf $dotnetDir >/dev/null 2>&1
	fi
fi

echo ""
echo "Thank you for using Technitium DNS Client!"
