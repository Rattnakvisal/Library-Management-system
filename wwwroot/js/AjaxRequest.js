function ajaxHandleRequest(controller, action, param_values, onSuccess, method) {
    var myDefer = $.Deferred();
    var hasCallBack = false;

    if (!method) {
        method = "POST";
    }
    if (typeof onSuccess == 'function') hasCallBack = true;

    // Check if param_values is a FormData object
    if (param_values instanceof FormData) {
        $.ajax({
            type: method,
            url: ['/', controller, '/', action].join(''),
            data: JSON.stringify(param_values),
            processData: false, // Prevent jQuery from processing the data
            contentType: false, // Prevent jQuery from setting a Content-Type
            success: function (response) {
                if (hasCallBack) {
                    onSuccess(response);
                } else {
                    myDefer.resolve(response);
                }
            },
            error: function (request, status, err) {
                window.location.href = "../Identity/Account/Login";
                return;
            }
        });
    } else {
        // Handle standard JSON request
        if (method == "POST") {
            $.ajax({
                type: method,
                url: ['/', controller, '/', action].join(''),
                data: JSON.stringify(param_values),
                contentType: "application/json; charset=utf-8",
                dataType: "json",
                success: function (response) {
                    if (hasCallBack) {
                        onSuccess(response);
                    } else {
                        myDefer.resolve(response);
                    }
                },

                error: function (xhr) {
                    if (xhr.status === 401 || xhr.status === 403) {
                        window.location.href = "/Identity/Account/Login";
                    } else {
                        console.error(xhr.responseText);
                        alert("Server error occurred");
                    }
                }
            });
        } else {
            $.ajax({
                type: method,
                url: ['/', controller, '/', action, '?', param_values].join(''),
                data: JSON.stringify(param_values),
                contentType: "application/json; charset=utf-8",
                dataType: "json",
                processData: true,
                success: function (response) {
                    if (hasCallBack) {
                        onSuccess(response);
                    } else {
                        myDefer.resolve(response);
                    }
                },
                error: function (xhr) {
                    if (xhr.status === 401 || xhr.status === 403) {
                        window.location.href = "/Identity/Account/Login";
                    } else {
                        console.error(xhr.responseText);
                        alert("Server error occurred");
                    }
                }

            });
        }
    }

    return myDefer.promise();
}
