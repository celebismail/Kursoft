using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using KursoftApiClient.Configuration;
using KursoftApiClient.Models;

namespace KursoftApiClient.Http;

/// <summary>
/// KURSOFT ERP API'sine yapılan tüm çağrıları tek bir yerden yöneten servis.
///
/// Her metod, docs/ klasöründeki ilgili Word dokümanında tarif edilen
/// endpoint'e karşılık gelir:
///   - CreateOrderAsync   → docs/CreateOrder_API_Dokumani.docx
///   - GetOrderListAsync  → docs/OrderList_API_Dokumani.docx
///   - CreatePaymentAsync → docs/PaymentCreate_API_Dokumani.docx
///   - GetCustomerListAsync → docs/Customerlist_API_Dokumani.docx
///
/// Tüm dokümanlarda belirtildiği gibi her istekte Username/Password
/// header'ları zorunludur; bu sınıf bunu HttpClient.DefaultRequestHeaders
/// üzerinden otomatik olarak ekler, her çağrıda tekrar tekrar yazmanıza gerek kalmaz.
/// </summary>
public sealed class KursoftApiService : IDisposable
{
    private readonly HttpClient _http;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        // Sunucu hangi casing'i kullanırsa kullansın (camelCase/PascalCase)
        // yanıtları güvenle okuyabilmek için case-insensitive bırakıyoruz.
        PropertyNameCaseInsensitive = true,
    };

    public KursoftApiService(ApiSettings settings)
    {
        _http = new HttpClient
        {
            BaseAddress = new Uri(settings.BaseUrl.TrimEnd('/') + "/"),
        };

        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        // Dokümanlarda tarif edilen "Username / Password her istekte Header'da
        // gönderilmelidir" kuralı gereği bu iki header'ı her isteğe otomatik ekliyoruz.
        _http.DefaultRequestHeaders.Add("Username", settings.Username);
        _http.DefaultRequestHeaders.Add("Password", settings.Password);
    }

    /// <summary>Sipariş Oluştur — POST /api/v2/Order/CreateOrder</summary>
    public async Task<CreateOrderResponse?> CreateOrderAsync(CreateOrderRequest request, CancellationToken ct = default)
    {
        using var response = await PostJsonAsync("api/v2/Order/CreateOrder", request, ct);
        return await ReadJsonAsync<CreateOrderResponse>(response, ct);
    }

    /// <summary>
    /// Sipariş Listesi — POST /api/v2/Order/OrderList
    /// orderNumber null bırakılırsa iptal edilmemiş son 100 sipariş döner.
    /// </summary>
    public async Task<OrderListResponse?> GetOrderListAsync(string? orderNumber = null, CancellationToken ct = default)
    {
        var url = "api/v2/Order/OrderList";
        if (!string.IsNullOrWhiteSpace(orderNumber))
            url += $"?orderNumber={Uri.EscapeDataString(orderNumber)}";

        using var response = await _http.PostAsync(url, content: null, ct);
        return await ReadJsonAsync<OrderListResponse>(response, ct);
    }

    /// <summary>Ödeme / Nakit Hareket Oluştur — POST /api/v2/Payment/PaymentCreate</summary>
    public async Task<PaymentCreateResponse?> CreatePaymentAsync(PaymentCreateRequest request, CancellationToken ct = default)
    {
        using var response = await PostJsonAsync("api/v2/Payment/PaymentCreate", request, ct);
        return await ReadJsonAsync<PaymentCreateResponse>(response, ct);
    }

    /// <summary>Müşteri Listesi — POST /api/v2/Customer/Customerlist</summary>
    public async Task<List<CustomerListItem>?> GetCustomerListAsync(CustomerListRequest request, CancellationToken ct = default)
    {
        using var response = await PostJsonAsync("api/v2/Customer/Customerlist", request, ct);
        return await ReadJsonAsync<List<CustomerListItem>>(response, ct);
    }

    // ---- yardımcı metodlar ----

    private async Task<HttpResponseMessage> PostJsonAsync<TRequest>(string url, TRequest body, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(body, _jsonOptions);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        return await _http.PostAsync(url, content, ct);
    }

    /// <summary>
    /// Yanıtı okur. Dokümanlarda görüldüğü gibi bazı endpoint'ler hata durumunda
    /// düz metin (text/plain) döner, bazıları ise JSON içinde status:false ile
    /// gelir — bu yüzden önce HTTP durum kodunu kontrol edip, hata varsa
    /// gövdeyi (JSON olmayabilir) olduğu gibi exception'a taşıyoruz.
    /// </summary>
    private async Task<T?> ReadJsonAsync<T>(HttpResponseMessage response, CancellationToken ct)
    {
        var raw = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            throw new KursoftApiException(
                $"{(int)response.StatusCode} {response.ReasonPhrase}: {raw}",
                response.StatusCode,
                raw);
        }

        if (string.IsNullOrWhiteSpace(raw))
            return default;

        return JsonSerializer.Deserialize<T>(raw, _jsonOptions);
    }

    public void Dispose() => _http.Dispose();
}

/// <summary>
/// API'den dönen hataları (hem JSON gövdeli hem düz metin gövdeli senaryoları)
/// tek bir exception tipinde toplar. RawBody, orijinal ham yanıtı taşır —
/// isterseniz kendi hata ayrıştırma mantığınızı buradan kurabilirsiniz.
/// </summary>
public sealed class KursoftApiException(string message, System.Net.HttpStatusCode statusCode, string rawBody)
    : Exception(message)
{
    public System.Net.HttpStatusCode StatusCode { get; } = statusCode;
    public string RawBody { get; } = rawBody;
}
