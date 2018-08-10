@echo off
pushd "%~dp0"
call :main %*
popd
goto :EOF

:main
setlocal
if not exist dist md dist
if not %errorlevel%==0 exit /b %errorlevel%
set REPO_COMMIT=N/A
if not "%~2"=="" set REPO_COMMIT=%2
for /f "usebackq tokens=*" %%v in (`PowerShell -C "type src\Core\WebLinq.csproj | ? { $_ -match '(?<=<PackageVersion>)[0-9]+(\.[0-9]+){2}' } | %% { $Matches[0] }"`) do (
    set VERSION=%%v
)
if not "%~1"=="" set VERSION=%VERSION%-%1
   call build ^
&& call msbuild  /t:Pack src\Core\WebLinq.csproj /v:m   ^
                "/p:Configuration=Release"              ^
                "/p:PackageVersion=%VERSION%"           ^
                "/p:PackageReleaseNotes=Commit @ %REPO_COMMIT%"
goto :EOF
