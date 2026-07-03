using CoreWCF;

namespace KisanMitraAI.Core.GovernmentIntegration;

/// <summary>
/// SOAP service contract for integration with government agricultural systems
/// </summary>
[ServiceContract(Namespace = "http://kisanmitra.gov.in/integration/v1")]
public interface IGovernmentIntegrationService
{
    /// <summary>
    /// Submit digitized Soil Health Card data to government systems
    /// </summary>
    [OperationContract]
    Task<SoilHealthCardResponse> SubmitSoilHealthCardAsync(SoilHealthCardRequest request);

    /// <summary>
    /// Register a farmer with government agricultural systems
    /// </summary>
    [OperationContract]
    Task<FarmerRegistrationResponse> RegisterFarmerAsync(FarmerRegistrationRequest request);

    /// <summary>
    /// Check farmer eligibility for agricultural subsidies
    /// </summary>
    [OperationContract]
    Task<SubsidyEligibilityResponse> CheckSubsidyEligibilityAsync(SubsidyEligibilityRequest request);
}
