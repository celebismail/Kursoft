# KURSOFT API — Örnek .NET İstemci

`docs/` klasöründeki dört endpoint'i (Sipariş Oluştur, Sipariş Listesi, Ödeme Oluştur, Müşteri Listesi) çağıran, çalışan bir .NET 8 konsol uygulaması. Amaç, entegrasyona başlarken dokümanı satır satır okuyup deneme-yanılmayla kod yazmak yerine, kopyalayıp kendi ortamınıza uyarlayabileceğiniz bir başlangıç noktası vermek.

Hiçbir NuGet paketine bağımlı değildir — sadece .NET'in kendi `HttpClient` ve `System.Text.Json` kütüphanelerini kullanır.

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

   **Alternatif — ortam değişkenleri:** Dosya oluşturmak istemiyorsanız aynı bilgileri ortam değişkeni olarak da verebilirsiniz:

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

   > ⚠️ `appsettings.json` dosyası repo'da boş placeholder olarak durur ve commit edilmesi güvenlidir. Gerçek bilgilerinizi **asla** bu dosyaya yazmayın — her zaman `appsettings.Local.json` ya da ortam değişkenlerini kullanın.

3. Çalıştırın:

   ```bash
   cd samples/dotnet-client/KursoftApiClient
   dotnet run
   ```

## Ne yapıyor?

`Program.cs` sırasıyla dört adımı çalıştırır:

| Adım | Endpoint | Ne yapar |
|---|---|---|
| 1 | `POST /api/v2/Order/CreateOrder` | Benzersiz bir sipariş numarasıyla örnek bir bireysel müşteri siparişi oluşturur |
| 2 | `POST /api/v2/Order/OrderList` | Filtresiz çağrı ile son siparişleri listeler |
| 3 | `POST /api/v2/Payment/PaymentCreate` | Örnek bir nakit tahsilat kaydı oluşturur |
| 4 | `POST /api/v2/Customer/Customerlist` | İstanbul'daki aktif cariler için filtreli bir liste çeker |

Her adım birbirinden bağımsızdır — `Program.cs` içinde ilgilenmediğiniz adımın çağrısını yorum satırına alıp sadece istediğiniz endpoint'i test edebilirsiniz.

## Proje yapısı

```
KursoftApiClient/
├── Program.cs                    # Ana akış — 4 endpoint'i sırayla çağırır
├── appsettings.json               # Boş placeholder, commit edilmesi güvenli
├── Configuration/
│   └── ApiSettings.cs             # BaseUrl/Username/Password'ü güvenli şekilde okur
├── Http/
│   └── KursoftApiService.cs       # HttpClient sarmalayıcısı, auth header'larını otomatik ekler
└── Models/
    ├── CreateOrderModels.cs
    ├── OrderListModels.cs
    ├── PaymentModels.cs
    └── CustomerModels.cs
```

## Kendi projenize entegre etmek isterseniz

`Http/KursoftApiService.cs` ve `Models/` klasörünü doğrudan kendi projenize kopyalayıp kullanabilirsiniz — dışarıya bağımlılığı yoktur. `KursoftApiService`, `HttpClient` etrafında ince bir sarmalayıcıdır; kendi DI (Dependency Injection) container'ınıza da kolayca eklenebilir:

```csharp
builder.Services.AddSingleton(ApiSettings.Load(AppContext.BaseDirectory));
builder.Services.AddSingleton<KursoftApiService>();
```

## Notlar

- `CreateOrderRequest.SendInvoice` alanı JSON'a giderken küçük harfle (`sendInvoice`) gönderilir — bu, `docs/CreateOrder_API_Dokumani.docx` dosyasında belgelenen alan adıyla birebir eşleşmesi içindir.
- `KursoftApiService`, hem JSON gövdeli hem düz metin gövdeli hata yanıtlarını (bkz. her dokümanın "Hata Referansı" bölümü) tek bir `KursoftApiException` tipinde toplar; `ex.RawBody` üzerinden orijinal yanıtı inceleyebilirsiniz.
- `OrderList` yanıtındaki `ShippingAddress` / `BillingAddress` / `Items` alanları `docs/OrderList_API_Dokumani.docx` bölüm 3.3–3.5'te belgelenen alanların tamamını içerir.
