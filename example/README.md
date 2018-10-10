# how to use

1. Move this directory to the project root directory.
1. service.config.nswag can be used directly by NSwagStudio or by using the npm project nswag
    1. NSwagStudio download address: [github](https://github.com/RSuter/NSwag/releases)
    2. npm Usage: npm install nswag -D
1. Set the API address: Modify the API swagger document address in service.config.nswag [swaggerGenerator:fromSwagger:url]
1. Set the generated destination address: in service.config.nswag [codeGenerators:swaggerToTypeScriptClient:output] Modify Generate target address (relative to this directory)
1. PowerShell environment, you can refer to the installation method of each system.
1. TS2ES: Can be set to compile to ES in ps1. Simply release the Ts2Es line in the ps1 file.
1. Run:
    1. ps1 single file version and multiple file version. The VUE project recommends using a multi-file version.
    2. The single file version uses the nswag of npm, so you must install nswag before using it.
    3. The multi-file version of the generator under MultipleTsFileGenerator, the default is Windows version, you can replace "win-x64" in refresh-multiple.ps1 with the corresponding system RID identifier, refer to the folder name under the MultipleTsFileGenerator
    4. Open powershell in the terminal, cd to the current directory, and execute the corresponding ps1 file.
