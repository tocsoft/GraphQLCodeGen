@echo Off

dotnet restore 

ECHO Building nuget packages
 if not "%GitVersion_NuGetVersion%" == "" (
    dotnet build ./Tocsoft.GraphQLCodeGen.Cli/ -c Release /p:packageversion=%GitVersion_NuGetVersion%
    dotnet build ./Tocsoft.GraphQLCodeGen.MsBuild/ -c Release /p:packageversion=%GitVersion_NuGetVersion%
)ELSE ( 
    dotnet build ./Tocsoft.GraphQLCodeGen.Cli/ -c Release
    dotnet build ./Tocsoft.GraphQLCodeGen.MsBuild/ -c Release
)
     if not "%errorlevel%"=="0" goto failure

dotnet test ./Tocsoft.GraphQLCodeGen.Tests/Tocsoft.GraphQLCodeGen.Tests.csproj -c Release


if not "%GitVersion_NuGetVersion%" == "" (
	dotnet pack Tocsoft.GraphQLCodeGen.MsBuild\Tocsoft.GraphQLCodeGen.MsBuild.csproj /p:NuspecFile=Tocsoft.GraphQLCodeGen.MsBuild.nuspec --verbosity normal  /p:nuspecproperties=\"version=%GitVersion_NuGetVersion%;configuration=Release\" -c Release --output ../artifacts --no-build /p:packageversion=%GitVersion_NuGetVersion%
)ELSE ( 
	dotnet pack Tocsoft.GraphQLCodeGen.MsBuild\Tocsoft.GraphQLCodeGen.MsBuild.csproj /p:NuspecFile=Tocsoft.GraphQLCodeGen.MsBuild.nuspec --verbosity normal  /p:nuspecproperties=\"version=1.0.0;configuration=Release\" -c Release --output ../artifacts --no-build /p:packageversion=1.0.0
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