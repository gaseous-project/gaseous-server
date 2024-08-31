// Purpose: javascript for the first setup page
var EmailChecker;
var PasswordChecker;

function SetupPage() {
    let emailInput = document.getElementById('login_email');
    let emailError = document.getElementById('email-error');

    let passwordInput = document.getElementById('login_password');
    let confirmPasswordInput = document.getElementById('login_confirmpassword');
    let passwordError = document.getElementById('password-error');

    let submitButton = document.getElementById('login_createaccount');

    EmailChecker = new EmailCheck(emailInput, emailError, true);
    PasswordChecker = new PasswordCheck(passwordInput, confirmPasswordInput, passwordError);

    emailInput.addEventListener('input', function () {
        ValidateForm();
    });
    passwordInput.addEventListener('input', function () {
        ValidateForm();
    });
    confirmPasswordInput.addEventListener('input', function () {
        ValidateForm();
    });

    submitButton.addEventListener('click', async function () {
        let model = {
            "userName": emailInput.value,
            "email": emailInput.value,
            "password": passwordInput.value,
            "confirmPassword": confirmPasswordInput.value
        };

        await fetch('/api/v1.1/FirstSetup/0', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(model)
        }).then(response => {
            if (response.ok) {
                window.location.replace('/index.html');
            } else {
                response.json().then(data => {
                    passwordError.innerHTML = '';
                    data.errors.forEach(error => {
                        let errorMessage = document.createElement('p');
                        errorMessage.innerHTML = error.description;
                        passwordError.appendChild(errorMessage);
                    });
                });
            }
        });
    });
}

async function ValidateForm() {
    let submitButton = document.getElementById('login_createaccount');
    if (
        await EmailCheck.CheckEmail(EmailChecker, EmailChecker.EmailElement) === true &&
        PasswordCheck.CheckPasswords(PasswordChecker, PasswordChecker.NewPasswordElement, PasswordChecker.ConfirmPasswordElement) === true
    ) {
        submitButton.removeAttribute('disabled');
    } else {
        submitButton.setAttribute('disabled', 'disabled');
    }
}

SetupPage();

// load background images
backgroundImageHandler = new BackgroundImageRotator([
    '/images/LoginWallpaper.jpg',
    '/images/SettingsWallpaper.jpg',
    '/images/CollectionsWallpaper.jpg',
    '/images/gamebg1.jpg',
    '/images/gamebg2.jpg',
    '/images/gamebg3.jpg'
], 'bgImage_LessBlur', true);