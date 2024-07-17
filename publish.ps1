
Remove-Item ./example/* -Include *.exe -Recurse -Force
# PublishSingleFile SingleFile
# IncludeNativeLibrariesForSelfExtract 包含Native 到单文件包 否则会有 coreclr.dll, clrjit.dll, clrcompression.dll, mscordaccore.dll 4个 dll
# PublishTrimmed 裁剪掉没有用到的程序集 /p:PublishTrimmed=true
# dotnet publish -c Release -r win-x64 -o ./example /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true ./src/NSwagTsSplitter/NSwagTsSplitter.csproj


dotnet publish -c Release -r win-x64 -o ./example/nswag/win-x64 ./src/NSwagTsSplitter/NSwagTsSplitter.csproj

dotnet publish -c Release -r linux-x64 -o ./example/nswag/linux-x64 ./src/NSwagTsSplitter/NSwagTsSplitter.csproj

dotnet publish -c Release -r osx-x64 -o ./example/nswag/osx-x64 ./src/NSwagTsSplitter/NSwagTsSplitter.csproj