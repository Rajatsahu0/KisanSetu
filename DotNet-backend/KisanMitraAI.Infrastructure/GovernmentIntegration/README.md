# Government Integration SOAP Service

## Overview

The Government Integration SOAP service provides a standardized interface for integrating Kisan Mitra AI with government agricultural systems. This service uses CoreWCF to expose SOAP endpoints for government agencies to consume.

## Service Endpoint

**URL**: `https://{host}/GovernmentIntegration.svc`

**WSDL**: `https://{host}/GovernmentIntegration.svc?wsdl`

**Namespace**: `http://kisanmitra.gov.in/integration/v1`

**Binding**: BasicHttpBinding with Transport Security (HTTPS)

## Operations

### 1. SubmitSoilHealthCardAsync

Submits digitized Soil Health Card data to government systems for record-keeping and analysis.

**Request**: `SoilHealthCardRequest`
- FarmerId: Unique farmer identifier
- FarmerName: Name of the farmer
- State: State name
- District: District name
- Village: Village name
- SoilData: Soil test results (N, P, K, pH, organic carbon, micronutrients)
- SubmissionDate: Date of submission

**Response**: `SoilHealthCardResponse`
- Success: Boolean indicating success/failure
- ReferenceNumber: Unique reference for tracking
- Message: Status message
- ProcessedDate: Processing timestamp
- Errors: List of validation or processing errors

**Example SOAP Request**:
```xml
<soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/" 
               xmlns:v1="http://kisanmitra.gov.in/integration/v1">
  <soap:Body>
    <v1:SubmitSoilHealthCardAsync>
      <v1:request>
        <v1:FarmerId>FR-MP-20260216-ABC123</v1:FarmerId>
        <v1:FarmerName>राम कुमार</v1:FarmerName>
        <v1:State>Madhya Pradesh</v1:State>
        <v1:District>Indore</v1:District>
        <v1:Village>Khajrana</v1:Village>
        <v1:SoilData>
          <v1:Nitrogen>250.5</v1:Nitrogen>
          <v1:Phosphorus>18.2</v1:Phosphorus>
          <v1:Potassium>180.0</v1:Potassium>
          <v1:pH>7.2</v1:pH>
          <v1:OrganicCarbon>0.45</v1:OrganicCarbon>
          <v1:Sulfur>12.5</v1:Sulfur>
          <v1:Zinc>0.8</v1:Zinc>
          <v1:Boron>0.5</v1:Boron>
          <v1:Iron>4.2</v1:Iron>
          <v1:Manganese>3.5</v1:Manganese>
          <v1:Copper>1.2</v1:Copper>
          <v1:TestDate>2026-01-15T00:00:00</v1:TestDate>
          <v1:LabId>LAB-MP-001</v1:LabId>
        </v1:SoilData>
        <v1:SubmissionDate>2026-02-16T10:30:00</v1:SubmissionDate>
      </v1:request>
    </v1:SubmitSoilHealthCardAsync>
  </soap:Body>
</soap:Envelope>
```

### 2. RegisterFarmerAsync

Registers a new farmer with government agricultural systems.

**Request**: `FarmerRegistrationRequest`
- FarmerName: Name of the farmer
- PhoneNumber: Contact number (Indian format)
- AadharNumber: 12-digit Aadhar number
- State, District, Village, PinCode: Location details
- Farms: List of farm details (survey number, area, soil type, coordinates)
- RegistrationDate: Registration timestamp

**Response**: `FarmerRegistrationResponse`
- Success: Boolean indicating success/failure
- FarmerId: Assigned unique farmer ID
- Message: Status message
- ProcessedDate: Processing timestamp
- Errors: List of validation or processing errors
- EligibleSchemes: List of schemes the farmer is eligible for

### 3. CheckSubsidyEligibilityAsync

Checks farmer eligibility for agricultural subsidies and schemes.

**Request**: `SubsidyEligibilityRequest`
- FarmerId: Unique farmer identifier
- SubsidySchemeCode: Code of the subsidy scheme (e.g., PM-KISAN, PMFBY)
- CropType: Type of crop
- FarmAreaInAcres: Farm area
- State: State name
- RequestDate: Request timestamp

**Response**: `SubsidyEligibilityResponse`
- Success: Boolean indicating success/failure
- IsEligible: Whether farmer is eligible
- SchemeName: Full name of the scheme
- SubsidyAmount: Eligible subsidy amount
- EligibilityCriteria: Criteria description
- ApplicationProcess: How to apply
- RequiredDocuments: List of required documents
- ProcessedDate: Processing timestamp
- Errors: List of validation or processing errors

## Validation Rules

### Soil Health Card Submission
- FarmerId, FarmerName, State, District are required
- pH must be between 0 and 14
- OrganicCarbon must be between 0 and 100
- LabId is required

### Farmer Registration
- FarmerName, PhoneNumber, AadharNumber are required
- PhoneNumber must be valid Indian format (10 digits starting with 6-9)
- AadharNumber must be exactly 12 digits
- At least one farm is required

### Subsidy Eligibility
- FarmerId, SubsidySchemeCode, State are required
- FarmAreaInAcres must be greater than 0

## Error Handling

All operations return structured error responses with:
- Success flag set to false
- Descriptive error messages in the Errors list
- User-friendly message in the Message field

Common error scenarios:
- Validation failures (missing or invalid fields)
- System errors (network issues, service unavailable)
- Business rule violations (duplicate registration, ineligible for scheme)

## Security

- All endpoints use HTTPS (Transport Security)
- Authentication can be added via WS-Security headers (future enhancement)
- Input validation prevents injection attacks
- Sensitive data (Aadhar numbers) should be encrypted in transit

## Testing

### Using SoapUI or Postman

1. Import WSDL from `https://{host}/GovernmentIntegration.svc?wsdl`
2. Generate sample requests
3. Modify with test data
4. Send requests to the service endpoint

### Using .NET Client

```csharp
// Add service reference or use BasicHttpBinding
var binding = new BasicHttpBinding(BasicHttpSecurityMode.Transport);
var endpoint = new EndpointAddress("https://localhost:5001/GovernmentIntegration.svc");
var factory = new ChannelFactory<IGovernmentIntegrationService>(binding, endpoint);
var client = factory.CreateChannel();

// Call service
var request = new SoilHealthCardRequest
{
    FarmerId = "FR-MP-20260216-TEST",
    FarmerName = "Test Farmer",
    State = "Madhya Pradesh",
    District = "Indore",
    Village = "Test Village",
    SoilData = new SoilTestData
    {
        Nitrogen = 250.5f,
        Phosphorus = 18.2f,
        Potassium = 180.0f,
        pH = 7.2f,
        OrganicCarbon = 0.45f,
        TestDate = DateTime.UtcNow,
        LabId = "LAB-TEST-001"
    },
    SubmissionDate = DateTime.UtcNow
};

var response = await client.SubmitSoilHealthCardAsync(request);
Console.WriteLine($"Success: {response.Success}, Reference: {response.ReferenceNumber}");
```

## Integration Notes

### For Government Agencies

1. **WSDL Discovery**: Access the WSDL at the service endpoint to generate client code
2. **Data Format**: All dates are in ISO 8601 format (UTC)
3. **Character Encoding**: UTF-8 for all text fields (supports Indian languages)
4. **Rate Limiting**: No rate limits currently enforced on SOAP endpoints
5. **Monitoring**: All requests are logged for audit purposes

### Future Enhancements

- WS-Security authentication with username/password tokens
- Digital signature support for non-repudiation
- Asynchronous callback notifications for long-running operations
- Bulk operations for batch processing
- Additional operations for scheme enrollment, payment tracking, etc.

## Support

For integration support or issues, contact:
- Technical Support: tech-support@kisanmitra.gov.in
- API Documentation: https://docs.kisanmitra.gov.in/soap-api
