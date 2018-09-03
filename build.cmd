@echo off
setlocal
for %%i in (NuGet.exe) do set nuget=%%~dpnx$PATH:i
if "%nuget%"=="" (
    echo WARNING! NuGet executable not found in PATH so build may fail!
    echo For more on NuGet, see https://github.com/nuget/home
)
pushd "%~dp0"
nuget restore ^
 && call :build Debug   %* ^
 && call :build Release %*
popd
goto :EOF

:build
setlocal
call msbuild /p:Configuration=%1 /v:m %2 %3 %4 %5 %6 %7 %8 %9
goto :EOF
