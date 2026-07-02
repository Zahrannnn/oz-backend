namespace Oz.Api.DTOs;

public record ProductDetailDto(
    long Id,
    string SchoolName,
    string GradeStageName,
    string ItemType,
    byte Gender,
    string? Color,
    bool IsInSet,
    IReadOnlyList<VariantSummaryDto> Variants,
    IReadOnlyList<ProductImageDto> Images,
    DateTime CreatedAt,
    DateTime UpdatedAt);
