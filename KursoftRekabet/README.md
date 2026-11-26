# KursoftRekabet v2

Pazaryeri ürün sayfalarından rekabet verisi toplayan, **arayüzü olmayan**, Windows Zamanlanmış Görevler ile çalışan konsol uygulaması + çekirdek kütüphane.

Tek bir ürün linkinden ürünün bilgilerini (ad, görseller, fiyatlar, satıcı ve puanı, diğer satıcılar, varyantlar, kargo, kampanya, yorum...) okuyup tek bir `UrunBilgiModel` döner. Faz 1: **Trendyol, Hepsiburada, N11**.

```csharp
using static KursoftRekabet.UrunBilgiGetir;

UrunBilgiModel sonuc = await trendyolurunbilgigetir(link);
UrunBilgiModel sonuc = await hepsiburadaurunbilgigetir(link);
UrunBilgiModel sonuc = await n11urunbilgigetir(link);
UrunBilgiModel sonuc = await urunbilgigetir(link);                         // platformu linkten bulur
UrunBilgiModel sonuc = await urunbilgigetir(link, Globals.ModulType.n11);  // platform açıkça
```

Bu fonksiyonlar **asla exception fırlatmaz**. Hata olduğunda `Basarili = false`, `HataKodu` ve `HataMesaji` dolu bir model döner.

---

## Proje yapısı

```
KursoftRekabet.sln
├─ KursoftRekabet.Cekirdek/        class library (net8.0)
│   ├─ Globals.cs                  ModulType, hata kodları, veri kaynakları, yollar, CalismaAyarlari
│   ├─ Logislemleri.cs             Tüm loglama (Detay/Bilgi/Uyari/Hata/Kritik)
│   ├─ UrunBilgiGetir.cs           Public giriş fonksiyonları
│   ├─ UrunBilgiAkisi.cs           Bir linkin baştan sona akışı (doğrula, hız sınırı, topla, ayrıştır, tekrar dene)
│   ├─ model/                      UrunBilgiModel, SaticiModel, GorselModel, VaryantModel, OzellikModel, HamSayfaVerisi
│   ├─ Tarayici/                   TarayiciYonetimi, AgYakalayici, SayfaVerisiOkuyucu
│   ├─ Ayristirma/                 SeciciAyarlari, AlanOkuyucu, OrtakAlanlar, DurumTespiti
│   ├─ Yardimcilar/                FiyatCevir, JsonYolu, LinkYardimcisi, HizSiniri, Tanilama
│   ├─ Trendyol/trendyolUrunBilgi.cs
│   ├─ Hepsiburada/hepsiburadaUrunBilgi.cs
│   └─ n11/n11UrunBilgi.cs
├─ KursoftRekabet.Gorev/           console exe (zamanlanmış görev bunu çalıştırır)
│   ├─ Program.cs                  Akış, komut satırı, exit code, SonucuIsle (ÖZELLEŞTİRME NOKTASI)
│   ├─ DbIslemleri.cs              Linkleri oku / sonuç ve log yaz (düz ADO.NET, parametreli)
│   ├─ CalismaOzeti.cs, GorevAyarlari.cs
│   └─ appsettings.json
├─ KursoftRekabet.Test/            xUnit, internetsiz testler
│   └─ Ornekler/{platform}/{ornek}/   kayıtlı sayfa verileri (Tanilama klasörüyle aynı format)
├─ sql/                            RekabetSonuc, RekabetIslemLog, RekabetCalismaLog CREATE script'leri
└─ secici-ayarlari.json            platform başına veri yolları (exe yanına kopyalanır)
```

### Mimari: toplama ile ayrıştırma ayrı

1. **Toplama (tarayıcı):** `SayfaVerisiOkuyucu` sayfayı açar, ürün verisinin gelmesini bekler ve **ham veriyi** `HamSayfaVerisi`'ne toplar. Ham veri şunlardan oluşur: ağ JSON cevapları, window global'leri, script JSON'ları, ld+json, HTML ve diğer satıcı sayfaları.
2. **Ayrıştırma (tarayıcısız):** `AlanOkuyucu`, `secici-ayarlari.json`'daki aday yolları sırayla dener ve modeli doldurur. Platform dosyası (`trendyolUrunBilgi.cs` vb.) yalnızca o platforma özel düzeltmeleri yapar.

Ayrıştırma kayıtlı dosyalarla da çalışır. Bu yüzden testler internetsiz koşar ve `--yenidenayristir` ile bir tanılama klasörü siteye gitmeden yeniden denenebilir.

**Yeni platform eklemek** üç adımdır:
1. `Globals.ModulType`'a bir değer ekleyin (Pazarama, Çiçeksepeti, İdefix, Amazon TR ve Koçtaş zaten tanımlı).
2. `secici-ayarlari.json`'a bir bölüm ekleyin.
3. Bir klasör açıp içine `xxxUrunBilgi.cs` koyun (`Getir` + `Ayristir`) ve `UrunBilgiGetir.cs`'e bir satır ekleyin.

---

## Kurulum

Gereksinim: Windows Server 2016+ / Windows 10+ x64. Self-contained publish'te .NET kurulu olması gerekmez.

### 1. Derleme / publish

```bash
dotnet publish KursoftRekabet.Gorev -c Release -r win-x64 --self-contained true -o C:\KursoftRekabet
```

Komut tek klasöre yayınlar. `C:\KursoftRekabet` içinde `KursoftRekabet.Gorev.exe`, `appsettings.json`, `secici-ayarlari.json` ve `.playwright\` (Playwright sürücüsü) bulunur.

### 2. Chromium kurulumu (bir kere)

```bash
C:\KursoftRekabet\KursoftRekabet.Gorev.exe --kurulum
```

Chromium kullanıcı profiline değil **exe'nin yanındaki `tarayicilar\` klasörüne** kurulur (`PLAYWRIGHT_BROWSERS_PATH`). Bu sayede SYSTEM ya da başka bir hesapla çalışan görev de aynı tarayıcıyı bulur. İnternetsiz bir sunucuya kurulum yapacaksanız, başka bir makinede oluşan `tarayicilar\` klasörünü kopyalamanız yeterli. Normal çalışmada Chromium bulunamazsa uygulama kurulumu kendisi dener.

### 3. Veritabanı

`sql/` altındaki üç script'i çalıştırın: `RekabetSonuc`, `RekabetIslemLog`, `RekabetCalismaLog`. Script'ler sadece yeni tablo oluşturur, mevcut tablolarınıza ALTER yapmaz.

Gerçek bağlantı cümlesini `appsettings.Local.json` dosyasına yazabilirsiniz. Bu dosya `appsettings.json`'ın üzerine birleşir ve repoya girmez:

```json
{ "BaglantiCumlesi": "Server=SQL01;Database=KURSOFT;User ID=rekabet;Password=...;TrustServerCertificate=True;" }
```

### 4. Zamanlanmış Görev

Görevi "kullanıcı oturum açmasa da çalıştır" ve en yüksek yetki ayarıyla, SYSTEM hesabında kurmak için:

```bash
schtasks /Create /TN "Kursoft\KursoftRekabet" /TR "\"C:\KursoftRekabet\KursoftRekabet.Gorev.exe\"" /SC HOURLY /MO 2 /RU "SYSTEM" /RL HIGHEST /F
```

Ayrı bir servis hesabıyla, "kullanıcı oturum açmasa da çalıştır" seçeneği ve başlangıç klasörüyle kurmak için (PowerShell):

```powershell
$a = New-ScheduledTaskAction -Execute "C:\KursoftRekabet\KursoftRekabet.Gorev.exe" -WorkingDirectory "C:\KursoftRekabet"
$t = New-ScheduledTaskTrigger -Daily -At 03:00
$s = New-ScheduledTaskSettingsSet -ExecutionTimeLimit (New-TimeSpan -Hours 3) -MultipleInstances IgnoreNew
Register-ScheduledTask -TaskName "Kursoft\KursoftRekabet" -Action $a -Trigger $t -Settings $s -User "DOMAIN\svc_rekabet" -Password "..." -RunLevel Highest
```

Notlar:
- **Başlangıç klasörü:** Uygulama her yolu `AppContext.BaseDirectory`'ye göre kurar. Görev `C:\Windows\System32`'den başlasa bile sorun olmaz; başlangıç klasörünü vermek yine de iyi bir alışkanlıktır.
- **SYSTEM ve SQL:** SYSTEM hesabında `Integrated Security=True` kullanılırsa SQL Server'a makine hesabıyla (`DOMAIN\MAKINE$`) bağlanılır.
- **Süre sınırı:** `ToplamZamanAsimiDk`, görevin `ExecutionTimeLimit` değerinden küçük olmalı. Böylece uygulama özet log'u yazıp kendisi kapanır.

---

## Komut satırı

| Komut | Ne yapar |
|---|---|
| *(argümansız)* | Linkleri DB'den okur (`LinkSorgusu`), işler, her sonucu `SonucuIsle`'ye verir, özet yazar |
| `--link "https://..."` | Tek link çalıştırır. Sonucu girintili JSON olarak `Logs\sonuc_*.json`'a yazar ve konsola basar. **DB'ye dokunmaz.** |
| `--tanila "https://..."` | `--link` gibi çalışır, ek olarak `Detay` log'u açar ve tüm ham veriyi `Logs\Tanilama\...` altına kaydeder |
| `--yenidenayristir "klasör"` | Kayıtlı tanılama klasörünü **siteye gitmeden** yeniden ayrıştırır (seçici denemek için) |
| `--kurulum` | Sadece Chromium'u kurar ya da kontrol eder |

### Exit code'lar (Görev Zamanlayıcı geçmişinde "Son çalıştırma sonucu")

| Kod | Anlamı |
|---|---|
| 0 | Hepsi başarılı (ya da işlenecek link yok) |
| 1 | Bazıları başarısız |
| 2 | Hepsi başarısız |
| 3 | Başka kopya çalışıyor (global mutex) |
| 4 | Ayar / DB hatası (appsettings, secici-ayarlari, linkler okunamadı, hatalı argüman) |
| 5 | Tarayıcı başlatılamadı / kurulamadı |

### Hata kodları (`UrunBilgiModel.HataKodu`)

| Kod | Anlamı | Tekrar denenir mi |
|---|---|---|
| `GECERSIZ_LINK` | Alan adı, ürün linki deseni ya da kısa link yönlendirmesi geçersiz | Hayır |
| `BULUNAMADI` | 404/410 ya da "sayfa bulunamadı" | Hayır |
| `KALDIRILMIS` | Ürün satıştan kaldırılmış | Hayır |
| `ENGEL` | Bot koruması (403/429, CAPTCHA, Cloudflare...). **Atlatılmaya çalışılmaz**, o platformun bekleme süresi katlanır | Hayır |
| `ZAMAN_ASIMI` | Link süresi ya da toplam süre doldu | Link süresi dolduysa evet |
| `VERI_YAPISI_DEGISTI` | Zorunlu alanlar (UrunAdi, SatisFiyati, Satici.SaticiAdi) hiçbir kaynaktan okunamadı. **"Seçici güncelle" alarmı** | Hayır |
| `BILINMEYEN` | Ağ hatası, tarayıcı çökmesi vb. | Evet |

Tükenmiş üründe fiyat olmayabilir. Bu durumda `SatisFiyati` zorunlu sayılmaz ve sonuç başarılı döner; `Uyarilar`'a "ürün tükenmiş" uyarısı eklenir.

---

## `appsettings.json` alanları

| Alan | Varsayılan | Açıklama |
|---|---|---|
| `BaglantiCumlesi` | | SQL Server bağlantısı. **Log'a asla yazılmaz.** |
| `LinkSorgusu` | | SQL metni ya da (boşluk içermiyorsa) SP adı. En az `ID`, `Link` kolonları; `ModulType` gelirse ona öncelik verilir |
| `SonucTabloAdi` | `RekabetSonuc` | `SonucuIsle`'nin varsayılan INSERT'ü bu tabloya yapılır |
| `DbKomutZamanAsimiSn` | 60 | SQL komut zaman aşımı |
| `DbLogAcik` | true | `RekabetIslemLog` / `RekabetCalismaLog` yazılsın mı. Yazılamazsa uygulama durmaz |
| `MaksimumEszamanliSayfa` | 2 | Aynı anda açık sayfa sayısı |
| `TekrarDenemeSayisi` | 2 | Geçici hatalarda ek deneme sayısı. Bekleme her denemede artar |
| `TekrarBeklemeMs` | 3000 | Denemeler arası temel bekleme (deneme no ile çarpılır) |
| `LinkZamanAsimiSn` | 90 | Link başına toplam süre |
| `SayfaYuklemeZamanAsimiSn` | 45 | Sayfa açılış zaman aşımı |
| `VeriBeklemeSn` | 15 | Ürün verisinin (JSON/global/seçici) gelmesi için en fazla bekleme |
| `ToplamZamanAsimiDk` | 120 | Exe'nin tamamı için üst sınır. Dolunca kalan linkler `ZAMAN_ASIMI` ile yazılır |
| `PlatformBeklemeMs` | `{trendyol:3000,...}` | Platform başına istekler arası bekleme (hız sınırı) |
| `VarsayilanPlatformBeklemeMs` | 3000 | Listede olmayan platformlar için bekleme |
| `MaksimumEngelCarpani` | 16 | ENGEL geldikçe bekleme 2x, 4x... en fazla bu kat. Başarılı isteklerle normale döner |
| `MedyaEngelle` | true | Görsel/font/medya isteklerini engelleyerek hızlandırır. Veriyi bozuyorsa `false` yapın (platform bazında `secici-ayarlari.json` → `MedyaEngelle`) |
| `HamJsonDondur` | false | `true` ise model `HamJson` alanını doldurur (DB'yi şişirir) |
| `DetayLog` | false | Adım adım her alanın hangi kaynaktan okunduğunu loglar |
| `HatadaTanilamaKaydet` | true | Başarısız işlemlerde ham veriyi Tanilama klasörüne kaydeder (`VERI_YAPISI_DEGISTI` her zaman kaydedilir) |
| `LogSaklamaGun` | 30 | Bu süreden eski log ve tanılama klasörleri silinir |
| `LogDosyaMaksimumMb` | 50 | Dosya bu boyutu geçince `genel_2.log` ... ile devam edilir |
| `Headless` | true | Tarayıcı görünmez çalışır |
| `TarayiciYolu` | | Boşsa `tarayicilar\` içindeki Chromium kullanılır |
| `KullaniciAjani` | | Boşsa tarayıcının kendi user-agent'ı kullanılır |
| `MaksimumDigerSaticiSayfasi` | 20 | Diğer satıcılar sayfalamasında üst sınır (sonsuz döngü koruması) |

---

## Loglama

```
Logs\
├─ 2026-11-26\
│   ├─ genel.log        herkes, düz metin
│   ├─ genel.jsonl      herkes, her satır JSON
│   ├─ trendyol.log     sadece o platform
│   ├─ hepsiburada.log
│   ├─ n11.log
│   ├─ hatalar.log      Uyari ve üstü (yığın izi dahil)
│   └─ hatalar.jsonl
├─ Tanilama\{platform}\{yyyyMMdd_HHmmss}_{IslemID}\   hata anındaki ham veri
└─ sonuc_*.json         --link / --tanila sonuçları
```

Her satırda şu bilgiler bulunur: zaman (ms hassasiyetinde), seviye, `CalismaID`, `IslemID`, `ModulType`, `Link`, `Adim`, `SureMs`, makine adı, process id ve sürüm.

Bir link için adımlar sırasıyla şöyle akar: `BASLADI` → `SAYFA_ACILDI` (süre, HTTP durumu) → `AG_JSON_YAKALANDI` / `VERI_HAZIR` → `KAYNAK_DENENDI` / `ALAN_OKUNDU` (Detay) → `AYRISTIRILDI` (alan kaynakları, eksik zorunlu alanlar) → `BITTI` (toplam süre).

Exe kapanırken `CALISMA_OZETI` yazılır: toplam, başarılı, başarısız, hata koduna göre dağılım, platform başına ortalama süre ve en yavaş 5 link. `DbLogAcik` açıksa aynı özet `RekabetCalismaLog` tablosuna da gider.

**5 dakikada teşhis:** `hatalar.log`'a bakın. `KRITIK VERI_YAPISI_DEGISTI` görüyorsanız site değişmiştir (aşağıdaki bölüme geçin). `ENGEL` görüyorsanız IP engellenmiştir. Ayrıntı için ilgili `IslemID`'yi `genel.jsonl`'de aratın.

---

## Site değişti, ne yapmalıyım?

1. `hatalar.log`'da `KRITIK VERI_YAPISI_DEGISTI` satırını ve tanılama klasörünü bulun. Ya da linki elle çalıştırın:
   ```bash
   KursoftRekabet.Gorev.exe --tanila "https://www.trendyol.com/...-p-123"
   ```
2. `Logs\Tanilama\trendyol\..._xxxx\` klasörünü açın:
   - `ozet.json`: HTTP durumu, sayfa başlığı, **sitenin eklediği global'lerin listesi** (`globalListesi`: ad, boyut, ürün verisi var mı) ve yakalanan ağ cevaplarının özeti
   - `ag\*.json`, `globaller\*.json`, `scriptler\*.json`, `ldjson\*.json`: ham JSON'lar (girintili)
   - `sayfa.html`, `ekran.png`, `sonuc.json`
3. Aradığınız değeri (ör. fiyat `1299.9`) bu dosyalarda arayın ve yolunu çıkarın (ör. `product.merchantListing.winnerVariant.price.sellingPrice.value`).
4. Yolu `secici-ayarlari.json`'da ilgili alanın aday listesinin **başına** ekleyin. Eski adaylar yerinde kalabilir; sırayla denenir. Global'in adı değiştiyse `Kaynaklar` bölümünde güncelleyin.
5. Siteye gitmeden deneyin:
   ```bash
   KursoftRekabet.Gorev.exe --yenidenayristir "Logs\Tanilama\trendyol\..._xxxx"
   ```
6. Tutuyorsa exe yanındaki `secici-ayarlari.json`'u güncelleyin. **Yeniden derleme gerekmez.** Tanılama klasörünü `KursoftRekabet.Test\Ornekler\trendyol\` altına kopyalarsanız test olarak da kalır.

JSON yolu yazımı: `a.b.c`, `a[0]`, `a[-1]` (son eleman), `a[*]`, `a[?selected=true]`, `a[?key.name=Renk]`, `..derinArama`, `a['ozel-ad']`. HTML adaylarında `Secici` (CSS) ve `Nitelik` (`""` metin, `"html"`, `"var"`, ya da nitelik adı) kullanılır. Ayrıntılar dosyanın başındaki `_aciklama` bölümünde.

---

## Veri kaynakları (platform başına)

Okuma sırası: **AG_JSON → WINDOW_GLOBAL → SCRIPT_JSON → LD_JSON → HTML**. Bir alan ilk değer veren kaynaktan alınır, eksik alanlar sonraki kaynaklardan tamamlanır. Her alanın gerçekte nereden okunduğu `UrunBilgiModel.AlanKaynaklari`'nda yazar.

| Alan | Trendyol | Hepsiburada | N11 |
|---|---|---|---|
| Ana ürün nesnesi | `window.__envoy__PROPS` → `product` (içinde `merchantListing`); yoksa `merchantListing` geçen ağ JSON'u ya da site global'i; en son eski `__PRODUCT_DETAIL_APP_INITIAL_STATE__` | `script#reduxStore` → `productState.product` | `__NEXT_DATA__` / ağ JSON'u (varsa), yoksa ld+json + HTML |
| Ad, marka | ürün JSON'u → ld+json → HTML | redux → ld+json → HTML | ld+json → HTML (`h1.proName`) |
| Satış / liste fiyatı | `merchantListing.winnerVariant.price.*` | `listings[0].prices[0]` | ld+json `offers.price` → HTML (`div.newPrice ins`, `div.oldPrice del`) |
| Sepette fiyat | `price.basketPrice` (adaydır) | **sadece HTML** (`[data-test-id=checkout-price]`) | HTML (adaydır) |
| Satıcı, puan | `merchantListing.merchant.name`, `.sellerScore.value` (10'luk) | `merchant.name` / `listings[0].merchantName`, `lifetimeRating` (10'luk) | HTML `a.unf-p-seller-name`, `div.shopPoint span.point` |
| Diğer satıcılar | `merchantListing.otherMerchants` (aynı JSON) | `product.listings` (aynı JSON) | "Diğer satıcılar" sekmesine tıklanır, sayfa sayfa dolaşılır (`EkHtmlSayfalari`) |
| Görseller | `images[]` (göreli, `cdn.dsmcdn.com` eklenir) | `images[].link` (`{size}` yer tutucusu 1500/200 ile doldurulur) | ld+json `image` → HTML |
| Varyantlar | `variants` / `allVariants` | `variantList` | HTML (adaydır) |
| Puan / yorum | `ratingScore.*` → ld+json `aggregateRating` | `rating`, `reviewCount` → ld+json | ld+json `aggregateRating` |
| Tükendi | HTML `button.sold-out` + JSON `inStock` | `isInStock` / `isOutOfStock` | ld+json `availability` + HTML |

---

## Testler

```bash
dotnet test
```

Testler internetsiz çalışır ve kayıtlı sayfa verilerini kullanır. Kapsadıkları:
- **Fiyat çevirme:** 15+ farklı biçim, aralık fiyat ve para birimi.
- **JSON yolu çözücü.**
- **Link doğrulama ve normalize etme.**
- **Platform başına örnekler:** normal, çok satıcılı, varyantlı, tükenmiş, engel ve yapı değişti.
- **Toplu genel kontrol:** `Ornekler\` altındaki **her klasör** için zorunlu alanlar, fiyat, görseller ve satıcı sayısı.

**Gerçek örnek eklemek:** `--tanila` ile oluşan klasörü `KursoftRekabet.Test\Ornekler\{platform}\{ad}\` altına kopyalayın. Toplu kontrol onu otomatik olarak test eder. Adı `engel`, `yapi_degisti`, `kaldirilmis` ya da `bulunamadi` içeren klasörlerde hata beklenir.

---

## Bilinen sınırlar

- **Canlı doğrulama yapılamadı.** Bu sürüm bir bulut sunucusunda geliştirildi. Trendyol ("blocked malicious bot"), Hepsiburada ("Güvenlik" sayfası) ve N11 (Cloudflare) o sunucunun IP'sini engelledi. Kurallar gereği engel atlatılmaya çalışılmadı.
  - Veri yolları eski projeden (`__envoy__PROPS`, `merchantListing`, `reduxStore`, `listings`, N11 HTML seçicileri) ve bu sitelerin bilinen yapılarından alındı.
  - `Ornekler\` altındaki örnekler **sentetiktir** (`sentetik_` önekli, `ozet.json` içinde not var).
  - İlk iş olarak Türkiye'deki bir makinede her platform için `--tanila` çalıştırın, `--yenidenayristir` ile kontrol edin ve gerekirse `secici-ayarlari.json`'u güncelleyin.
  - Dosyada `DOGRULANMADI` notu olan bölümler henüz canlı sayfada görülmedi.
- **Veri merkezi IP'leri** üç platformda da engelleniyor. Uygulama ofis ya da müşteri sunucusu gibi normal bir IP'den çalışmalı. Engel gelince `ENGEL` döner ve bekleme süresi katlanır.
- **Hepsiburada sepette fiyatı** sadece HTML'de bulunuyor (eski projede de böyleydi). HTML değişirse bu alan `null` kalır, diğer alanlar etkilenmez.
- **Trendyol diğer satıcılar** yalnızca `merchantListing.otherMerchants` içinde gelenlerden okunur. Trendyol bu listeyi ileride sayfalarsa `DigerSaticiSayfasi` ayarıyla genişletilmesi gerekir.
- **N11 satıcı puanı ölçeği** 100 kabul edildi (`PuanOlcegi`). Doğrulanmadı; gerekirse `secici-ayarlari.json`'dan değiştirin.
- **Ham JSON modelleri (json2csharp tarzı `Root`/`Content`/`Variant` sınıfları) bilerek yazılmadı.** Yollar "koda gömülmesin, yeniden derlemeden güncellenebilsin" kuralı gereği `secici-ayarlari.json`'da duruyor. Tipli ham model, yapıyı yeniden koda gömmek anlamına gelirdi. Ham veri `JToken` üzerinden okunur, ortak model (`UrunBilgiModel`) ayrıdır.
- **Window global taraması** döngüsel ve tekrar eden nesne referanslarını atlar (JSON'a çevrilebilmesi için). Aynı nesneye iki yerden referans veren yapılarda ikinci kopya boş gelir.
- **Görev Zamanlayıcı'dan "Sonlandır"** süreci doğrudan öldürür (`TerminateProcess`). Bu durumda özet log yazılamaz. Ctrl+C ve normal kapanış sinyallerinde yarım işler bırakılır ve özet yazılır.
- **DB kodu** (`DbIslemleri`) gerçek bir SQL Server'a karşı çalıştırılmadı. Script'lerdeki kolonlar INSERT'lerle birebir aynı tutuldu; ilk kurulumda bir `--link` ve bir normal çalışma ile kontrol edin.
