
Remove-Item ./example/* -Include *.exe -Recurse -Force
# /p:PublishSingleFile=true 单文件
# /p:IncludeNativeLibrariesForSelfExtract=true 包含Native 到单文件包 否则会有 coreclr.dll, clrjit.dll, clrcompression.dll, mscordaccore.dll 4个 dll   已在csproj中配置
# /p:PublishTrimmed=true 裁剪掉没有用到的程序集 
# dotnet publish -c Release -r win-x64 -o ./example /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true /p:PublishTrimmed=true ./src/NSwagTsSplitter/NSwagTsSplitter.csproj


dotnet publish -c Release -r win-x64 --self-contained -o ./example/nswag/win-x64 ./src/NSwagTsSplitter/NSwagTsSplitter.csproj

# dotnet publish -c Release -r linux-x64 --self-contained -o ./example/nswag/linux-x64 ./src/NSwagTsSplitter/NSwagTsSplitter.csproj

# dotnet publish -c Release -r osx-x64 --self-contained -o ./example/nswag/osx-x64 ./src/NSwagTsSplitter/NSwagTsSplitter.csproj
