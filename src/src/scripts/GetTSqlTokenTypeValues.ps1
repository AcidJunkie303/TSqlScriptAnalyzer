$assemblyRelativePath  = "..\..\src\DatabaseAnalyzer.App\bin\Debug\net9.0\Microsoft.SqlServer.TransactSql.ScriptDom.dll"
$assemblyPath = [System.IO.Path]::GetFullPath($assemblyRelativePath, $PSScriptRoot)
$assembly = [System.Reflection.Assembly]::LoadFile( $assemblyPath )
$type = $assembly.GetTypes() | Where-Object { $_.Name -eq "TSqlTokenType" -and $_.Namespace -eq "Microsoft.SqlServer.TransactSql.ScriptDom"} | Select-Object -First 1
$fields = $type.GetFields("Static,Public")

$fields | Where-Object { $_.Name -like "*bit*"} | Select-Object -ExpandProperty Name
