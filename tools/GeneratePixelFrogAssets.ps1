param(
    [Parameter(Mandatory = $true)]
    [string]$VideoPath
)

$ErrorActionPreference = "Stop"

$projectRoot = Split-Path -Parent $PSScriptRoot
$generator = Join-Path $PSScriptRoot "generate_video_pixel_frog.py"
$output = Join-Path $projectRoot "src\MilkyFrog.App\Assets\Character"

if (-not (Test-Path -LiteralPath $VideoPath -PathType Leaf)) {
    throw "Video not found: $VideoPath"
}

& python $generator --video $VideoPath --output $output

if ($LASTEXITCODE -ne 0) {
    throw "Pixel frame generation failed with exit code $LASTEXITCODE"
}
