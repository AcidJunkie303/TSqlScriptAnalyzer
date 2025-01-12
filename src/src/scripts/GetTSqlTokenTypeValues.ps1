$assemblyRelativePath  = "..\..\src\DatabaseAnalyzer.App\bin\Debug\net9.0\Microsoft.SqlServer.TransactSql.ScriptDom.dll"
$assemblyPath = [System.IO.Path]::GetFullPath($assemblyRelativePath, $PSScriptRoot)
$assembly = [System.Reflection.Assembly]::LoadFile( $assemblyPath )
$allTypes  = $assembly.GetTypes() 

$tSqlTokenType = $allTypes | Where-Object { $_.Name -eq "TSqlTokenType" -and $_.Namespace -eq "Microsoft.SqlServer.TransactSql.ScriptDom"} | Select-Object -First 1
$tSqlTokenTypeValues = $tSqlTokenType.GetFields("Static,Public")
$tSqlTokenTypeValues | Where-Object { $_.Name -like "*OrAlter*"} | Select-Object -ExpandProperty Name

$allTypes | Where-Object { $_.Name -like "*OrAlter*"} | Select-Object -ExpandProperty Name