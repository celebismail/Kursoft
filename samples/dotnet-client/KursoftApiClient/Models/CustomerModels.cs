namespace KursoftApiClient.Models;

// docs/Customerlist_API_Dokumani.docx dosyasındaki
// CustomerListRequestModel ile birebir eşleşir.

public sealed class CustomerListRequest
{
    public string? CustomerCode { get; set; }  // Tam eşleşme (LIKE değil)
    public string? CustomerName { get; set; }  // Kısmi eşleşme (LIKE %değer%)
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

// docs/TransactionHistory_API_Dokumani.docx dosyasındaki
// TransactionHistoryRequestModel / CustomerTransactionItem ile birebir eşleşir.

/// <summary>
/// Filter değerleri için docs/TransactionHistory_API_Dokumani.docx
/// bölüm 2.2'deki CariTransactionFilter enum referansına bakınız.
/// </summary>
public enum CariTransactionFilter
{
    All = 0,
    Payables = 1,
    OverduePayables = 2,
    OverdueReceivables = 3,
    Receivables = 4,
    PendingOrders = 5,
}

public sealed class TransactionHistoryRequest
{
    public required int CustomerId { get; set; }
    public required int Year { get; set; }
    public required string Currency { get; set; }
    public int Filter { get; set; } = 0;
    public string Search { get; set; } = "";

    // StartDate/EndDate gönderilirse Year'ın satır filtresi olarak etkisi kalkar
    // (yalnızca devir bakiyesi hesaplamasının başlangıç yılını belirlemek için kullanılır).
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

public sealed class TransactionHistoryResponse
{
    public bool IsSuccessful { get; set; }
    public string? Message { get; set; }
    public int RecordCount { get; set; }
    public List<CustomerTransactionItem> Transactions { get; set; } = new();
}

public sealed class CustomerTransactionItem
{
    public int Id { get; set; }
    public string? TransactionType { get; set; }
    public string? TransactionNumber { get; set; }
    public DateTime TransactionDate { get; set; }
    public decimal Amount { get; set; }
    public string? Currency { get; set; }
    public string? Description { get; set; }
    public DateTime? DueDate { get; set; }

    /// <summary>"(A)" alacak, "(B)" borç, "(-)" bekleyen sipariş.</summary>
    public string? DebitCreditFlag { get; set; }
    public string? SourceTable { get; set; }
    public decimal RunningBalance { get; set; }
}
