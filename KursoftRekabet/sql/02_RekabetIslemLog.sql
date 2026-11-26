-- KursoftRekabet: link bazında işlem logu (appsettings.json -> DbLogAcik = true ise yazılır)
-- Yazılamazsa uygulama durmaz, dosya loguna devam eder.
IF OBJECT_ID(N'dbo.RekabetIslemLog', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RekabetIslemLog
    (
        ID               BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_RekabetIslemLog PRIMARY KEY,
        IslemID          UNIQUEIDENTIFIER NOT NULL,
        CalismaID        NVARCHAR(50)    NULL,
        KayitID          NVARCHAR(100)   NULL,
        ModulType        NVARCHAR(30)    NOT NULL,
        Link             NVARCHAR(2000)  NULL,
        Basarili         BIT             NOT NULL,
        HataKodu         NVARCHAR(50)    NULL,
        HataMesaji       NVARCHAR(4000)  NULL,
        VeriKaynagi      NVARCHAR(30)    NULL,
        UyariSayisi      INT             NOT NULL CONSTRAINT DF_RekabetIslemLog_UyariSayisi DEFAULT (0),
        Uyarilar         NVARCHAR(4000)  NULL,
        DenemeSayisi     INT             NOT NULL CONSTRAINT DF_RekabetIslemLog_DenemeSayisi DEFAULT (1),
        SureMs           BIGINT          NOT NULL,
        TanilamaKlasoru  NVARCHAR(500)   NULL,
        MakineAdi        NVARCHAR(100)   NULL,
        IstekZamani      DATETIME2(3)    NOT NULL,
        KayitZamani      DATETIME2(3)    NOT NULL CONSTRAINT DF_RekabetIslemLog_KayitZamani DEFAULT (SYSDATETIME())
    );

    CREATE INDEX IX_RekabetIslemLog_CalismaID ON dbo.RekabetIslemLog (CalismaID);
    CREATE INDEX IX_RekabetIslemLog_HataKodu ON dbo.RekabetIslemLog (HataKodu, IstekZamani DESC) WHERE Basarili = 0;
END
GO
