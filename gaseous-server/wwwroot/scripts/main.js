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

function showDialog(dialogPage, variables) {
    // Get the modal
    var modal = document.getElementById("myModal");

    // Get the modal content
    var modalContent = document.getElementById("modal-content");

    // Get the button that opens the modal
    var btn = document.getElementById("myBtn");

    // Get the <span> element that closes the modal
    var span = document.getElementsByClassName("close")[0];

    // When the user clicks on the button, open the modal 
    modal.style.display = "block";

    // When the user clicks on <span> (x), close the modal
    span.onclick = function () {
        modal.style.display = "none";
        modalContent.innerHTML = "";
        modalVariables = null;
    }

    // When the user clicks anywhere outside of the modal, close it
    window.onclick = function (event) {
        if (event.target == modal) {
            modal.style.display = "none";
            modalContent.innerHTML = "";
            modalVariables = null;
        }
    }

    modalVariables = variables;

    $('#modal-content').load('/pages/dialogs/' + dialogPage + '.html');
}