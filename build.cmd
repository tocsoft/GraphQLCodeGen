@echo Off

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

PUSHD Tocsoft.GraphQLCodeGen.Npm

REM lets copy the published files from the release folders into a binaries folder ready from publish

xcopy ..\Tocsoft.GraphQLCodeGen.Cli\bin\Release\net461\publish binaries\net461 /IYS
xcopy ..\Tocsoft.GraphQLCodeGen.Cli\bin\Release\netcoreapp1.0\publish binaries\netcoreapp1.0 /IYS

call npm version 0.0.1 
if not "%GitVersion_NuGetVersion%" == "" (
	call npm version "%GitVersion_NuGetVersion%" 
)ELSE ( 
	call npm version "1.0.0" 
)

call npm pack 

call npm version 0.0.1 
call npm version "1.0.0" 
xcopy tocsoft.graphql-codegen-*.tgz ..\artifacts /Y

REM cleanup and delete build artifacts again
del tocsoft.graphql-codegen-*.tgz 
rem rmdir binaries /S /Q
popd
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