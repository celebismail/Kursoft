using KursoftApiClient.Configuration;
using KursoftApiClient.Http;
using KursoftApiClient.Models;
using Microsoft.OpenApi.Models;

// ============================================================================
// KURSOFT ERP API — Örnek İstemci (Swagger UI ile interaktif test aracı)
//
// Bu proje, repo'daki docs/ klasöründe belgelenen on endpoint'i
// tarayıcıdan tıklayarak (Postman gerekmeden) deneyebileceğiniz, Swagger UI
// ile açılan bir ASP.NET Core Web API'dir. Her /demo/* rotası, gerçek
// KURSOFT API'sine bir istek atıp yanıtı olduğu gibi size geri döner —
// yani burası "sahte" bir API değil, gerçek API'ye giden bir vitrin/köprüdür.
//
// Çalıştırmadan önce: samples/dotnet-client/README.md dosyasındaki
// "Kurulum" adımlarını izleyerek BaseUrl / Username / Password
// bilgilerinizi appsettings.Local.json içine ya da ortam değişkenlerine
// tanımlayın. Bu bilgiler kesinlikle appsettings.json içine YAZILMAMALI
// (o dosya repo'ya commit edilir).
//
// Çalıştırın: dotnet run   →  tarayıcı otomatik http://localhost:5080/swagger adresini açar.
// ============================================================================

var builder = WebApplication.CreateBuilder(args);

var settings = ApiSettings.Load(AppContext.BaseDirectory);
builder.Services.AddSingleton(settings);
builder.Services.AddSingleton<KursoftApiService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "KURSOFT ERP — Örnek İstemci",
        Version = "v1",
        Description =
            "docs/ klasöründeki API dokümanlarına karşılık gelen, gerçek KURSOFT API'sine " +
            "istek atan interaktif bir test arayüzü. Her rota altındaki 'Try it out' " +
            "butonuyla gerçek bir çağrı yapabilirsiniz.\n\n" +
            (settings.IsConfigured
                ? $"Bağlı ortam: {settings.BaseUrl}"
                : "⚠️ BaseUrl/Username/Password henüz ayarlanmamış — istekler 'yapılandırma eksik' hatası döner. " +
                  "Kurulum için samples/dotnet-client/README.md dosyasına bakın."),
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "KURSOFT ERP — Örnek İstemci v1");
    options.DocumentTitle = "KURSOFT ERP — Örnek İstemci";
});

app.MapGet("/", () => Results.Redirect("/swagger"));

// ----------------------------------------------------------------------
// Ortak hata işleyici: her demo endpoint'i bu sarmalayıcı içinden geçer.
// KursoftApiException'ı gerçek API'nin döndürdüğü durum koduyla, yapılandırma
// eksikse 400 ile açıklayıcı bir mesajla, beklenmeyen hatalarda 500 ile döner.
// ----------------------------------------------------------------------
async Task<IResult> InvokeAsync(ApiSettings apiSettings, Func<Task<IResult>> action)
{
    if (!apiSettings.IsConfigured)
    {
        return Results.BadRequest(new
        {
            error = "Yapılandırma eksik.",
            detail = "BaseUrl / Username / Password ayarlanmamış. samples/dotnet-client/README.md " +
                     "dosyasındaki kurulum adımlarını izleyip appsettings.Local.json oluşturun ya da " +
                     "KURSOFT_BASEURL / KURSOFT_USERNAME / KURSOFT_PASSWORD ortam değişkenlerini tanımlayın.",
        });
    }

    try
    {
        return await action();
    }
    catch (KursoftApiException ex)
    {
        return Results.Json(new { error = ex.Message, rawBody = ex.RawBody }, statusCode: (int)ex.StatusCode);
    }
}

// ----------------------------------------------------------------------
// 1) Sipariş Oluştur — docs/CreateOrder_API_Dokumani.docx
// ----------------------------------------------------------------------
app.MapPost("/demo/create-order", async (CreateOrderRequest request, KursoftApiService api, ApiSettings apiSettings) =>
    await InvokeAsync(apiSettings, async () => Results.Ok(await api.CreateOrderAsync(request))))
    .WithTags("1. Sipariş")
    .WithSummary("Sipariş Oluştur (CreateOrder)")
    .WithDescription("POST /api/v2/Order/CreateOrder — docs/CreateOrder_API_Dokumani.docx")
    .Produces<CreateOrderResponse>(200)
    .Accepts<CreateOrderRequest>("application/json");

// ----------------------------------------------------------------------
// 2) Sipariş Listesi — docs/OrderList_API_Dokumani.docx
// ----------------------------------------------------------------------
app.MapPost("/demo/order-list", async (string? orderNumber, KursoftApiService api, ApiSettings apiSettings) =>
    await InvokeAsync(apiSettings, async () => Results.Ok(await api.GetOrderListAsync(orderNumber))))
    .WithTags("1. Sipariş")
    .WithSummary("Sipariş Listesi (OrderList)")
    .WithDescription("POST /api/v2/Order/OrderList — docs/OrderList_API_Dokumani.docx. orderNumber boş bırakılırsa son 100 sipariş döner.")
    .Produces<OrderListResponse>(200);

// ----------------------------------------------------------------------
// 3) Ödeme Oluştur — docs/PaymentCreate_API_Dokumani.docx
// ----------------------------------------------------------------------
app.MapPost("/demo/payment-create", async (PaymentCreateRequest request, KursoftApiService api, ApiSettings apiSettings) =>
    await InvokeAsync(apiSettings, async () => Results.Ok(await api.CreatePaymentAsync(request))))
    .WithTags("2. Ödeme")
    .WithSummary("Ödeme / Nakit Hareket Oluştur (PaymentCreate)")
    .WithDescription("POST /api/v2/Payment/PaymentCreate — docs/PaymentCreate_API_Dokumani.docx. TransactionType değerleri için dokümandaki enum referansına bakınız.")
    .Produces<PaymentCreateResponse>(200);

// ----------------------------------------------------------------------
// 4) Müşteri Listesi — docs/Customerlist_API_Dokumani.docx
// ----------------------------------------------------------------------
app.MapPost("/demo/customer-list", async (CustomerListRequest request, KursoftApiService api, ApiSettings apiSettings) =>
    await InvokeAsync(apiSettings, async () => Results.Ok(await api.GetCustomerListAsync(request))))
    .WithTags("3. Müşteri")
    .WithSummary("Müşteri Listesi (Customerlist)")
    .WithDescription("POST /api/v2/Customer/Customerlist — docs/Customerlist_API_Dokumani.docx")
    .Produces<List<CustomerListItem>>(200);

// ----------------------------------------------------------------------
// 5) Cari İşlem Takibi — docs/TransactionHistory_API_Dokumani.docx
// ----------------------------------------------------------------------
app.MapPost("/demo/transaction-history", async (TransactionHistoryRequest request, KursoftApiService api, ApiSettings apiSettings) =>
    await InvokeAsync(apiSettings, async () => Results.Ok(await api.GetTransactionHistoryAsync(request))))
    .WithTags("3. Müşteri")
    .WithSummary("Cari İşlem Takibi (TransactionHistory)")
    .WithDescription("POST /api/v2/Customer/TransactionHistory — docs/TransactionHistory_API_Dokumani.docx. Filter değerleri için dokümandaki CariTransactionFilter enum referansına bakınız.")
    .Produces<TransactionHistoryResponse>(200);

// ----------------------------------------------------------------------
// 6) Ürün Ekle — docs/Product_API_Dokumani.docx (bölüm 2)
// ----------------------------------------------------------------------
app.MapPost("/demo/product-create", async (CreateProductRequest request, KursoftApiService api, ApiSettings apiSettings) =>
    await InvokeAsync(apiSettings, async () => Results.Ok(await api.CreateProductAsync(request))))
    .WithTags("4. Ürün")
    .WithSummary("Ürün Ekle (CreateProduct)")
    .WithDescription("POST /api/v2/Product/CreateProduct — docs/Product_API_Dokumani.docx (bölüm 2)")
    .Produces<CreateProductResponse>(200);

// ----------------------------------------------------------------------
// 7) Stok Güncelle — docs/Product_API_Dokumani.docx (bölüm 3)
// ----------------------------------------------------------------------
app.MapPost("/demo/stock-update", async (StockUpdateRequest[] request, KursoftApiService api, ApiSettings apiSettings) =>
    await InvokeAsync(apiSettings, async () => Results.Ok(await api.UpdateStockAsync(request))))
    .WithTags("4. Ürün")
    .WithSummary("Stok Güncelle (StockUpdate)")
    .WithDescription("POST /api/v2/Product/StockUpdate — docs/Product_API_Dokumani.docx (bölüm 3). En fazla 100 kayıt; hata yönetimi satır bazlıdır.")
    .Produces<List<StockUpdateResponse>>(200);

// ----------------------------------------------------------------------
// 8) Stok Fiyat Güncelle — docs/Product_API_Dokumani.docx (bölüm 4)
// ----------------------------------------------------------------------
app.MapPost("/demo/stock-price-update", async (StockPriceUpdateRequest[] request, KursoftApiService api, ApiSettings apiSettings) =>
    await InvokeAsync(apiSettings, async () => Results.Ok(await api.UpdateStockPriceAsync(request))))
    .WithTags("4. Ürün")
    .WithSummary("Stok Fiyat Güncelle (StockPriceUpdate)")
    .WithDescription("POST /api/v2/Product/StockPriceUpdate — docs/Product_API_Dokumani.docx (bölüm 4). ⚠️ Şu an yalnızca varyantsız ürünlerde çalışır.")
    .Produces<List<StockUpdateResponse>>(200);

// ----------------------------------------------------------------------
// 9) Ürün Listesi — docs/Product_API_Dokumani.docx (bölüm 5)
// ----------------------------------------------------------------------
app.MapPost("/demo/product-list", async (ProductListRequest request, KursoftApiService api, ApiSettings apiSettings) =>
    await InvokeAsync(apiSettings, async () => Results.Ok(await api.GetProductListAsync(request))))
    .WithTags("4. Ürün")
    .WithSummary("Ürün Listesi (ProductList)")
    .WithDescription("POST /api/v2/Product/ProductList — docs/Product_API_Dokumani.docx (bölüm 5). Sayfalı, StockCode/Barcode ile filtrelenebilir.")
    .Produces<ProductListResponse>(200);

// ----------------------------------------------------------------------
// 10) Stok İşlem Takibi — docs/Product_API_Dokumani.docx (bölüm 6)
// ----------------------------------------------------------------------
app.MapPost("/demo/stock-transaction-history", async (StockTransactionRequest request, KursoftApiService api, ApiSettings apiSettings) =>
    await InvokeAsync(apiSettings, async () => Results.Ok(await api.GetStockTransactionHistoryAsync(request))))
    .WithTags("4. Ürün")
    .WithSummary("Stok İşlem Takibi (StockTransactionHistory)")
    .WithDescription("POST /api/v2/Product/StockTransactionHistory — docs/Product_API_Dokumani.docx (bölüm 6). Bir ürünün stok hareket dökümünü ve kümülatif bakiyesini döner.")
    .Produces<StockTransactionResponse>(200);

app.Run();
