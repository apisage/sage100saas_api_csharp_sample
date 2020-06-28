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
        button[i].onclick = function () {
           setTimeout(function () { spinner(); }, 200);
        }   
    }
}
var spinner = function () {
    document.querySelector(".spinner").style.display = "block";
}
window.addEventListener("load", function () {addSpinnerToButton()});

//Affiche ou masque les paramètres OData utilisés pour filtrer les clients
var showHideOdata = function () {
    var odatainfos = document.getElementById("odatainfos");
    odatainfos.style.display = (odatainfos.style.display == "none") ? "" : "none";
}

//Affichage légèrement différé info BETA
var displayBETA = function () {
    document.querySelector(".BETA").style.left = "5px";
}
window.addEventListener("load",function(){displayBETA()})
