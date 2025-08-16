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

// check if social login buttons should be displayed
fetch('/api/v1/Account/social-login', {
    method: 'GET',
    headers: {
        'Content-Type': 'application/json'
    }
})
    .then(response => response.json())
    .then(data => {
        // Hide all social login buttons initially
        let socialLoginButtons = document.querySelectorAll('[id^="social_login_button_"]');
        socialLoginButtons.forEach(button => {
            button.style.display = 'none';
        });

        // Show buttons based on the data received
        if (data.includes('Password')) {
            document.getElementById('social_login_button_password').style.display = '';
        }
        if (data.includes('Google')) {
            document.getElementById('social_login_button_google').style.display = 'table-row';
        }
        if (data.includes('Microsoft')) {
            document.getElementById('social_login_button_microsoft').style.display = 'table-row';
        }
        if (data.includes('OIDC')) {
            document.getElementById('social_login_button_oidc').style.display = 'table-row';
        }
    })
    .catch(error => {
        console.error('Error fetching social login options:', error);
    });

function SocialLogin(provider) {
    switch (provider) {
        case 'google':
            window.location.href = '/api/v1.0/Account/signin-google';
            break;
        case 'microsoft':
            window.location.href = '/api/v1.0/Account/signin-microsoft';
            break;
        case 'oidc':
            window.location.href = '/api/v1.0/Account/signin-oidc';
            break;
        default:
            console.error('Unsupported social login provider:', provider);
            break;
    }
}
// end of social login functionality

// load background images
backgroundImageHandler = new BackgroundImageRotator([
    '/images/LoginWallpaper.jpg',
    '/images/SettingsWallpaper.jpg',
    '/images/CollectionsWallpaper.jpg',
    '/images/gamebg1.jpg',
    '/images/gamebg2.jpg',
    '/images/gamebg3.jpg'
], 'bgImage_LessBlur', true);