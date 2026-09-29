$(function() {
    $(window).load(function() {
        $(':input:visible:enabled:first').focus();
    });
})

var username = document.getElementById("username");
username.addEventListener("keyup", function(event) {

    if (event.keyCode === 13) {
        event.preventDefault();
        $("#btn-login").trigger("click");
    }

})

var password = document.getElementById("password");
password.addEventListener("keyup", function(event) {
    if (event.keyCode === 13) {
        event.preventDefault();
        $("#btn-login").trigger("click");
    }

})


document.getElementById('login-form').addEventListener('submit', function(e) {
    if (!isCaptchaVerified && isCaptchaRequired) {
        e.preventDefault();
        alert("Por favor, completa la verificación de CAPTCHA.");
        return
    }
});

let isCaptchaRequired = false

$(document).ready(function() {
    $("#repwd2").keyup(function() {
        rePWD();
    });

    if ($("#captchaCkeck").val() == 1) isCaptchaRequired = true
});

//btnSubmit = document.getElementById('Ingresar'); 
//btnSubmit.addEventListener('click',function(){
//	var form = document.getElementById('login-form');
//	if (CheckForm(form)) {
//		form.submit();
//	} 
//})

let isCaptchaVerified = false
let captchaToken = null

function onCaptchaSuccess(token) {
    isCaptchaVerified = true;
    captchaToken = token;
}

function onCaptchaExpired() {
    isCaptchaVerified = false;
    captchaToken = null;
}

function onCaptchaError(error) {
    isCaptchaVerified = false;
}


function Recuperar() {
    if (!isCaptchaVerified && isCaptchaRequired) {
        alert("Por favor, completa la verificación de CAPTCHA.");
        return
    }

    usr = document.getElementById('username').value;
    if (usr != null && usr != "") {
        Alert("¡Confirmar recuperación!", "¿Realmente desea confirmar la recuperación de contraseña del usuario: " + usr + " ?", "confirm", "Confirmar,Cancelar");
        var recuperar = document.getElementById('btnPrimaryAlert');
        var response;
        recuperar.addEventListener('click', function() {
            form = document.getElementById('login-form');
            AJAX_PARAMETRIZADO("AjaxLogin", form, 'response', 'RECUPERAR_PASS', false)
            var response = document.getElementById('response').innerHTML.trim();
            var correo = response.substring(2, response.length);
            var ok = response.substring(0, 2);
            if (ok == "OK") {
                Alert("¡Contraseña Recuperada!", "Usuario: " + usr + ". Su contraseña ha sido enviada a su correo. " + correo + "<br> Recuerde revisar también la bandeja de Spam o correo no deseado", "alert", "Aceptar");
            } else {
                Alert("¡Datos Incorrectos!", response, "alert", "Aceptar");
            }
        })
    } else {
        Alert("¡Campos Requeridos!", "Por Favor, nombre de usuario", "alert", "Aceptar");
    }
}

function rePWD() {
    var pwd = document.getElementById("pwd2").value;
    var repwd = document.getElementById("repwd2").value;
    if (pwd == repwd) {
        document.getElementById('repwd2').style.color = "black";
        $(".fa-check").removeClass("fa-close");
        $(".fa-close").addClass("fa-check");
        $(".fa-check").css("color", "green");
    } else {
        document.getElementById('repwd2').style.color = "red";
        $(".fa-close").removeClass("fa-check");
        $(".fa-check").addClass("fa-close");
        $(".fa-close").css("color", "red");
    }
}

// Acción sobre recuperar contraseña
var recuperaPWD = document.getElementById('recuperaPWD');
recuperaPWD.addEventListener('click', function() {
    Recuperar();
})

function error(mensaje) {
    $("#error").css("display", "block");
    $("#error").html(mensaje);
}

function quitarEspacios(input) {
    input.value = input.value.replace(/ /g, "");
    input.value = input.value.replace(/	/g, "");
}

var consultar = document.getElementById('consultar');
consultar.addEventListener('click', function(e) {
    e.preventDefault();
    var documento = document.getElementById('documento').value;
    var form = document.getElementById('envio');
    AJAX_PARAMETRIZADO("AjaxLogin", form, 'respuesta', 'ConsultarUsuario', false)

})


