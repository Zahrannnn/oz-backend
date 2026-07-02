namespace Oz.Api.DTOs;

public record ProductCardDto(
    long Id,
    string ItemType,
    byte Gender,
    string? Color,
    bool IsInSet,
    decimal? PriceFrom,
    string? ThumbnailUrl,
    string StockStatus,
    IReadOnlyList<VariantSummaryDto> Variants);
