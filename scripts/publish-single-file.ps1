param(
    [string] $Configuration = 'Release',
    [string] $RuntimeIdentifier = 'win-x64',
    [string] $OutputDir = 'dist/self-contained-single',
    [switch] $SkipSmokeTest
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$projectPath = Join-Path $repoRoot 'src/ThsHevoSyncTool.App/ThsHevoSyncTool.App.csproj'
$outputPath = [System.IO.Path]::GetFullPath((Join-Path $repoRoot $OutputDir))

function Assert-DotnetSdkAvailable {
    $sdks = & dotnet --list-sdks 2>$null
    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace(($sdks | Out-String))) {
        throw '未检测到 .NET SDK，无法执行单文件发布。请先安装 .NET 8 SDK。'
    }
}

function Format-HexExitCode {
    param(
        [Parameter(Mandatory = $true)]
        [int] $ExitCode
    )

    return ('0x{0:X8}' -f (([int64] $ExitCode) -band 0xffffffff))
}

function Reset-OutputDirectory {
    param(
        [Parameter(Mandatory = $true)]
        [string] $DirectoryPath
    )

    if (-not $DirectoryPath.StartsWith($repoRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "输出目录越界：$DirectoryPath"
    }

    if (Test-Path -LiteralPath $DirectoryPath) {
        Remove-Item -LiteralPath $DirectoryPath -Recurse -Force
    }

    New-Item -ItemType Directory -Path $DirectoryPath | Out-Null
}

function Assert-StandalonePublishOutput {
    param(
        [Parameter(Mandatory = $true)]
        [string] $DirectoryPath
    )

    $exe = Get-ChildItem -LiteralPath $DirectoryPath -Filter 'ThsHevoSyncTool.exe' -File | Select-Object -First 1
    if ($null -eq $exe) {
        throw "发布目录中未找到 ThsHevoSyncTool.exe：$DirectoryPath"
    }

    $unexpected = @(Get-ChildItem -LiteralPath $DirectoryPath -File | Where-Object {
        $_.Extension -notin @('.exe', '.pdb')
    })
    if ($unexpected.Count -gt 0) {
        $names = $unexpected.Name -join ', '
        throw "发布目录中存在额外产物，当前不是可直接上传的单文件结果：$names"
    }

    $sidecarDlls = @(Get-ChildItem -LiteralPath $DirectoryPath -Filter '*.dll' -File)
    if ($sidecarDlls.Count -gt 0) {
        $names = $sidecarDlls.Name -join ', '
        throw "发布目录中存在 sidecar DLL，单文件发布失败：$names"
    }

    return $exe.FullName
}

function Invoke-ColdStartSmokeTest {
    param(
        [Parameter(Mandatory = $true)]
        [string] $ExePath
    )

    $smokeRoot = Join-Path ([System.IO.Path]::GetTempPath()) ('ths-single-smoke-' + [guid]::NewGuid().ToString('N'))
    $runDir = Join-Path $smokeRoot 'run'
    $extractRoot = Join-Path $smokeRoot 'extract'
    $copiedExe = Join-Path $runDir ([System.IO.Path]::GetFileName($ExePath))
    $process = $null

    New-Item -ItemType Directory -Path $runDir | Out-Null

    try {
        Copy-Item -LiteralPath $ExePath -Destination $copiedExe
        $process = Start-Process -FilePath $copiedExe -WorkingDirectory $runDir -Environment @{
            DOTNET_BUNDLE_EXTRACT_BASE_DIR = $extractRoot
        } -PassThru

        Start-Sleep -Seconds 5

        if ($process.HasExited) {
            $hex = Format-HexExitCode -ExitCode $process.ExitCode
            throw "冷启动 smoke test 失败：进程提前退出，ExitCode=$($process.ExitCode) ($hex)"
        }

        $requiredNativeFiles = @(
            'PresentationNative_cor3.dll',
            'wpfgfx_cor3.dll'
        )
        $extractedFiles = @(Get-ChildItem -LiteralPath $extractRoot -Recurse -File -ErrorAction SilentlyContinue |
            Select-Object -ExpandProperty Name)
        $missing = @($requiredNativeFiles | Where-Object { $_ -notin $extractedFiles })
        if ($missing.Count -gt 0) {
            throw "冷启动 smoke test 失败：未在解包目录找到关键 WPF native 依赖：$($missing -join ', ')"
        }
    }
    finally {
        if ($null -ne $process -and -not $process.HasExited) {
            Stop-Process -Id $process.Id -Force
            Start-Sleep -Milliseconds 300
        }

        if (Test-Path -LiteralPath $smokeRoot) {
            Remove-Item -LiteralPath $smokeRoot -Recurse -Force -ErrorAction SilentlyContinue
        }
    }
}

Assert-DotnetSdkAvailable
Reset-OutputDirectory -DirectoryPath $outputPath

$publishArgs = @(
    'publish',
    $projectPath,
    '-c', $Configuration,
    '-r', $RuntimeIdentifier,
    '--self-contained', 'true',
    '-o', $outputPath,
    '/p:PublishSingleFile=true',
    '/p:IncludeNativeLibrariesForSelfExtract=true'
)

Write-Host ('Publishing single-file build to ' + $outputPath)
& dotnet @publishArgs
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish 失败，退出码：$LASTEXITCODE"
}

$exePath = Assert-StandalonePublishOutput -DirectoryPath $outputPath

if (-not $SkipSmokeTest) {
    Write-Host 'Running cold-start smoke test...'
    Invoke-ColdStartSmokeTest -ExePath $exePath
}

Write-Host ('Single-file publish succeeded: ' + $exePath)
Get-ChildItem -LiteralPath $outputPath -File | Select-Object Name, Length
