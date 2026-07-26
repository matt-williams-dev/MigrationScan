CREATE TABLE [dbo].[StatementLine]
(
    [StatementLineId] INT             NOT NULL PRIMARY KEY IDENTITY,
    [AccountId]       INT             NOT NULL REFERENCES [dbo].[Account] ([AccountId]),
    [Amount]          DECIMAL (18, 2) NOT NULL,
    [Posted]          DATETIME2 (7)   NOT NULL
);
