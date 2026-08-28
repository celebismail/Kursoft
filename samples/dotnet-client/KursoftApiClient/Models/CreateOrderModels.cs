using System.Text.Json.Serialization;

namespace KursoftApiClient.Models;

// Bu dosyadaki sınıflar, docs/CreateOrder_API_Dokumani.docx dosyasında
// belgelenen istek/yanıt alanlarının birebir karşılığıdır.

public sealed class CreateOrderRequest
{
    public required DateTime OrderDate { get; set; }
    public required decimal GrandTotal { get; set; }

    // Dokümanda alan adı küçük harfle "sendInvoice" olarak geçiyor;
    // C# tarafında PascalCase kullanıp JSON'a giderken doğru isme çeviriyoruz.
    [JsonPropertyName("sendInvoice")]
    public bool SendInvoice { get; set; }

    public required List<OrderProductLine> Products { get; set; }
    public required InvoiceAddress InvoiceAddress { get; set; }
    public required OrderInfo OrderInfo { get; set; }
}

public sealed class OrderProductLine
{
    public string ProductCode { get; set; } = "";
    public string ProductName { get; set; } = "";
    public string Barcode { get; set; } = "";
    public decimal GrossPrice { get; set; }
    public decimal Quantity { get; set; }
    public int WarehouseId { get; set; }
    public string Unit { get; set; } = "ADET";
    public decimal VATRate { get; set; }
    public int VariantId { get; set; }
    public decimal SecondaryUnitQty { get; set; }
    public string ProductCurrency { get; set; } = "";
    public decimal ExchangeRate { get; set; }
    public decimal ForeignCurrencyUnitPrice { get; set; }
    public decimal TotalVAT { get; set; }
    public decimal LineDiscount { get; set; }
    public decimal DiscountedUnitPriceLocal { get; set; }
    public decimal Discount1 { get; set; }
    public decimal Discount2 { get; set; }
    public decimal Discount3 { get; set; }
    public decimal Discount4 { get; set; }
    public decimal Discount5 { get; set; }
    public decimal WithholdingRate { get; set; }
    public decimal WithholdingAmount { get; set; }
    public decimal SpecialConsumptionTaxRate { get; set; }
    public decimal SpecialConsumptionTaxAmount { get; set; }
    public decimal CommunicationTaxRate { get; set; }
    public decimal CommunicationTaxAmount { get; set; }
    public decimal WithholdingTaxRate { get; set; }
    public decimal WithholdingTaxAmount { get; set; }
    public decimal AccommodationTaxRate { get; set; }
    public decimal AccommodationTaxAmount { get; set; }
    public string ShipmentNumber { get; set; } = "";
    public string CarrierName { get; set; } = "";
    public string TrackingLink { get; set; } = "";
    public string SerialNumber { get; set; } = "";
    public string LinkedProductId { get; set; } = "";
    public string LineStatus { get; set; } = "";
    public string LinePackageId { get; set; } = "";
    public string LineNote { get; set; } = "";
}

/// <summary>
/// Bireysel müşteri için FirstName + LastName + NationalId,
/// kurumsal müşteri için CompanyName + TaxNumber doldurulmalıdır.
/// (bkz. docs/CreateOrder_API_Dokumani.docx, bölüm 2.3)
/// </summary>
public sealed class InvoiceAddress
{
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string NationalId { get; set; } = "";
    public string CompanyName { get; set; } = "";
    public string TaxNumber { get; set; } = "";
    public string TaxOffice { get; set; } = "";
    public required string Email { get; set; }
    public required string Phone { get; set; }
    public required string FullAddress { get; set; }
    public required string District { get; set; }
    public required string City { get; set; }
    public string Country { get; set; } = "TR";
    public string PostalCode { get; set; } = "";
}

public sealed class OrderInfo
{
    public required string OrderNumber { get; set; }
    public required string InvoiceEmail { get; set; }
    public required DeliveryAddress DeliveryAddress { get; set; }
}

public sealed class DeliveryAddress
{
    public required string FullName { get; set; }
    public string CompanyName { get; set; } = "";
    public required string Email { get; set; }
    public required string Phone { get; set; }
    public required string FullAddress { get; set; }
    public required string District { get; set; }
    public required string City { get; set; }
    public string Country { get; set; } = "TR";
    public required string PostalCode { get; set; }
}

public sealed class CreateOrderResponse
{
    public bool Status { get; set; }
    public bool InvoiceStatus { get; set; }
    public string? InvoiceStatusMessage { get; set; }
    public int OrderID { get; set; }
    public string? Ressonmessage { get; set; } // API'deki gerçek alan adı bu şekilde yazılmıştır (typo korunmuştur)
    public string? OrderInvoiceNumber { get; set; }
    public string? OrderInvoiceLink { get; set; }
}
