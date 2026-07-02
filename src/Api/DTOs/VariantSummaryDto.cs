namespace Oz.Api.DTOs;

public record VariantSummaryDto(long Id, string SizeLabel, decimal PriceInclVat, int Stock);
