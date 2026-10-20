# VIVEおよびSteam関連のOpenXR APIレイヤーをすべて無効化するスクリプト
$regPath = "HKLM:\SOFTWARE\Khronos\OpenXR\1\ApiLayers\Implicit"

if (-not (Test-Path $regPath)) {
    Write-Host "該当のレジストリキーが見つかりませんでした。" -ForegroundColor Yellow
    Read-Host "Enterキーを押して終了"
    exit
}

$item = Get-Item -Path $regPath
$names = $item.GetValueNames()

$disabled = 0
foreach ($name in $names) {
    $currentVal = $item.GetValue($name)
    Write-Host "検出: $name = $currentVal"
    if ($name -imatch "(vive|steam)") {
        Set-ItemProperty -Path $regPath -Name $name -Value 0
        Write-Host "  → 無効化しました (0)" -ForegroundColor Green
        $disabled++
    }
}

Write-Host ""
if ($disabled -gt 0) {
    Write-Host "完了: $disabled 個の競合レイヤーを無効化しました。" -ForegroundColor Green
} else {
    Write-Host "VIVE/Steam 関連のレイヤーは見つかりませんでした。" -ForegroundColor Yellow
    Write-Host "現在の全レイヤーを再確認してください。" -ForegroundColor Yellow
}

Read-Host "Enterキーを押して終了"
