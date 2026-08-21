using System.Text.Json.Serialization;

namespace BizSrt.Model.Session;

public class SecurityProfile
{
    [JsonPropertyName("type")]
    public int Type { get; set; }

    [JsonPropertyName("autoPost")]
    public bool AutoPost { get; set; }

    [JsonPropertyName("canRelease_Peer")]
    public bool CanRelease_Peer { get; set; }

    [JsonPropertyName("canSuspend")]
    public bool CanSuspend { get; set; }

    [JsonPropertyName("canReview_Staff")]
    public bool CanReview_Staff { get; set; }

    [JsonPropertyName("canEdit_All")]
    public bool CanEdit_All { get; set; }

    [JsonPropertyName("canDelete_All")]
    public bool CanDelete_All { get; set; }

    [JsonPropertyName("canProduce_Company")]
    public bool CanProduce_Company { get; set; }

    [JsonPropertyName("canProduce_Offering")]
    public bool CanProduce_Offering { get; set; }

    [JsonPropertyName("canManage_OffensiveList")]
    public bool CanManage_OffensiveList { get; set; }

    [JsonPropertyName("canManage_Categories")]
    public bool CanManage_Categories { get; set; }

    [JsonPropertyName("canManage_Locations")]
    public bool CanManage_Locations { get; set; }

    [JsonPropertyName("canManage_Users")]
    public bool CanManage_Users { get; set; }

    [JsonPropertyName("canManage_CompanyImport")]
    public bool CanManage_CompanyImport { get; set; }

    [JsonPropertyName("canManage_OfferingImport")]
    public bool CanManage_OfferingImport { get; set; }
}
