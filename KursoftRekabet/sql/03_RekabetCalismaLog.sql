-- KursoftRekabet: her exe çalışmasının özeti (appsettings.json -> DbLogAcik = true ise yazılır)
IF OBJECT_ID(N'dbo.RekabetCalismaLog', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RekabetCalismaLog
    (
        ID               BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_RekabetCalismaLog PRIMARY KEY,
        CalismaID        NVARCHAR(50)    NOT NULL,
        MakineAdi        NVARCHAR(100)   NULL,
        Surum            NVARCHAR(30)    NULL,
        BaslangicZamani  DATETIME2(3)    NOT NULL,
        BitisZamani      DATETIME2(3)    NOT NULL,
        ToplamSureMs     BIGINT          NOT NULL,
        ToplamLink       INT             NOT NULL,
        Basarili         INT             NOT NULL,
        Basarisiz        INT             NOT NULL,
        HataDagilimi     NVARCHAR(MAX)   NULL,   -- {"ENGEL":3,"ZAMAN_ASIMI":1}
        PlatformOzeti    NVARCHAR(MAX)   NULL,   -- platform bazında adet / başarılı / ortalama süre
        EnYavaslar       NVARCHAR(MAX)   NULL,   -- en yavaş 5 link
        ExitCode         INT             NOT NULL,
        IptalEdildi      BIT             NOT NULL CONSTRAINT DF_RekabetCalismaLog_IptalEdildi DEFAULT (0),
        KayitZamani      DATETIME2(3)    NOT NULL CONSTRAINT DF_RekabetCalismaLog_KayitZamani DEFAULT (SYSDATETIME())
    );

    CREATE INDEX IX_RekabetCalismaLog_Baslangic ON dbo.RekabetCalismaLog (BaslangicZamani DESC);
END
GO
