@echo Off

dotnet restore 

ECHO Building nuget packages
 if not "%GitVersion_NuGetVersion%" == "" (
    dotnet build ./Tocsoft.GraphQLCodeGen.MsBuild/ -c Release /p:packageversion=%GitVersion_NuGetVersion%
)ELSE ( 
    dotnet build ./Tocsoft.GraphQLCodeGen.MsBuild/ -c Release
)
     if not "%errorlevel%"=="0" goto failure

dotnet test ./Tocsoft.GraphQLCodeGen.Tests/Tocsoft.GraphQLCodeGen.Tests.csproj -c Release


if not "%GitVersion_NuGetVersion%" == "" (
    dotnet pack ./Tocsoft.GraphQLCodeGen.MsBuild/ -c Release --output ../artifacts --no-build /p:packageversion=%GitVersion_NuGetVersion%
)ELSE ( 
    dotnet pack ./Tocsoft.GraphQLCodeGen.MsBuild/ -c Release --output ../artifacts --no-build 
)
if not "%errorlevel%"=="0" goto failure



:success
ECHO successfully built project
REM exit 0
goto end

:failure
ECHO failed to build.
REM exit -1
goto end

:end