// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

function logIn() {
  const userName = $('#userName').val();
  const password = $('#password').val();
  $.ajax({
    method: "POST",
    url: "security/createToken",
    data: JSON.stringify({ userName, password }),
    contentType: "application/json; charset=utf-8",
    dataType: "json"
  })
    .done(token => {
      window.open(`/cap/index.html?access_token=${token}`, '_blank')
    });
}