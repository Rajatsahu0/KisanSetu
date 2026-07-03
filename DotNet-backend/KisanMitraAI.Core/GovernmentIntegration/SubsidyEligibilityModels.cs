using System.Runtime.Serialization;

namespace KisanMitraAI.Core.GovernmentIntegration;

/// <summary>
/// Request to check farmer eligibility for agricultural subsidies
/// </summary>
[DataContract(Namespace = "http://kisanmitra.gov.in/integration/v1")]
public class SubsidyEligibilityRequest
{
    [DataMember(Order = 1)]
    public string FarmerId { get; set; } = string.Empty;

    [DataMember(Order = 2)]
    public string SubsidySchemeCode { get; set; } = string.Empty;

    [DataMember(Order = 3)]
    public string CropType { get; set; } = string.Empty;

    [DataMember(Order = 4)]
    public float FarmAreaInAcres { get; set; }

    [DataMember(Order = 5)]
    public string State { get; set; } = string.Empty;

    [DataMember(Order = 6)]
    public DateTime RequestDate { get; set; }
}

/// <summary>
/// Response from government system with subsidy eligibility information
/// </summary>
[DataContract(Namespace = "http://kisanmitra.gov.in/integration/v1")]
public class SubsidyEligibilityResponse
{
    [DataMember(Order = 1)]
    public bool Success { get; set; }

    [DataMember(Order = 2)]
    public bool IsEligible { get; set; }

    [DataMember(Order = 3)]
    public string SchemeName { get; set; } = string.Empty;

    [DataMember(Order = 4)]
    public decimal SubsidyAmount { get; set; }

    [DataMember(Order = 5)]
    public string EligibilityCriteria { get; set; } = string.Empty;

    [DataMember(Order = 6)]
    public string ApplicationProcess { get; set; } = string.Empty;

    [DataMember(Order = 7)]
    public DateTime ProcessedDate { get; set; }

    [DataMember(Order = 8)]
    public List<string> RequiredDocuments { get; set; } = new();

    [DataMember(Order = 9)]
    public List<string> Errors { get; set; } = new();
}
