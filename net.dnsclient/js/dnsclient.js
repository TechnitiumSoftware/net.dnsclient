$(function () {

    $('.dropdown-menu a').click(function () {
        $(this).closest('.dropdown').find('input.dnsserver').val($(this).text());
    });

    $("#btnResolve").click(function () {

        var btn = $(this).button('loading');

        var server = $("#txtServer").val();
        var domain = $("#txtDomain").val();
        var type = $("#optType").val();

        var i = server.indexOf("(");
        if (i > -1) {
            var j = server.indexOf(")");
            server = server.substring(i + 1, j);
        }

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

        var apiUrl = "/api/dnsclient/?server=" + server + "&domain=" + domain + "&type=" + type;
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
            success: function (responseJson, status, jqXHR) {
                divOutput.html("<pre>" + JSON.stringify(responseJson, null, 2) + "</pre>");
                btn.button('reset');
            },
            error: function (jqXHR, textStatus, errorThrown) {

                if (jqXHR.responseJSON == null)
                    showAlert("danger", "Error!", jqXHR.status + " " + jqXHR.statusText);
                else
                    showAlert("danger", "Error!", jqXHR.responseJSON.Message);

                divOutput.hide();
                btn.button('reset');
            }
        });

    });

});

function showAlert(type, title, message) {
    var alertHTML = "<div class=\"alert alert-" + type + "\" role=\"alert\">\
    <button type=\"button\" class=\"close\" data-dismiss=\"alert\">&times;</button>\
    <strong>" + title + "</strong>&nbsp;" + message + "\
    </div>";

    var divAlert = $(".AlertPlaceholder");

    divAlert.html(alertHTML);
    divAlert.show();

    if (type == "success") {
        setTimeout(function () {
            hideAlert();
        }, 5000);
    }

    return true;
}

function hideAlert() {
    $(".AlertPlaceholder").hide();
}