function validateForm() {
    var username = document.forms["registrationForm"]["Username"].value;
    var email = document.forms["registrationForm"]["Email"].value;
    var password = document.forms["registrationForm"]["Password"].value;
    var confirmPassword = document.forms["registrationForm"]["ConfirmPassword"].value;
    var errorMessage = "";

    if (username == "") {
        errorMessage += "Username is required. ";
    }
    if (email == "") {
        errorMessage += "Email is required. ";
    } else {
        var emailPattern = /^[^ ]+@[^ ]+\.[a-z]{2,6}$/;
        if (!email.match(emailPattern)) {
            errorMessage += "Invalid email format. ";
        }
    }
    if (password == "") {
        errorMessage += "Password is required. ";
    } else if (password.length < 6) {
        errorMessage += "Password must be at least 6 characters long. ";
    }
    if (confirmPassword == "") {
        errorMessage += "Confirm Password is required. ";
    } else if (password != confirmPassword) {
        errorMessage += "Passwords do not match. ";
    }
    if (errorMessage != "") {
        document.getElementById("error-message").innerText = errorMessage;
        return false;
    }
    return true;
}
