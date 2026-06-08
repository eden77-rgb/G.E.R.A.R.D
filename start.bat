@ECHO OFF
SETLOCAL ENABLEDELAYEDEXPANSION

REM ============================================================
REM   START.BAT - Lancement complet de l'application
REM   Services : Ollama | Backend Python | Client WPF .NET10
REM ============================================================

CALL :print_cyan "========================================================"
CALL :print_cyan "        Lancement complet de l''application              "
CALL :print_cyan "   [Ollama]  [Backend Python]  [Client WPF .NET10]      "
CALL :print_cyan "========================================================"
ECHO.


REM ============================================================
REM   1. OLLAMA
REM ============================================================
CALL :print_yellow "[OLLAMA] Verification du service Ollama..."

REM Verifier si ollama est accessible
WHERE ollama >NUL 2>&1
IF %ERRORLEVEL% NEQ 0 (
    CALL :print_red "[ERREUR] Ollama n''est pas installe ou pas dans le PATH."
    CALL :print_red "         Telechargez-le sur https://ollama.com"
    PAUSE
    EXIT /B 1
)

REM Verifier si le modele est deja present
ollama list 2>NUL | findstr /I "qwen2.5:3b" >NUL
IF %ERRORLEVEL% NEQ 0 (
    CALL :print_yellow "[OLLAMA] Modele qwen2.5:3b non trouve. Telechargement en cours..."
    ollama pull qwen2.5:3b
    ollama list 2>NUL | findstr /I "qwen2.5:3b" >NUL
    IF %ERRORLEVEL% NEQ 0 (
        CALL :print_red "[ERREUR] Le modele qwen2.5:3b n'est pas present apres le pull."
        PAUSE
        EXIT /B 1
    )
    CALL :print_green "[OK] Modele qwen2.5:3b telecharge."
) ELSE (
    CALL :print_green "[OK] Modele qwen2.5:3b deja present."
)

REM Lancer ollama serve dans une nouvelle fenetre
CALL :check_port 11434
IF %ERRORLEVEL% EQU 0 (
    CALL :print_green "[OK] Serveur Ollama deja actif sur le port 11434, demarrage ignore."
) ELSE (
    CALL :print_yellow "[OLLAMA] Demarrage du serveur Ollama..."
    start "Ollama Server" cmd /k "ollama serve"
    FOR /F "tokens=2" %%P IN ('tasklist /FI "WINDOWTITLE eq Ollama Server" /NH 2^>NUL') DO SET "PID_OLLAMA=%%P"
    CALL :print_yellow "[OLLAMA] Attente de disponibilite du serveur (port 11434)..."
    CALL :wait_for_port 11434 15
    IF !ERRORLEVEL! NEQ 0 (
        CALL :print_red "[ERREUR] Le serveur Ollama ne repond pas apres 15 secondes."
        PAUSE
        EXIT /B 1
    )
    CALL :print_green "[OK] Serveur Ollama demarre et pret."
)


REM ============================================================
REM   2. BACKEND PYTHON
REM ============================================================
CALL :print_yellow "[PYTHON] Installation des dependances..."

IF NOT EXIST ".\server\requirements.txt" (
    CALL :print_red "[ERREUR] Fichier requirements.txt introuvable dans .\server\"
    PAUSE
    EXIT /B 1
)

pip install -r .\server\requirements.txt --quiet
IF %ERRORLEVEL% NEQ 0 (
    CALL :print_red "[ERREUR] Echec de l''installation des dependances Python."
    PAUSE
    EXIT /B 1
)
CALL :print_green "[OK] Dependances Python installees."

CALL :print_yellow "[PYTHON] Demarrage du serveur FastAPI..."
start "Backend Python" cmd /k "cd .\server && fastapi run app\main.py"
FOR /F "tokens=2" %%P IN ('tasklist /FI "WINDOWTITLE eq Backend Python" /NH 2^>NUL') DO SET "PID_PYTHON=%%P"

IF NOT DEFINED PID_PYTHON (
    CALL :print_red "[ERREUR] Impossible de trouver le processus du backend."
    PAUSE
    EXIT /B 1
)
CALL :print_green "[OK] Serveur Python demarre."

REM Attendre que le backend soit pret (port 8000 par defaut FastAPI)
CALL :print_yellow "[PYTHON] Attente de disponibilite du backend (port 8000)..."
CALL :wait_for_port 8000 20
IF %ERRORLEVEL% NEQ 0 (
    CALL :print_red "[ERREUR] Le backend Python ne repond pas apres 20 secondes."
    PAUSE
    EXIT /B 1
)
CALL :print_green "[OK] Backend Python est pret."
ECHO.


REM ============================================================
REM   3. CLIENT WPF .NET10
REM ============================================================
CALL :print_yellow "[CLIENT] Compilation du projet WPF..."

IF NOT EXIST ".\client\client.csproj" (
    CALL :print_red "[ERREUR] Fichier client.csproj introuvable dans .\client\"
    PAUSE
    EXIT /B 1
)

dotnet build .\client\client.csproj --nologo -v quiet
IF %ERRORLEVEL% NEQ 0 (
    CALL :print_red "[ERREUR] La compilation du client a echoue."
    PAUSE
    EXIT /B 1
)
CALL :print_green "[OK] Compilation reussie."

CALL :print_yellow "[CLIENT] Lancement de l''application WPF..."
FOR /F %%P IN ('powershell -NoProfile -Command "(Start-Process '.\client\bin\Debug\net10.0-windows\client.exe' -PassThru).Id"') DO SET "PID_CLIENT=%%P"
IF NOT DEFINED PID_CLIENT (
    CALL :print_red "[ERREUR] Impossible de lancer le client WPF."
    PAUSE
    EXIT /B 1
)
CALL :print_green "[OK] Client WPF lance."
ECHO.


REM ============================================================
REM   RESUME FINAL
REM ============================================================
CALL :print_cyan "========================================================"
CALL :print_green "   Tous les services sont demarres avec succes !        "
CALL :print_cyan "   Ollama  : http://localhost:11434                      "
CALL :print_cyan "   Backend : http://localhost:8000                       "
CALL :print_cyan "   Client  : Application WPF ouverte                    "
CALL :print_cyan "========================================================"

ECHO.
CALL :print_yellow "   Appuyez sur une touche pour ARRETER tous les services..."
PAUSE >NUL


REM ============================================================
REM   NETTOYAGE - Fermeture des services lances par ce script
REM ============================================================
ECHO.
CALL :print_yellow "[STOP] Arret des services en cours..."

IF DEFINED PID_OLLAMA (
    taskkill /PID %PID_OLLAMA% /F /T >NUL 2>&1
    CALL :print_green "[OK] Serveur Ollama arrete."
)
 
IF DEFINED PID_PYTHON (
    taskkill /PID %PID_PYTHON% /F /T >NUL 2>&1
    CALL :print_green "[OK] Backend Python arrete."
)

IF DEFINED PID_CLIENT (
    taskkill /PID %PID_CLIENT% /F /T >NUL 2>&1
    CALL :print_green "[OK] Client WPF arrete."
)
 
REM Tuer les processus Python/uvicorn restants lies au backend si necessaire
taskkill /FI "WINDOWTITLE eq Backend Python" /F /T >NUL 2>&1
taskkill /FI "WINDOWTITLE eq Ollama Server" /F /T >NUL 2>&1
 
CALL :print_green "[OK] Nettoyage termine."
TIMEOUT /T 2 /NOBREAK >NUL
EXIT /B 0


REM ============================================================
REM   FONCTIONS UTILITAIRES
REM ============================================================

REM Verifie immediatement si un port est ouvert (sans attente)
REM Usage : CALL :check_port <PORT>  → ERRORLEVEL 0 si ouvert, 1 sinon
:check_port
powershell -Command "try { $t = New-Object Net.Sockets.TcpClient; $t.Connect('127.0.0.1', %~1); $t.Close(); exit 0 } catch { exit 1 }" >NUL 2>&1
EXIT /B %ERRORLEVEL%


REM Attente active sur un port TCP
REM Usage : CALL :wait_for_port <PORT> <MAX_SECONDES>
:wait_for_port
SET "_PORT=%~1"
SET "_MAX=%~2"
SET "_COUNT=0"
:wait_loop
    powershell -Command "try { $t = New-Object Net.Sockets.TcpClient; $t.Connect('127.0.0.1', %_PORT%); $t.Close(); exit 0 } catch { exit 1 }" >NUL 2>&1
    IF %ERRORLEVEL% EQU 0 EXIT /B 0
    SET /A _COUNT+=1
    IF !_COUNT! GEQ %_MAX% EXIT /B 1
    timeout /t 1 /nobreak >NUL
GOTO wait_loop


REM Fonctions couleur via PowerShell
:print_green
powershell -Command "Write-Host '%~1' -ForegroundColor Green"
EXIT /B 0

:print_red
powershell -Command "Write-Host '%~1' -ForegroundColor Red"
EXIT /B 0

:print_yellow
powershell -Command "Write-Host '%~1' -ForegroundColor Yellow"
EXIT /B 0

:print_cyan
powershell -Command "Write-Host '%~1' -ForegroundColor Cyan"
EXIT /B 0