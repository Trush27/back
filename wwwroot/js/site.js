﻿document.addEventListener("DOMContentLoaded", function () {
    const togglePassword = document.querySelector("#togglePassword");
    const password = document.querySelector("#password");

    togglePassword.addEventListener("click", function () {

        const type = password.getAttribute("type") === "password" ? "text" : "password";
        password.setAttribute("type", type);

        this.classList.toggle("bi-eye");
    });

    var floatingInputs = document.querySelectorAll('.form-floating .form-control');
    floatingInputs.forEach(function (input) {
        input.addEventListener('focus', function () {
            this.parentElement.querySelector('label').classList.add('active');
        });
        input.addEventListener('blur', function () {
            if (!this.value) {
                this.parentElement.querySelector('label').classList.remove('active');
            }
        });
    });
});
function addUser() {
    window.location.href = 'add-user.html';
}
function backToUsers() {
    window.location.href = 'users.html';
}
function editUser() {
    window.location.href = 'edit-user.html'
}
function navigateToForgotPassword() {
    var email = document.getElementById('Email').value;
    window.location.href = '@Url.Action("ForgotPassword", "Home")' + '?email=' + encodeURIComponent(email);
}