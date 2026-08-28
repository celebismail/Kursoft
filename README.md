<p align="center">
  <!-- Logo eklemek için: <img src="docs/assets/logo.png" width="160" alt="KURSOFT logo" /> -->
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

Bu repo, KURSOFT ERP sistemine entegre olmak isteyen partnerler ve geliştiriciler için:

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
| 2. Ödeme | `POST /api/v2/Payment/PaymentCreate` | Nakit tahsilat, ödeme, havale/EFT gibi nakit hareket kayıtları oluşturur. |
| 3. Müşteri | `POST /api/v2/Customer/Customerlist` | Filtrelenmiş ve sıralanmış müşteri (cari) listesini döndürür. |

Her endpoint'in tam alan listesi, enum değerleri ve hata referansı için `docs/` klasöründeki ilgili Word dokümanına bakınız.

## Dokümanlar

| Dosya | İçerik |
|---|---|
| `docs/CreateOrder_API_Dokumani.docx` | Sipariş oluşturma — istek/yanıt alanları, örnek istekler, hata referansı |
| `docs/PaymentCreate_API_Dokumani.docx` | Ödeme/nakit hareket oluşturma — işlem türü (enum) referansı dahil |
| `docs/Customerlist_API_Dokumani.docx` | Müşteri listesi — filtre ve sıralama alanları, OrderBy beyaz listesi |

## Repo Yapısı

```
.
├── docs/                                  # Word formatında API referans dokümanları
│   ├── CreateOrder_API_Dokumani.docx
│   ├── PaymentCreate_API_Dokumani.docx
│   └── Customerlist_API_Dokumani.docx
└── postman/
    ├── ERP_API.postman_collection.json    # Ana koleksiyon (3 endpoint, örnek response'larla)
    └── environments/
        └── Test.postman_environment.json  # baseURL / kullanıcı bilgisi şablonu
```

## Genel Kurallar

- Tüm isteklerde `Content-Type: application/json` ve `Username` / `Password` header'ları zorunludur.
- Aynı `OrderNumber` / `PaymentNumber` ile tekrar gönderilen istekler mükerrer kayıt oluşturmaz.
- Hata yanıtları endpoint'e göre farklılık gösterir: bazı endpoint'ler JSON gövdede `status:false` döner, bazıları düz metin gövdeyle HTTP 400 döner. Detaylar için ilgili dokümanın "Hata Referansı" bölümüne bakınız.

## Sürüm Notları

| Tarih | Değişiklik |
|---|---|
| 2026-08-28 | İlk sürüm: CreateOrder, PaymentCreate, Customerlist endpoint'leri ve Postman koleksiyonu eklendi. |

## Destek

Entegrasyon sırasında sorularınız için: destek@kursoft.com.tr

---

<p align="center"><sub>© 2026 KURSOFT. Bu doküman ve koleksiyon yalnızca yetkili entegrasyon partnerleri için hazırlanmıştır.</sub></p>
