# KURSOFT API — Örnek İstemci (Swagger UI)

`docs/` klasöründeki sekiz endpoint'i (Sipariş Oluştur, Sipariş Listesi, Ödeme Oluştur, Müşteri Listesi, Cari İşlem Takibi, Ürün Ekle, Stok Güncelle, Stok Fiyat Güncelle) tarayıcıdan tıklayarak deneyebileceğiniz, Swagger UI ile açılan bir ASP.NET Core Web API.

Bu proje "sahte" bir API değildir — her `/demo/*` rotası, arka planda gerçek KURSOFT API'sine bir istek atar ve yanıtı olduğu gibi size geri döndürür. Yani Postman'e alternatif, tarayıcı içinde çalışan interaktif bir test aracıdır.

Tek bağımlılığı **Swashbuckle.AspNetCore** (Swagger UI için). `HttpClient` ve `System.Text.Json` .NET'in kendi kütüphaneleridir.

## Kurulum

1. [.NET 8 SDK](https://dotnet.microsoft.com/download) kurulu olmalı.

2. Bu klasörde `appsettings.Local.json` adında yeni bir dosya oluşturun (bu dosya `.gitignore`'da olduğu için asla commit edilmez):

   ```json
   {
     "BaseUrl": "https://test-api.kursoft.com.tr",
     "Username": "kendi-kullanici-adiniz",
     "Password": "kendi-sifreniz"
   }
   ```

   **Alternatif — ortam değişkenleri:**

   ```bash
   export KURSOFT_BASEURL="https://test-api.kursoft.com.tr"
   export KURSOFT_USERNAME="kendi-kullanici-adiniz"
   export KURSOFT_PASSWORD="kendi-sifreniz"
   ```

   Windows PowerShell:
   ```powershell
   $env:KURSOFT_BASEURL="https://test-api.kursoft.com.tr"
   $env:KURSOFT_USERNAME="kendi-kullanici-adiniz"
   $env:KURSOFT_PASSWORD="kendi-sifreniz"
   ```

   > ⚠️ `appsettings.json` dosyası repo'da boş placeholder olarak durur ve commit edilmesi güvenlidir. Gerçek bilgilerinizi **asla** bu dosyaya yazmayın.

   Bu adımı atlarsanız proje yine de açılır (Swagger UI çöker demez), ancak her istek "Yapılandırma eksik." hatası döner — hangi bilgilerin eksik olduğunu açıklayan bir mesajla.

3. Çalıştırın:

   ```bash
   cd samples/dotnet-client/KursoftApiClient
   dotnet run
   ```

   Tarayıcı otomatik olarak `http://localhost:5080/swagger` adresini açar. Açılmazsa adresi elle girin.

## Nasıl kullanılır?

Swagger UI'da endpoint'ler dokümanlardaki gibi dört grupta listelenir: **1. Sipariş**, **2. Ödeme**, **3. Müşteri**, **4. Ürün**. Herhangi birinin üzerine tıklayıp **"Try it out"** ile örnek gövdeyi düzenleyip **"Execute"** dediğinizde, istek gerçekten KURSOFT API'nize gider ve ham yanıtı ekranda görürsünüz — tarayıcı konsolunu veya Postman'i açmanıza gerek kalmaz.

| Grup | Rota | Karşılık geldiği gerçek endpoint |
|---|---|---|
| 1. Sipariş | `POST /demo/create-order` | `POST /api/v2/Order/CreateOrder` |
| 1. Sipariş | `POST /demo/order-list` | `POST /api/v2/Order/OrderList` |
| 2. Ödeme | `POST /demo/payment-create` | `POST /api/v2/Payment/PaymentCreate` |
| 3. Müşteri | `POST /demo/customer-list` | `POST /api/v2/Customer/Customerlist` |
| 3. Müşteri | `POST /demo/transaction-history` | `POST /api/v2/Customer/TransactionHistory` |
| 4. Ürün | `POST /demo/product-create` | `POST /api/v2/Product/CreateProduct` |
| 4. Ürün | `POST /demo/stock-update` | `POST /api/v2/Product/StockUpdate` |
| 4. Ürün | `POST /demo/stock-price-update` | `POST /api/v2/Product/StockPriceUpdate` |

## Proje yapısı

```
KursoftApiClient/
├── Program.cs                    # Swagger + 8 demo rotası (minimal API)
├── appsettings.json               # Boş placeholder, commit edilmesi güvenli
├── Properties/
│   └── launchSettings.json        # dotnet run ile doğrudan /swagger açılmasını sağlar
├── Configuration/
│   └── ApiSettings.cs             # BaseUrl/Username/Password'ü güvenli şekilde okur
├── Http/
│   └── KursoftApiService.cs       # HttpClient sarmalayıcısı, auth header'larını otomatik ekler
└── Models/
    ├── CreateOrderModels.cs
    ├── OrderListModels.cs
    ├── PaymentModels.cs
    ├── CustomerModels.cs          # Customerlist + TransactionHistory
    └── ProductModels.cs           # CreateProduct + StockUpdate + StockPriceUpdate
```

## Kendi projenize entegre etmek isterseniz

`Http/KursoftApiService.cs` ve `Models/` klasörünü doğrudan kendi projenize kopyalayıp kullanabilirsiniz — `KursoftApiService`'in kendisi Swagger'a veya minimal API'ye bağımlı değildir, `Program.cs`'teki `/demo/*` rotaları yalnızca bu servisi bir web arayüzünden çağırmanın örneğidir. Kendi DI (Dependency Injection) container'ınıza da kolayca eklenebilir:

```csharp
builder.Services.AddSingleton(ApiSettings.Load(AppContext.BaseDirectory));
builder.Services.AddSingleton<KursoftApiService>();
```

## Notlar

- `CreateOrderRequest.SendInvoice` alanı JSON'a giderken küçük harfle (`sendInvoice`) gönderilir — `docs/CreateOrder_API_Dokumani.docx` dosyasında belgelenen alan adıyla birebir eşleşmesi içindir.
- `KursoftApiService`, hem JSON gövdeli hem düz metin gövdeli hata yanıtlarını (bkz. her dokümanın "Hata Referansı" bölümü) tek bir `KursoftApiException` tipinde toplar; gerçek API'nin döndürdüğü HTTP durum kodu ve ham gövde `/demo/*` rotalarından da olduğu gibi yansıtılır.
- `StockUpdate` ve `StockPriceUpdate` satır bazlı hata döndürür (dizi elemanlarının bir kısmı başarısız olsa bile HTTP 200 gelebilir) — `docs/Product_API_Dokumani.docx` bölüm 3.4 ve 4.4'teki notlara bakınız.
- `StockPriceUpdate`, dokümanda da belirtildiği gibi şu an yalnızca varyantsız ürünlerde çalışır.
