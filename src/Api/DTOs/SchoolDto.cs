using Oz.Domain.Entities;

namespace Oz.Api.DTOs;

public record SchoolDto(long Id, string Name, SchoolType Type, bool IsArchived, DateTime CreatedAt);
