using System.Diagnostics.CodeAnalysis;
using DatabaseAnalyzer.Testing;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.Naming;
using Xunit.Abstractions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Tests.Analyzers.Naming;

#pragma warning disable S125

[SuppressMessage("Roslynator", "RCS1262:Unnecessary raw string literal", Justification = "For better test code alignment")]
[SuppressMessage("Major Code Smell", "S4144:Methods should not have identical implementations")]
public sealed class NameQuotingAnalyzerTests(ITestOutputHelper testOutputHelper)
    : ScriptAnalyzerTestsBase<NameQuotingAnalyzer>(testOutputHelper)
{
    [Theory]
    [InlineData(NameQuotingPolicy.Undefined, """ /*                 0000 */ CREATE TABLE  dbo.T1                                                            (Id int) """)]
    [InlineData(NameQuotingPolicy.Undefined, """ /*                 0001 */ CREATE TABLE  [dbo].T1                                                          (Id int) """)]
    [InlineData(NameQuotingPolicy.Undefined, """ /*                 0002 */ CREATE TABLE  dbo.[T1]                                                          (Id int) """)]
    [InlineData(NameQuotingPolicy.Undefined, """ /*                 0003 */ CREATE TABLE  "dbo".T1                                                          (Id int) """)]
    [InlineData(NameQuotingPolicy.Undefined, """ /*                 0004 */ CREATE TABLE  dbo."T1"                                                          (Id int) """)]
    //
    [InlineData(NameQuotingPolicy.Required, """  /*                 0010 */ CREATE TABLE  █AJ5038░script_0.sql░MyDb.dbo.T1░table░T1░[T1]███T1█              (Id int) """)]
    [InlineData(NameQuotingPolicy.Required, """  /*                 0011 */ CREATE TABLE  [dbo].█AJ5038░script_0.sql░MyDb.dbo.T1░table░T1░[T1]███T1█        (Id int) """)]
    [InlineData(NameQuotingPolicy.Required, """  /*                 0012 */ CREATE TABLE  "dbo".█AJ5038░script_0.sql░MyDb.dbo.T1░table░T1░[T1]███T1█        (Id int) """)]
    [InlineData(NameQuotingPolicy.Required, """  /*                 0013 */ CREATE TABLE  [dbo].[T1]                                                        (Id int) """)]
    [InlineData(NameQuotingPolicy.Required, """  /*                 0014 */ CREATE TABLE  "dbo"."T1"                                                        (Id int) """)]
    //
    [InlineData(NameQuotingPolicy.NotAllowed, """  /*               0020 */ CREATE TABLE  T1                                                                (Id int) """)]
    [InlineData(NameQuotingPolicy.NotAllowed, """  /*               0021 */ CREATE TABLE  dbo.T1                                                            (Id int) """)]
    [InlineData(NameQuotingPolicy.NotAllowed, """  /*               0022 */ CREATE TABLE  █AJ5038░script_0.sql░MyDb.dbo.T1░table░[dbo]░dbo███[dbo]█.T1      (Id int) """)]
    [InlineData(NameQuotingPolicy.NotAllowed, """  /*               0023 */ CREATE TABLE  █AJ5038░script_0.sql░MyDb.dbo.T1░table░"dbo"░dbo███"dbo"█.T1      (Id int) """)]
    [InlineData(NameQuotingPolicy.NotAllowed, """  /*               0024 */ CREATE TABLE  dbo.█AJ5038░script_0.sql░MyDb.dbo.T1░table░[T1]░T1███[T1]█        (Id int) """)]
    [InlineData(NameQuotingPolicy.NotAllowed, """  /*               0025 */ CREATE TABLE  dbo.█AJ5038░script_0.sql░MyDb.dbo.T1░table░"T1"░T1███"T1"█        (Id int) """)]
    //
    [InlineData(NameQuotingPolicy.DoubleQuotesRequired, """  /*     0030 */ CREATE TABLE  █AJ5038░script_0.sql░MyDb.dbo.T1░table░T1░"T1"███T1█              (Id int) """)]
    [InlineData(NameQuotingPolicy.DoubleQuotesRequired, """  /*     0031 */ CREATE TABLE  "dbo".█AJ5038░script_0.sql░MyDb.dbo.T1░table░[T1]░"T1"███[T1]█    (Id int) """)]
    [InlineData(NameQuotingPolicy.DoubleQuotesRequired, """  /*     0032 */ CREATE TABLE  "dbo".█AJ5038░script_0.sql░MyDb.dbo.T1░table░T1░"T1"███T1█        (Id int) """)]
    [InlineData(NameQuotingPolicy.DoubleQuotesRequired, """  /*     0033 */ CREATE TABLE  "dbo"."T1"                                                        (Id int) """)]
    [InlineData(NameQuotingPolicy.DoubleQuotesRequired, """  /*     0034 */ CREATE TABLE  █AJ5038░script_0.sql░MyDb.dbo.T1░table░dbo░"dbo"███dbo█."T1"      (Id int) """)]
    [InlineData(NameQuotingPolicy.DoubleQuotesRequired, """  /*     0035 */ CREATE TABLE  █AJ5038░script_0.sql░MyDb.dbo.T1░table░[dbo]░"dbo"███[dbo]█."T1"  (Id int) """)]
    //
    [InlineData(NameQuotingPolicy.SquareBracketsRequired, """  /*   0040 */ CREATE TABLE  █AJ5038░script_0.sql░MyDb.dbo.T1░table░T1░[T1]███T1█              (Id int) """)]
    [InlineData(NameQuotingPolicy.SquareBracketsRequired, """  /*   0041 */ CREATE TABLE  [dbo].█AJ5038░script_0.sql░MyDb.dbo.T1░table░"T1"░[T1]███"T1"█    (Id int) """)]
    [InlineData(NameQuotingPolicy.SquareBracketsRequired, """  /*   0042 */ CREATE TABLE  [dbo].█AJ5038░script_0.sql░MyDb.dbo.T1░table░T1░[T1]███T1█        (Id int) """)]
    [InlineData(NameQuotingPolicy.SquareBracketsRequired, """  /*   0043 */ CREATE TABLE  [dbo].[T1]                                                        (Id int) """)]
    [InlineData(NameQuotingPolicy.SquareBracketsRequired, """  /*   0044 */ CREATE TABLE  █AJ5038░script_0.sql░MyDb.dbo.T1░table░dbo░[dbo]███dbo█.[T1]      (Id int) """)]
    [InlineData(NameQuotingPolicy.SquareBracketsRequired, """  /*   0045 */ CREATE TABLE  █AJ5038░script_0.sql░MyDb.dbo.T1░table░"dbo"░[dbo]███"dbo"█.[T1]  (Id int) """)]
    public void WithTableCreation_Theory(NameQuotingPolicy nameQuotingPolicy, string code)
    {
        Aj5038Settings settings = new
        (
            NameQuotingPolicyDuringObjectCreation: nameQuotingPolicy,
            NameQuotingPolicyForColumnReferences: NameQuotingPolicy.Undefined,
            NameQuotingPolicyForTableAliases: NameQuotingPolicy.Undefined,
            NameQuotingPolicyForSchemaNameReferences: NameQuotingPolicy.Undefined
        );

        const string codePrefix = """
                                  USE MyDb
                                  GO


                                  """;

        Verify(settings, codePrefix + code);
    }

    [Theory]
    [InlineData(NameQuotingPolicy.Undefined, """ /*                 0000 */ CREATE FUNCTION  dbo.T1                                                             () RETURNS INT AS BEGIN RETURN 1 END """)]
    [InlineData(NameQuotingPolicy.Undefined, """ /*                 0001 */ CREATE FUNCTION  [dbo].T1                                                           () RETURNS INT AS BEGIN RETURN 1 END """)]
    [InlineData(NameQuotingPolicy.Undefined, """ /*                 0002 */ CREATE FUNCTION  dbo.[T1]                                                           () RETURNS INT AS BEGIN RETURN 1 END """)]
    [InlineData(NameQuotingPolicy.Undefined, """ /*                 0003 */ CREATE FUNCTION  "dbo".T1                                                           () RETURNS INT AS BEGIN RETURN 1 END """)]
    [InlineData(NameQuotingPolicy.Undefined, """ /*                 0004 */ CREATE FUNCTION  dbo."T1"                                                           () RETURNS INT AS BEGIN RETURN 1 END """)]
    //
    [InlineData(NameQuotingPolicy.Required, """  /*                 0010 */ CREATE FUNCTION  █AJ5038░script_0.sql░MyDb.dbo.T1░function░T1░[T1]███T1█               () RETURNS INT AS BEGIN RETURN 1 END """)]
    [InlineData(NameQuotingPolicy.Required, """  /*                 0011 */ CREATE FUNCTION  [dbo].█AJ5038░script_0.sql░MyDb.dbo.T1░function░T1░[T1]███T1█         () RETURNS INT AS BEGIN RETURN 1 END """)]
    [InlineData(NameQuotingPolicy.Required, """  /*                 0012 */ CREATE FUNCTION  "dbo".█AJ5038░script_0.sql░MyDb.dbo.T1░function░T1░[T1]███T1█         () RETURNS INT AS BEGIN RETURN 1 END """)]
    [InlineData(NameQuotingPolicy.Required, """  /*                 0013 */ CREATE FUNCTION  [dbo].[T1]                                                         () RETURNS INT AS BEGIN RETURN 1 END """)]
    [InlineData(NameQuotingPolicy.Required, """  /*                 0014 */ CREATE FUNCTION  "dbo"."T1"                                                         () RETURNS INT AS BEGIN RETURN 1 END """)]
    //
    [InlineData(NameQuotingPolicy.NotAllowed, """  /*               0020 */ CREATE FUNCTION  T1                                                                 () RETURNS INT AS BEGIN RETURN 1 END """)]
    [InlineData(NameQuotingPolicy.NotAllowed, """  /*               0021 */ CREATE FUNCTION  dbo.T1                                                             () RETURNS INT AS BEGIN RETURN 1 END """)]
    [InlineData(NameQuotingPolicy.NotAllowed, """  /*               0022 */ CREATE FUNCTION  █AJ5038░script_0.sql░MyDb.dbo.T1░function░[dbo]░dbo███[dbo]█.T1       () RETURNS INT AS BEGIN RETURN 1 END """)]
    [InlineData(NameQuotingPolicy.NotAllowed, """  /*               0023 */ CREATE FUNCTION  █AJ5038░script_0.sql░MyDb.dbo.T1░function░"dbo"░dbo███"dbo"█.T1       () RETURNS INT AS BEGIN RETURN 1 END """)]
    [InlineData(NameQuotingPolicy.NotAllowed, """  /*               0024 */ CREATE FUNCTION  dbo.█AJ5038░script_0.sql░MyDb.dbo.T1░function░[T1]░T1███[T1]█         () RETURNS INT AS BEGIN RETURN 1 END """)]
    [InlineData(NameQuotingPolicy.NotAllowed, """  /*               0025 */ CREATE FUNCTION  dbo.█AJ5038░script_0.sql░MyDb.dbo.T1░function░"T1"░T1███"T1"█         () RETURNS INT AS BEGIN RETURN 1 END """)]
    //
    [InlineData(NameQuotingPolicy.DoubleQuotesRequired, """  /*     0030 */ CREATE FUNCTION  █AJ5038░script_0.sql░MyDb.dbo.T1░function░T1░"T1"███T1█               () RETURNS INT AS BEGIN RETURN 1 END """)]
    [InlineData(NameQuotingPolicy.DoubleQuotesRequired, """  /*     0031 */ CREATE FUNCTION  "dbo".█AJ5038░script_0.sql░MyDb.dbo.T1░function░[T1]░"T1"███[T1]█     () RETURNS INT AS BEGIN RETURN 1 END """)]
    [InlineData(NameQuotingPolicy.DoubleQuotesRequired, """  /*     0032 */ CREATE FUNCTION  "dbo".█AJ5038░script_0.sql░MyDb.dbo.T1░function░T1░"T1"███T1█         () RETURNS INT AS BEGIN RETURN 1 END """)]
    [InlineData(NameQuotingPolicy.DoubleQuotesRequired, """  /*     0033 */ CREATE FUNCTION  "dbo"."T1"                                                         () RETURNS INT AS BEGIN RETURN 1 END """)]
    [InlineData(NameQuotingPolicy.DoubleQuotesRequired, """  /*     0034 */ CREATE FUNCTION  █AJ5038░script_0.sql░MyDb.dbo.T1░function░dbo░"dbo"███dbo█."T1"       () RETURNS INT AS BEGIN RETURN 1 END """)]
    [InlineData(NameQuotingPolicy.DoubleQuotesRequired, """  /*     0035 */ CREATE FUNCTION  █AJ5038░script_0.sql░MyDb.dbo.T1░function░[dbo]░"dbo"███[dbo]█."T1"   () RETURNS INT AS BEGIN RETURN 1 END """)]
    //
    [InlineData(NameQuotingPolicy.SquareBracketsRequired, """  /*   0040 */ CREATE FUNCTION  █AJ5038░script_0.sql░MyDb.dbo.T1░function░T1░[T1]███T1█               () RETURNS INT AS BEGIN RETURN 1 END """)]
    [InlineData(NameQuotingPolicy.SquareBracketsRequired, """  /*   0041 */ CREATE FUNCTION  [dbo].█AJ5038░script_0.sql░MyDb.dbo.T1░function░"T1"░[T1]███"T1"█     () RETURNS INT AS BEGIN RETURN 1 END """)]
    [InlineData(NameQuotingPolicy.SquareBracketsRequired, """  /*   0042 */ CREATE FUNCTION  [dbo].█AJ5038░script_0.sql░MyDb.dbo.T1░function░T1░[T1]███T1█         () RETURNS INT AS BEGIN RETURN 1 END """)]
    [InlineData(NameQuotingPolicy.SquareBracketsRequired, """  /*   0043 */ CREATE FUNCTION  [dbo].[T1]                                                         () RETURNS INT AS BEGIN RETURN 1 END """)]
    [InlineData(NameQuotingPolicy.SquareBracketsRequired, """  /*   0044 */ CREATE FUNCTION  █AJ5038░script_0.sql░MyDb.dbo.T1░function░dbo░[dbo]███dbo█.[T1]       () RETURNS INT AS BEGIN RETURN 1 END """)]
    [InlineData(NameQuotingPolicy.SquareBracketsRequired, """  /*   0045 */ CREATE FUNCTION  █AJ5038░script_0.sql░MyDb.dbo.T1░function░"dbo"░[dbo]███"dbo"█.[T1]   () RETURNS INT AS BEGIN RETURN 1 END """)]
    public void WithFunctionCreation_Theory(NameQuotingPolicy nameQuotingPolicy, string code)
    {
        Aj5038Settings settings = new
        (
            NameQuotingPolicyDuringObjectCreation: nameQuotingPolicy,
            NameQuotingPolicyForColumnReferences: NameQuotingPolicy.Undefined,
            NameQuotingPolicyForTableAliases: NameQuotingPolicy.Undefined,
            NameQuotingPolicyForSchemaNameReferences: NameQuotingPolicy.Undefined
        );

        const string codePrefix = """
                                  USE MyDb
                                  GO


                                  """;

        Verify(settings, codePrefix + code);
    }

    [Theory]
    [InlineData(NameQuotingPolicy.Undefined, """ /*                 0000 */ CREATE PROCEDURE  dbo.T1                                                             AS BEGIN SELECT 1 END """)]
    [InlineData(NameQuotingPolicy.Undefined, """ /*                 0001 */ CREATE PROCEDURE  [dbo].T1                                                           AS BEGIN SELECT 1 END """)]
    [InlineData(NameQuotingPolicy.Undefined, """ /*                 0002 */ CREATE PROCEDURE  dbo.[T1]                                                           AS BEGIN SELECT 1 END """)]
    [InlineData(NameQuotingPolicy.Undefined, """ /*                 0003 */ CREATE PROCEDURE  "dbo".T1                                                           AS BEGIN SELECT 1 END """)]
    [InlineData(NameQuotingPolicy.Undefined, """ /*                 0004 */ CREATE PROCEDURE  dbo."T1"                                                           AS BEGIN SELECT 1 END """)]
    //
    [InlineData(NameQuotingPolicy.Required, """  /*                 0010 */ CREATE PROCEDURE  █AJ5038░script_0.sql░MyDb.dbo.T1░procedure░T1░[T1]███T1█               AS BEGIN SELECT 1 END """)]
    [InlineData(NameQuotingPolicy.Required, """  /*                 0011 */ CREATE PROCEDURE  [dbo].█AJ5038░script_0.sql░MyDb.dbo.T1░procedure░T1░[T1]███T1█         AS BEGIN SELECT 1 END """)]
    [InlineData(NameQuotingPolicy.Required, """  /*                 0012 */ CREATE PROCEDURE  "dbo".█AJ5038░script_0.sql░MyDb.dbo.T1░procedure░T1░[T1]███T1█         AS BEGIN SELECT 1 END """)]
    [InlineData(NameQuotingPolicy.Required, """  /*                 0013 */ CREATE PROCEDURE  [dbo].[T1]                                                         AS BEGIN SELECT 1 END """)]
    [InlineData(NameQuotingPolicy.Required, """  /*                 0014 */ CREATE PROCEDURE  "dbo"."T1"                                                         AS BEGIN SELECT 1 END """)]
    //
    [InlineData(NameQuotingPolicy.NotAllowed, """  /*               0020 */ CREATE PROCEDURE  T1                                                                 AS BEGIN SELECT 1 END """)]
    [InlineData(NameQuotingPolicy.NotAllowed, """  /*               0021 */ CREATE PROCEDURE  dbo.T1                                                             AS BEGIN SELECT 1 END """)]
    [InlineData(NameQuotingPolicy.NotAllowed, """  /*               0022 */ CREATE PROCEDURE  █AJ5038░script_0.sql░MyDb.dbo.T1░procedure░[dbo]░dbo███[dbo]█.T1       AS BEGIN SELECT 1 END """)]
    [InlineData(NameQuotingPolicy.NotAllowed, """  /*               0023 */ CREATE PROCEDURE  █AJ5038░script_0.sql░MyDb.dbo.T1░procedure░"dbo"░dbo███"dbo"█.T1       AS BEGIN SELECT 1 END """)]
    [InlineData(NameQuotingPolicy.NotAllowed, """  /*               0024 */ CREATE PROCEDURE  dbo.█AJ5038░script_0.sql░MyDb.dbo.T1░procedure░[T1]░T1███[T1]█         AS BEGIN SELECT 1 END """)]
    [InlineData(NameQuotingPolicy.NotAllowed, """  /*               0025 */ CREATE PROCEDURE  dbo.█AJ5038░script_0.sql░MyDb.dbo.T1░procedure░"T1"░T1███"T1"█         AS BEGIN SELECT 1 END """)]
    //
    [InlineData(NameQuotingPolicy.DoubleQuotesRequired, """  /*     0030 */ CREATE PROCEDURE  █AJ5038░script_0.sql░MyDb.dbo.T1░procedure░T1░"T1"███T1█               AS BEGIN SELECT 1 END """)]
    [InlineData(NameQuotingPolicy.DoubleQuotesRequired, """  /*     0031 */ CREATE PROCEDURE  "dbo".█AJ5038░script_0.sql░MyDb.dbo.T1░procedure░[T1]░"T1"███[T1]█     AS BEGIN SELECT 1 END """)]
    [InlineData(NameQuotingPolicy.DoubleQuotesRequired, """  /*     0032 */ CREATE PROCEDURE  "dbo".█AJ5038░script_0.sql░MyDb.dbo.T1░procedure░T1░"T1"███T1█         AS BEGIN SELECT 1 END """)]
    [InlineData(NameQuotingPolicy.DoubleQuotesRequired, """  /*     0033 */ CREATE PROCEDURE  "dbo"."T1"                                                         AS BEGIN SELECT 1 END """)]
    [InlineData(NameQuotingPolicy.DoubleQuotesRequired, """  /*     0034 */ CREATE PROCEDURE  █AJ5038░script_0.sql░MyDb.dbo.T1░procedure░dbo░"dbo"███dbo█."T1"       AS BEGIN SELECT 1 END """)]
    [InlineData(NameQuotingPolicy.DoubleQuotesRequired, """  /*     0035 */ CREATE PROCEDURE  █AJ5038░script_0.sql░MyDb.dbo.T1░procedure░[dbo]░"dbo"███[dbo]█."T1"   AS BEGIN SELECT 1 END """)]
    //
    [InlineData(NameQuotingPolicy.SquareBracketsRequired, """  /*   0040 */ CREATE PROCEDURE  █AJ5038░script_0.sql░MyDb.dbo.T1░procedure░T1░[T1]███T1█               AS BEGIN SELECT 1 END """)]
    [InlineData(NameQuotingPolicy.SquareBracketsRequired, """  /*   0041 */ CREATE PROCEDURE  [dbo].█AJ5038░script_0.sql░MyDb.dbo.T1░procedure░"T1"░[T1]███"T1"█     AS BEGIN SELECT 1 END """)]
    [InlineData(NameQuotingPolicy.SquareBracketsRequired, """  /*   0042 */ CREATE PROCEDURE  [dbo].█AJ5038░script_0.sql░MyDb.dbo.T1░procedure░T1░[T1]███T1█         AS BEGIN SELECT 1 END """)]
    [InlineData(NameQuotingPolicy.SquareBracketsRequired, """  /*   0043 */ CREATE PROCEDURE  [dbo].[T1]                                                         AS BEGIN SELECT 1 END """)]
    [InlineData(NameQuotingPolicy.SquareBracketsRequired, """  /*   0044 */ CREATE PROCEDURE  █AJ5038░script_0.sql░MyDb.dbo.T1░procedure░dbo░[dbo]███dbo█.[T1]       AS BEGIN SELECT 1 END """)]
    [InlineData(NameQuotingPolicy.SquareBracketsRequired, """  /*   0045 */ CREATE PROCEDURE  █AJ5038░script_0.sql░MyDb.dbo.T1░procedure░"dbo"░[dbo]███"dbo"█.[T1]   AS BEGIN SELECT 1 END """)]
    public void WithProcedureCreation_Theory(NameQuotingPolicy nameQuotingPolicy, string code)
    {
        Aj5038Settings settings = new
        (
            NameQuotingPolicyDuringObjectCreation: nameQuotingPolicy,
            NameQuotingPolicyForColumnReferences: NameQuotingPolicy.Undefined,
            NameQuotingPolicyForTableAliases: NameQuotingPolicy.Undefined,
            NameQuotingPolicyForSchemaNameReferences: NameQuotingPolicy.Undefined
        );

        const string codePrefix = """
                                  USE MyDb
                                  GO


                                  """;

        Verify(settings, codePrefix + code);
    }

    [Theory]
    [InlineData(NameQuotingPolicy.Undefined, """ /*                 0000 */ CREATE VIEW  dbo.T1                                                             AS SELECT 1 AS Expr1 """)]
    [InlineData(NameQuotingPolicy.Undefined, """ /*                 0001 */ CREATE VIEW  [dbo].T1                                                           AS SELECT 1 AS Expr1 """)]
    [InlineData(NameQuotingPolicy.Undefined, """ /*                 0002 */ CREATE VIEW  dbo.[T1]                                                           AS SELECT 1 AS Expr1 """)]
    [InlineData(NameQuotingPolicy.Undefined, """ /*                 0003 */ CREATE VIEW  "dbo".T1                                                           AS SELECT 1 AS Expr1 """)]
    [InlineData(NameQuotingPolicy.Undefined, """ /*                 0004 */ CREATE VIEW  dbo."T1"                                                           AS SELECT 1 AS Expr1 """)]
    //
    [InlineData(NameQuotingPolicy.Required, """  /*                 0010 */ CREATE VIEW  █AJ5038░script_0.sql░MyDb.dbo.T1░view░T1░[T1]███T1█                AS SELECT 1 AS Expr1 """)]
    [InlineData(NameQuotingPolicy.Required, """  /*                 0011 */ CREATE VIEW  [dbo].█AJ5038░script_0.sql░MyDb.dbo.T1░view░T1░[T1]███T1█          AS SELECT 1 AS Expr1 """)]
    [InlineData(NameQuotingPolicy.Required, """  /*                 0012 */ CREATE VIEW  "dbo".█AJ5038░script_0.sql░MyDb.dbo.T1░view░T1░[T1]███T1█          AS SELECT 1 AS Expr1 """)]
    [InlineData(NameQuotingPolicy.Required, """  /*                 0013 */ CREATE VIEW  [dbo].[T1]                                                         AS SELECT 1 AS Expr1 """)]
    [InlineData(NameQuotingPolicy.Required, """  /*                 0014 */ CREATE VIEW  "dbo"."T1"                                                         AS SELECT 1 AS Expr1 """)]
    //
    [InlineData(NameQuotingPolicy.NotAllowed, """  /*               0020 */ CREATE VIEW  T1                                                                 AS SELECT 1 AS Expr1 """)]
    [InlineData(NameQuotingPolicy.NotAllowed, """  /*               0021 */ CREATE VIEW  dbo.T1                                                             AS SELECT 1 AS Expr1 """)]
    [InlineData(NameQuotingPolicy.NotAllowed, """  /*               0022 */ CREATE VIEW  █AJ5038░script_0.sql░MyDb.dbo.T1░view░[dbo]░dbo███[dbo]█.T1        AS SELECT 1 AS Expr1 """)]
    [InlineData(NameQuotingPolicy.NotAllowed, """  /*               0023 */ CREATE VIEW  █AJ5038░script_0.sql░MyDb.dbo.T1░view░"dbo"░dbo███"dbo"█.T1        AS SELECT 1 AS Expr1 """)]
    [InlineData(NameQuotingPolicy.NotAllowed, """  /*               0024 */ CREATE VIEW  dbo.█AJ5038░script_0.sql░MyDb.dbo.T1░view░[T1]░T1███[T1]█          AS SELECT 1 AS Expr1 """)]
    [InlineData(NameQuotingPolicy.NotAllowed, """  /*               0025 */ CREATE VIEW  dbo.█AJ5038░script_0.sql░MyDb.dbo.T1░view░"T1"░T1███"T1"█          AS SELECT 1 AS Expr1 """)]
    //
    [InlineData(NameQuotingPolicy.DoubleQuotesRequired, """  /*     0030 */ CREATE VIEW  █AJ5038░script_0.sql░MyDb.dbo.T1░view░T1░"T1"███T1█                AS SELECT 1 AS Expr1 """)]
    [InlineData(NameQuotingPolicy.DoubleQuotesRequired, """  /*     0031 */ CREATE VIEW  "dbo".█AJ5038░script_0.sql░MyDb.dbo.T1░view░[T1]░"T1"███[T1]█      AS SELECT 1 AS Expr1 """)]
    [InlineData(NameQuotingPolicy.DoubleQuotesRequired, """  /*     0032 */ CREATE VIEW  "dbo".█AJ5038░script_0.sql░MyDb.dbo.T1░view░T1░"T1"███T1█          AS SELECT 1 AS Expr1 """)]
    [InlineData(NameQuotingPolicy.DoubleQuotesRequired, """  /*     0033 */ CREATE VIEW  "dbo"."T1"                                                         AS SELECT 1 AS Expr1 """)]
    [InlineData(NameQuotingPolicy.DoubleQuotesRequired, """  /*     0034 */ CREATE VIEW  █AJ5038░script_0.sql░MyDb.dbo.T1░view░dbo░"dbo"███dbo█."T1"        AS SELECT 1 AS Expr1 """)]
    [InlineData(NameQuotingPolicy.DoubleQuotesRequired, """  /*     0035 */ CREATE VIEW  █AJ5038░script_0.sql░MyDb.dbo.T1░view░[dbo]░"dbo"███[dbo]█."T1"    AS SELECT 1 AS Expr1 """)]
    //
    [InlineData(NameQuotingPolicy.SquareBracketsRequired, """  /*   0040 */ CREATE VIEW  █AJ5038░script_0.sql░MyDb.dbo.T1░view░T1░[T1]███T1█                AS SELECT 1 AS Expr1 """)]
    [InlineData(NameQuotingPolicy.SquareBracketsRequired, """  /*   0041 */ CREATE VIEW  [dbo].█AJ5038░script_0.sql░MyDb.dbo.T1░view░"T1"░[T1]███"T1"█      AS SELECT 1 AS Expr1 """)]
    [InlineData(NameQuotingPolicy.SquareBracketsRequired, """  /*   0042 */ CREATE VIEW  [dbo].█AJ5038░script_0.sql░MyDb.dbo.T1░view░T1░[T1]███T1█          AS SELECT 1 AS Expr1 """)]
    [InlineData(NameQuotingPolicy.SquareBracketsRequired, """  /*   0043 */ CREATE VIEW  [dbo].[T1]                                                         AS SELECT 1 AS Expr1 """)]
    [InlineData(NameQuotingPolicy.SquareBracketsRequired, """  /*   0044 */ CREATE VIEW  █AJ5038░script_0.sql░MyDb.dbo.T1░view░dbo░[dbo]███dbo█.[T1]        AS SELECT 1 AS Expr1 """)]
    [InlineData(NameQuotingPolicy.SquareBracketsRequired, """  /*   0045 */ CREATE VIEW  █AJ5038░script_0.sql░MyDb.dbo.T1░view░"dbo"░[dbo]███"dbo"█.[T1]    AS SELECT 1 AS Expr1 """)]
    public void WithViewCreation_Theory(NameQuotingPolicy nameQuotingPolicy, string code)
    {
        Aj5038Settings settings = new
        (
            NameQuotingPolicyDuringObjectCreation: nameQuotingPolicy,
            NameQuotingPolicyForColumnReferences: NameQuotingPolicy.Undefined,
            NameQuotingPolicyForTableAliases: NameQuotingPolicy.Undefined,
            NameQuotingPolicyForSchemaNameReferences: NameQuotingPolicy.Undefined
        );

        const string codePrefix = """
                                  USE MyDb
                                  GO


                                  """;

        Verify(settings, codePrefix + code);
    }

    [Theory]
    [InlineData(NameQuotingPolicy.Undefined, """ /*                 0000 */ CREATE TRIGGER  dbo.T1                                                             ON dbo.Table1 AFTER INSERT AS BEGIN PRINT 303 END """)]
    [InlineData(NameQuotingPolicy.Undefined, """ /*                 0001 */ CREATE TRIGGER  [dbo].T1                                                           ON dbo.Table1 AFTER INSERT AS BEGIN PRINT 303 END """)]
    [InlineData(NameQuotingPolicy.Undefined, """ /*                 0002 */ CREATE TRIGGER  dbo.[T1]                                                           ON dbo.Table1 AFTER INSERT AS BEGIN PRINT 303 END """)]
    [InlineData(NameQuotingPolicy.Undefined, """ /*                 0003 */ CREATE TRIGGER  "dbo".T1                                                           ON dbo.Table1 AFTER INSERT AS BEGIN PRINT 303 END """)]
    [InlineData(NameQuotingPolicy.Undefined, """ /*                 0004 */ CREATE TRIGGER  dbo."T1"                                                           ON dbo.Table1 AFTER INSERT AS BEGIN PRINT 303 END """)]
    //
    [InlineData(NameQuotingPolicy.Required, """  /*                 0010 */ CREATE TRIGGER  █AJ5038░script_0.sql░MyDb.dbo.T1░trigger░T1░[T1]███T1█                ON dbo.Table1 AFTER INSERT AS BEGIN PRINT 303 END """)]
    [InlineData(NameQuotingPolicy.Required, """  /*                 0011 */ CREATE TRIGGER  [dbo].█AJ5038░script_0.sql░MyDb.dbo.T1░trigger░T1░[T1]███T1█          ON dbo.Table1 AFTER INSERT AS BEGIN PRINT 303 END """)]
    [InlineData(NameQuotingPolicy.Required, """  /*                 0012 */ CREATE TRIGGER  "dbo".█AJ5038░script_0.sql░MyDb.dbo.T1░trigger░T1░[T1]███T1█          ON dbo.Table1 AFTER INSERT AS BEGIN PRINT 303 END """)]
    [InlineData(NameQuotingPolicy.Required, """  /*                 0013 */ CREATE TRIGGER  [dbo].[T1]                                                         ON dbo.Table1 AFTER INSERT AS BEGIN PRINT 303 END """)]
    [InlineData(NameQuotingPolicy.Required, """  /*                 0014 */ CREATE TRIGGER  "dbo"."T1"                                                         ON dbo.Table1 AFTER INSERT AS BEGIN PRINT 303 END """)]
    //
    [InlineData(NameQuotingPolicy.NotAllowed, """  /*               0020 */ CREATE TRIGGER  T1                                                                 ON dbo.Table1 AFTER INSERT AS BEGIN PRINT 303 END """)]
    [InlineData(NameQuotingPolicy.NotAllowed, """  /*               0021 */ CREATE TRIGGER  dbo.T1                                                             ON dbo.Table1 AFTER INSERT AS BEGIN PRINT 303 END """)]
    [InlineData(NameQuotingPolicy.NotAllowed, """  /*               0022 */ CREATE TRIGGER  █AJ5038░script_0.sql░MyDb.dbo.T1░trigger░[dbo]░dbo███[dbo]█.T1        ON dbo.Table1 AFTER INSERT AS BEGIN PRINT 303 END """)]
    [InlineData(NameQuotingPolicy.NotAllowed, """  /*               0023 */ CREATE TRIGGER  █AJ5038░script_0.sql░MyDb.dbo.T1░trigger░"dbo"░dbo███"dbo"█.T1        ON dbo.Table1 AFTER INSERT AS BEGIN PRINT 303 END """)]
    [InlineData(NameQuotingPolicy.NotAllowed, """  /*               0024 */ CREATE TRIGGER  dbo.█AJ5038░script_0.sql░MyDb.dbo.T1░trigger░[T1]░T1███[T1]█          ON dbo.Table1 AFTER INSERT AS BEGIN PRINT 303 END """)]
    [InlineData(NameQuotingPolicy.NotAllowed, """  /*               0025 */ CREATE TRIGGER  dbo.█AJ5038░script_0.sql░MyDb.dbo.T1░trigger░"T1"░T1███"T1"█          ON dbo.Table1 AFTER INSERT AS BEGIN PRINT 303 END """)]
    //
    [InlineData(NameQuotingPolicy.DoubleQuotesRequired, """  /*     0030 */ CREATE TRIGGER  █AJ5038░script_0.sql░MyDb.dbo.T1░trigger░T1░"T1"███T1█                ON dbo.Table1 AFTER INSERT AS BEGIN PRINT 303 END """)]
    [InlineData(NameQuotingPolicy.DoubleQuotesRequired, """  /*     0031 */ CREATE TRIGGER  "dbo".█AJ5038░script_0.sql░MyDb.dbo.T1░trigger░[T1]░"T1"███[T1]█      ON dbo.Table1 AFTER INSERT AS BEGIN PRINT 303 END """)]
    [InlineData(NameQuotingPolicy.DoubleQuotesRequired, """  /*     0032 */ CREATE TRIGGER  "dbo".█AJ5038░script_0.sql░MyDb.dbo.T1░trigger░T1░"T1"███T1█          ON dbo.Table1 AFTER INSERT AS BEGIN PRINT 303 END """)]
    [InlineData(NameQuotingPolicy.DoubleQuotesRequired, """  /*     0033 */ CREATE TRIGGER  "dbo"."T1"                                                         ON dbo.Table1 AFTER INSERT AS BEGIN PRINT 303 END """)]
    [InlineData(NameQuotingPolicy.DoubleQuotesRequired, """  /*     0034 */ CREATE TRIGGER  █AJ5038░script_0.sql░MyDb.dbo.T1░trigger░dbo░"dbo"███dbo█."T1"        ON dbo.Table1 AFTER INSERT AS BEGIN PRINT 303 END """)]
    [InlineData(NameQuotingPolicy.DoubleQuotesRequired, """  /*     0035 */ CREATE TRIGGER  █AJ5038░script_0.sql░MyDb.dbo.T1░trigger░[dbo]░"dbo"███[dbo]█."T1"    ON dbo.Table1 AFTER INSERT AS BEGIN PRINT 303 END """)]
    //
    [InlineData(NameQuotingPolicy.SquareBracketsRequired, """  /*   0040 */ CREATE TRIGGER  █AJ5038░script_0.sql░MyDb.dbo.T1░trigger░T1░[T1]███T1█                ON dbo.Table1 AFTER INSERT AS BEGIN PRINT 303 END """)]
    [InlineData(NameQuotingPolicy.SquareBracketsRequired, """  /*   0041 */ CREATE TRIGGER  [dbo].█AJ5038░script_0.sql░MyDb.dbo.T1░trigger░"T1"░[T1]███"T1"█      ON dbo.Table1 AFTER INSERT AS BEGIN PRINT 303 END """)]
    [InlineData(NameQuotingPolicy.SquareBracketsRequired, """  /*   0042 */ CREATE TRIGGER  [dbo].█AJ5038░script_0.sql░MyDb.dbo.T1░trigger░T1░[T1]███T1█          ON dbo.Table1 AFTER INSERT AS BEGIN PRINT 303 END """)]
    [InlineData(NameQuotingPolicy.SquareBracketsRequired, """  /*   0043 */ CREATE TRIGGER  [dbo].[T1]                                                         ON dbo.Table1 AFTER INSERT AS BEGIN PRINT 303 END """)]
    [InlineData(NameQuotingPolicy.SquareBracketsRequired, """  /*   0044 */ CREATE TRIGGER  █AJ5038░script_0.sql░MyDb.dbo.T1░trigger░dbo░[dbo]███dbo█.[T1]        ON dbo.Table1 AFTER INSERT AS BEGIN PRINT 303 END """)]
    [InlineData(NameQuotingPolicy.SquareBracketsRequired, """  /*   0045 */ CREATE TRIGGER  █AJ5038░script_0.sql░MyDb.dbo.T1░trigger░"dbo"░[dbo]███"dbo"█.[T1]    ON dbo.Table1 AFTER INSERT AS BEGIN PRINT 303 END """)]
    public void WithTriggerCreation_Theory(NameQuotingPolicy nameQuotingPolicy, string code)
    {
        Aj5038Settings settings = new
        (
            NameQuotingPolicyDuringObjectCreation: nameQuotingPolicy,
            NameQuotingPolicyForColumnReferences: NameQuotingPolicy.Undefined,
            NameQuotingPolicyForTableAliases: NameQuotingPolicy.Undefined,
            NameQuotingPolicyForSchemaNameReferences: NameQuotingPolicy.Undefined
        );

        const string codePrefix = """
                                  USE MyDb
                                  GO


                                  """;

        Verify(settings, codePrefix + code);
    }
    continue here...
    // test cases for column references
}
