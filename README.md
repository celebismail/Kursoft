<p align="center">
  <h1 align="center">KURSOFT ERP Entegrasyon API'leri</h1>
  <p align="center">Sipariş, ödeme ve müşteri verilerinizi ERP sistemimizle entegre etmek için referans API dokümanları ve Postman koleksiyonu.</p>
</p>

<p align="center">
  <a href="#hızlı-başlangıç"><b>Hızlı Başlangıç</b></a> ·
  <a href="#endpointler"><b>Endpoint'ler</b></a> ·
  <a href="#dokümanlar"><b>Dokümanlar</b></a> ·
  <a href="#destek"><b>Destek</b></a>
</p>

---

## Genel Bakış

Bu repo, **KURSOFT** ERP sistemine entegre olmak isteyen partnerler ve geliştiriciler için:

- Her endpoint'in alan/tip/zorunluluk açıklamalarını içeren **referans dokümanları** (`docs/`)
- Uçtan uca test edilebilir, örnek request/response'larla hazır **Postman koleksiyonu** (`postman/`)

sağlar. Amaç, entegrasyon sürecini dokümanı satır satır okuyup deneme-yanılmayla kod yazmak yerine, çalışan örnekler üzerinden hızlandırmaktır.

> **Not:** Bu repo aktif geliştirilmektedir. Yeni endpoint'ler ve alanlar eklendikçe hem dokümanlar hem koleksiyon güncellenecektir. Sürüm geçmişi için bkz. [Sürüm Notları](#sürüm-notları).

## Hızlı Başlangıç

1. **Postman'i açın** ve `postman/ERP_API.postman_collection.json` dosyasını import edin
   (Postman → *Import* → dosyayı sürükleyin)
2. `postman/environments/Test.postman_environment.json` dosyasını da import edin ve sağ üstten aktif environment olarak seçin
3. Environment içindeki değişkenleri doldurun:

   | Değişken | Açıklama |
   |---|---|
   | `baseURL` | Size iletilen test veya canlı API adresi |
   | `apiUsername` | Entegrasyon kullanıcı adınız |
   | `apiPassword` | Entegrasyon şifreniz |

   > ⚠️ `apiUsername` / `apiPassword` alanları `secret` tipindedir — bu değerleri **asla** repo'ya commit etmeyin, sadece kendi Postman environment'ınızda saklayın.

4. Koleksiyondaki klasörlerden istediğiniz isteği açıp **Send** ile deneyin. Her isteğin altında kayıtlı başarılı/hatalı örnek yanıtları da inceleyebilirsiniz (*Examples* sekmesi).

## Endpoint'ler

| Klasör | Endpoint | Açıklama |
|---|---|---|
| 1. Sipariş | `POST /api/v2/Order/CreateOrder` | Yeni satış siparişi oluşturur, opsiyonel olarak e-fatura akışını tetikler. |
| 1. Sipariş | `POST /api/v2/Order/OrderList` | Sipariş listesini opsiyonel sipariş/fatura numarası filtresiyle döndürür. |
| 2. Ödeme | `POST /api/v2/Payment/PaymentCreate` | Nakit tahsilat, ödeme, havale/EFT gibi nakit hareket kayıtları oluşturur. |
| 3. Müşteri | `POST /api/v2/Customer/Customerlist` | Filtrelenmiş ve sıralanmış müşteri (cari) listesini döndürür. |
| 3. Müşteri | `POST /api/v2/Customer/TransactionHistory` | Bir carinin işlem dökümünü ve kümülatif bakiyesini döndürür. |
| 4. Ürün | `POST /api/v2/Product/CreateProduct` | Yeni ürün/stok kartı oluşturur. |
| 4. Ürün | `POST /api/v2/Product/StockUpdate` | Barkoda göre stok mevcudunu toplu günceller (en fazla 100 kayıt). |
| 4. Ürün | `POST /api/v2/Product/StockPriceUpdate` | Barkoda göre fiyat kademelerini toplu günceller (en fazla 100 kayıt). |
| 4. Ürün | `POST /api/v2/Product/ProductList` | Ürün/varyant listesini sayfalı ve filtreli olarak döndürür. |
| 4. Ürün | `POST /api/v2/Product/StockTransactionHistory` | Bir ürünün stok hareket dökümünü ve kümülatif bakiyesini döndürür. |

Her endpoint'in tam alan listesi, enum değerleri ve hata referansı için `docs/` klasöründeki ilgili Word dokümanına bakınız.

## Dokümanlar

| Dosya | İçerik |
|---|---|
| `docs/CreateOrder_API_Dokumani.docx` | Sipariş oluşturma — istek/yanıt alanları, örnek istekler, hata referansı |
| `docs/OrderList_API_Dokumani.docx` | Sipariş listesi — sorgu parametresi, yanıt alanları, hata referansı |
| `docs/PaymentCreate_API_Dokumani.docx` | Ödeme/nakit hareket oluşturma — işlem türü (enum) referansı dahil |
| `docs/Customerlist_API_Dokumani.docx` | Müşteri listesi — filtre ve sıralama alanları, OrderBy beyaz listesi |
| `docs/TransactionHistory_API_Dokumani.docx` | Cari işlem takibi — filtre enum'u, devreden bakiye mantığı, kümülatif bakiye hesaplama |
| `docs/Product_API_Dokumani.docx` | Ürün ekleme, stok/fiyat güncelleme, ürün listesi, stok işlem takibi (5 endpoint tek dokümanda) |

## Repo Yapısı

```
.
├── docs/                                  # Word formatında API referans dokümanları
│   ├── CreateOrder_API_Dokumani.docx
│   ├── OrderList_API_Dokumani.docx
│   ├── PaymentCreate_API_Dokumani.docx
│   └── Customerlist_API_Dokumani.docx
├── postman/
│   ├── ERP_API.postman_collection.json    # Ana koleksiyon (endpoint'ler, örnek response'larla)
│   └── environments/
│       └── Test.postman_environment.json  # baseURL / kullanıcı bilgisi şablonu
└── samples/
    └── dotnet-client/                     # Çalışan .NET 8 örnek istemci (4 endpoint)
        ├── README.md                      # Kurulum ve çalıştırma talimatları
        └── KursoftApiClient/
```

## Örnek İstemci (.NET — Swagger UI)

`samples/dotnet-client/` altında, dokümanlardaki sekiz endpoint'i çağıran çalışan bir ASP.NET Core 8 Web API bulunur. `dotnet run` ile başlattığınızda tarayıcıda Swagger UI açılır; her endpoint'i "Try it out" ile tıklayarak canlı test edebilirsiniz — Postman veya konsol çıktısı okumaya gerek kalmadan. Kurulum adımları için `samples/dotnet-client/README.md` dosyasına bakınız.

> ⚠️ Bu örnekte de aynı kural geçerlidir: `BaseUrl` / `Username` / `Password` bilgilerinizi asla repo'daki dosyalara yazıp commit etmeyin — `appsettings.Local.json` (git tarafından yoksayılır) ya da ortam değişkenlerini kullanın.

> ⚠️ Projeyi kendi bilgisayarınızda derleyip çalıştırdıktan sonra `git status` ile `bin/` ve `obj/` klasörlerinin izlenmediğinden emin olun. Yanlışlıkla commit ettiyseniz: `git rm -r --cached samples/dotnet-client/KursoftApiClient/bin samples/dotnet-client/KursoftApiClient/obj` ile git takibinden çıkarıp tekrar commit edin (`.gitignore` bu klasörleri zaten kapsıyor, sadece daha önce eklenmiş dosyaları geri almaz).

## Genel Kurallar

- Tüm isteklerde `Content-Type: application/json` ve `Username` / `Password` header'ları zorunludur.
- Aynı `OrderNumber` / `PaymentNumber` ile tekrar gönderilen istekler mükerrer kayıt oluşturmaz.
- Hata yanıtları endpoint'e göre farklılık gösterir: bazı endpoint'ler JSON gövdede `status:false` döner, bazıları düz metin gövdeyle HTTP 400 döner. Detaylar için ilgili dokümanın "Hata Referansı" bölümüne bakınız.

## Sürüm Notları

| Tarih | Değişiklik |
|---|---|
| 2026-08-28 | İlk sürüm: CreateOrder, PaymentCreate, Customerlist endpoint'leri ve Postman koleksiyonu eklendi. |
| 2026-08-28 | CreateOrder koleksiyonuna iki edge-case senaryosu eklendi: mükerrer sipariş (aynı OrderNumber) ve InvoiceAddress null hatası. |
| 2026-08-28 | Sipariş Listesi (OrderList) endpoint'i eklendi. ShippingAddress / BillingAddress / Items alt model detayları henüz eksik, ayrıca güncellenecek. |
| 2026-08-28 | OrderList sorgusundaki SQL injection açığı parametreli sorguya çevrildi; ShippingAddress / BillingAddress / Items alt model alanları tam olarak eklendi. |
| 2026-08-28 | 4 endpoint'i çağıran çalışan bir .NET 8 örnek istemci eklendi (`samples/dotnet-client/`). |
| 2026-08-28 | Cari İşlem Takibi (TransactionHistory) ve Ürün/Stok API'leri (CreateProduct, StockUpdate, StockPriceUpdate) eklendi. Örnek istemci konsol yerine Swagger UI ile açılan interaktif bir ASP.NET Core Web API'ye dönüştürüldü (8 endpoint). |
| 2026-09-03 | TransactionHistory'e StartDate/EndDate filtresi eklendi (tarih aralığı gönderilince Donem/Year satır filtresi devre dışı kalır). OrderList'e InvoiceNumer alanı eklendi, sipariş no eşleşmesi artık S_BELGENO'yu da kapsıyor. Customerlist'te CustomerCode artık tam eşleşme yapıyor. Ürün API'sine ProductList ve StockTransactionHistory endpoint'leri eklendi (toplam 10 endpoint). Örnek istemci ve Postman koleksiyonu bu değişikliklerle güncellendi. |

## Destek

Entegrasyon sırasında sorularınız için: **destek@kursoft.com.tr**

---

<p align="center"><sub>© 2026 KURSOFT. Bu doküman ve koleksiyon yalnızca yetkili entegrasyon partnerleri için hazırlanmıştır.</sub></p>
