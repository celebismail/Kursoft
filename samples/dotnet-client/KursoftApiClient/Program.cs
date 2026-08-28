using KursoftApiClient.Configuration;
using KursoftApiClient.Http;
using KursoftApiClient.Models;

// ============================================================================
// KURSOFT ERP API — Örnek İstemci (.NET 8 Console App)
//
// Bu proje, repo'daki docs/ klasöründe belgelenen dört endpoint'i nasıl
// çağıracağınızı gösteren çalışan bir referanstır:
//   1) Sipariş Oluştur   (CreateOrder)
//   2) Sipariş Listesi   (OrderList)
//   3) Ödeme Oluştur     (PaymentCreate)
//   4) Müşteri Listesi   (Customerlist)
//
// Çalıştırmadan önce: samples/dotnet-client/README.md dosyasındaki
// "Kurulum" adımlarını izleyerek BaseUrl / Username / Password
// bilgilerinizi appsettings.Local.json içine ya da ortam değişkenlerine
// tanımlayın. Bu bilgiler kesinlikle appsettings.json içine YAZILMAMALI
// (o dosya repo'ya commit edilir).
// ============================================================================

Console.OutputEncoding = System.Text.Encoding.UTF8;

Console.WriteLine("=== KURSOFT ERP API — Örnek İstemci ===\n");

ApiSettings settings;
try
{
    settings = ApiSettings.Load(AppContext.BaseDirectory);
}
catch (InvalidOperationException ex)
{
    Console.Error.WriteLine($"Ayarlar okunamadı: {ex.Message}");
    return 1;
}

Console.WriteLine($"BaseUrl : {settings.BaseUrl}");
Console.WriteLine($"Username: {settings.Username}");
Console.WriteLine("Password: ****\n");

using var api = new KursoftApiService(settings);

// Her adım birbirinden bağımsızdır; istediğinizi yorum satırına alıp
// sadece ilgilendiğiniz endpoint'i test edebilirsiniz.
await RunCreateOrderDemoAsync(api);
await RunOrderListDemoAsync(api);
await RunPaymentCreateDemoAsync(api);
await RunCustomerListDemoAsync(api);

Console.WriteLine("\n=== Tamamlandı ===");
return 0;


// ----------------------------------------------------------------------
// 1) Sipariş Oluştur
// ----------------------------------------------------------------------
static async Task RunCreateOrderDemoAsync(KursoftApiService api)
{
    PrintStep("1) Sipariş Oluştur (CreateOrder) — Bireysel Müşteri");

    // Aynı OrderNumber ile tekrar denerseniz API isteği sessizce
    // yok sayar (status:false döner) — bu yüzden her çalıştırmada
    // benzersiz bir numara üretiyoruz.
    var orderNumber = $"ORNEK-{DateTime.UtcNow:yyyyMMddHHmmss}";

    var request = new CreateOrderRequest
    {
        OrderDate = DateTime.UtcNow,
        GrandTotal = 1416.00m,
        SendInvoice = false,
        Products =
        [
            new OrderProductLine
            {
                ProductCode = "STK-0001",
                ProductName = "Test Urunu A",
                Barcode = "8690000000001",
                GrossPrice = 600.00m,
                Quantity = 2,
                WarehouseId = 1,
                Unit = "ADET",
                VATRate = 18,
                TotalVAT = 216.00m,
                LineNote = "Örnek istemciden gönderildi",
            },
        ],
        InvoiceAddress = new InvoiceAddress
        {
            FirstName = "Ahmet",
            LastName = "Yilmaz",
            NationalId = "12345678901",
            Email = "ahmet@test.com",
            Phone = "05551234567",
            FullAddress = "Levent Mah. Buyukdere Cad. No:1 Kat:5",
            District = "Besiktas",
            City = "Istanbul",
        },
        OrderInfo = new OrderInfo
        {
            OrderNumber = orderNumber,
            InvoiceEmail = "ahmet@test.com",
            DeliveryAddress = new DeliveryAddress
            {
                FullName = "Ahmet Yilmaz",
                Email = "ahmet@test.com",
                Phone = "05551234567",
                FullAddress = "Levent Mah. Buyukdere Cad. No:1 Kat:5",
                District = "Besiktas",
                City = "Istanbul",
                PostalCode = "34330",
            },
        },
    };

    try
    {
        var result = await api.CreateOrderAsync(request);
        if (result is { Status: true })
            Console.WriteLine($"  ✅ Sipariş oluşturuldu. OrderID={result.OrderID}, OrderNumber={orderNumber}");
        else
            Console.WriteLine($"  ⚠️  Sipariş oluşturulamadı: {result?.Ressonmessage}");
    }
    catch (KursoftApiException ex)
    {
        Console.WriteLine($"  ❌ Hata: {ex.Message}");
    }
}


// ----------------------------------------------------------------------
// 2) Sipariş Listesi
// ----------------------------------------------------------------------
static async Task RunOrderListDemoAsync(KursoftApiService api)
{
    PrintStep("2) Sipariş Listesi (OrderList) — Filtresiz, son 100 kayıt");

    try
    {
        var result = await api.GetOrderListAsync(orderNumber: null);
        Console.WriteLine($"  ✅ {result?.OrderCount ?? 0} sipariş bulundu.");

        foreach (var order in (result?.Orders ?? []).Take(5))
        {
            Console.WriteLine($"     - #{order.Id} {order.OrderNumber} | {order.Customer} | {order.TotalAmount:0.00} | {order.PackageStatus}");
        }
    }
    catch (KursoftApiException ex)
    {
        Console.WriteLine($"  ❌ Hata: {ex.Message}");
    }
}


// ----------------------------------------------------------------------
// 3) Ödeme Oluştur
// ----------------------------------------------------------------------
static async Task RunPaymentCreateDemoAsync(KursoftApiService api)
{
    PrintStep("3) Ödeme Oluştur (PaymentCreate) — Nakit Tahsilat");

    var paymentNumber = $"ORNEK-ODEME-{DateTime.UtcNow:yyyyMMddHHmmss}";

    var request = new PaymentCreateRequest
    {
        TransactionType = (int)TransactionType.NakitTahsilat, // 1
        CashAccountId = 3,
        Amount = 1500.00m,
        Description = "Örnek istemciden gönderildi",
        CustomerId = 482, // Kendi ortamınızda geçerli bir CustomerId ile değiştirin
        Currency = "TRY",
        Date = DateTime.UtcNow,
        PaymentNumber = paymentNumber,
    };

    try
    {
        var result = await api.CreatePaymentAsync(request);
        Console.WriteLine($"  ✅ {result?.Mesaj} (islemID={result?.IslemID})");
    }
    catch (KursoftApiException ex)
    {
        Console.WriteLine($"  ❌ Hata: {ex.Message}");
    }
}


// ----------------------------------------------------------------------
// 4) Müşteri Listesi
// ----------------------------------------------------------------------
static async Task RunCustomerListDemoAsync(KursoftApiService api)
{
    PrintStep("4) Müşteri Listesi (Customerlist) — İstanbul, aktif cariler");

    var request = new CustomerListRequest
    {
        City = "Istanbul",
        IsActive = true,
        OrderBy = "CustomerName",
        OrderDirection = "ASC",
        Top = 10,
    };

    try
    {
        var result = await api.GetCustomerListAsync(request);
        Console.WriteLine($"  ✅ {result?.Count ?? 0} müşteri bulundu.");

        foreach (var customer in result ?? [])
        {
            Console.WriteLine($"     - {customer.CustomerCode} | {customer.CustomerName} | Bakiye: {customer.Balance:0.00} {customer.Currency}");
        }
    }
    catch (KursoftApiException ex)
    {
        Console.WriteLine($"  ❌ Hata: {ex.Message}");
    }
}


static void PrintStep(string title)
{
    Console.WriteLine($"\n--- {title} ---");
}
