@echo off
setlocal

cd "%~dp0"

if '%Configuration%' == '' if not '%1' == '' set Configuration=%1
if '%Configuration%' == '' set Configuration=Debug

dotnet tool update coverlet.console --tool-path packages
dotnet tool update dotnet-reportgenerator-globaltool --tool-path packages

if exist bld\coverage rd /s /q bld\coverage
md bld\coverage

packages\coverlet "JeremyAnsel.ColorQuant.Tests\bin\%Configuration%\netcoreapp3.0\JeremyAnsel.ColorQuant.Tests.dll" --target "dotnet" --targetargs "test -f netcoreapp3.0 --no-build" --output "bld\coverage\results-netcoreapp3.0.xml" --format opencover
if %ERRORLEVEL% neq 0 exit /b %ERRORLEVEL%

packages\coverlet "JeremyAnsel.ColorQuant.Tests\bin\%Configuration%\net452\JeremyAnsel.ColorQuant.Tests.dll" --target "dotnet" --targetargs "test -f net452 --no-build" --output "bld\coverage\results-net452.xml" --format opencover
if %ERRORLEVEL% neq 0 exit /b %ERRORLEVEL%

packages\reportgenerator -reports:"bld\coverage\results-netcoreapp3.0.xml;bld\coverage\results-net452.xml" -reporttypes:Html;Badges -targetdir:bld\coverage -verbosity:Info
if %ERRORLEVEL% neq 0 exit /b %ERRORLEVEL%
