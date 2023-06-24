function ajaxCall(endpoint, method, successFunction) {
    $.ajax({

        // Our sample url to make request
        url:
            endpoint,

        // Type of Request
        type: method,

        // Function to call when to
        // request is ok
        success: function (data) {
            var x = JSON.stringify(data);
            console.log(x);
            successFunction(data);
        },

        // Error handling
        error: function (error) {
            console.log(`Error ${error}`);
        }
    });
}
