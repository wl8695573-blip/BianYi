[CmdletBinding()]
param([switch]$Publish, [switch]$WithdrawLegacyReleases)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$repo = 'wl8695573-blip/GuGuGaGaTranslator'
[xml]$props = Get-Content -LiteralPath (Join-Path $root 'Directory.Build.props') -Encoding UTF8
$version = [string]$props.Project.PropertyGroup.Version
if ($version -notmatch '^\d+\.\d+\.\d+$') { throw 'Invalid release version' }
$tag = "v$version"
$names = @("BianYi-Setup-$version.exe", "BianYi-win-x64-$version.zip", "LCTA-Setup-$version.exe", "SHA256SUMS-$version.txt")
$dist = Join-Path $root 'dist'
Push-Location $root
try {
    if (git status --porcelain) { throw 'Commit changes before publishing' }
    $commit = (git rev-parse HEAD).Trim()
    if (((git ls-remote origin refs/heads/main) -split '\s+')[0] -ne $commit) { throw 'Remote main differs from this checkout' }
} finally { Pop-Location }
foreach ($name in $names) {
    if (-not (Test-Path -LiteralPath (Join-Path $dist $name))) { throw "Missing artifact: $name" }
}
$checksumRows = @(Get-Content -LiteralPath (Join-Path $dist "SHA256SUMS-$version.txt"))
if ($checksumRows.Count -ne 3) { throw 'Expected three checksummed release payloads' }
foreach ($line in $checksumRows) {
    if ($line -notmatch '^([0-9a-f]{64})  ([^\\/]+)$') { throw 'Invalid checksum line' }
    $hash = $Matches[1]; $name = $Matches[2]
    if ($name -notin $names[0..2] -or (Get-FileHash -LiteralPath (Join-Path $dist $name) -Algorithm SHA256).Hash.ToLowerInvariant() -ne $hash) {
        throw "Checksum mismatch: $name"
    }
}
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [IO.Compression.ZipFile]::OpenRead((Join-Path $dist "BianYi-win-x64-$version.zip"))
try {
    $entry = $zip.GetEntry('BianYi/build-manifest.json')
    if (-not $entry) { throw 'Package has no build manifest' }
    $reader = [IO.StreamReader]::new($entry.Open())
    try { $build = $reader.ReadToEnd() | ConvertFrom-Json } finally { $reader.Dispose() }
    if ($build.sourceCommit -ne $commit -or $build.sourceDirty -or $build.version -ne $version) { throw 'Package was not built from the clean release commit' }
} finally { $zip.Dispose() }
$notes = [IO.File]::ReadAllText((Join-Path $PSScriptRoot 'release-notes-bianyi.md'), [Text.Encoding]::UTF8)
if ($Publish) {
    $acceptance = Join-Path $root ".artifacts/package-bianyi-$version.txt"
    if (-not (Test-Path -LiteralPath $acceptance) -or -not (Select-String -LiteralPath $acceptance -SimpleMatch 'Acceptance artifacts:')) {
        throw 'Run actual package acceptance before publication'
    }
    $notes += "`n`n## 本次发布验收`n`n244 项回归、223 项完整交互、27/27 固定 OCR 样本和 36/36 四语样本通过。实际日语截图的称号、姓名与正文分区通过；六种方向完成真实在线翻译并核对。安装、LCTA 1.6.5 升级、再次升级、卸载、便携包与 SHA256 验收通过。`n"
}
$env:GIT_TERMINAL_PROMPT = '0'
$credentialLines = "protocol=https`nhost=github.com`n`n" | git -C $root credential fill 2>$null
$tokenLine = $credentialLines | Where-Object { $_.StartsWith('password=') } | Select-Object -First 1
if (-not $tokenLine) { throw 'No stored GitHub credential' }
$headers = @{ Authorization = 'Bearer ' + $tokenLine.Substring(9).Trim(); Accept = 'application/vnd.github+json'; 'User-Agent' = 'BianYi-release'; 'X-GitHub-Api-Version' = '2022-11-28' }
function Api([string]$Method, [string]$Url, $Body = $null) {
    $arguments = @{ Method = $Method; Uri = $Url; Headers = $headers }
    if ($null -ne $Body) {
        $arguments.ContentType = 'application/json; charset=utf-8'
        $arguments.Body = [Text.Encoding]::UTF8.GetBytes(($Body | ConvertTo-Json -Depth 10 -Compress))
    }
    Invoke-RestMethod @arguments
}
$api = "https://api.github.com/repos/$repo"
$releases = @(Api GET "$api/releases?per_page=100")
$release = $releases | Where-Object tag_name -eq $tag | Select-Object -First 1
if ($release -and -not $release.draft) { throw 'This release is already public; refusing to replace it' }
if (-not $release) {
    $release = Api POST "$api/releases" @{ tag_name = $tag; target_commitish = $commit; name = "边译 $version"; body = $notes; draft = $true; prerelease = $false }
}
if ($release.target_commitish -ne $commit) { throw 'Draft belongs to another source commit' }
foreach ($name in $names) {
    $file = Get-Item -LiteralPath (Join-Path $dist $name)
    $digest = 'sha256:' + (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
    $existing = @($release.assets) | Where-Object name -eq $name
    if ($existing -and $existing.state -eq 'uploaded' -and $existing.size -eq $file.Length -and $existing.digest -eq $digest) { continue }
    if ($existing) { throw "Draft asset differs: $name. Inspect it before replacing." }
    Write-Host "Uploading $name"
    Invoke-RestMethod -Method POST -Uri ("https://uploads.github.com/repos/$repo/releases/$($release.id)/assets?name=" + [uri]::EscapeDataString($name)) -Headers $headers -InFile $file.FullName -ContentType 'application/octet-stream' | Out-Null
}
$release = Api GET "$api/releases/$($release.id)"
foreach ($name in $names) {
    $asset = @($release.assets) | Where-Object name -eq $name
    $file = Get-Item -LiteralPath (Join-Path $dist $name)
    if (-not $asset -or $asset.state -ne 'uploaded' -or $asset.size -ne $file.Length -or $asset.digest -ne ('sha256:' + (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant())) {
        throw "Upload verification failed: $name"
    }
}
if ($Publish) {
    $runs = Api GET "$api/actions/runs?head_sha=$commit&per_page=20"
    $run = $runs.workflow_runs | Where-Object { $_.name -eq 'Build and regression checks' -and $_.head_sha -eq $commit } | Select-Object -First 1
    if (-not $run -or $run.status -ne 'completed' -or $run.conclusion -ne 'success') { throw 'Release CI has not passed; draft remains private' }
    $notes += "`nGitHub CI：$($run.html_url)`n源码提交：$commit`n"
    $release = Api PATCH "$api/releases/$($release.id)" @{ name = "边译 $version"; body = $notes; draft = $false; prerelease = $false; make_latest = 'true' }
    $latest = Api GET "$api/releases/latest"
    if ($release.draft -or $latest.tag_name -ne $tag) { throw 'Publication was not confirmed' }
    Api PATCH $api @{ description = '边译 · Windows 屏幕与游戏字幕翻译助手，中日英韩互译，独立术语库与悬浮翻译。' } | Out-Null
    if ($WithdrawLegacyReleases) {
        $older = @($releases | Where-Object { -not $_.draft -and $_.tag_name -match '^v?\d+\.\d+\.\d+$' -and [version]($_.tag_name.TrimStart('v')) -lt [version]$version })
        $older | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath (Join-Path $root '.artifacts/withdrawn-legacy-releases.json') -Encoding UTF8
        foreach ($old in $older) {
            Api PATCH "$api/releases/$($old.id)" @{ draft = $true } | Out-Null
            Write-Host "Withdrew legacy download $($old.tag_name); preserved as private draft"
        }
    }
}
$receipt = [ordered]@{ version = $tag; draft = $release.draft; url = $release.html_url; commit = $commit; assets = @($release.assets | Select-Object name,size,digest,browser_download_url) }
$receipt | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $root ".artifacts/release-bianyi-$version.json") -Encoding UTF8
Write-Host "Verified $tag; draft=$($release.draft); $($release.html_url)"
