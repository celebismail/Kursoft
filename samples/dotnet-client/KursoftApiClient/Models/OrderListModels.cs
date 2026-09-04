namespace KursoftApiClient.Models;

// Bu dosyadaki sınıflar docs/OrderList_API_Dokumani.docx dosyasındaki
// yanıt alanlarının birebir karşılığıdır (bölüm 3.1 - 3.5).

public sealed class OrderListResponse
{
    public bool IsSuccessful { get; set; }
    public string? Message { get; set; }
    public int OrderCount { get; set; }
    public List<OrderListItem> Orders { get; set; } = new();
}

public sealed class OrderListItem
{
    public int Id { get; set; }

    // Sipariş numarası boşsa otomatik olarak Fatura Belge No'ya (InvoiceNumer) düşer.
    public string? OrderNumber { get; set; }

    // API'deki alan adı "InvoiceNumer" şeklindedir (yazım hatası korunmuştur).
    public string? InvoiceNumer { get; set; }
    public string? Customer { get; set; }
    public DateTime OrderDate { get; set; }
    public decimal TotalAmount { get; set; }
    public string? Platform { get; set; }
    public string? DeliveryPerson { get; set; }
    public string? PackageStatus { get; set; }
    public bool IsMicroExport { get; set; }
    public int CustomerId { get; set; }
    public string? TransactionType { get; set; }
    public bool IsEInvoiceSent { get; set; }
    public DateTime InvoiceDate { get; set; }
    public ShipmentAddress? ShippingAddress { get; set; }
    public InvoiceAddressDetail? BillingAddress { get; set; }
    public List<OrderLine> Items { get; set; } = new();
}

public sealed class ShipmentAddress
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? OrderEmail { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? District { get; set; }
    public string? PostalCode { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
}

/// <summary>
/// OrderList yanıtındaki fatura adresi. CreateOrder'daki InvoiceAddress'ten
/// farklı bir yapıya sahip olduğu için ayrı bir sınıf olarak tutulur.
/// </summary>
public sealed class InvoiceAddressDetail
{
    public string? CompanyName { get; set; }
    public string? TaxOffice { get; set; }
    public string? TaxNumber { get; set; }
    public string? ContactPerson { get; set; }
    public string? NationalId { get; set; }
    public int PaymentTermDays { get; set; }
    public decimal DiscountRate { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? City { get; set; }
    public string? District { get; set; }
    public string? PostalCode { get; set; }
}

public sealed class OrderLine
{
    public int StockId { get; set; }
    public string? StockCode { get; set; }
    public string? ProductName { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPriceExclVAT { get; set; }
    public string? Currency { get; set; }
    public decimal VATRate { get; set; }
    public decimal DiscountedPrice { get; set; }
    public decimal TotalVAT { get; set; }
    public string? Barcode { get; set; }
    public string? ShipmentNumber { get; set; }
    public string? CargoCompany { get; set; }
    public string? Status { get; set; }
    public string? LineDescription { get; set; }
}
