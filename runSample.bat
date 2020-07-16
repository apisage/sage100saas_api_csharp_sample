chcp 65001
SETLOCAL ENABLEDELAYEDEXPANSION
@echo off
:DEBUT
cls
echo -----------
echo Lancement de l'exemple C#

cd app
start chrome http://localhost:8080
dotnet run 
pause
GOTO DEBUT:




