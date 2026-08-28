namespace KursoftApiClient.Models;

// docs/Customerlist_API_Dokumani.docx dosyasındaki
// CustomerListRequestModel ile birebir eşleşir.

public sealed class CustomerListRequest
{
    public string? CustomerCode { get; set; }
    public string? CustomerName { get; set; }
    public string? TaxNumber { get; set; }
    public string? City { get; set; }
    public string? CustomerGroup { get; set; }
    public bool? IsActive { get; set; }
    public decimal? MinBalance { get; set; }
    public decimal? MaxBalance { get; set; }

    // Sıralama alanı için beyaz liste (docs bölüm 2.3) dışında bir
    // değer gönderilirse sunucu otomatik olarak CustomerName'e döner.
    public string OrderBy { get; set; } = "CustomerName";
    public string OrderDirection { get; set; } = "ASC";
    public int Top { get; set; } = 100;
}

public sealed class CustomerListItem
{
    public int Id { get; set; }
    public string? CustomerCode { get; set; }
    public string? CustomerName { get; set; }
    public string? AuthorizedPerson { get; set; }
    public string? TaxOffice { get; set; }
    public string? TaxNumber { get; set; }
    public string? Phone { get; set; }
    public string? Phone2 { get; set; }
    public string? Address { get; set; }
    public string? Country { get; set; }
    public string? City { get; set; }
    public string? Email { get; set; }
    public DateTime CreatedDate { get; set; }
    public decimal Balance { get; set; }
    public string? BalanceType { get; set; }
    public string? Currency { get; set; }
    public bool IsActive { get; set; }
    public string? CustomerGroup { get; set; }
}
