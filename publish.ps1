
Remove-Item ./example/* -Include *.exe -Recurse -Force

dotnet publish -c Release -r win-x64 -o ./example /p:PublishSingleFile=true /p:PublishTrimmed=true /p:IncludeSymbolsInSingleFile=true  ./src/NSwagTsSplitter/NSwagTsSplitter.csproj