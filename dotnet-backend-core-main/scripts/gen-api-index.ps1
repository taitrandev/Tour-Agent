param(
    [Parameter(Mandatory = $true)][string]$SwaggerPath,
    [Parameter(Mandatory = $true)][string]$OutPath
)
$ErrorActionPreference = 'Stop'

$methods = 'get','post','put','patch','delete','head','options'
$doc = Get-Content -Raw -Encoding UTF8 -Path $SwaggerPath | ConvertFrom-Json

$rows = foreach ($p in $doc.paths.PSObject.Properties) {
    foreach ($op in $p.Value.PSObject.Properties) {
        if ($methods -notcontains $op.Name) { continue }   # bo qua 'parameters', 'summary'...
        $tag = if ($op.Value.tags) { $op.Value.tags[0] } else { '' }
        [pscustomobject]@{
            Tag  = $tag
            Path = $p.Name
            Line = "{0}`t{1}`t[{2}]" -f $op.Name.ToUpper(), $p.Name, $tag
        }
    }
}

$lines = $rows | Sort-Object Tag, Path, Line | ForEach-Object { $_.Line }
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
[System.IO.File]::WriteAllLines($OutPath, [string[]]$lines, $utf8NoBom)
Write-Host "ApiIndex -> $OutPath ($($lines.Count) endpoints)"