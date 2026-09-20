$MOD_NAME = "QudUX_v2"
$MODS_DIR = "$env:USERPROFILE/AppData/LocalLow/Freehold Games/CavesOfQud/Mods"
$DEST = "$MODS_DIR/$MOD_NAME"

$EXCLUDE = @('.git', '.gitignore', '*.zip', 'move-mod.ps1', 'Makefile', 'bin', 'obj')

Write-Host "Copying to $DEST..."
if (Test-Path $DEST) { Remove-Item $DEST -Recurse -Force }
New-Item -ItemType Directory -Path $DEST | Out-Null

Get-ChildItem -Exclude $EXCLUDE | Copy-Item -Destination $DEST -Recurse -Force

Write-Host "Done."
