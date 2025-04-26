#!/bin/sh

dotnetDir="/opt/dotnet"
dotnetVersion="8.0"
dotnetRuntime="Microsoft.AspNetCore.App 8.0."
dotnetUrl="https://dot.net/v1/dotnet-install.sh"

dnsClientDir="/opt/technitium/dnsclient"
dnsClientTar="$dnsClientDir/DnsClientPortable.tar.gz"
dnsClientUrl="https://download.technitium.com/dnsclient/DnsClientPortable.tar.gz"

installLog="$dnsClientDir/install.log"

echo ""
echo "==============================="
echo "Technitium DNS Client Installer"
echo "==============================="
echo ""

mkdir -p $dnsClientDir
echo "" > $installLog

if dotnet --list-runtimes 2> /dev/null | grep -q "$dotnetRuntime"; 
then
    dotnetFound="yes"
else
    dotnetFound="no"
fi

if [ ! -d $dotnetDir ] && [ "$dotnetFound" = "yes" ]
then
    echo "ASP.NET Core Runtime is already installed."
else
    if [ -d $dotnetDir ] && [ "$dotnetFound" = "yes" ]
    then
        dotnetUpdate="yes"
        echo "Updating ASP.NET Core Runtime..."
    else
        dotnetUpdate="no"
        echo "Installing ASP.NET Core Runtime..."
    fi

    curl -sSL $dotnetUrl | bash /dev/stdin -c $dotnetVersion --runtime aspnetcore --no-path --install-dir $dotnetDir --verbose >> $installLog 2>&1

    if [ ! -f "/usr/bin/dotnet" ]
    then
        ln -s $dotnetDir/dotnet /usr/bin >> $installLog 2>&1
    fi

    if dotnet --list-runtimes 2> /dev/null | grep -q "$dotnetRuntime"; 
    then
        if [ "$dotnetUpdate" = "yes" ]
        then
            echo "ASP.NET Core Runtime was updated successfully!"
        else
            echo "ASP.NET Core Runtime was installed successfully!"
        fi
    else
        echo "Failed to install ASP.NET Core Runtime. Please check '$installLog' for details."
        exit 1
    fi
fi

echo ""
echo "Downloading Technitium DNS Client..."

if ! curl -o $dnsClientTar --fail $dnsClientUrl >> $installLog 2>&1
then
    echo "Failed to download Technitium DNS Client from: $dnsClientUrl"
    echo "Please check '$installLog' for details."
    exit 1
fi

echo "Installing Technitium DNS Client..."

tar -zxf $dnsClientTar -C $dnsClientDir >> $installLog 2>&1

echo ""

if ! [ "$(ps --no-headers -o comm 1 | tr -d '\n')" = "systemd" ] 
then
    echo "Failed to install Technitium DNS Client: systemd was not detected."
    exit 1
fi

if [ -f "/etc/systemd/system/dnsclient.service" ]
then
    echo "Restarting systemd service..."
    systemctl restart dnsclient.service >> $installLog 2>&1
else
    echo "Configuring systemd service..."
    cp $dnsClientDir/systemd.service /etc/systemd/system/dnsclient.service
    systemctl enable dnsclient.service >> $installLog 2>&1    
    systemctl start dnsclient.service >> $installLog 2>&1
fi

echo ""
echo "Technitium DNS Client was installed successfully!"
echo "Open http://$(cat /proc/sys/kernel/hostname):8001/ to access the DNS Client web service."
echo ""
echo "Note! Edit the '/etc/systemd/system/dnsclient.service' service config file to change the DNS Client web server port."
echo ""
echo "Donate! Make a contribution by becoming a Patron: https://www.patreon.com/technitium"
echo ""
