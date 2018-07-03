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
set VERSION_SUFFIX=
if not "%~1"=="" set VERSION_SUFFIX=-%1
call build /v:m ^
  && NuGet pack src\Core\WebLinq.csproj                      ^
                -Symbol -OutputDirectory dist                ^
                -Properties "VersionSuffix=%VERSION_SUFFIX%" ^
                -Properties "RepoCommit=%REPO_COMMIT%"
goto :EOF
