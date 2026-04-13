namespace AggregationService.Domain;

public record StockInfo(
    string ProductId,
    int Quantity,
    bool InStock);
