using KursoftRekabet.Yardimcilar;
using Xunit;

namespace KursoftRekabet.Test
{
    public class FiyatCevirTestleri
    {
        [Theory]
        [InlineData("1.950,99 TL", "1950.99")]
        [InlineData("1950,99", "1950.99")]
        [InlineData("₺1.950", "1950")]
        [InlineData("1.950 TL", "1950")]
        [InlineData("1.250.000,50 TL", "1250000.50")]
        [InlineData("19,90 TL", "19.90")]
        [InlineData("19.99", "19.99")]
        [InlineData("1,950.99", "1950.99")]
        [InlineData("Sepette 899,90 TL", "899.90")]
        [InlineData("  2.499,00TL  ", "2499.00")]
        [InlineData("TRY 349,5", "349.5")]
        [InlineData("1 950,99 TL", "1950.99")]
        [InlineData("1950", "1950")]
        [InlineData("0,00 TL", "0")]
        [InlineData("₺ 12.345,67", "12345.67")]
        [InlineData("Kargo Bedava - 129,99 TL", "129.99")]
        [InlineData("1.599,00 TL'den başlayan", "1599.00")]
        [InlineData("₺1.234,5", "1234.5")]
        public void TekFiyatCevrilir(string metin, string beklenen)
        {
            decimal? sonuc = FiyatCevir.Cevir(metin);
            Assert.Equal(decimal.Parse(beklenen, System.Globalization.CultureInfo.InvariantCulture), sonuc);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("Fiyat yok")]
        [InlineData("Tükendi")]
        public void CevrilemeyenNullDoner(string metin)
        {
            FiyatSonucu s = FiyatCevir.Coz(metin);
            Assert.Null(s.Deger);
            Assert.NotNull(s.Uyari);
        }

        [Fact]
        public void AralikFiyatEnDusukDegeriVerir()
        {
            FiyatSonucu s = FiyatCevir.Coz("100,00 TL - 200,50 TL");
            Assert.True(s.AralikMi);
            Assert.Equal(100.00m, s.Deger);
            Assert.Equal(200.50m, s.UstDeger);
        }

        [Fact]
        public void AralikFiyatTireIleBoşluksuz()
        {
            FiyatSonucu s = FiyatCevir.Coz("1.299-1.499 TL");
            Assert.True(s.AralikMi);
            Assert.Equal(1299m, s.Deger);
            Assert.Equal(1499m, s.UstDeger);
        }

        [Theory]
        [InlineData("1.950,99 TL", "TRY")]
        [InlineData("₺1.950", "TRY")]
        [InlineData("1950 TRY", "TRY")]
        [InlineData("$19.99", "USD")]
        [InlineData("1950", null)]
        public void ParaBirimiBulunur(string metin, string beklenen)
        {
            Assert.Equal(beklenen, FiyatCevir.Coz(metin).ParaBirimi);
        }

        [Fact]
        public void JsonSayisiDogrudanCevrilir()
        {
            Assert.Equal(1950.99m, FiyatCevir.JsonDegeriCevir(1950.99d));
            Assert.Equal(1950m, FiyatCevir.JsonDegeriCevir(1950L));
            Assert.Equal(1950.99m, FiyatCevir.JsonDegeriCevir("1.950,99 TL"));
        }
    }
}
