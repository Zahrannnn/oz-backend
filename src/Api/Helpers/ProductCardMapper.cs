using Oz.Api.DTOs;
using Oz.Domain.Entities;

namespace Oz.Api.Helpers;

public static class ProductCardMapper
{
    public static ProductCardDto Map(Product p)
    {
        var variants = p.Variants.OrderBy(v => v.Id).ToList();

        var priceFrom = variants.Count != 0
            ? variants.Min(v => (decimal?)v.PriceInclVat)
            : null;

        var thumbnailUrl = p.Images
            .OrderBy(i => i.SortOrder)
            .FirstOrDefault()?.Url;

        var stockStatus = ComputeStockStatus(variants);

        var variantDtos = variants
            .Select(v => new VariantSummaryDto(v.Id, v.SizeLabel, v.PriceInclVat, v.Stock))
            .ToList();

        return new ProductCardDto(
            p.Id,
            p.ItemType.Name,
            (byte)p.Gender,
            p.Color,
            p.IsInSet,
            priceFrom,
            thumbnailUrl,
            stockStatus,
            variantDtos);
    }

    public static string ComputeStockStatus(List<Variant> variants)
    {
        if (variants.Count == 0 || variants.All(v => v.Stock == 0))
            return "out_of_stock";

        if (variants.Any(v => v.Stock <= v.LowStockThreshold))
            return "low_stock";

        return "in_stock";
    }
}
