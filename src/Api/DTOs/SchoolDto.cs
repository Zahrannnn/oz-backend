namespace Oz.Api.DTOs;

public record SchoolDto(long Id, string Name, string NameAr, string Slug, bool IsActive, DateTime CreatedAt);
