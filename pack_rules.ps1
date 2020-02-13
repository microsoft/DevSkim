$out = @()

$jsonFiles = Get-ChildItem -Path $path -Filter *.json -Recurse
ForEach-Object -InputObject $jsonFiles
{
    $out = @(out; Get-Content -Path $_.FullName -Raw | ConvertFrom-Json)
}

$out |
    ConvertTo-Json -Depth 5 |
    Out-File -FilePath $outPath