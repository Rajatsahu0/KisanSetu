using System.Runtime.Serialization;

namespace KisanMitraAI.Core.GovernmentIntegration;

/// <summary>
/// Request to submit Soil Health Card data to government systems
/// </summary>
[DataContract(Namespace = "http://kisanmitra.gov.in/integration/v1")]
public class SoilHealthCardRequest
{
    [DataMember(Order = 1)]
    public string FarmerId { get; set; } = string.Empty;

    [DataMember(Order = 2)]
    public string FarmerName { get; set; } = string.Empty;

    [DataMember(Order = 3)]
    public string State { get; set; } = string.Empty;

    [DataMember(Order = 4)]
    public string District { get; set; } = string.Empty;

    [DataMember(Order = 5)]
    public string Village { get; set; } = string.Empty;

    [DataMember(Order = 6)]
    public SoilTestData SoilData { get; set; } = new();

    [DataMember(Order = 7)]
    public DateTime SubmissionDate { get; set; }
}

/// <summary>
/// Soil test data extracted from Soil Health Card
/// </summary>
[DataContract(Namespace = "http://kisanmitra.gov.in/integration/v1")]
public class SoilTestData
{
    [DataMember(Order = 1)]
    public float Nitrogen { get; set; }

    [DataMember(Order = 2)]
    public float Phosphorus { get; set; }

    [DataMember(Order = 3)]
    public float Potassium { get; set; }

    [DataMember(Order = 4)]
    public float pH { get; set; }

    [DataMember(Order = 5)]
    public float OrganicCarbon { get; set; }

    [DataMember(Order = 6)]
    public float Sulfur { get; set; }

    [DataMember(Order = 7)]
    public float Zinc { get; set; }

    [DataMember(Order = 8)]
    public float Boron { get; set; }

    [DataMember(Order = 9)]
    public float Iron { get; set; }

    [DataMember(Order = 10)]
    public float Manganese { get; set; }

    [DataMember(Order = 11)]
    public float Copper { get; set; }

    [DataMember(Order = 12)]
    public DateTime TestDate { get; set; }

    [DataMember(Order = 13)]
    public string LabId { get; set; } = string.Empty;
}

/// <summary>
/// Response from government system after Soil Health Card submission
/// </summary>
[DataContract(Namespace = "http://kisanmitra.gov.in/integration/v1")]
public class SoilHealthCardResponse
{
    [DataMember(Order = 1)]
    public bool Success { get; set; }

    [DataMember(Order = 2)]
    public string ReferenceNumber { get; set; } = string.Empty;

    [DataMember(Order = 3)]
    public string Message { get; set; } = string.Empty;

    [DataMember(Order = 4)]
    public DateTime ProcessedDate { get; set; }

    [DataMember(Order = 5)]
    public List<string> Errors { get; set; } = new();
}
