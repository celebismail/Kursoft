using System.ComponentModel.DataAnnotations;

namespace KursoftApiClient.Models;

// docs/Product_API_Dokumani.docx dosyasındaki StockRequestDto ile birebir eşleşir.
// Validasyon attribute'ları da orijinal DTO ile aynıdır; Swagger UI bu sayede
// "Try it out" formunda zorunlu alanları otomatik işaretler.

public sealed class CreateProductRequest
{
    [Required]
    public int AdminId { get; set; }

    [Required, MaxLength(200)]
    public string ProductName { get; set; } = "";

    [Required, MaxLength(100)]
    public string Sku { get; set; } = "";

    [MaxLength(100)]
    public string? Barcode1 { get; set; }

    [MaxLength(50)]
    public string? Category { get; set; }

    [MaxLength(20)]
    public string? Unit { get; set; }

    [Range(0, double.MaxValue)]
    public decimal Quantity { get; set; }

    [Range(0, double.MaxValue)]
    public decimal MinStock { get; set; }

    [Range(0, double.MaxValue)]
    public decimal Price { get; set; }

    [MaxLength(10)]
    public string Currency { get; set; } = "TRY";

    public int Kdv { get; set; } = 20;

    [MaxLength(200)]
    public string? Supplier { get; set; }

    [MaxLength(200)]
    public string? Warehouse { get; set; }

    [MaxLength(20)]
    public string Status { get; set; } = "active";

    [MaxLength(1000)]
    public string? Description { get; set; }

    public string? Image { get; set; }
}

public sealed class CreateProductResponse
{
    public bool Success { get; set; }
    public int Id { get; set; }
    public string? Message { get; set; }
}

public sealed class StockUpdateRequest
{
    public required string Barcode { get; set; }
    public required int StockQuantity { get; set; }
}

public sealed class StockPriceUpdateRequest
{
    public required string Barcode { get; set; }
    public decimal Price1 { get; set; }
    public decimal Price2 { get; set; }
    public decimal Price3 { get; set; }
    public decimal Price4 { get; set; }
    public decimal Price5 { get; set; }
    public decimal Price6 { get; set; }
    public decimal Price7 { get; set; }
    public decimal Price8 { get; set; }
    public decimal Price9 { get; set; }
    public decimal Price10 { get; set; }
}

public sealed class StockPriceResponse
{
    public decimal Price1 { get; set; }
    public decimal Price2 { get; set; }
    public decimal Price3 { get; set; }
    public decimal Price4 { get; set; }
    public decimal Price5 { get; set; }
    public decimal Price6 { get; set; }
    public decimal Price7 { get; set; }
    public decimal Price8 { get; set; }
    public decimal Price9 { get; set; }
    public decimal Price10 { get; set; }
}

public sealed class StockUpdateResponse
{
    public bool Status { get; set; }
    public string StatusMessage { get; set; } = "";
    public string? Barcode { get; set; }
    public int StockQuantity { get; set; }
    public StockPriceResponse? StockPrice { get; set; }
}

// docs/Product_API_Dokumani.docx bölüm 5 (ProductList) ile birebir eşleşir.

public sealed class ProductListRequest
{
    public string StockCode { get; set; } = "";  // Kısmi eşleşme (LIKE %değer%)
    public string Barcode { get; set; } = "";       // Tam eşleşme
    public int Page { get; set; } = 1;
    public int Limit { get; set; } = 100;
}

public sealed class ProductListResponse
{
    public ProductDetail[] ProductDetails { get; set; } = Array.Empty<ProductDetail>();
}

public sealed class ProductDetail
{
    public string? StockCode { get; set; }
    public string? ProductName { get; set; }
    public string? Color { get; set; }
    public string? Size { get; set; }
    public string? VariantCode { get; set; }
    public string? Barcode { get; set; }
    public string? Category { get; set; }
    public string? Brand { get; set; }
    public int StockQuantity { get; set; }
    public decimal SalePrice { get; set; }
    public int VAT { get; set; }
    public int MainProductId { get; set; }
    public int VariantId { get; set; }
}

// docs/Product_API_Dokumani.docx bölüm 6 (StockTransactionHistory) ile birebir eşleşir.

public sealed class StockTransactionRequest
{
    public required int ProductId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string TransactionType { get; set; } = "";  // Tam eşleşme (İşlem Türü)
    public int Page { get; set; } = 1;
    public int Limit { get; set; } = 100;
}

public sealed class StockTransactionResponse
{
    public bool IsSuccessful { get; set; }
    public string? Message { get; set; }
    public int RecordCount { get; set; }
    public List<StockTransactionItem> Transactions { get; set; } = new();
}

public sealed class StockTransactionItem
{
    public int Id { get; set; }
    public string? Barcode { get; set; }
    public string? ProductName { get; set; }
    public string? TransactionType { get; set; }
    public string? TransactionNumber { get; set; }
    public string? SerialNumber { get; set; }
    public string? PerformedBy { get; set; }
    public string? Salesperson { get; set; }
    public string? Variant { get; set; }
    public string? LineCurrency { get; set; }
    public string? TransactionCurrency { get; set; }

    /// <summary>"(G)" Giriş, "(Ç)" Çıkış, "(GÇ)" Giriş-Çıkış (depo transferi).</summary>
    public string? MovementFlag { get; set; }
    public decimal Quantity { get; set; }
    public decimal BalanceAfter { get; set; }
    public DateTime TransactionDate { get; set; }
    public string? WarehouseName { get; set; }
    public string? Description { get; set; }
}
