
Remove-Item ./example/* -Include *.exe -Recurse -Force
# PublishSingleFile 单文件
# IncludeNativeLibrariesForSelfExtract 包含Native 到单文件包 否则会有 coreclr.dll, clrjit.dll, clrcompression.dll, mscordaccore.dll 4个 dll
# PublishTrimmed 裁剪掉没有用到的程序集 /p:PublishTrimmed=true
dotnet publish -c Release -r win-x64 -o ./example /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true ./src/NSwagTsSplitter/NSwagTsSplitter.csproj