namespace KursoftApiClient.Models;

// docs/PaymentCreate_API_Dokumani.docx dosyasındaki
// MobilNakitHareketRequestModel ile birebir eşleşir.

/// <summary>
/// TransactionType değerleri için docs/PaymentCreate_API_Dokumani.docx
/// bölüm 2.2'deki islemTurleri enum referansına bakınız.
/// En sık kullanılan birkaçı burada da tekrarlanmıştır.
/// </summary>
public enum TransactionType
{
    NakitTahsilat = 1,
    NakitOdeme = 2,
    GelenHavaleEFT = 3,
    GidenHavaleEFT = 4,
    KrediKartiTahsilat = 5,
    KrediKartiOdeme = 6,
}

public sealed class PaymentCreateRequest
{
    public required int TransactionType { get; set; }
    public int CashAccountId { get; set; } = -1;
    public int BankId { get; set; } = -1;
    public int SpecialCodeId { get; set; } = -1;
    public required decimal Amount { get; set; }
    public string Description { get; set; } = "";
    public required int CustomerId { get; set; }
    public required string Currency { get; set; }
    public required DateTime Date { get; set; }
    public required string PaymentNumber { get; set; }
}

public sealed class PaymentCreateResponse
{
    public string? Mesaj { get; set; }
    public int IslemID { get; set; }
}
