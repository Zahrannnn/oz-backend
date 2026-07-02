namespace Oz.Api.DTOs;

public record AdminProductDto(
    long Id,
    long SchoolId,
    long GradeStageId,
    long ItemTypeId,
    byte Gender,
    string? Color,
    bool IsInSet,
    bool IsArchived);
