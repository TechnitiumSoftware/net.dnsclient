#!/bin/sh

dotnetDir="/opt/dotnet"
dotnetVersion="10.0"
dotnetRuntime="Microsoft.AspNetCore.App 10.0."
dotnetUrl="https://dot.net/v1/dotnet-install.sh"

dnsClientDir="/opt/technitium/dnsclient"
dnsClientTar="$dnsClientDir/DnsClientPortable.tar.gz"
dnsClientUrl="https://download.technitium.com/dnsclient/DnsClientPortable.tar.gz"

serviceUser="dns-client"
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
    
    # On Alpine Linux dotnet requires libstdc++
    if command -v apk >/dev/null 2>&1
    then
        echo "Installing ASP.NET Core Runtime dependencies..."
        apk add --no-cache libstdc++ >> $installLog 2>&1
    fi

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

if [ -f "/etc/systemd/system/dnsclient.service" ] || [ -f "/etc/init.d/dnsclient" ]
then
    echo "Updating Technitium DNS Client..."
else
    echo "Installing Technitium DNS Client..."
fi

tar -zxf $dnsClientTar -C $dnsClientDir >> $installLog 2>&1

echo ""

if [ "$(ps --no-headers -o comm 1 | tr -d '\n')" = "systemd" ] 
then
    if [ -f "/etc/systemd/system/dnsclient.service" ]
    then
        echo "Configuring permissions..."
        chown -R $serviceUser:$serviceUser $dnsClientDir >> $installLog 2>&1

        echo "Restarting systemd service..."
        systemctl restart dnsclient.service >> $installLog 2>&1
    else
        echo "Configuring user and permissions..."
        useradd --system -M --shell /usr/sbin/nologin --user-group $serviceUser >> $installLog 2>&1
        chown -R $serviceUser:$serviceUser $dnsClientDir >> $installLog 2>&1

        echo "Configuring systemd service..."
        cp $dnsClientDir/systemd.service /etc/systemd/system/dnsclient.service
        systemctl enable dnsclient.service >> $installLog 2>&1    
        systemctl start dnsclient.service >> $installLog 2>&1
    fi
elif [ -x "/sbin/rc-service" ]
then
    if [ -f "/etc/init.d/dnsclient" ]
    then
        echo "Configuring permissions..."
        chown -R $serviceUser:$serviceUser $dnsClientDir >> $installLog 2>&1

        echo "Restarting OpenRC service..."
        rc-service dnsclient stop >> $installLog 2>&1
        rc-service dnsclient start >> $installLog 2>&1
    else
        echo "Configuring user and permissions..."
        addgroup -S $serviceUser >> $installLog 2>&1
        adduser -H -S -D -s /bin/false -G $serviceUser $serviceUser >> $installLog 2>&1
        chown -R $serviceUser:$serviceUser $dnsClientDir >> $installLog 2>&1

        echo "Configuring OpenRC service..."
        cp $dnsClientDir/openrc.service /etc/init.d/dnsclient
        chmod +x /etc/init.d/dnsclient
        rc-update add dnsclient >> $installLog 2>&1
        rc-service dnsclient start >> $installLog 2>&1
    fi
else
    echo "Failed to install Technitium DNS Client: systemd/openrc was not detected."
    exit 1
fi 2>/dev/null

echo ""
echo "Technitium DNS Client was installed successfully!"
echo "Open http://$(cat /proc/sys/kernel/hostname):8001/ to access the DNS Client web service."
echo ""

if [ -f "/etc/systemd/system/dnsclient.service" ]
then
    echo "Note! Edit the '/etc/systemd/system/dnsclient.service' service config file to change the DNS Client web server port."
elif [ -f "/etc/init.d/dnsclient" ]
then
    echo "Note! Edit the '/etc/init.d/dnsclient' service config file to change the DNS Client web server port."
fi

echo ""
echo "Donate! Make a contribution by becoming a Patron: https://www.patreon.com/technitium"
echo ""
