using Oz.Domain.Entities;

namespace Oz.Api.DTOs;

public record SchoolListDto(long Id, string Name, string Type, string TypeLabel)
{
    private static readonly Dictionary<SchoolType, string> Labels = new()
    {
        [SchoolType.Arabic] = "Arabic",
        [SchoolType.Experimental] = "Experimental",
        [SchoolType.AzharEldelta] = "Azhar_Eldelta",
        [SchoolType.ElHoda] = "El_Hoda",
        [SchoolType.ElTegara] = "El_Tegara",
        [SchoolType.Custom] = "Custom",
    };

    public static SchoolListDto FromEntity(School s) => new(
        s.Id,
        s.Name,
        s.Type.ToString(),
        Labels.GetValueOrDefault(s.Type, s.Type.ToString())
    );
}
