# 如何使用

1. 将本目录移入项目根目录。
1. service.config.nswag 可以通过 NSwagStudio直接使用，亦可以使用 npm 项目 nswag 使用
    1. NSwagStudio下载地址：[github](https://github.com/RSuter/NSwag/releases)
    2. npm 使用方式 : npm install nswag -D
1. 设置 API 地址 ：在 service.config.nswag 中 [swaggerGenerator:fromSwagger:url] 修改 API swagger 文档地址
1. 设置 生成目标地址：在 service.config.nswag 中 [codeGenerators:swaggerToTypeScriptClient:output] 修改 生成目标地址（相对于该目录）
1. PowerShell环境，可以参考各系统安装方式。
1. TS2ES: 可以在ps1中设置是否编译成 ES 。只需将 ps1 文件中的 Ts2Es 行注释放开即可。
1. 运行：
    1. ps1 分单文件版和多文件版。VUE项目建议使用多文件版。
    2. 单文件版使用的是 npm 的 nswag，所以使用前，必须先安装 nswag
    3. 多文件版使用的 MultipleTsFileGenerator 下的生成器，默认是Windows版，可以将 refresh-multiple.ps1 中 "win-x64" 替换成对应系统RID标识即可，参考 MultipleTsFileGenerator 下层的文件夹名称
    4. 在终端中打开powershell ,cd 至当前目录， 执行对应 ps1 文件