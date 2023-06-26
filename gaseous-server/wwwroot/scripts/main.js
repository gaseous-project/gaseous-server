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

function formatBytes(bytes, decimals = 2) {
    if (!+bytes) return '0 Bytes'

    const k = 1024
    const dm = decimals < 0 ? 0 : decimals
    const sizes = ['Bytes', 'KiB', 'MiB', 'GiB', 'TiB', 'PiB', 'EiB', 'ZiB', 'YiB']

    const i = Math.floor(Math.log(bytes) / Math.log(k))

    return `${parseFloat((bytes / Math.pow(k, i)).toFixed(dm))} ${sizes[i]}`
}