@echo off
echo CLEANING
dotnet clean Microsoft.DevSkim --configuration Release --framework netstandard2.0 
dotnet clean Microsoft.DevSkim --configuration Release --framework net45
dotnet clean Microsoft.DevSkim.CLI --configuration Release --framework netcoreapp2.0 --runtime win-x64
dotnet clean Microsoft.DevSkim.CLI --configuration Release --framework netcoreapp2.0 --runtime linux-x64
dotnet clean Microsoft.DevSkim.CLI --configuration Release --framework netcoreapp2.0 --runtime osx-x64

echo BUILDING FIRST RUN
dotnet pack Microsoft.DevSkim --configuration Release
dotnet build Microsoft.DevSkim.CLI --configuration Release

@echo PACKING RULES
dotnet Microsoft.DevSkim.CLI\bin\Release\netcoreapp2.0\devskim.dll pack ..\..\rules Microsoft.DevSkim.CLI\Resources\devskim-rules.json --indent

@echo BUILDING SECOND RUN
dotnet build Microsoft.DevSkim.CLI --configuration Release

echo PUBLISHING
dotnet publish Microsoft.DevSkim.CLI --configuration Release --framework netcoreapp2.0 --runtime win-x64
dotnet publish Microsoft.DevSkim.CLI --configuration Release --framework netcoreapp2.0 --runtime linux-x64
dotnet publish Microsoft.DevSkim.CLI --configuration Release --framework netcoreapp2.0 --runtime osx-x64

rem d:\nuget pack Microsoft.DevSkim.CLI\Microsoft.DevSkim.CLI.nuspec -OutputDirectory Microsoft.DevSkim.CLI\bin\Release\netcoreapp2.0

rem echo CREATING TEMP DIRECTORY FOR .DEB PACKAGE
rem mkdir temp\devskim-ver_amd64
rem xcopy Microsoft.DevSkim.CLI\Packaging\LinuxDeb\*.* temp\devskim-ver_amd64 /E
rem xcopy Microsoft.DevSkim.CLI\bin\Release\netcoreapp2.0\linux-x64\publish\*.* temp\devskim-ver_amd64\usr\share\devskim\lib\ /E
rem cd temp
rem bash -c "chmod 775 devskim-ver_amd64/DEBIAN"
rem bash -c "chmod +x devskim-ver_amd64/usr/share/devskim/bin/devskim"
rem bash -c "dpkg-deb --build devskim-ver_amd64"
rem cd ..
