param(
    [string] $Repository,
    [string] $Tag,
    [string] $Title,
    [string] $Target = 'main',
    [string] $RuntimeIdentifier = 'win-x64',
    [string] $AssetPath,
    [string] $NotesFile,
    [string] $Body,
    [switch] $KeepDraft,
    [switch] $MakeLatest,
    [switch] $DryRun
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$versionPropsPath = Join-Path $repoRoot 'Directory.Build.props'

function Get-ReleaseVersion {
    param(
        [Parameter(Mandatory = $true)]
        [string] $VersionFilePath
    )

    if (-not (Test-Path -LiteralPath $VersionFilePath)) {
        throw "未找到版本文件：$VersionFilePath"
    }

    [xml] $versionXml = Get-Content -LiteralPath $VersionFilePath
    $version = $versionXml.Project.PropertyGroup.Version | Select-Object -First 1
    if ([string]::IsNullOrWhiteSpace($version)) {
        throw "版本文件中缺少 <Version>：$VersionFilePath"
    }

    return $version.Trim()
}

function Get-AbsolutePath {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Path
    )

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return [System.IO.Path]::GetFullPath($Path)
    }

    return [System.IO.Path]::GetFullPath((Join-Path $repoRoot $Path))
}

function Resolve-GitHubRepository {
    param(
        [string] $ExplicitRepository
    )

    if (-not [string]::IsNullOrWhiteSpace($ExplicitRepository)) {
        return $ExplicitRepository.Trim()
    }

    $remoteUrl = (& git remote get-url origin).Trim()
    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($remoteUrl)) {
        throw '无法从 git remote origin 解析 GitHub 仓库，请使用 -Repository owner/repo 显式传入。'
    }

    $patterns = @(
        '^git@github\.com:(?<owner>[^/]+)/(?<repo>[^/]+?)(?:\.git)?$',
        '^https://github\.com/(?<owner>[^/]+)/(?<repo>[^/]+?)(?:\.git)?/?$',
        '^ssh://git@github\.com/(?<owner>[^/]+)/(?<repo>[^/]+?)(?:\.git)?/?$'
    )

    foreach ($pattern in $patterns) {
        if ($remoteUrl -match $pattern) {
            return "$($Matches.owner)/$($Matches.repo)"
        }
    }

    throw "无法识别的 GitHub remote URL：$remoteUrl"
}

function Get-GitHubToken {
    $token = (& gh auth token).Trim()
    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($token)) {
        throw '无法通过 gh auth token 读取 GitHub Token，请先执行 gh auth login。'
    }

    return $token
}

function Invoke-WithoutProxyEnv {
    param(
        [Parameter(Mandatory = $true)]
        [scriptblock] $ScriptBlock
    )

    $proxyNames = @(
        'HTTP_PROXY', 'HTTPS_PROXY', 'ALL_PROXY', 'NO_PROXY',
        'http_proxy', 'https_proxy', 'all_proxy', 'no_proxy'
    )
    $saved = @{}

    foreach ($name in $proxyNames) {
        $saved[$name] = [Environment]::GetEnvironmentVariable($name, 'Process')
        [Environment]::SetEnvironmentVariable($name, $null, 'Process')
    }

    try {
        & $ScriptBlock
    }
    finally {
        foreach ($name in $proxyNames) {
            [Environment]::SetEnvironmentVariable($name, $saved[$name], 'Process')
        }
    }
}

function New-GitHubHeaders {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Token
    )

    return @{
        Authorization = "Bearer $Token"
        Accept        = 'application/vnd.github+json'
        'User-Agent'  = 'ThsHevoSyncTool-ReleaseScript'
    }
}

function Get-HttpStatusCode {
    param(
        [Parameter(Mandatory = $true)]
        $Exception
    )

    if ($null -eq $Exception.Response) {
        return $null
    }

    return [int] $Exception.Response.StatusCode
}

function Invoke-GitHubJsonApi {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Method,
        [Parameter(Mandatory = $true)]
        [string] $Url,
        [Parameter(Mandatory = $true)]
        [hashtable] $Headers,
        $BodyObject,
        [int] $TimeoutSec = 300
    )

    if ($PSBoundParameters.ContainsKey('BodyObject')) {
        $jsonBody = $BodyObject | ConvertTo-Json -Depth 10
        return Invoke-RestMethod -Uri $Url -Method $Method -Headers $Headers -ContentType 'application/json; charset=utf-8' -Body $jsonBody -TimeoutSec $TimeoutSec
    }

    return Invoke-RestMethod -Uri $Url -Method $Method -Headers $Headers -TimeoutSec $TimeoutSec
}

function Get-ReleaseByTagOrNull {
    param(
        [Parameter(Mandatory = $true)]
        [string] $RepositoryName,
        [Parameter(Mandatory = $true)]
        [string] $ReleaseTag,
        [Parameter(Mandatory = $true)]
        [hashtable] $Headers
    )

    $url = "https://api.github.com/repos/$RepositoryName/releases/tags/$ReleaseTag"

    try {
        return Invoke-GitHubJsonApi -Method 'GET' -Url $url -Headers $Headers
    }
    catch {
        if ((Get-HttpStatusCode -Exception $_.Exception) -eq 404) {
            return $null
        }

        throw
    }
}

function Read-ReleaseBodyText {
    param(
        [string] $ExplicitBody,
        [string] $BodyFile
    )

    if (-not [string]::IsNullOrWhiteSpace($ExplicitBody)) {
        return $ExplicitBody
    }

    if (-not [string]::IsNullOrWhiteSpace($BodyFile)) {
        return Get-Content -LiteralPath (Get-AbsolutePath -Path $BodyFile) -Raw
    }

    return ''
}

function Remove-ReleaseAssetIfExists {
    param(
        [Parameter(Mandatory = $true)]
        $Release,
        [Parameter(Mandatory = $true)]
        [string] $AssetName,
        [Parameter(Mandatory = $true)]
        [hashtable] $Headers,
        [Parameter(Mandatory = $true)]
        [string] $RepositoryName
    )

    $assets = @($Release.assets)
    $matched = @($assets | Where-Object { $_.name -eq $AssetName })
    foreach ($asset in $matched) {
        $deleteUrl = "https://api.github.com/repos/$RepositoryName/releases/assets/$($asset.id)"
        Invoke-GitHubJsonApi -Method 'DELETE' -Url $deleteUrl -Headers $Headers | Out-Null
    }
}

function Upload-ReleaseAsset {
    param(
        [Parameter(Mandatory = $true)]
        $Release,
        [Parameter(Mandatory = $true)]
        [string] $AssetFilePath,
        [Parameter(Mandatory = $true)]
        [hashtable] $Headers
    )

    $assetName = [System.IO.Path]::GetFileName($AssetFilePath)
    $baseUploadUrl = ($Release.upload_url -replace '\{\?name,label\}$', '')
    $uploadUrl = "${baseUploadUrl}?name=$([System.Uri]::EscapeDataString($assetName))"

    return Invoke-RestMethod -Uri $uploadUrl -Method 'POST' -Headers $Headers -ContentType 'application/octet-stream' -InFile $AssetFilePath -TimeoutSec 1800
}

function Publish-ReleaseIfNeeded {
    param(
        [Parameter(Mandatory = $true)]
        $Release,
        [Parameter(Mandatory = $true)]
        [string] $RepositoryName,
        [Parameter(Mandatory = $true)]
        [hashtable] $Headers,
        [switch] $ShouldKeepDraft,
        [switch] $ShouldMakeLatest
    )

    $patch = @{}

    if (-not $ShouldKeepDraft.IsPresent -and $Release.draft) {
        $patch.draft = $false
    }

    if ($ShouldMakeLatest.IsPresent) {
        $patch.make_latest = 'true'
    }

    if ($patch.Count -eq 0) {
        return $Release
    }

    $url = "https://api.github.com/repos/$RepositoryName/releases/$($Release.id)"
    return Invoke-GitHubJsonApi -Method 'PATCH' -Url $url -Headers $Headers -BodyObject $patch
}

$version = Get-ReleaseVersion -VersionFilePath $versionPropsPath
if ([string]::IsNullOrWhiteSpace($Tag)) {
    $Tag = "v$version"
}
if ([string]::IsNullOrWhiteSpace($Title)) {
    $Title = $Tag
}

$repositoryName = Resolve-GitHubRepository -ExplicitRepository $Repository
$resolvedAssetPath = if ([string]::IsNullOrWhiteSpace($AssetPath)) {
    Get-AbsolutePath -Path "dist/ThsHevoSyncTool-v$version-$RuntimeIdentifier.zip"
}
else {
    Get-AbsolutePath -Path $AssetPath
}

if (-not (Test-Path -LiteralPath $resolvedAssetPath)) {
    throw "未找到待上传资产：$resolvedAssetPath"
}

$releaseBody = Read-ReleaseBodyText -ExplicitBody $Body -BodyFile $NotesFile
$assetName = [System.IO.Path]::GetFileName($resolvedAssetPath)
$token = Get-GitHubToken

Write-Host "Repository: $repositoryName"
Write-Host "Tag: $Tag"
Write-Host "Title: $Title"
Write-Host "Asset: $resolvedAssetPath"
Write-Host "KeepDraft: $($KeepDraft.IsPresent)"
Write-Host "MakeLatest: $($MakeLatest.IsPresent)"

if ($DryRun) {
    Write-Host 'DryRun: 未执行任何网络上传。'
    return
}

$elapsed = [System.Diagnostics.Stopwatch]::StartNew()
$result = Invoke-WithoutProxyEnv {
    $headers = New-GitHubHeaders -Token $token
    $release = Get-ReleaseByTagOrNull -RepositoryName $repositoryName -ReleaseTag $Tag -Headers $headers

    if ($null -eq $release) {
        $createBody = @{
            tag_name         = $Tag
            target_commitish = $Target
            name             = $Title
            draft            = $true
            prerelease       = $false
            body             = $releaseBody
        }
        $release = Invoke-GitHubJsonApi -Method 'POST' -Url "https://api.github.com/repos/$repositoryName/releases" -Headers $headers -BodyObject $createBody
    }

    Remove-ReleaseAssetIfExists -Release $release -AssetName $assetName -Headers $headers -RepositoryName $repositoryName
    $uploadedAsset = Upload-ReleaseAsset -Release $release -AssetFilePath $resolvedAssetPath -Headers $headers
    $release = Publish-ReleaseIfNeeded -Release $release -RepositoryName $repositoryName -Headers $headers -ShouldKeepDraft:$KeepDraft.IsPresent -ShouldMakeLatest:$MakeLatest.IsPresent

    return [pscustomobject]@{
        ReleaseUrl = $release.html_url
        AssetName = $uploadedAsset.name
        AssetUrl = $uploadedAsset.browser_download_url
        Draft = $release.draft
    }
}
$elapsed.Stop()

Write-Host ''
Write-Host ('Release URL: ' + $result.ReleaseUrl)
Write-Host ('Asset Name: ' + $result.AssetName)
Write-Host ('Asset URL: ' + $result.AssetUrl)
Write-Host ('Draft: ' + $result.Draft)
Write-Host ('Elapsed: ' + [math]::Round($elapsed.Elapsed.TotalSeconds, 2) + 's')
