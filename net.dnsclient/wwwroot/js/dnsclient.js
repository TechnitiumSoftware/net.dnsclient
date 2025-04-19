/*
Technitium dnsclient.net
Copyright (C) 2025  Shreyas Zare (shreyas@technitium.com)

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
    loadVersion();

    loadServerList();

    processUrlBookmark();

    $('.dropdown-menu').on('click', 'a', function (e) {
        e.preventDefault();

        var itemText = $(this).text();
        $(this).closest('.dropdown').find('input').val(itemText);

        if (itemText.indexOf("QUIC") !== -1)
            $("#optProtocol").val("QUIC");
        else if ((itemText.indexOf("TLS") !== -1) || (itemText.indexOf(":853") !== -1))
            $("#optProtocol").val("TLS");
        else if ((itemText.indexOf("HTTPS") !== -1) || (itemText.indexOf("http://") !== -1) || (itemText.indexOf("https://") !== -1))
            $("#optProtocol").val("HTTPS");
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
});

function loadServerList() {
    $.ajax({
        type: "GET",
        url: "json/dnsclient-server-list-custom.json",
        dataType: "json",
        cache: false,
        async: false,
        success: function (responseJSON, status, jqXHR) {
            loadServerListFrom(responseJSON);
        },
        error: function (jqXHR, textStatus, errorThrown) {
            $.ajax({
                type: "GET",
                url: "json/dnsclient-server-list-builtin.json",
                dataType: "json",
                cache: false,
                async: false,
                success: function (responseJSON, status, jqXHR) {
                    loadServerListFrom(responseJSON);
                },
                error: function (jqXHR, textStatus, errorThrown) {
                    showAlert("danger", "Error!", "Failed to load server list: " + jqXHR.status + " " + jqXHR.statusText);
                }
            });
        }
    });
}

function loadServerListFrom(responseJSON) {
    if ((responseJSON.length > 0) && (responseJSON[0].addresses.length > 0)) {
        if ((responseJSON[0].name == null) || (responseJSON[0].name.length == 0))
            $("#txtServer").val(responseJSON[0].addresses[0]);
        else
            $("#txtServer").val(responseJSON[0].name + " {" + responseJSON[0].addresses[0] + "}");
    }
    else {
        $("#txtServer").val("");
    }

    var htmlList = "";

    for (var i = 0; i < responseJSON.length; i++) {
        for (var j = 0; j < responseJSON[i].addresses.length; j++) {
            if ((responseJSON[i].name == null) || (responseJSON[i].name.length == 0))
                htmlList += "<li><a href=\"#\">" + htmlEncode(responseJSON[i].addresses[j]) + "</a></li>";
            else
                htmlList += "<li><a href=\"#\">" + htmlEncode(responseJSON[i].name) + " {" + htmlEncode(responseJSON[i].addresses[j]) + "}</a></li>";
        }
    }

    $("#optDnsClientNameServers").html(htmlList);
}

function loadVersion() {
    $.ajax({
        type: "GET",
        url: "api/version",
        dataType: 'json',
        cache: false,
        success: function (responseJSON, status, jqXHR) {
            $("#lblVersion").text("v" + responseJSON.response.version);
        }
    });
}

function processUrlBookmark() {
    if (window.location.hash.length > 0) {
        var values = window.location.hash.substring(1).split("/");
        if (values.length >= 3) {
            $("#txtServer").val(decodeURIComponent(values[0]));
            $("#txtDomain").val(decodeURIComponent(values[1]));
            $("#optType").val(values[2]);

            if (values.length >= 4)
                $("#optProtocol").val(values[3]);
            else
                $("#optProtocol").val("UDP");

            if (values.length >= 5)
                $("#chkDnssecValidation").prop("checked", values[4].toLowerCase() === "true");
            else
                $("#chkDnssecValidation").prop("checked", false);

            if (values.length >= 6)
                $("#txtClientSubnet").val(decodeURIComponent(values[5]));
            else
                $("#txtClientSubnet").val("");

            if ($("#txtServer").val() === "Recursive Query (recursive-resolver)")
                $("#txtServer").val("Recursive Query {recursive-resolver}");

            resolveDomain();
        }
    }
}

function resolveDomain() {
    var btn = $("#btnResolve").button('loading');

    var server = $("#txtServer").val();

    if ((server.indexOf("recursive-resolver") !== -1) || (server.indexOf("system-dns") !== -1))
        $("#optProtocol").val("UDP");

    var domain = $("#txtDomain").val();
    var type = $("#optType").val();
    var protocol = $("#optProtocol").val();
    var dnssecValidation = $("#chkDnssecValidation").prop("checked");
    var eDnsClientSubnet = $("#txtClientSubnet").val();

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
        $("#txtServer").trigger("focus");
        return;
    }

    if ((domain === null) || (domain === "")) {
        showAlert("warning", "Missing!", "Please enter a domain name to query.");
        btn.button('reset');
        $("#txtDomain").trigger("focus");
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

    window.location.hash = encodeURIComponent($("#txtServer").val()) + "/" + encodeURIComponent(domain) + "/" + type + "/" + protocol + "/" + dnssecValidation + "/" + encodeURIComponent(eDnsClientSubnet);

    var apiUrl = "api/dnsclient/?server=" + encodeURIComponent(server) + "&domain=" + encodeURIComponent(domain) + "&type=" + type + "&protocol=" + protocol + "&dnssec=" + dnssecValidation + "&eDnsClientSubnet=" + encodeURIComponent(eDnsClientSubnet);

    var divLoader = $("#divLoader");
    var divOutputAccordion = $("#divOutputAccordion");

    //show loader
    hideAlert();
    divOutputAccordion.hide();
    divLoader.show();

    $.ajax({
        type: "GET",
        url: apiUrl,
        dataType: 'json',
        cache: false,
        success: function (responseJSON, status, jqXHR) {
            divLoader.hide();
            btn.button('reset');

            switch (responseJSON.status) {
                case "warning":
                    showAlert("warning", "Warning!", responseJSON.warningMessage);

                case "ok":
                    $("#preFinalResponse").text(JSON.stringify(responseJSON.response, null, 2));
                    $("#divFinalResponseCollapse").collapse("show");
                    $("#divRawResponsesCollapse").collapse("hide");
                    divOutputAccordion.show();
                    break;

                case "error":
                    showAlert("danger", "Error!", responseJSON.errorMessage + (responseJSON.innerErrorMessage == null ? "" : " " + responseJSON.innerErrorMessage));
                    break;

                default:
                    showAlert("danger", "Error!", "Invalid status code was received.");
                    break;
            }

            if ((responseJSON.rawResponses != null)) {
                if (responseJSON.rawResponses.length == 0) {
                    $("#divRawResponsePanel").hide();
                }
                else {
                    var rawListHtml = "";

                    for (var i = 0; i < responseJSON.rawResponses.length; i++) {
                        rawListHtml += "<li class=\"list-group-item\"><pre style=\"margin-top: 5px; margin-bottom: 5px;\">" + JSON.stringify(responseJSON.rawResponses[i], null, 2) + "</pre></li>";
                    }

                    $("#spanRawResponsesCount").text(responseJSON.rawResponses.length);
                    $("#ulRawResponsesList").html(rawListHtml);
                    $("#divRawResponsesCollapse").collapse("hide");
                    $("#divRawResponsePanel").show();
                }
            }
        },
        error: function (jqXHR, textStatus, errorThrown) {
            showAlert("danger", "Error!", jqXHR.status + " " + jqXHR.statusText);
            divLoader.hide();
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
        $("ul.dropdown-menu").prepend("<li><a href=\"#\">" + htmlEncode(txtServerName) + "</a></li>");
}

function showAlert(type, title, message) {
    var alertHTML = "<div class=\"alert alert-" + type + "\" style=\"margin-top: 15px; margin-bottom: 0px;\" role=\"alert\">\
    <button type=\"button\" class=\"close\" data-dismiss=\"alert\">&times;</button>\
    <strong>" + title + "</strong>&nbsp;" + htmlEncode(message) + "\
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

function htmlEncode(value) {
    return $('<div/>').text(value).html().replace(/"/g, "&quot;");
}
