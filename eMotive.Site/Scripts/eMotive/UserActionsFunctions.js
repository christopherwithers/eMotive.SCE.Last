﻿$(function () {

    $("#ConfirmSendCertificate").click(function () {

        Ajax.DoQuery(window.Routes.URL("SessionAttendanceCertificate"),
            function (data) {
                if (data.success) {

                    // $("#" + id).remove();
                    $("#TrainingCertificateModal").modal('toggle');
                } else {
                    var html = "";
                    $.each(data.message, function (key, value) {
                        html += "<p>" + value + "</p>";
                    });
                    $("#TrainingCertificateError").html(html);
                    $("#TrainingCertificateError").show();
                }
            },
            { Username: $("#CertificateForUsername").val(), trainingSession: $('#TrainingDatelists').val() });
    });

    $("#ConfirmDelete").click(function () {

        Ajax.DoQuery(window.Routes.URL("DeleteUser"),
            function (data) {
                if (data.success) {

                    // $("#" + id).remove();
                    $("#DeleteModal").modal('toggle');
                    window.location.replace("http://mymds.bham.ac.uk/sce/Admin/Users");
                } else {
                    var html = "";
                    $.each(data.message, function (key, value) {
                        html += "<p>" + value + "</p>";
                    });
                    $("#DeleteErrorMessage").html(html);
                    $("#DeleteError").show();
                }
            },
            { Username: $("#DeleteID").val() });
    });

    $("#SaveNotes").click(function () {
        Ajax.DoQuery(window.Routes.URL("SaveUserNotes"), function (data) { $("#NotesModal").modal('toggle'); }, { username: $("#NotesUsername").val(), notes: $("#NotesText").val() });
    });

    $("#SelectedRoleFilter").change(function () {
        $('form#searchForm').submit();
        return false;
    });
});

