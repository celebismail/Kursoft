using KursoftRekabet.Ayristirma;
using KursoftRekabet.Yardimcilar;
using Newtonsoft.Json.Linq;
using Xunit;

namespace KursoftRekabet.Test
{
    public class JsonYoluTestleri
    {
        private static readonly JToken _ornek = JToken.Parse(@"{
            ""product"": {
                ""name"": ""Ürün"",
                ""images"": [""/a.jpg"", ""/b.jpg""],
                ""variants"": [ { ""v"": ""S"", ""selected"": false }, { ""v"": ""M"", ""selected"": true } ],
                ""attributes"": [ { ""key"": { ""name"": ""Renk"" }, ""value"": { ""name"": ""Siyah"" } } ],
                ""deep"": { ""x"": { ""merchantListing"": { ""ok"": 1 } } },
                ""bos"": null,
                ""ozel-ad"": 5
            }
        }");

        [Fact]
        public void DuzYol() { Assert.Equal("Ürün", JsonYolu.Metin(_ornek, "product.name")); }

        [Fact]
        public void DiziIndexVeSondan()
        {
            Assert.Equal("/a.jpg", JsonYolu.Metin(_ornek, "product.images[0]"));
            Assert.Equal("/b.jpg", JsonYolu.Metin(_ornek, "product.images[-1]"));
            Assert.Null(JsonYolu.Metin(_ornek, "product.images[5]"));
        }

        [Fact]
        public void YildizHepsiniVerir() { Assert.Equal(2, JsonYolu.Hepsi(_ornek, "product.variants[*].v").Count); }

        [Fact]
        public void Filtre()
        {
            Assert.Equal("M", JsonYolu.Metin(_ornek, "product.variants[?selected=true].v"));
            Assert.Equal("Siyah", JsonYolu.Metin(_ornek, "product.attributes[?key.name=Renk].value.name"));
        }

        [Fact]
        public void DerinArama() { Assert.Equal("1", JsonYolu.Metin(_ornek, "..merchantListing.ok")); }

        [Fact]
        public void OzelAd() { Assert.Equal("5", JsonYolu.Metin(_ornek, "product['ozel-ad']")); }

        [Fact]
        public void OlmayanVeNullYolExceptionAtmaz()
        {
            Assert.Null(JsonYolu.Metin(_ornek, "product.bos.x"));
            Assert.Null(JsonYolu.Metin(_ornek, "yok.hic.yok"));
            Assert.Null(JsonYolu.Metin(null, "a.b"));
            Assert.Null(JsonYolu.Metin(_ornek, "product[[bozuk"));
        }

        [Fact]
        public void AnahtarlariIcerenNesneBulunur()
        {
            JObject o = JsonYolu.AnahtarlariIcerenNesne(JToken.Parse(@"{""a"":{""b"":[{""name"":""x"",""merchantListing"":{}}]}}"), new List<string> { "merchantListing", "name" }, 10);
            Assert.NotNull(o);
            Assert.Equal("x", (string)o["name"]);
        }
    }

    public class LinkTestleri
    {
        public LinkTestleri()
        {
            OrnekYardimcisi.AyarlariYukle();
        }

        [Theory]
        [InlineData("https://www.trendyol.com/marka/urun-p-123?boutiqueId=1&merchantId=5&utm_source=x#yorum", "https://www.trendyol.com/marka/urun-p-123?boutiqueId=1&merchantId=5")]
        [InlineData("trendyol.com/marka/urun-p-123", "https://trendyol.com/marka/urun-p-123")]
        [InlineData("https://m.trendyol.com/marka/urun-p-123?gclid=1", "https://m.trendyol.com/marka/urun-p-123")]
        public void TrendyolNormalize(string link, string beklenen)
        {
            LinkSonucu s = LinkYardimcisi.Dogrula(link, SeciciAyarlari.Platform(Globals.ModulType.trendyol));
            Assert.True(s.Gecerli, s.Hata);
            Assert.Equal(beklenen, s.NormalizeLink);
        }

        [Theory]
        [InlineData("https://www.trendyol.com/sr?q=kulaklik")]
        [InlineData("https://www.trendyol.com.sahte.com/urun-p-1")]
        [InlineData("https://evil-trendyol.com/urun-p-1")]
        [InlineData("ftp://www.trendyol.com/urun-p-1")]
        [InlineData("")]
        [InlineData("bu bir link değil")]
        public void TrendyolGecersiz(string link)
        {
            Assert.False(LinkYardimcisi.Dogrula(link, SeciciAyarlari.Platform(Globals.ModulType.trendyol)).Gecerli);
        }

        [Fact]
        public void KisaLinkKabulEdilir()
        {
            LinkSonucu s = LinkYardimcisi.Dogrula("https://ty.gl/abc123", SeciciAyarlari.Platform(Globals.ModulType.trendyol));
            Assert.True(s.Gecerli);
            Assert.True(s.KisaLinkMi);
        }

        [Theory]
        [InlineData("https://www.trendyol.com/a/b-p-1", Globals.ModulType.trendyol)]
        [InlineData("https://ty.gl/x", Globals.ModulType.trendyol)]
        [InlineData("https://www.hepsiburada.com/a-p-HBC1", Globals.ModulType.hepsiburada)]
        [InlineData("https://www.n11.com/urun/a-1", Globals.ModulType.n11)]
        [InlineData("https://www.amazon.com.tr/dp/X", Globals.ModulType.bilinmiyor)]
        public void PlatformBulunur(string link, Globals.ModulType beklenen)
        {
            Assert.Equal(beklenen, LinkYardimcisi.PlatformBul(link));
        }

        [Fact]
        public async Task BilinmeyenPlatformExceptionAtmazModelDoner()
        {
            KursoftRekabet.model.UrunBilgiModel m = await UrunBilgiGetir.urunbilgigetir("https://www.ornek.com/x");
            Assert.False(m.Basarili);
            Assert.Equal(Globals.HataKodlari.GECERSIZ_LINK, m.HataKodu);
        }

        [Fact]
        public async Task GecersizLinkTarayiciAcmadanDoner()
        {
            KursoftRekabet.model.UrunBilgiModel m = await UrunBilgiGetir.trendyolurunbilgigetir("https://www.trendyol.com/sr?q=x");
            Assert.False(m.Basarili);
            Assert.Equal(Globals.HataKodlari.GECERSIZ_LINK, m.HataKodu);
        }
    }
}
