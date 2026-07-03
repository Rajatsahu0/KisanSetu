using System.Runtime.Serialization;

namespace KisanMitraAI.Core.GovernmentIntegration;

/// <summary>
/// Request to register a farmer with government agricultural systems
/// </summary>
[DataContract(Namespace = "http://kisanmitra.gov.in/integration/v1")]
public class FarmerRegistrationRequest
{
    [DataMember(Order = 1)]
    public string FarmerName { get; set; } = string.Empty;

    [DataMember(Order = 2)]
    public string PhoneNumber { get; set; } = string.Empty;

    [DataMember(Order = 3)]
    public string AadharNumber { get; set; } = string.Empty;

    [DataMember(Order = 4)]
    public string State { get; set; } = string.Empty;

    [DataMember(Order = 5)]
    public string District { get; set; } = string.Empty;

    [DataMember(Order = 6)]
    public string Village { get; set; } = string.Empty;

    [DataMember(Order = 7)]
    public string PinCode { get; set; } = string.Empty;

    [DataMember(Order = 8)]
    public List<FarmDetails> Farms { get; set; } = new();

    [DataMember(Order = 9)]
    public DateTime RegistrationDate { get; set; }
}

/// <summary>
/// Farm details for farmer registration
/// </summary>
[DataContract(Namespace = "http://kisanmitra.gov.in/integration/v1")]
public class FarmDetails
{
    [DataMember(Order = 1)]
    public string SurveyNumber { get; set; } = string.Empty;

    [DataMember(Order = 2)]
    public float AreaInAcres { get; set; }

    [DataMember(Order = 3)]
    public string SoilType { get; set; } = string.Empty;

    [DataMember(Order = 4)]
    public string IrrigationType { get; set; } = string.Empty;

    [DataMember(Order = 5)]
    public double Latitude { get; set; }

    [DataMember(Order = 6)]
    public double Longitude { get; set; }
}

/// <summary>
/// Response from government system after farmer registration
/// </summary>
[DataContract(Namespace = "http://kisanmitra.gov.in/integration/v1")]
public class FarmerRegistrationResponse
{
    [DataMember(Order = 1)]
    public bool Success { get; set; }

    [DataMember(Order = 2)]
    public string FarmerId { get; set; } = string.Empty;

    [DataMember(Order = 3)]
    public string Message { get; set; } = string.Empty;

    [DataMember(Order = 4)]
    public DateTime ProcessedDate { get; set; }

    [DataMember(Order = 5)]
    public List<string> Errors { get; set; } = new();

    [DataMember(Order = 6)]
    public List<string> EligibleSchemes { get; set; } = new();
}
