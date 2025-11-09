async function UserLogin() {
    let loginObj = {
        "email": document.getElementById('login_email').value,
        "password": document.getElementById('login_password').value,
        "rememberMe": true
    }

    try {
        const response = await fetch('/api/v1.1/Account/Login', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(loginObj)
        });
        // attempt to parse JSON if possible
        let payload = null;
        const contentType = response.headers.get('content-type') || '';
        if (contentType.includes('application/json')) {
            try { payload = await response.json(); } catch { payload = null; }
        }
        loginCallback(response, payload, loginObj.rememberMe);
    } catch (error) {
        loginCallback(error);
    }

    function loginCallback(result, payload = null, rememberMe = false) {
        // if server hints 2FA required, show 2FA UI
        if (payload && payload.requiresTwoFactor) {
            // show 2FA section, hide password section
            ShowTwoFactorSection();
            // store remember me state
            window.__rememberMe = !!rememberMe;
            return;
        }
        switch (result.status) {
            case 200:
                window.location.replace('/index.html');
                break;
            default:
                // login failed
                document.getElementById('login_errorrow').style.display = '';
                document.getElementById('login_errorlabel').innerHTML = window.lang.translate('loginpage.incorrect_password_error');
                break;
        }
    }
}

function ShowSocialButtons(visible) {

    let socialLoginButtons = document.querySelectorAll('[id^="social_login_button_"]');
    socialLoginButtons.forEach(button => {
        button.style.display = 'none';
    });

    if (visible) {
        // Show buttons based on the socialLoginButtonVisibility received
        if (socialLoginButtonVisibility.includes('Password')) {
            document.getElementById('social_login_button_password').style.display = '';
        }
        if (socialLoginButtonVisibility.includes('Google')) {
            document.getElementById('social_login_button_google').style.display = 'table-row';
        }
        if (socialLoginButtonVisibility.includes('Microsoft')) {
            document.getElementById('social_login_button_microsoft').style.display = 'table-row';
        }
        if (socialLoginButtonVisibility.includes('OIDC')) {
            document.getElementById('social_login_button_oidc').style.display = 'table-row';
        }
    }
}

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
            console.error(window.lang.translate('loginpage.unsupported_social_login_provider_error', [provider]));
            break;
    }
}
// end of social login functionality

// 2FA helpers
function ShowTwoFactorSection() {
    // Hide password section
    const pw = document.getElementById('social_login_button_password');
    if (pw) pw.style.display = 'none';
    // Hide recovery section
    const rec = document.getElementById('recovery_section');
    if (rec) rec.style.display = 'none';
    // Show 2FA section
    const two = document.getElementById('twofactor_section');
    if (two) two.style.display = '';

    // hide all social login buttons
    ShowSocialButtons(false);
}

function ShowPasswordSection() {
    const pw = document.getElementById('social_login_button_password');
    if (pw) pw.style.display = '';
    const two = document.getElementById('twofactor_section');
    if (two) two.style.display = 'none';
    const rec = document.getElementById('recovery_section');
    if (rec) rec.style.display = 'none';

    // show all social login buttons
    ShowSocialButtons(true);
}

function ShowRecoverySection() {
    const pw = document.getElementById('social_login_button_password');
    if (pw) pw.style.display = 'none';
    const two = document.getElementById('twofactor_section');
    if (two) two.style.display = 'none';
    const rec = document.getElementById('recovery_section');
    if (rec) rec.style.display = '';

    // hide all social login buttons
    ShowSocialButtons(false);
}

async function VerifyTwoFactor() {
    const codeEl = document.getElementById('twofactor_code');
    const rememberDevice = true;
    const code = (codeEl?.value || '').trim();
    if (!code) {
        document.getElementById('twofactor_errorrow').style.display = '';
    document.getElementById('twofactor_errorlabel').innerText = window.lang.translate('loginpage.enter_six_digit_code_error');
        return;
    }

    try {
        const res = await fetch('/api/v1.1/Account/Login2FA', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ code, rememberMe: !!window.__rememberMe, rememberMachine: !!rememberDevice })
        });
        if (res.status === 200) {
            window.location.replace('/index.html');
        } else {
            document.getElementById('twofactor_errorrow').style.display = '';
            document.getElementById('twofactor_errorlabel').innerText = window.lang.translate('loginpage.invalid_code_error');
        }
    } catch (e) {
        document.getElementById('twofactor_errorrow').style.display = '';
    document.getElementById('twofactor_errorlabel').innerText = window.lang.translate('loginpage.error_verifying_code_error');
    }
}

async function VerifyRecoveryCode() {
    const codeEl = document.getElementById('recovery_code');
    const recoveryCode = (codeEl?.value || '').trim();
    if (!recoveryCode) {
        document.getElementById('recovery_errorrow').style.display = '';
    document.getElementById('recovery_errorlabel').innerText = window.lang.translate('loginpage.enter_recovery_code_error');
        return;
    }
    try {
        const res = await fetch('/api/v1.1/Account/LoginRecoveryCode', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ recoveryCode })
        });
        if (res.status === 200) {
            window.location.replace('/index.html');
        } else {
            document.getElementById('recovery_errorrow').style.display = '';
            document.getElementById('recovery_errorlabel').innerText = window.lang.translate('loginpage.invalid_recovery_code_error');
        }
    } catch (e) {
        document.getElementById('recovery_errorrow').style.display = '';
    document.getElementById('recovery_errorlabel').innerText = window.lang.translate('loginpage.error_verifying_recovery_code_error');
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

let socialLoginButtonVisibility = [];
fetch('/api/v1/Account/social-login', {
    method: 'GET',
    headers: {
        'Content-Type': 'application/json'
    }
})
    .then(response => response.json())
    .then(data => {
        socialLoginButtonVisibility = data;
        ShowSocialButtons(true);
    })
    .catch(error => {
    console.error(window.lang.translate('loginpage.error_fetching_social_login_options_error', [error]));
    });
