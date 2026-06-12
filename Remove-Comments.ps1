$paths = @(
    "C:\Users\ricma\Documents\GitHub\The_Last_Sip_game\Assets\scripts",
    "C:\Users\ricma\Documents\GitHub\The_Last_Sip_game\Assets\UI\MainMenu\Scripts",
    "C:\Users\ricma\Documents\GitHub\The_Last_Sip_game\Assets\UI\MainMenu\Editor"
)

foreach ($path in $paths) {
    if (Test-Path $path) {
        $files = Get-ChildItem -Path $path -Filter *.cs -Recurse
        foreach ($file in $files) {
            $content = Get-Content $file.FullName -Raw
            
            # Remove multi-line comments /* ... */
            $content = [System.Text.RegularExpressions.Regex]::Replace($content, '(?s)/\*.*?\*/', '')
            
            # Remove single-line comments // ...
            # We match // and everything after it, but we need to ensure we don't break lines
            $content = [System.Text.RegularExpressions.Regex]::Replace($content, '(?m)[ \t]*//.*$', '')
            
            # Remove multiple blank lines resulting from comment removal
            $content = [System.Text.RegularExpressions.Regex]::Replace($content, '(?m)^\s*[\r\n]', '')
            
            Set-Content -Path $file.FullName -Value $content -NoNewline
            Write-Host "Cleaned: $($file.Name)"
        }
    }
}
Write-Host "Done!"
