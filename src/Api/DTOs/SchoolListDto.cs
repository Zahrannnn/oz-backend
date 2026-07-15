using Oz.Domain.Entities;

namespace Oz.Api.DTOs;

public record SchoolListDto(long Id, string Name, string Type, string TypeLabel)
{
    /// <summary>Stable English API codes for clients that parse Type as a string.</summary>
    private static readonly Dictionary<SchoolType, string> Codes = new()
    {
        [SchoolType.Governmental] = "Governmental",
        [SchoolType.Experimental] = "Experimental",
        [SchoolType.Arabic] = "Arabic",
        [SchoolType.Language] = "Language",
        [SchoolType.International] = "International",
        [SchoolType.Private] = "Private",
    };

    /// <summary>Arabic labels unified with admin + storefront UI.</summary>
    private static readonly Dictionary<SchoolType, string> LabelsAr = new()
    {
        [SchoolType.Governmental] = "حكومي",
        [SchoolType.Experimental] = "تجريبي",
        [SchoolType.Arabic] = "عربي",
        [SchoolType.Language] = "لغات",
        [SchoolType.International] = "دولي",
        [SchoolType.Private] = "خاص",
    };

    public static SchoolListDto FromEntity(School s) => new(
        s.Id,
        s.Name,
        Codes.GetValueOrDefault(s.Type, s.Type.ToString()),
        LabelsAr.GetValueOrDefault(s.Type, s.Type.ToString())
    );
}
