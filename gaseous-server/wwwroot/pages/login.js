function UserLogin() {
    // let loginObj = {
    //     "email": document.getElementById('login_email').value,
    //     "password": document.getElementById('login_password').value,
    //     "rememberMe": document.getElementById('login_rememberme').checked
    // }
    let loginObj = {
        "email": document.getElementById('login_email').value,
        "password": document.getElementById('login_password').value,
        "rememberMe": true
    }

    ajaxCall(
        '/api/v1.1/Account/Login',
        'POST',
        function (result) {
            loginCallback(result);
        },
        function (error) {
            loginCallback(error);
        },
        JSON.stringify(loginObj)
    );

    function loginCallback(result) {
        switch (result.status) {
            case 200:
                window.location.replace('/index.html');
                break;
            default:
                // login failed
                document.getElementById('login_errorrow').style.display = '';
                document.getElementById('login_errorlabel').innerHTML = 'Incorrect password';
                break;
        }
    }
}

// load background images
backgroundImageHandler = new BackgroundImageRotator([
    '/images/LoginWallpaper.jpg',
    '/images/SettingsWallpaper.jpg',
    '/images/CollectionsWallpaper.jpg',
    '/images/gamebg1.jpg',
    '/images/gamebg2.jpg',
    '/images/gamebg3.jpg'
], 'bgImage_LessBlur', true);