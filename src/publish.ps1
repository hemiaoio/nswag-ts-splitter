Remove-Item .\NSwagTsSplitter\Bin\* -Recurse -Force

dotnet publish -c Release -r win-x64 --force --self-contained

dotnet publish -c Release -r osx-x64 --force --self-contained

dotnet publish -c Release -r linux-x64 --force --self-contained


ii .\NSwagTsSplitter\bin\Release\netcoreapp2.1\