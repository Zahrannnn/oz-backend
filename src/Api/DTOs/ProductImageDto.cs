namespace Oz.Api.DTOs;

public record ProductImageDto(long Id, long ProductId, string Url, int SortOrder);
