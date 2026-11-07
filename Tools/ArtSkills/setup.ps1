# Art skill pipeline setup (Windows PowerShell 5.1+)
# Clones the 7 vendored art tools at pinned commits into Tools/ArtSkills/vendor
# and creates a Python venv at Tools/ArtSkills/.venv with their dependencies.
#
#   powershell -ExecutionPolicy Bypass -File Tools/ArtSkills/setup.ps1
#   powershell -ExecutionPolicy Bypass -File Tools/ArtSkills/setup.ps1 -WithPixelOE   # + PixelOE (torch, large)
#   powershell -ExecutionPolicy Bypass -File Tools/ArtSkills/setup.ps1 -Update         # move pins to latest main
#
# Catalog: docs/art-pipeline/README.md

param(
    [switch]$WithPixelOE,
    [switch]$Update
)

$ErrorActionPreference = 'Stop'
$env:PYTHONUTF8 = '1'   # this PC defaults to cp949; vendored scripts read UTF-8 files
$root   = $PSScriptRoot
$vendor = Join-Path $root 'vendor'
$venv   = Join-Path $root '.venv'
New-Item -ItemType Directory -Force $vendor | Out-Null
New-Item -ItemType Directory -Force (Join-Path $root '_work') | Out-Null

# Pinned 2026-11-06
$repos = [ordered]@{
    'agent-sprite-forge' = @{ url = 'https://github.com/0x0funky/agent-sprite-forge.git'; rev = '72fa5ad92d79b811acff464da32c8f3b1caa010f' }
    'Pixelbolt'          = @{ url = 'https://github.com/skulitom/Pixelbolt.git';          rev = '8814970d7774e94432762c683b756ad3db86e896' }
    'codixel'            = @{ url = 'https://github.com/mankidev/codixel.git';            rev = '604a9dcbcc4e386bc48188b5f7f49099f5d04db7' }
    'PixelOE'            = @{ url = 'https://github.com/KohakuBlueleaf/PixelOE.git';      rev = '7ce444b36d3876a151d845d4493240e904454d89' }
    'pixelfx'            = @{ url = 'https://github.com/patch-gg/pixelfx.git';            rev = 'a84159688af3ca37a4be05a0d422b94451b2e24e' }
    'sprite-gen'         = @{ url = 'https://github.com/aldegad/sprite-gen.git';          rev = 'c30322bc6a51a86c26d0d9aadcc2e7db7345027e' }
    '2dimg2motion'       = @{ url = 'https://github.com/WU-HAOTIAN34/2dimg2motion.git';   rev = 'd4d73e243b6ff64b7cbb3879e45e951f972a7e62' }
}

foreach ($name in $repos.Keys) {
    $dir = Join-Path $vendor $name
    $r = $repos[$name]
    if (-not (Test-Path (Join-Path $dir '.git'))) {
        Write-Host "clone  $name"
        git clone -q $r.url $dir
        if ($LASTEXITCODE -ne 0) { throw "clone failed: $name" }
    } else {
        git -C $dir fetch -q origin
    }
    if ($Update) {
        git -C $dir checkout -q origin/HEAD
        Write-Host ("update $name -> " + (git -C $dir rev-parse HEAD) + "  (update the pin in setup.ps1)")
    } else {
        git -C $dir checkout -q $r.rev 2>$null
        if ($LASTEXITCODE -ne 0) { Write-Warning "$name : pinned commit not found, staying on current HEAD" }
    }
}

# Python venv
# sprite-gen uses PEP 701 f-strings -> Python 3.12+ required.
# Use the py launcher, not bare `python`: other python.exe (e.g. a 3.11 agent venv) may come first on PATH.
$py = Join-Path $venv 'Scripts\python.exe'
if (Test-Path $py) {
    $ok = & $py -c "import sys; print(int(sys.version_info >= (3, 12)))"
    if ($ok -ne '1') {
        Write-Host ('venv is ' + (& $py --version) + ' (< 3.12) -> recreate')
        Remove-Item -Recurse -Force $venv
    }
}
if (-not (Test-Path $py)) {
    $base = $null
    # sprite-gen CI runs 3.14; ASF was measured on 3.13
    foreach ($v in @('3.14', '3.13', '3.12')) {
        try {
            $null = & py "-$v" -c "import sys" 2>&1
            if ($LASTEXITCODE -eq 0) { $base = $v; break }
        } catch { }
    }
    if (-not $base) { throw 'Python 3.12+ not found via the py launcher (py -3.13). Install Python 3.13.' }
    Write-Host "create venv (Python $base)"
    & py "-$base" -m venv $venv
}
& $py -m pip install -q --upgrade pip
& $py -m pip install -q -r (Join-Path $vendor 'agent-sprite-forge\requirements.txt')
& $py -m pip install -q -r (Join-Path $vendor 'agent-sprite-forge\requirements-codeart.txt')
& $py -m pip install -q -e (Join-Path $vendor 'sprite-gen')
if ($WithPixelOE) {
    # RTX 50xx needs a CUDA 12.8+ torch build; install it first so pixeloe does not pull a CPU wheel.
    & $py -m pip install -q torch torchvision --index-url https://download.pytorch.org/whl/cu128
    & $py -m pip install -q -e (Join-Path $vendor 'PixelOE')
}

# Tool check
Write-Host ''
Write-Host '--- check ---'
Write-Host ('node    ' + (node --version))
Write-Host ('ffmpeg  ' + ((ffmpeg -version | Select-Object -First 1) -replace 'Copyright.*',''))
& $py -c "import numpy, PIL, scipy; print('python  numpy/Pillow/scipy ok')"
& (Join-Path $venv 'Scripts\sprite-gen.exe') --help | Out-Null; if ($?) { Write-Host 'sprite-gen ok' }
if ($WithPixelOE) { & $py -c "import pixeloe, torch; print('pixeloe ok, cuda =', torch.cuda.is_available())" }
# Claude Code has no built-in image_gen / image_to_video tool, so declare none.
# python.path WARN (other python.exe on PATH) is informational: always run tools with $py.
& $py (Join-Path $vendor 'agent-sprite-forge\skills\generate2dmedia\scripts\forge_doctor.py') --host-tools none
