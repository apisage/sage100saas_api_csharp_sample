// Met en évidence l'onglet courant
var currentTab = location.origin + "/" + location.pathname.split("/")[1];
var button = document.querySelector(".menu").querySelectorAll("a.button");
for (i = 0; i < button.length;i++) {
    if (button[i].href != currentTab && button[i].className.indexOf("secondary")==-1)
        button[i].className = button[i].className.replace("button", "button secondary");
}

// Boucle sur tous les boutons ayant une classe bouton ou liens avec classe addspinner pour ajouter un affichage spinner quand on clique sur le bouton
var addSpinnerToButton = function () {
    var button = document.querySelectorAll("button.button,.addspinner");
    for (var i = 0; i < button.length; i++) {
        button[i].addEventListener("click", function () { TimerSpinner = setTimeout(function () { spinner(); }, 200); }); 
    }
}
var spinner = function () {
    document.querySelector(".spinner").style.display = "block";
    document.getElementById("durationlastAPI").style.display = "none";
}
window.addEventListener("load", function () { addSpinnerToButton() });

var cancelSpinner = function () {
    if (TimerSpinner) {
        clearTimeout(TimerSpinner);
        TimerSpinner = null;
        document.querySelector(".spinner").style.display = "none";
    }
}

//Affiche ou masque les paramètres OData utilisés pour filtrer les clients
var showHideOdata = function () {
    var odatainfos = document.getElementById("odatainfos");
    odatainfos.style.display = (odatainfos.style.display == "none") ? "" : "none";
}

//Affichage légèrement différé info BETA
var displayBETA = function () {
    document.querySelector(".BETA").style.left = "5px";
}
window.addEventListener("load", function () { displayBETA() })

//Affichage d'une erreur contextuelle sur la saisie d'un champ
// textError : message à afficher
// forceFocus : donne le focus à l'objet forceFocus
// isInfo : message d'info et non message d'erreur
var affectContextError = function (textError, forceFocus,isInfo) {
    if (textError && forceFocus) forceFocus.focus();
    var contextError = document.getElementById("CONTEXTERROR");
    if (contextError) {
        if (textError) {
            contextError.innerHTML = textError;
            contextError.style.display = "block";
            contextError.className = (!isInfo) ? "contextError" : "contextInfo";
         }
        else {  
            contextError.innerHTML = "";                                                
            contextError.style.display = "none";
          }
    }
}

//En sortie de champ on efface le message courant
document.addEventListener('blur', (event) => {
    affectContextError();
}, true);


//Donne le focus au premier champ de saisie d'un formulaire
setTimeout(function(){
    if (!document.forms.length) return;
    form = document.forms[0];
    for (var i = 0; i < form.length; i++) {
        e = form.elements[i];
        if (e.name && e.type != "submit" && e.type != "hidden" && !e.disabled && !e.readOnly) {
            if (e.className.indexOf("nofocus") == -1) {
                e.focus();
                break;
            }
        }
    }
}, 200);   

//boite de dialogue
var dlgbox = function (title, question, action) {
    document.getElementById("Yes").onclick = action;

    document.getElementById("dlgboxTitle").innerHTML = title;
    document.getElementById("dlgboxQuestion").innerHTML = question;
    document.getElementById("dlgbox").style.display = "block";
    document.getElementById("modal-background").style.display = "block";
}
var dlgboxCancel = function () {
    document.getElementById("dlgbox").style.display = "none";
    document.getElementById("modal-background").style.display = "none";
}

