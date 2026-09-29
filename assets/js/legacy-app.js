/**
 * METODOS DE USO GENERAL
 */


/**************** CONSTANTES ****************/
const pathURL = getPathUrl();


/**************** FUNCIONES ****************/


//Deshabilita alerts javascript
//window.alert = function() {}; //Descomentar en producción
//window.console.log = function(){
//	return false;
//}
//window.console.error = function(){
//	return false;
//}


// PATH
function getPathUrl() {
	var url = window.location.protocol+"//"+window.location.host;
	var path = "/"+window.location.pathname.split("/")[1];
	var existeUrl;
	try {
		var http = new XMLHttpRequest();
		http.open('HEAD', url+path, false);
		http.send();
		if (http.status==200) {
			existeUrl = true;
		} else {
			existeUrl = false;
		}
	}catch(e) {
		existeUrl = false;
	}
	if (!existeUrl) {
		path = "";
	} 
	return url+path+"/";
}
// FIN PATH

//AJAX PARAMETRIZADO
function AJAX_PARAMETRIZADO(endpoint,value,componentID,name,async) { 
	var request = "";
	if (async == null) async = true;
	var component = document.getElementById(componentID);
	var http = getXMLHTTPRequest(); 
	var url = pathURL+endpoint;
	var type; 
	try {
		value.elements.length;
		type = "form";
	}catch(e) {
		type = "element";
	}
	if(type == "form") {
		var form = value;
	    var datosForm = "value="+name+"&";
	    var sepCampos = "";
	    for (var i=0; i<=form.elements.length-1; i++) {
	    	datosForm += sepCampos+form.elements[i].name+'='+encodeURI(form.elements[i].value);
	        sepCampos="&";
	    }
	    http.open("POST", url, async); 
		http.setRequestHeader('Content-Type', 'application/x-www-form-urlencoded; charset=ISO-8859-1');
		http.onreadystatechange = function () {
			if (http.readyState == 4) {
				if(http.status == 200) {
					if (componentID != null) {
						if (component != "" && component != null) {
							$('#loading1').hide();
							$('#loading').hide();
							$('#tablaEstudiantes').show();
							component.innerHTML = http.responseText;
						} 
					} else {
						request = http.responseText;
					}
				} else if (http.status == 401) {
					window.location.href = 'Logout';
				} else if (http.status == 403) {
					window.location.href = 'Error?e=NotAuthorized';
				}
			}
		};
		http.send(datosForm);
	}
	if(type == "element") {
		if (value.value != null) {
			url += "?value="+name+"&"+value.name+"="+value.value;
		} else {
			url += "?value="+name+"&content="+value;
		}
		http.open("GET", url, async); 
		http.onreadystatechange = function () {
			if (http.readyState == 4) {
				if(http.status == 200) {
					$('#loading').hide();
					$('#tablaEstudiantes').show();
					if (componentID != null) {
						if (component != "" && component != null) {
							component.innerHTML = http.responseText;
						} 
					} else {
						request = http.responseText;
					}
				} else if (http.status == 401) {
					window.location.href = 'Logout';
				} else if (http.status == 403) {
					window.location.href = 'Error?e=NotAuthorized';
				}
			}
		};
		http.send(null);
	}
	if (componentID == null) {
		return request;
	}
}

//AJAX
function AJAX(value,componentID,name,async) { 
	var request = "";
	if (async == null) async = true;
	var component = document.getElementById(componentID);
	var http = getXMLHTTPRequest(); 
	var url = pathURL+"Ajax";
	var type; 
	try {
		value.elements.length;
		type = "form";
	}catch(e) {
		type = "element";
	}
	if(type == "form") {
		var form = value;
	    var datosForm = "value="+name+"&";
	    var sepCampos = "";
	    for (var i=0; i<=form.elements.length-1; i++) {
	    	datosForm += sepCampos+form.elements[i].name+'='+encodeURI(form.elements[i].value);
	        sepCampos="&";
	    }
	    http.open("POST", url, async); 
		http.setRequestHeader('Content-Type', 'application/x-www-form-urlencoded; charset=ISO-8859-1');
		http.onreadystatechange = function () {
			if (http.readyState == 4) {
				if(http.status == 200) {
					if (componentID != null) {
						if (component != "" && component != null) {
							$('#loading1').hide();
							$('#loading').hide();
							$('#tablaEstudiantes').show();
							component.innerHTML = http.responseText;
						} 
					} else {
						request = http.responseText;
					}
				}
			}
		};
		http.send(datosForm);
	}
	if(type == "element") {
		if (value.value != null) {
			url += "?value="+name+"&"+value.name+"="+value.value;
		} else {
			url += "?value="+name+"&content="+value;
		}
		http.open("GET", url, async); 
		http.onreadystatechange = function () {
			if (http.readyState == 4) {
				if(http.status == 200) {
					$('#loading').hide();
					$('#tablaEstudiantes').show();
					if (componentID != null) {
						if (component != "" && component != null) {
							component.innerHTML = http.responseText;
						} 
					} else {
						request = http.responseText;
					}
				} 
			}
		};
		http.send(null);
	}
	if (componentID == null) {
		return request;
	}
}
function getXMLHTTPRequest() {
	try {
		http = new XMLHttpRequest();
	} catch(err1) {
		try {
			http = new ActiveXObject("Msxml2.XMLHTTP");
		} catch (err2) {
			try {
				http = new ActiveXObject("Microsoft.XMLHTTP");
			} catch (err3) {
				http = false;
			}
		}
	}
	return http;
}
// FIN AJAX

//AJAX2
function AJAX2(value,componentID,name,async) { 
	var request = "";
	if (async == null) async = true;
	var component = document.getElementById(componentID);
	var http = getXMLHTTPRequest(); 
	var url = pathURL+"Ajax2";
	var type; 
	try {
		value.elements.length;
		type = "form";
	}catch(e) {
		type = "element";
	}
	if(type == "form") {
		var form = value;
	    var datosForm = "value="+name+"&";
	    var sepCampos = "";
	    for (var i=0; i<=form.elements.length-1; i++) {
	    	datosForm += sepCampos+form.elements[i].name+'='+encodeURI(form.elements[i].value);
	        sepCampos="&";
	    }
	    http.open("POST", url, async); 
		http.setRequestHeader('Content-Type', 'application/x-www-form-urlencoded; charset=ISO-8859-1');
//		http.setRequestHeader('Content-Type', 'multipart/form-data; charset=ISO-8859-1');
		http.onreadystatechange = function () {
			if (http.readyState == 4) {
				if(http.status == 200) {
					if (componentID != null) {
						if (component != "" && component != null) {
							$('#loading1').hide();
							component.innerHTML = http.responseText;
						} 
					} else {
						request = http.responseText;
					}
				}
			}
		};
		http.send(datosForm);
	}
	if(type == "element") {
		if (value.value != null) {
			url += "?value="+name+"&"+value.name+"="+value.value;
		} else {
			url += "?value="+name+"&content="+value;
		}
		http.open("GET", url, async); 
		http.onreadystatechange = function () {
			if (http.readyState == 4) {
				if(http.status == 200) {
					$('#loading').hide();
					$('#tablaEstudiantes').show();
					if (componentID != null) {
						if (component != "" && component != null) {
							component.innerHTML = http.responseText;
						} 
					} else {
						request = http.responseText;
					}
				} 
			}
		};
		http.send(null);
	}
	if (componentID == null) {
		return request;
	}
}
function getXMLHTTPRequest() {
	try {
		http = new XMLHttpRequest();
	} catch(err1) {
		try {
			http = new ActiveXObject("Msxml2.XMLHTTP");
		} catch (err2) {
			try {
				http = new ActiveXObject("Microsoft.XMLHTTP");
			} catch (err3) {
				http = false;
			}
		}
	}
	return http;
}
// FIN AJAX2

//AJAX3
function AJAX3(value,componentID,name,async) { 
	var request = "";
	if (async == null) async = true;
	var component = document.getElementById(componentID);
	var http = getXMLHTTPRequest(); 
	var url = pathURL+"Ajax3";
	var type; 
	try {
		value.elements.length;
		type = "form";
	}catch(e) {
		type = "element";
	}
	if(type == "form") {
		var form = value;
	    var datosForm = "value="+name+"&";
	    var sepCampos = "";
	    for (var i=0; i<=form.elements.length-1; i++) {
	    	datosForm += sepCampos+form.elements[i].name+'='+encodeURI(form.elements[i].value);
	        sepCampos="&";
	    }
	    http.open("POST", url, async); 
		http.setRequestHeader('Content-Type', 'application/x-www-form-urlencoded; charset=ISO-8859-1');
//		http.setRequestHeader('Content-Type', 'multipart/form-data; charset=ISO-8859-1');
		http.onreadystatechange = function () {
			if (http.readyState == 4) {
				if(http.status == 200) {
					if (componentID != null) {
						if (component != "" && component != null) {
							$('#loading1').hide();
							component.innerHTML = http.responseText;
						} 
					} else {
						request = http.responseText;
					}
				}
			}
		};
		http.send(datosForm);
	}
	if(type == "element") {
		if (value.value != null) {
			url += "?value="+name+"&"+value.name+"="+value.value;
		} else {
			url += "?value="+name+"&content="+value;
		}
		http.open("GET", url, async); 
		http.onreadystatechange = function () {
			if (http.readyState == 4) {
				if(http.status == 200) {
					$('#loading').hide();
					$('#tablaEstudiantes').show();
					if (componentID != null) {
						if (component != "" && component != null) {
							component.innerHTML = http.responseText;
						} 
					} else {
						request = http.responseText;
					}
				} 
			}
		};
		http.send(null);
	}
	if (componentID == null) {
		return request;
	}
}

//AJAX4
function AJAX4(value,componentID,name,async) { 
	var request = "";
	if (async == null) async = true;
	var component = document.getElementById(componentID);
	var http = getXMLHTTPRequest(); 
	var url = pathURL+"Ajax4";
	var type; 
	try {
		value.elements.length;
		type = "form";
	}catch(e) {
		type = "element";
	}
	if(type == "form") {
		var form = value;
	    var datosForm = "value="+name+"&";
	    var sepCampos = "";
	    for (var i=0; i<=form.elements.length-1; i++) {
	    	datosForm += sepCampos+form.elements[i].name+'='+encodeURI(form.elements[i].value);
	        sepCampos="&";
	    }
	    http.open("POST", url, async); 
		http.setRequestHeader('Content-Type', 'application/x-www-form-urlencoded; charset=ISO-8859-1');
//		http.setRequestHeader('Content-Type', 'multipart/form-data; charset=ISO-8859-1');
		http.onreadystatechange = function () {
			if (http.readyState == 4) {
				if(http.status == 200) {
					if (componentID != null) {
						if (component != "" && component != null) {
							$('#loading1').hide();
							component.innerHTML = http.responseText;
						} 
					} else {
						request = http.responseText;
					}
				}
			}
		};
		http.send(datosForm);
	}
	if(type == "element") {
		if (value.value != null) {
			url += "?value="+name+"&"+value.name+"="+value.value;
		} else {
			url += "?value="+name+"&content="+value;
		}
		http.open("GET", url, async); 
		http.onreadystatechange = function () {
			if (http.readyState == 4) {
				if(http.status == 200) {
					$('#loading').hide();
					$('#tablaEstudiantes').show();
					if (componentID != null) {
						if (component != "" && component != null) {
							component.innerHTML = http.responseText;
						} 
					} else {
						request = http.responseText;
					}
				} 
			}
		};
		http.send(null);
	}
	if (componentID == null) {
		return request;
	}
}
function getXMLHTTPRequest() {
	try {
		http = new XMLHttpRequest();
	} catch(err1) {
		try {
			http = new ActiveXObject("Msxml2.XMLHTTP");
		} catch (err2) {
			try {
				http = new ActiveXObject("Microsoft.XMLHTTP");
			} catch (err3) {
				http = false;
			}
		}
	}
	return http;
}
// FIN AJAX3

//AJAX5
function AJAX5(value,componentID,name,async) { 
	var request = "";
	if (async == null) async = true;
	var component = document.getElementById(componentID);
	var http = getXMLHTTPRequest(); 
	var url = pathURL+"Ajax5";
	var type; 
	try {
		value.elements.length;
		type = "form";
	}catch(e) {
		type = "element";
	}
	if(type == "form") {
		var form = value;
	    var datosForm = "value="+name+"&";
	    var sepCampos = "";
	    for (var i=0; i<=form.elements.length-1; i++) {
	    	datosForm += sepCampos+form.elements[i].name+'='+encodeURI(form.elements[i].value);
	        sepCampos="&";
	    }
	    http.open("POST", url, async); 
		http.setRequestHeader('Content-Type', 'application/x-www-form-urlencoded; charset=ISO-8859-1');
//		http.setRequestHeader('Content-Type', 'multipart/form-data; charset=ISO-8859-1');
		http.onreadystatechange = function () {
			if (http.readyState == 4) {
				if(http.status == 200) {
					if (componentID != null) {
						if (component != "" && component != null) {
							$('#loading1').hide();
							component.innerHTML = http.responseText;
						} 
					} else {
						request = http.responseText;
					}
				}
			}
		};
		http.send(datosForm);
	}
	if(type == "element") {
		if (value.value != null) {
			url += "?value="+name+"&"+value.name+"="+value.value;
		} else {
			url += "?value="+name+"&content="+value;
		}
		http.open("GET", url, async); 
		http.onreadystatechange = function () {
			if (http.readyState == 4) {
				if(http.status == 200) {
					$('#loading').hide();
					$('#tablaEstudiantes').show();
					if (componentID != null) {
						if (component != "" && component != null) {
							component.innerHTML = http.responseText;
						} 
					} else {
						request = http.responseText;
					}
				} 
			}
		};
		http.send(null);
	}
	if (componentID == null) {
		return request;
	}
}
function getXMLHTTPRequest() {
	try {
		http = new XMLHttpRequest();
	} catch(err1) {
		try {
			http = new ActiveXObject("Msxml2.XMLHTTP");
		} catch (err2) {
			try {
				http = new ActiveXObject("Microsoft.XMLHTTP");
			} catch (err3) {
				http = false;
			}
		}
	}
	return http;
}
// FIN AJAX5

//AJAX6
function AJAX6(value,componentID,name,async) {
	var request = "";
	if (async == null) async = true;
	var component = document.getElementById(componentID);
	var http = getXMLHTTPRequest();
	var url = pathURL+"Ajax6";
	var type;
	try {
		value.elements.length;
		type = "form";
	}catch(e) {
		type = "element";
	}
	if(type == "form") {
		var form = value;
	    var datosForm = "value="+name+"&";
	    var sepCampos = "";
	    for (var i=0; i<=form.elements.length-1; i++) {
	    	datosForm += sepCampos+form.elements[i].name+'='+encodeURI(form.elements[i].value);
	        sepCampos="&";
	    }
	    http.open("POST", url, async);
		http.setRequestHeader('Content-Type', 'application/x-www-form-urlencoded; charset=ISO-8859-1');
//		http.setRequestHeader('Content-Type', 'multipart/form-data; charset=ISO-8859-1');
		http.onreadystatechange = function () {
			if (http.readyState == 4) {
				if(http.status == 200) {
					if (componentID != null) {
						if (component != "" && component != null) {
							$('#loading1').hide();
							component.innerHTML = http.responseText;
						}
					} else {
						request = http.responseText;
					}
				}
			}
		};
		http.send(datosForm);
	}
	if(type == "element") {
		if (value.value != null) {
			url += "?value="+name+"&"+value.name+"="+value.value;
		} else {
			url += "?value="+name+"&content="+value;
		}
		http.open("GET", url, async);
		http.onreadystatechange = function () {
			if (http.readyState == 4) {
				if(http.status == 200) {
					$('#loading').hide();
					$('#tablaEstudiantes').show();
					if (componentID != null) {
						if (component != "" && component != null) {
							component.innerHTML = http.responseText;
						}
					} else {
						request = http.responseText;
					}
				}
			}
		};
		http.send(null);
	}
	if (componentID == null) {
		return request;
	}
}
function getXMLHTTPRequest() {
	try {
		http = new XMLHttpRequest();
	} catch(err1) {
		try {
			http = new ActiveXObject("Msxml2.XMLHTTP");
		} catch (err2) {
			try {
				http = new ActiveXObject("Microsoft.XMLHTTP");
			} catch (err3) {
				http = false;
			}
		}
	}
	return http;
}
// FIN AJAX6
/* VALIDACIÓN DE FORMULARIOS */

//Busca todos los formularios con data-ckeck="true" y asigna el evento
var forms = document.getElementsByTagName("form");
for(var i=0; i<forms.length; i++){
	if (forms[i].dataset.check == "true") {
		var form = forms[i];
		var action = form.getAttribute("action");
		CheckInputsType(form);
		for(var i=0; i<form.elements.length; i++){
			var elementForm = form.elements[i];
			if(elementForm.name == "Submit" && elementForm.type == "button"){
				CreateEvent(elementForm, "click", function(){
					SendForm(this.form);
//					Función a ejecutar antes de validar el formulario
//					var fn = form.dataset.fn;
//					if (fn != null) {
//						var send = window[fn].apply(this, []);
//						if (send == true) {
//							SendForm(this.form);
//						}
//					} else {
//						SendForm(this.form);
//					}
				});
			}
			if(elementForm.name == "Cancel" && elementForm.type == "button"){
				CreateEvent(elementForm, "click", function(){
					window.location.href = action;
				});
			}
		}
	}
}

//Envia el formulario si cumple las condiciones
function SendForm(form) {
	if (CheckForm(form)) {
		//Función a ejecutar antes de enviar el formulario
		var fn = form.dataset.fn;
		if (fn != null) {
			var send = window[fn].apply(this, []);
			if (send == true) {
				if (form.dataset.confirm == "true") {
					Alert("¡Confirmar acción!","¿Esta seguro de guardar los cambios?","confirm","Aceptar,Cancelar");
					var aceptar = document.getElementById('btnPrimaryAlert');
					aceptar.addEventListener('click',function(){
						EnabledElementsForm(form);
						form.submit();
					})
				} else {
					EnabledElementsForm(form);
					form.submit();
				}
			}
		} else {
			if (form.dataset.confirm == "true") {
				Alert("¡Confirmar acción!","¿Esta seguro de guardar los cambios?","confirm","Aceptar,Cancelar");
				var aceptar = document.getElementById('btnPrimaryAlert');
				aceptar.addEventListener('click',function(){
					EnabledElementsForm(form);
					form.submit();
				})
			} else {
				EnabledElementsForm(form);
				form.submit();
			}
		}
	} 
}

//Habilitar todos los elementos del formulario antes de enviarlo
function EnabledElementsForm(form) {
	for(var i=0; i<form.elements.length; i++){
		var elementForm = form.elements[i];
		elementForm.disabled = false;
	}
}

//Valida si el formulario cumple las condiciones para ser enviado
function CheckForm(form) {
	if (CheckRequiredFields(form) && CheckEmail(form)){
		return true;
	}
	return false;
}

//Valida los tipos de dato de los inputs del formulario y asigna los eventos
function CheckInputsType(form) {
	for(var i=0; i<form.elements.length; i++){
		var elementForm = form.elements[i];
		if(elementForm.type != "select-one" && elementForm.type != "checkbox") {
			CreateEvent(elementForm, "input", function(){
				CheckInputs(this);
			});
		}
		if(elementForm.type == "checkbox"){
			CreateEvent(elementForm, "click", function(){
				CheckCheckbox(this);
			});
		}
		if (elementForm.dataset.enter == "true") {
			CreateEvent(elementForm, "keypress", function(e){
				var tecla = (document.all) ? e.keyCode : e.which;
				if (tecla == 13) {
					SendForm(form);
				}
			});
		}
	}
}

//Metodo para asignar eventos
function CreateEvent(elemento, evento, funcion) {
	if (elemento != null) {
		if (elemento.addEventListener) {
	          elemento.addEventListener(evento, funcion, false);
	    } else {
	          elemento.attachEvent("on" + evento, funcion);
	    }
	}
}

//Valida el tipo de input (text-free alpht-accent alpht-no-accent number) y limita tamaño
function CheckInputs(elementForm) {
	var type = elementForm.dataset.type;
	var limit = 255;
	if (elementForm.dataset.size != null) {
		limit = elementForm.dataset.size;
	}
	LimitCharacters(limit, elementForm);
	switch(type) {
	    case "alpht-no-accent":
	    	elementForm.value = CheckTextAlpht(elementForm.value);
	        break;
	    case "alpht":
	    	elementForm.value = CheckTextAlphtAccent(elementForm.value);
	        break;
	    case "number":
	    	elementForm.value = CheckNumber(elementForm.value);
	        break;
	    case "decimal":
	    	elementForm.value = CheckDecimal(elementForm.value, elementForm.dataset.size);
	        break;
	    default:
	    	elementForm.value = elementForm.value;
	}
}

//Valida las acciones sobre los input checkbox
function CheckCheckbox(elementForm) {
	if(elementForm.value == "0") {
		elementForm.value = "1";
		elementForm.checked = true;
	} else if(elementForm.value == "1") {
		elementForm.value = "0";
		elementForm.checked = false;
	}
}

//Limita los caracteres de un input
function LimitCharacters(limit, element) {
	if (element.dataset.type == "number" || element.dataset.type == "decimal") {
		if (element.value.charAt(0) == "-") {
			limit++;
		}
		if (element.value.indexOf(".") != -1) {
			limit++;
		}
	}
    if (element.value != undefined) {
		if(element.value.length > limit) {
			element.value = element.value.slice(0,limit); 
		} 		
	}
}

//Filtra el contenido del input a solo letras del alphabeto sin acento
function CheckTextAlpht(elementValue) {
	var out = "";
	const space = " ";
	var filtro = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ"+space;
	for (var i=0; i<elementValue.length; i++)
		if (filtro.indexOf(elementValue.charAt(i)) != -1) {
			out += elementValue.charAt(i);
     	} 
	return out;
} 

//Filtra el contenido del input a solo letras del alphabeto con acento
function CheckTextAlphtAccent(elementValue) {
	var out = "";
	const space = " ";
	var acentos = "ÀÁÂÃÄÅàáâãäåÒÓÔÕÖØòóôõöøÈÉÊËèéêëÇçÌÍÎÏìíîïÙÚÛÜùúûüÿÑñ";
	var filtro = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ"+space+acentos;
	for (var i=0; i<elementValue.length; i++)
		if (filtro.indexOf(elementValue.charAt(i)) != -1) {
			out += elementValue.charAt(i);
     	} 
	return out;
}

//Filtra el contenido del input a solo números
function CheckNumber(elementValue) {
	var length = elementValue.length;
	var out = elementValue.substring(0,length-1);
	var filtro = "-1234567890";
	
	for (var i=0; i<filtro.length; i++) {
		if (elementValue.substring(length-1,length) == filtro.charAt(i)) {
			if (filtro.charAt(i) == "-") {
				if (length==1) {
					out += filtro.charAt(i);
				}
			} else {
				out += filtro.charAt(i);
			}
		}
	}
	return out;
} 

//Filtra el contenido del input a solo números decimales
function CheckDecimal(elementValue, size) {
	var length = elementValue.length;
	var out = elementValue.substring(0,length-1);
	var filtro = "-.1234567890";
	
	for (var i=0; i<filtro.length; i++) {
		if (elementValue.substring(length-1,length) == filtro.charAt(i)) {
			if (filtro.charAt(i) == "-") {
				if (length == 1) {
					out += filtro.charAt(i);
				}
			} else {
				if (filtro.charAt(i) == ".") {
					if (out.includes("-")) {
						if (length != 2 && !out.includes(".") && out.length-1 != size) {
							out += filtro.charAt(i);
						}
					} else {
						if (length != 1 && !out.includes(".") && out.length != size) {
							out += filtro.charAt(i);
						}
					}
				} else {
					out += filtro.charAt(i);
				}
			}
		}
	}
	return out;
} 

//Validaciones sobre campos (input,textarea,select,checkbox) requeridos
function CheckRequiredFields(form) {
	var body = document.getElementsByTagName("body")[0];
	var emptyField = false;
	for(var i=0; i<form.elements.length; i++){
		if(form.elements[i].dataset.req == "true"){
			if(form.elements[i].value.trim() == ""){
	    		Alert("¡Campos Requeridos!","Por Favor, "+form.elements[i].dataset.msj,"alert","Aceptar");
	    		emptyField = true;
	    	}
	    	if(form.elements[i].type == "select-one"){
	        	if(form.elements[i].options[form.elements[i].selectedIndex].value == "0") {
	        		Alert("¡Campos Requeridos!","Por Favor, "+form.elements[i].dataset.msj,"alert","Aceptar");
	        		emptyField = true;
	        	}
	    	}
	    	if(form.elements[i].type == "checkbox"){
	        	if(form.elements[i].value == "0" || form.elements[i].value == "") {
	        		Alert("¡Campos Requeridos!","Por Favor, "+form.elements[i].dataset.msj,"alert","Aceptar");
	        		emptyField = true;
	        	}
	    	}
	    	if(emptyField) {
	    		var timer = setInterval(function() {
		  		  	if(!body.classList.contains("modal-open")) {
		    			form.elements[i].focus();
		  		  		clearInterval(timer);
		  		  	}
		        }, 1000);
	    		return false;
	    	}
		}
	}
	return true;
}

//Valida que los input email sean correctos
function CheckEmail(form) {
	var body = document.getElementsByTagName("body")[0];
	var valido = true;
	var regExp = /^[a-zA-Z0-9.!#$%&'*+/=?^_`{|}~-]+@[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?(?:\.[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)*$/;
	for(var i=0; i<form.elements.length; i++){
		var email = form.elements[i].value;
  	if(form.elements[i].type == "email" && email != ""){
  		if(!regExp.test(email)) {
  			Alert("¡Correo Inválido!","Por Favor, "+form.elements[i].title,"alert","Aceptar");
  			valido = false;
  	    }
  		if(!valido) {
	    		var timer = setInterval(function() {
	    			form.elements[i].focus();
		  		  	if(!body.classList.contains("modal-open")) clearInterval(timer);
		  		  	console.log('post-interval'); 
		        }, 1000);
	    		return false;
	    	}
  	}
	}
	return valido;
}



/* MENSAJES DE ALERTA TIPO MODAL */
function CreateAlert() {
	var buttonAlert = document.createElement("button");
	buttonAlert.setAttribute("id", "Alert");
	buttonAlert.setAttribute("name", "Alert");
	buttonAlert.setAttribute("class", "btn btn-default hidden");
	buttonAlert.setAttribute("data-toggle", "modal");
	buttonAlert.setAttribute("data-target", "#alertModal");
	document.body.appendChild(buttonAlert);

	var divAlert = document.createElement("div");
	divAlert.setAttribute("id", "alertModal");
	divAlert.setAttribute("class", "modal fade");
	divAlert.setAttribute("tabindex", "-1");
	divAlert.setAttribute("role", "dialog");
	divAlert.setAttribute("aria-labelledby", "headerModal");
	document.body.appendChild(divAlert);

	document.getElementById("alertModal").innerHTML = ""+
		"<div id='mdalert' class='modal-dialog' role='document'>" +
		  	"<div class='modal-content'>" +
		    		"<div class='modal-header modalHeader'>" +
		      		"<button type='button' class='close modalBtnClose' data-dismiss='modal' aria-label='Close'><span aria-hidden='true'>&times;</span></button>" +
		      		"<h4 class='modal-title' name='titleAlert' id='titleAlert'></h4>" +
		    		"</div>" +
		    		"<div class='modal-body modalBody'>" +
		    			"<p id='txtAlert' name='txtAlert'></p>" +
						"<textarea class='form-control' id='inputAlert' name='inputAlert' rows='3'></textarea>" +
						"<div class='modal-body modalBody' id='personalizeAlert' name='personalizeAlert'>" +
			    		"</div>" +
			    	"</div>" +
				    "<div class='modal-footer'>" +
				        "<button id='btnDefaultAlert' name='btnDefaultAlert' type='button' class='btn modalBtnDefault' data-dismiss='modal'></button>" +
				        "<button id='btnPrimaryAlert' name='btnPrimaryAlert' type='button' class='btn modalBtnPrimary' data-dismiss='modal'></button>" +
				    "</div>" +
		  	"</div>" +
		"</div>";
}

function Alert(titulo,texto,tipo,botones,ancho){
	CreateAlert();
	
	$('#btnPrimaryAlert').off('click');
	$('#btnDefaultAlert').off('click');
	//Los nombres de los botones separados por comas
	var botones = botones.split(",");
	//Alerta normal, solo muestra el texto y un boton
	if(tipo=="alert"){
		$('#titleAlert').html(titulo);
	    $('#txtAlert').html(texto);
	    $('#btnPrimaryAlert').html(botones[0]);
	    $('#btnDefaultAlert').hide();
	    $('#inputAlert').hide();
	    $('#personalizeAlert').hide();      
	}
	//Alerta de tipo confirm, texto con pregunta y dos botones
	if(tipo=="confirm"){
	    $('#titleAlert').html(titulo);
	    $('#txtAlert').html(texto);
	    $('#btnPrimaryAlert').html(botones[0]);
	    $('#btnDefaultAlert').show();
	    $('#btnDefaultAlert').html(botones[1]);
	    $('#inputAlert').hide();
	    $('#personalizeAlert').hide();     
	}
	//Alerta de tipo input, dos botones y una caja de texto (inputAlert)
	if(tipo=="input"){
	    $('#titleAlert').html(titulo);
	    $('#txtAlert').html(texto);
	    $('#btnPrimaryAlert').html(botones[0]);
	    $('#btnDefaultAlert').show();
	    $('#btnDefaultAlert').html(botones[1]);
	    $('#inputAlert').show();
	    $('#personalizeAlert').hide();       
	}
	//Alerta personalizable con contenido propio enviando el html
	if(tipo=="personalize"){
	    $('#titleAlert').html(titulo);
	    $('#personalizeAlert').html(texto);
	    $('#btnPrimaryAlert').html(botones[0]);
	    $('#btnDefaultAlert').hide();
	    if (botones.length > 1) {
		    $('#btnDefaultAlert').show();
		    $('#btnDefaultAlert').html(botones[1]);
	    }
	    $('#txtAlert').hide();
	    $('#inputAlert').hide();
	    $('#personalizeAlert').show();    
	    $('.modal-footer').css("border","none");
	    $('#mdalert').width(ancho);   
	}
	$("#alertModal").modal('show');
}

var btnPrimaryAlert = document.getElementById('btnPrimaryAlert');
if (btnPrimaryAlert != null) {
	btnPrimaryAlert.addEventListener('click',function(){
		document.body.removeChild(buttonAlert);
		document.body.removeChild(divAlert);
	})
}


/* FUNCIONES */

//Replace all
String.prototype.replaceAll = function(search, replacement) {
  var target = this;
  return target.replace(new RegExp(search, 'g'), replacement);
};

//Bloquear botón atras del navegador
function noBackButton() {	
 window.location.hash = "no-back-button";
 window.location.hash = "Again-No-back-button"
 window.onhashchange = function() {
	   window.location.hash="no-back-button";
 }
}

//Remueve los espacios antes y despues
function removeSpaces(cadena) {
	var cadenaOld = cadena.split("");
	var inicio = 0;
	var fin = 0;
	for(i=0; i<cadenaOld.length; i++){
		if (cadenaOld[i] == " "){
			inicio++;
		} else {
			break;
		}
	}
	for(i=cadenaOld.length; i>0; i--){
		if (cadenaOld[i] == " "){
			fin++;
		} else {
			break;
		}
	}
	var cadenaNew = cadena.substring(inicio,fin);
	console.log("inicio "+inicio+" - "+" fin "+fin);
	return cadenaNew;
}

//DatePicker
if($.datepicker != undefined){
	
	$.datepicker.regional['es'] = {
			closeText: 'Cerrar',
			prevText: '< Ant',
			nextText: 'Sig >',
			currentText: 'Hoy',
			monthNames: ['Enero', 'Febrero', 'Marzo', 'Abril', 'Mayo', 'Junio', 'Julio', 'Agosto', 'Septiembre', 'Octubre', 'Noviembre', 'Diciembre'],
			monthNamesShort: ['Ene','Feb','Mar','Abr', 'May','Jun','Jul','Ago','Sep', 'Oct','Nov','Dic'],
			dayNames: ['Domingo', 'Lunes', 'Martes', 'Miércoles', 'Jueves', 'Viernes', 'Sábado'],
			dayNamesShort: ['Dom','Lun','Mar','Mié','Juv','Vie','Sáb'],
			dayNamesMin: ['Do','Lu','Ma','Mi','Ju','Vi','Sá'],
			weekHeader: 'Sm',
		 	dateFormat: 'yy-mm-dd',
			firstDay: 1,
			isRTL: false,
			showMonthAfterYear: false,
			yearSuffix: '',
			yearRange: '-50:+5' //Rango de años para mostrar hacia adelante
		}; 
		$.datepicker.setDefaults($.datepicker.regional['es']);
}

var datePickers = document.getElementsByClassName("datepicker");
for (var i=0; i<datePickers.length; i++) {
	$("#"+datePickers[i].id).datepicker({
  		changeMonth: true,
  		changeYear: true
	});
}

//SelectPicker
var selects = document.getElementsByTagName("select");
for (var i=0; i<selects.length; i++) {
	if (selects[i].classList.contains('selectpicker')) {
		selects[i].style.zIndex = '-1';
	}
}

//Cambiar valor de inputs o select
function ChangeValue(nameElement, newValue) {
	var element = document.getElementById(nameElement);
	if (element.type == "select-one"){
		element.value = newValue;
		$("#"+element.id).selectpicker('refresh'); 
	} else {
		element.value = newValue;
	}
}

//Deshabilitar inputs o select
function DisabledElement(nameElement,action) {
	var element = document.getElementById(nameElement);
	if (element.type == "select-one"){
		element.disabled = action;
		$("#"+element.id).selectpicker('refresh'); 
	} else {
		element.disabled = action;
	}
}

//Volver requeridos inputs o select
function RequiredElement(nameElement,action) {
	var element = document.getElementById(nameElement);
	element.dataset.req = action;
}

//Refrescar bootstrap select
function RefreshSelect(nameElement) {
	$("#"+nameElement).selectpicker('refresh'); 
}

//Convertir texto tipo oraciòn
function FirstMayus(string){
	return string.charAt(0).toUpperCase() + string.slice(1);
}

//Obtiene fecha
function getDateTime() {
	var now     = new Date(); 
    var year    = now.getFullYear();
    var month   = now.getMonth()+1; 
    var day     = now.getDate();
    var hour    = now.getHours();
    var minute  = now.getMinutes();
    var second  = now.getSeconds(); 
    if(month.toString().length == 1) {
        var month = '0'+month;
    }
    if(day.toString().length == 1) {
        var day = '0'+day;
    }   
    if(hour.toString().length == 1) {
        var hour = '0'+hour;
    }
    if(minute.toString().length == 1) {
        var minute = '0'+minute;
    }
    if(second.toString().length == 1) {
        var second = '0'+second;
    }   
    var dateTime = day+'/'+month+'/'+year+' '+hour+':'+minute+':'+second;   
    return dateTime;
}
//AJAX7
function AJAX7(value,componentID,name,async) {
	var request = "";
	if (async == null) async = true;
	var component = document.getElementById(componentID);
	var http = getXMLHTTPRequest();
	var url = pathURL+"Ajax7";
	var type;
	try {
		value.elements.length;
		type = "form";
	}catch(e) {
		type = "element";
	}
	if(type == "form") {
		var form = value;
		var datosForm = "value="+name+"&";
		var sepCampos = "";
		for (var i=0; i<=form.elements.length-1; i++) {
			datosForm += sepCampos+form.elements[i].name+'='+encodeURI(form.elements[i].value);
			sepCampos="&";
		}
		http.open("POST", url, async);
		http.setRequestHeader('Content-Type', 'application/x-www-form-urlencoded; charset=ISO-8859-1');
//		http.setRequestHeader('Content-Type', 'multipart/form-data; charset=ISO-8859-1');
		http.onreadystatechange = function () {
			if (http.readyState == 4) {
				if(http.status == 200) {
					if (componentID != null) {
						if (component != "" && component != null) {
							$('#loading1').hide();
							component.innerHTML = http.responseText;
						}
					} else {
						request = http.responseText;
					}
				}
			}
		};
		http.send(datosForm);
	}
	if(type == "element") {
		if (value.value != null) {
			url += "?value="+name+"&"+value.name+"="+value.value;
		} else {
			url += "?value="+name+"&content="+value;
		}
		http.open("GET", url, async);
		http.onreadystatechange = function () {
			if (http.readyState == 4) {
				if(http.status == 200) {
					$('#loading').hide();
					$('#tablaEstudiantes').show();
					if (componentID != null) {
						if (component != "" && component != null) {
							component.innerHTML = http.responseText;
						}
					} else {
						request = http.responseText;
					}
				}
			}
		};
		http.send(null);
	}
	if (componentID == null) {
		return request;
	}
}
