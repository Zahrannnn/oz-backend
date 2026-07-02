using Oz.Domain.Entities;

namespace Oz.Api.DTOs;

public record CreateSchoolDto(string Name, SchoolType Type);
