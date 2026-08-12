-- This project manages schema by hand (no EF Core Migrations project is checked in).
-- Run this once against the SmartShelf database (connection string "SmartShelfDbCon").

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'EslBrandMaster')
BEGIN
    CREATE TABLE EslBrandMaster
    (
        Id            BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Code          NVARCHAR(50)  NOT NULL,
        Name          NVARCHAR(100) NOT NULL,
        IsActive      BIT           NOT NULL DEFAULT (1),
        CreatedDate   DATETIME2     NOT NULL DEFAULT (SYSUTCDATETIME()),
        UpdatedDate   DATETIME2     NULL,
        CreatedUser   INT           NOT NULL DEFAULT (0),
        UpdatedUser   INT           NULL
    );

    CREATE UNIQUE INDEX UX_EslBrandMaster_Code ON EslBrandMaster (Code);
END
GO

-- Seed the two brand values already in use via DeviceMaster.DeviceType.
IF NOT EXISTS (SELECT 1 FROM EslBrandMaster WHERE Code = 'Minew')
    INSERT INTO EslBrandMaster (Code, Name, IsActive) VALUES ('Minew', 'Minew ESL', 1);

IF NOT EXISTS (SELECT 1 FROM EslBrandMaster WHERE Code = 'Standard')
    INSERT INTO EslBrandMaster (Code, Name, IsActive) VALUES ('Standard', 'Standard', 1);
GO
