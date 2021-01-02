/*
Technitium dnsclient.net
Copyright (C) 2020  Shreyas Zare (shreyas@technitium.com)

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <http://www.gnu.org/licenses/>.

*/

$(function () {

    $('.dropdown-menu').on('click', 'a', function (e) {
        e.preventDefault();

        var itemText = $(this).text();
        $(this).closest('.dropdown').find('input').val(itemText);

        if ((itemText.indexOf("TLS") !== -1) || (itemText.indexOf(":853") !== -1))
            $("#optProtocol").val("TLS");
        else if (itemText.indexOf("HTTPS-JSON") !== -1)
            $("#optProtocol").val("HttpsJson");
        else if ((itemText.indexOf("HTTPS") !== -1) || (itemText.indexOf("http://") !== -1) || (itemText.indexOf("https://") !== -1))
            $("#optProtocol").val("Https");
        else {
            switch ($("#optProtocol").val()) {
                case "UDP":
                case "TCP":
                    break;

                default:
                    $("#optProtocol").val("UDP");
                    break;
            }
        }
    });

    $("#btnResolve").click(function () {

        var btn = $(this).button('loading');

        var server = $("#txtServer").val();

        if (server.indexOf("recursive-resolver") !== -1)
            $("#optProtocol").val("UDP");

        var domain = $("#txtDomain").val();
        var type = $("#optType").val();
        var protocol = $("#optProtocol").val();

        {
            var i = server.indexOf("{");
            if (i > -1) {
                var j = server.lastIndexOf("}");
                server = server.substring(i + 1, j);
            }
        }

        server = server.trim();

        if ((server === null) || (server === "")) {
            showAlert("warning", "Missing!", "Please enter a valid DNS server.");
            btn.button('reset');
            return;
        }

        if ((domain === null) || (domain === "")) {
            showAlert("warning", "Missing!", "Please enter a domain name to query.");
            btn.button('reset');
            return;
        }
        else {
            var i = domain.indexOf("://");
            if (i > -1) {
                var j = domain.indexOf(":", i + 3);

                if (j < 0)
                    j = domain.indexOf("/", i + 3);

                if (j > -1)
                    domain = domain.substring(i + 3, j);
                else
                    domain = domain.substring(i + 3);

                $("#txtDomain").val(domain);
            }
        }

        window.location.hash = encodeURIComponent($("#txtServer").val()) + "/" + encodeURIComponent(domain) + "/" + type + "/" + protocol;

        var apiUrl = "/api/dnsclient/?server=" + server + "&domain=" + domain + "&type=" + type + "&protocol=" + protocol;
        var divOutput = $("#divOutput");

        //show loader
        divOutput.html("<pre><img class='center-block' src='/img/loader.gif' /></pre>");
        divOutput.show();
        hideAlert();

        $.ajax({
            type: "GET",
            url: apiUrl,
            dataType: 'json',
            cache: false,
            success: function (responseJSON, status, jqXHR) {

                switch (responseJSON.status) {
                    case "ok":
                        divOutput.html("<pre>" + JSON.stringify(responseJSON.response, null, 2) + "</pre>");
                        break;

                    case "error":
                        showAlert("danger", "Error!", responseJSON.response.Message + (responseJSON.response.InnerException == null ? "" : " " + responseJSON.response.InnerException.Message));
                        divOutput.hide();
                        break;

                    default:
                        showAlert("danger", "Error!", "Invalid status code was received.");
                        divOutput.hide();
                        break;
                }

                btn.button('reset');
            },
            error: function (jqXHR, textStatus, errorThrown) {
                showAlert("danger", "Error!", jqXHR.status + " " + jqXHR.statusText);
                divOutput.hide();
                btn.button('reset');
            }
        });

        //add server name to list if doesnt exists
        var txtServerName = $("#txtServer").val();
        var containsServer = false;

        $("ul.dropdown-menu a").each(function () {
            if ($(this).html() === txtServerName)
                containsServer = true;
        });

        if (!containsServer)
            $("ul.dropdown-menu").prepend('<li><a href="#">' + txtServerName + '</a></li>');
    });

    //read hash values at doc ready
    {
        if (window.location.hash.length > 0) {
            var values = window.location.hash.substr(1).split("/");
            if (values.length >= 3) {
                $("#txtServer").val(decodeURIComponent(values[0]));
                $("#txtDomain").val(decodeURIComponent(values[1]));
                $("#optType").val(values[2]);

                if (values.length === 4)
                    $("#optProtocol").val(values[3]);
                else
                    $("#optProtocol").val("UDP");

                if ($("#txtServer").val() === "Recursive Query (recursive-resolver)")
                    $("#txtServer").val("Recursive Query {recursive-resolver}");

                $("#btnResolve").click();
            }
        }
    }
});

function showAlert(type, title, message) {
    var alertHTML = "<div class=\"alert alert-" + type + "\" role=\"alert\">\
    <button type=\"button\" class=\"close\" data-dismiss=\"alert\">&times;</button>\
    <strong>" + title + "</strong>&nbsp;" + message + "\
    </div>";

    var divAlert = $(".AlertPlaceholder");

    divAlert.html(alertHTML);
    divAlert.show();

    if (type === "success") {
        setTimeout(function () {
            hideAlert();
        }, 5000);
    }

    return true;
}

function hideAlert() {
    $(".AlertPlaceholder").hide();
}