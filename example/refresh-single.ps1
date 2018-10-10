﻿$x = Split-Path -Parent $MyInvocation.MyCommand.Definition
$configContent = Get-Content $x"\service.config.nswag" -ReadCount 0

$nswagContent = $configContent | ConvertFrom-Json
$swaggerPath = $nswagContent.swaggerGenerator.fromSwagger.url

$outPutPath =  $nswagContent.codeGenerators.swaggerToTypeScriptClient.output

if($outPutPath.IndexOf(".ts") -le 0){
    $outPutPath = $outPutPath + "/" + "service-proxies.ts";
}

$outPutFloder = Split-Path -Path $x"\"$outPutPath
Remove-Item $outPutFloder"\*" -Force -Recurse

$nswagContent.codeGenerators.swaggerToTypeScriptClient.output = $outPutPath;

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
$configContentNew = $nswagContent|ConvertTo-Json|Format-Json
$configContentNew = $configContentNew.Replace("""wrapResponseMethods"": """",","""wrapResponseMethods"": [],");
$configContentNew = $configContentNew.Replace("""protectedMethods"": """",","""protectedMethods"": [],");
$configContentNew = $configContentNew.Replace("""classTypes"": """",","""classTypes"": [],");
$configContentNew = $configContentNew.Replace("""extendedClasses"": """",","""extendedClasses"": [],");
$configContentNew = $configContentNew.Replace("""excludedTypeNames"": """",","""excludedTypeNames"": [],");
Set-Content $x"\service.config.nswag"  -Value $configContentNew -Encoding UTF8

$tsPath =$x +"\"+ $nswagContent.codeGenerators.swaggerToTypeScriptClient.output
Write-Host " 从 $swaggerPath 生成 TS 脚本 开始：" 
$start = Get-Date
Push-Location -Path $x
..\node_modules\.bin\nswag run
$end = Get-Date
Write-Host " 从 $swaggerPath 生成 TS 脚本 结束。耗时 " ($end - $start).TotalMilliseconds

# $jsPath = $tsPath.Replace(".ts",".js")

# Function Ts2Es($tsPath){
#     Write-Host "TS 转 ES 开始：" 
#     $start = Get-Date
#     tsc $tsPath --target es2015 --allowJs true
#     $end = Get-Date
#     Write-Host "TS 转 ES 结束。耗时：" ($end - $start).TotalMilliseconds 
# }

Function Replacement($content)
{
    $targetContent = @("// 最后生成时间："+$start)
    foreach ($line in $content)
    {
        $liner = $line
        if($liner.indexOf("beforeSend: this.beforeSend") -ge 0){
            continue;
        }
        
        if($liner.indexOf("dataType") -ge 0){
            continue;
        }

        if($liner.IndexOf("import * as jQuery from 'jquery';") -ge 0){
            $liner = $liner.Replace("import * as jQuery from 'jquery';","")
        }

        if($liner.IndexOf("jQuery.ajax({") -ge 0){
            $liner = $liner.Replace("jQuery.ajax({","abp.ajax.request({")
        }
        if($liner.IndexOf("}).done((") -ge 0){
            $liner = $liner.Replace("}).done((_data, _textStatus, xhr) => {" , "}).then((xhr) => {" )
        }

        if($liner.IndexOf("}).fail((") -ge 0){
            $liner = $liner.Replace("}).fail((xhr) => {","},(xhr) => {`n`t`t`txhr=xhr.response;")
        }

        if($liner.IndexOf(" status, _responseText, _headers") -ge 0){
            $liner = $liner.Replace(" status, _responseText, _headers"," status, _responseText, _headers, xhr.data")
        }

        if($liner.IndexOf("_responseText === """" ? null :") -ge 0){
            $liner = $liner.Replace("_responseText === """" ? null :","(_responseText === """"|| _responseText === undefined) ? (xhr.data === null ? null : xhr.data.result ):")
        }
        if($liner.IndexOf(" type: """) -ge 0){
            $liner = $liner.Replace(" type: """," method: """)
        }
        $targetContent += $liner
    }

    return $targetContent
}

Function ReplaceScriptContent($scriptPath)
{
    Write-Host "Replacement $scriptPath 开始："
    $start = Get-Date
    $content = Get-Content $scriptPath -ReadCount 0
    Clear-Content $scriptPath
    $targetContent = Replacement $content
    Set-Content $scriptPath -Value $targetContent -Encoding UTF8
    $end = Get-Date
    Write-Host "Replacement $scriptPath 结束.耗时：" ($end - $start).TotalMilliseconds
}

ReplaceScriptContent $tsPath

# Ts2Es $tsPath


Pop-Location