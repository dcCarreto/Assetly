@echo off
setlocal

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0executar.ps1"
set "ASSETLY_EXIT_CODE=%ERRORLEVEL%"

if not "%ASSETLY_EXIT_CODE%"=="0" (
    echo.
    echo Assetly nao iniciou corretamente. Codigo de saida: %ASSETLY_EXIT_CODE%
    echo Pressione qualquer tecla para fechar esta janela.
    pause >nul
)

exit /b %ASSETLY_EXIT_CODE%
