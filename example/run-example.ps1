$x = Split-Path -Parent $MyInvocation.MyCommand.Definition
if(!(Test-Path $x"\NSwagTsSplitter.exe")){
    Push-Location -Path ../
    powershell.exe -command ./publish.ps1
    Pop-Location
}

$content = Get-Content $x"\service.config.nswag" -ReadCount 0
$nswagContent = $content | ConvertFrom-Json
$swaggerPath = $nswagContent.documentGenerator.fromDocument.url
$outPutPath = $nswagContent.codeGenerators.openApiToTypeScriptClient.output
if ($outPutPath.IndexOf(".ts") -ge 0) {
    $outPutPath = $outPutPath.Remove($outPutPath.LastIndexOf("/"));
}

$nswagContent.codeGenerators.openApiToTypeScriptClient.output = $outPutPath;

# Formats JSON in a nicer format than the built-in ConvertTo-Json does.
function Format-Json([Parameter(Mandatory, ValueFromPipeline)][String] $json) {
    $indent = 0;
    ($json -Split '\n' |
        % {
            if ($_ -match '[\}\]]') {
                # This line contains  ] or }, decrement the indentation level
                $indent--
            }
            $line = (' ' * $indent * 2) + $_.TrimStart().Replace(':  ', ': ')
            if ($_ -match '[\{\[]') {
                # This line contains [ or {, increment the indentation level
                $indent++
            }
            $line
        }) -Join "`n"
}
$configContentNew = $nswagContent | ConvertTo-Json | Format-Json
$configContentNew = $configContentNew.Replace("""wrapResponseMethods"": """",", """wrapResponseMethods"": [],");
$configContentNew = $configContentNew.Replace("""protectedMethods"": """",", """protectedMethods"": [],");
$configContentNew = $configContentNew.Replace("""classTypes"": """",", """classTypes"": [],");
$configContentNew = $configContentNew.Replace("""extendedClasses"": """",", """extendedClasses"": [],");
$configContentNew = $configContentNew.Replace("""excludedTypeNames"": """",", """excludedTypeNames"": [],");
Set-Content $x"\service.config.nswag"  -Value $configContentNew -Encoding UTF8

Write-Host " 从 $swaggerPath 生成 TS 脚本 开始：" 
$start = Get-Date
Push-Location -Path $x
.\NSwagTsSplitter -c $x"\service.config.nswag"
$end = Get-Date
Write-Host " 从 $swaggerPath 生成 TS 脚本 结束。耗时 " ($end - $start).TotalMilliseconds

$tsPath = $x + "\" + $nswagContent.codeGenerators.swaggerToTypeScriptClient.output
$jsPath = $tsPath.Replace(".ts", ".js")

Function Ts2Es($tsPath) {
    Write-Host "TS 转 ES 开始：" 
    $start = Get-Date
    tsc $tsPath --target es2015 --allowJs true
    $end = Get-Date
    Write-Host "TS 转 ES 结束。耗时：" ($end - $start).TotalMilliseconds 
}
# Ts2Es $tsPath

# ReplaceScriptContent $jsPath

Pop-Location