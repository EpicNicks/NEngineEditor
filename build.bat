@echo off

dotnet build --configuration Release

if %ERRORLEVEL% NEQ 0 (
	echo Build Failed
	exit /b %ERRORLEVEL%
)

IF %ERRORLEVEL% NEQ 0 (
    echo Failed to navigate to the output directory.
    exit /b %ERRORLEVEL%
)

SET "OUTPUT_DIR=.\NEngineEditor\bin\Release\net8.0-windows"
IF NOT EXIST "%OUTPUT_DIR%" (
    SET "OUTPUT_DIR=.\NEngineEditor\bin\Release\net8.0"
)

start "" "%OUTPUT_DIR%\NEngineEditor.exe"
exit