using System.Text.Json;

namespace PMIS.Application.DTOs;

public class NafathUserClaim
{
    public string NameIdentifier { get; set; } = string.Empty;
    public string ArabicFirstName { get; set; } = string.Empty;
    public string ArabicFatherName { get; set; } = string.Empty;
    public string ArabicGrandFatherName { get; set; } = string.Empty;
    public string ArabicFamilyName { get; set; } = string.Empty;
    public string ArabicName { get; set; } = string.Empty;
    public string DobHijri { get; set; } = string.Empty;
    public string DobGregorian { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;

    public string Nationality { get; set; } = string.Empty;

    public JsonElement ArabicNationality { get; set; }
}
