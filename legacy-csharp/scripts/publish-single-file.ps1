param(
    [string] $Configuration = 'Release',
    [string] $RuntimeIdentifier = 'win-x64',
    [string] $OutputDir = 'dist/self-contained-single',
    [string] $ReleaseArtifactsDir = 'dist',
    [switch] $SkipSmokeTest,
    [switch] $SkipReleaseArtifacts
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$projectPath = Join-Path $repoRoot 'src/ThsHevoSyncTool.App/ThsHevoSyncTool.App.csproj'
$outputPath = [System.IO.Path]::GetFullPath((Join-Path $repoRoot $OutputDir))
$versionPropsPath = Join-Path $repoRoot 'Directory.Build.props'

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

function Assert-PathWithinRepo {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Path
    )

    if (-not $Path.StartsWith($repoRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "路径越界：$Path"
    }
}

function Get-ReleaseVersion {
    param(
        [Parameter(Mandatory = $true)]
        [string] $VersionFilePath
    )

    if (-not (Test-Path -LiteralPath $VersionFilePath)) {
        return $null
    }

    [xml] $versionXml = Get-Content -LiteralPath $VersionFilePath
    $version = $versionXml.Project.PropertyGroup.Version | Select-Object -First 1
    if ([string]::IsNullOrWhiteSpace($version)) {
        return $null
    }

    return $version.Trim()
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
    $previousExtractBaseDir = $env:DOTNET_BUNDLE_EXTRACT_BASE_DIR

    New-Item -ItemType Directory -Path $runDir | Out-Null

    try {
        Copy-Item -LiteralPath $ExePath -Destination $copiedExe
        $env:DOTNET_BUNDLE_EXTRACT_BASE_DIR = $extractRoot
        $process = Start-Process -FilePath $copiedExe -WorkingDirectory $runDir -PassThru

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
        $env:DOTNET_BUNDLE_EXTRACT_BASE_DIR = $previousExtractBaseDir

        if ($null -ne $process -and -not $process.HasExited) {
            Stop-Process -Id $process.Id -Force
            Start-Sleep -Milliseconds 300
        }

        if (Test-Path -LiteralPath $smokeRoot) {
            Remove-Item -LiteralPath $smokeRoot -Recurse -Force -ErrorAction SilentlyContinue
        }
    }
}

function New-ReleaseArtifacts {
    param(
        [Parameter(Mandatory = $true)]
        [string] $ExePath,
        [Parameter(Mandatory = $true)]
        [string] $RuntimeIdentifier,
        [Parameter(Mandatory = $true)]
        [string] $ArtifactsRoot
    )

    $fullRoot = [System.IO.Path]::GetFullPath((Join-Path $repoRoot $ArtifactsRoot))
    Assert-PathWithinRepo -Path $fullRoot

    if (-not (Test-Path -LiteralPath $fullRoot)) {
        New-Item -ItemType Directory -Path $fullRoot | Out-Null
    }

    $version = Get-ReleaseVersion -VersionFilePath $versionPropsPath
    if ([string]::IsNullOrWhiteSpace($version)) {
        $version = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($ExePath).ProductVersion
    }
    if ([string]::IsNullOrWhiteSpace($version)) {
        $version = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($ExePath).FileVersion
    }
    if ([string]::IsNullOrWhiteSpace($version)) {
        $version = '0.0.0'
    }

    $safeVersion = ($version -replace '[^0-9A-Za-z._-]', '_')
    $releaseName = "ThsHevoSyncTool-v$safeVersion-$RuntimeIdentifier"
    $releaseDir = Join-Path $fullRoot $releaseName
    $releaseZip = Join-Path $fullRoot ($releaseName + '.zip')

    Reset-OutputDirectory -DirectoryPath $releaseDir

    $retryCount = 20
    while ($retryCount -gt 0 -and -not (Test-Path -LiteralPath $ExePath)) {
        Start-Sleep -Milliseconds 200
        $retryCount -= 1
    }

    if (-not (Test-Path -LiteralPath $ExePath)) {
        throw "发布产物不存在，无法创建正式包：$ExePath"
    }

    $releaseExePath = Join-Path $releaseDir 'ThsHevoSyncTool.exe'
    Copy-Item -LiteralPath $ExePath -Destination $releaseExePath -Force

    if (Test-Path -LiteralPath $releaseZip) {
        Remove-Item -LiteralPath $releaseZip -Force
    }

    Start-Sleep -Milliseconds 300
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    [System.IO.Compression.ZipFile]::CreateFromDirectory($releaseDir, $releaseZip)

    return [pscustomobject]@{
        Version = $safeVersion
        ReleaseDir = $releaseDir
        ReleaseZip = $releaseZip
        ReleaseExePath = $releaseExePath
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

if (-not $SkipReleaseArtifacts) {
    $releaseArtifacts = New-ReleaseArtifacts -ExePath $exePath -RuntimeIdentifier $RuntimeIdentifier -ArtifactsRoot $ReleaseArtifactsDir
    Write-Host ('Release artifact directory: ' + $releaseArtifacts.ReleaseDir)
    Write-Host ('Release artifact zip: ' + $releaseArtifacts.ReleaseZip)
}
