$targetFilePath = "C:\projects\TSqlScriptAnalyzer\src\src\DatabaseAnalyzer.Contracts.DefaultImplementations\SqlParsing\TrackingSqlConcreteFragmentVisitor.generated.cs"

$assemblyRelativePath  = "..\..\src\DatabaseAnalyzer.App\bin\Debug\net9.0\Microsoft.SqlServer.TransactSql.ScriptDom.dll"
$assemblyPath = [System.IO.Path]::GetFullPath($assemblyRelativePath, $PSScriptRoot)
$assembly = [System.Reflection.Assembly]::LoadFile( $assemblyPath )
$allTypes  = $assembly.GetTypes() 

$visitorType = $allTypes `
    | Where-Object { $_.Name -eq "TSqlConcreteFragmentVisitor" -and $_.Namespace -eq "Microsoft.SqlServer.TransactSql.ScriptDom"} `
    | Select-Object -First 1 
$methods = $visitorType.GetMethods("Public,Instance") `
    | Where-Object {($_.IsAbstract -eq $false -and $_.IsVirtual -and $_.IsFinal -eq $false -and ($_.Name -eq "Visit" -or $_.Name -eq "VisitExplicit"))} `

$buffer = [System.Text.StringBuilder]::new()

$buffer.Append(@"
// ReSharper disable All

using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DatabaseAnalyzer.Contracts.DefaultImplementations.SqlParsing;

public class TrackingSqlConcreteFragmentVisitor : TSqlConcreteFragmentVisitor
{
    private readonly HashSet<TSqlFragment> _visitedNodes = [];

    protected bool TrackNodeAndCheck(TSqlFragment node) => _visitedNodes.Add(node);
"@) | Out-Null

foreach($method in $methods)
{
    $parameterType = $method.GetParameters() | Select-Object -First 1 -ExpandProperty ParameterType
    $parameterTypeName = $parameterType.Name
    $parameterName = $method.GetParameters() | Select-Object -First 1 -ExpandProperty Name

    if (!($parameterTypeName))
    {
        continue
    }
    if (!($parameterName))
    {
        continue
    }

    $methodCode = @"


    public override void $($method.Name)($parameterTypeName $parameterName)
    {
        if (!TrackNodeAndCheck($parameterName))
        {
            return;
        }

        base.Visit($parameterName);
    }
"@

    $buffer.Append($methodCode) | Out-Null
}

$buffer.AppendLine() | Out-Null
$buffer.Append("}")| Out-Null

$code = $buffer.ToString()

Set-Content -Path $targetFilePath -Value $code



#$tSqlTokenTypeValues | Where-Object { $_.Name -like "*OrAlter*"} | Select-Object -ExpandProperty Name

#$allTypes | Where-Object { $_.Name -like "*OrAlter*"} | Select-Object -ExpandProperty Name