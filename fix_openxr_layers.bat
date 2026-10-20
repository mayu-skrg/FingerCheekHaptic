@echo off
chcp 65001 > nul
echo ========================================================
echo Meta Quest Link の OpenXR 競合レイヤー (VIVE/Steam) を無効化します
echo ========================================================

net session >nul 2>&1
if %errorLevel% neq 0 (
    echo [エラー] 管理者権限が必要です。
    echo このファイルを「右クリック」して「管理者として実行」してください。
    pause
    exit /b
)

powershell -Command "$regPath = 'HKLM:\SOFTWARE\Khronos\OpenXR\1\ApiLayers\Implicit'; if (Test-Path $regPath) { (Get-ItemProperty -Path $regPath).PSObject.Properties | Where-Object { $_.Name -match '(?i)(vive|steam)' } | ForEach-Object { Write-Host '無効化中:' $_.Name; Set-ItemProperty -Path $regPath -Name $_.Name -Value 0 -ErrorAction SilentlyContinue }; Write-Host '全ての競合レイヤーを正常に無効化しました。' -ForegroundColor Green } else { Write-Host '該当のレジストリキーは見つかりませんでした。' }"

echo ========================================================
echo 処理が完了しました。何かキーを押して閉じてください。
echo ========================================================
pause
