CREATE TABLE [dbo].[Account]
(
    [AccountId]     INT           NOT NULL PRIMARY KEY IDENTITY,
    [AccountNumber] NVARCHAR (16) NOT NULL,
    [BranchCode]    NVARCHAR (8)  NOT NULL
);
