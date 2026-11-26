-- KursoftRekabet: varsayılan sonuç tablosu (appsettings.json -> SonucTabloAdi)
-- Program.SonucuIsle -> DbIslemleri.SonucYaz bu kolonlara parametreli INSERT yapar.
-- Mevcut tablolarınıza dokunmaz; sadece yoksa oluşturur.
IF OBJECT_ID(N'dbo.RekabetSonuc', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RekabetSonuc
    (
        ID                    BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_RekabetSonuc PRIMARY KEY,
        KayitID               NVARCHAR(100)   NULL,      -- LinkSorgusu'ndan gelen ID
        IslemID               UNIQUEIDENTIFIER NOT NULL,
        CalismaID             NVARCHAR(50)    NULL,
        ModulType             NVARCHAR(30)    NOT NULL,
        Link                  NVARCHAR(2000)  NULL,
        NormalizeLink         NVARCHAR(2000)  NULL,
        Basarili              BIT             NOT NULL,
        HataKodu              NVARCHAR(50)    NULL,
        HataMesaji            NVARCHAR(4000)  NULL,
        PlatformUrunID        NVARCHAR(100)   NULL,
        UrunAdi               NVARCHAR(1000)  NULL,
        Marka                 NVARCHAR(250)   NULL,
        FiyatMetni            NVARCHAR(100)   NULL,
        SatisFiyati           DECIMAL(18,2)   NULL,
        ListeFiyati           DECIMAL(18,2)   NULL,
        SepetteFiyat          DECIMAL(18,2)   NULL,
        ParaBirimi            NVARCHAR(10)    NULL,
        StokVar               BIT             NULL,
        SaticiAdi             NVARCHAR(250)   NULL,
        SaticiPuani           DECIMAL(9,2)    NULL,
        DigerSaticiSayisi     INT             NOT NULL CONSTRAINT DF_RekabetSonuc_DigerSaticiSayisi DEFAULT (0),
        EnDusukFiyat          DECIMAL(18,2)   NULL,
        EnDusukFiyatliSatici  NVARCHAR(250)   NULL,
        UrunPuani             DECIMAL(9,2)    NULL,
        YorumSayisi           INT             NULL,
        VeriKaynagi           NVARCHAR(30)    NULL,
        Uyarilar              NVARCHAR(4000)  NULL,
        SureMs                BIGINT          NOT NULL,
        IstekZamani           DATETIME2(3)    NOT NULL,
        SonucJson             NVARCHAR(MAX)   NULL,      -- UrunBilgiModel'in tamamı (diğer satıcılar, varyantlar, görseller...)
        KayitZamani           DATETIME2(3)    NOT NULL CONSTRAINT DF_RekabetSonuc_KayitZamani DEFAULT (SYSDATETIME())
    );

    CREATE INDEX IX_RekabetSonuc_KayitID ON dbo.RekabetSonuc (KayitID, IstekZamani DESC);
    CREATE INDEX IX_RekabetSonuc_CalismaID ON dbo.RekabetSonuc (CalismaID);
END
GO
