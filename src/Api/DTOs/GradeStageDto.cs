namespace Oz.Api.DTOs;

public record GradeStageDto(long Id, long SchoolId, string Name, int DisplayOrder, DateTime? CreatedAt = null);
