using DatabaseAnalyzer.Testing;
using DatabaseAnalyzers.DefaultAnalyzers.Analyzers.ObjectCreation;
using Xunit.Abstractions;

namespace DatabaseAnalyzers.DefaultAnalyzers.Tests.Analyzers.Playground;

public sealed class PlaygroundTests(ITestOutputHelper testOutputHelper)
    : ScriptAnalyzerTestsBase<ObjectCreationWithoutOrAlterAnalyzer>(testOutputHelper)
{
    [Fact]
    public void PlaygroundTests1()
    {
        const string code = """
                            USE [database1]

                            SET ANSI_NULLS ON
                            SET QUOTED_IDENTIFIER ON
                            GO

                            CREATE TABLE [dbo].[T2]
                            (
                                [Id] [int] NOT NULL,
                                [Value] [nvarchar](50) NOT NULL,
                                CONSTRAINT [PK_T2] PRIMARY KEY CLUSTERED
                                (
                                    [Id] ASC
                                ) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
                            ) ON [PRIMARY]

                            GO

                            SET ANSI_PADDING ON

                            GO

                            CREATE NONCLUSTERED INDEX [IX_T1_Value1] ON [dbo].[T1]
                            (
                                [Value1] ASC
                            ) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]

                            GO

                            CREATE NONCLUSTERED INDEX [IX_T1_Value2_Value3] ON [dbo].[T1]
                            (
                                [Value2] ASC,
                                [Value3] ASC
                            ) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
                            GO

                            ALTER TABLE [dbo].[T1]  WITH CHECK ADD  CONSTRAINT [FK_T1_T2] FOREIGN KEY([OtherId])
                            REFERENCES [dbo].[T2] ([Id])

                            GO

                            ALTER TABLE [dbo].[T1] CHECK CONSTRAINT [FK_T1_T2]
                            """;

        const string script2 = """
                               USE [database1]

                               GO

                               SET ANSI_NULLS ON
                               SET QUOTED_IDENTIFIER ON
                               GO

                               CREATE TABLE [dbo].[T1]
                               (
                                   [Id] [int] NOT NULL,
                                   [Value1] [nvarchar](50) NOT NULL,
                                   [Value2] [nchar](10) NULL,
                                   [Value3] [nchar](10) NULL,
                                   [OtherId] [int] NULL,
                                   CONSTRAINT [PK_T1] PRIMARY KEY CLUSTERED
                                   (
                                       [Id] ASC
                                   ) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
                               ) ON [PRIMARY]
                               """;

        var tester = GetDefaultTesterBuilder(code)
            .AddAdditionalScriptFile(script2, "script2.sql", "database1")
            .Build();
        Verify(tester);
    }
}
