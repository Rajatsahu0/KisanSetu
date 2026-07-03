# Security Infrastructure

This directory contains security-related services and configurations for the Kisan Mitra AI platform.

## Components

### Encryption Service
- **Purpose**: Provides AES-256 encryption for sensitive data at rest
- **Implementation**: Uses AWS KMS for key management
- **Usage**: Encrypt personal data (name, phone, location) before storing in DynamoDB and RDS

### TLS Configuration
- **Purpose**: Enforces TLS 1.3 for all API communications
- **Features**:
  - HTTPS-only enforcement with automatic redirection
  - HSTS (HTTP Strict Transport Security) with 1-year max-age
  - Security headers (X-Content-Type-Options, X-Frame-Options, CSP, etc.)
  - Certificate management via AWS Certificate Manager

### Malware Scanning Service
- **Purpose**: Scans uploaded files for malware before processing
- **Implementation**: Integrates with AWS GuardDuty or ClamAV Lambda
- **Coverage**: All uploaded files (images, documents, audio)

### Data Deletion Service
- **Purpose**: Handles GDPR-compliant data deletion requests
- **Scope**: Removes personal data from S3, DynamoDB, RDS, and Timestream
- **Timeline**: Completes deletion within 30 days

## Configuration

### AWS KMS Key
Configure the KMS key ID in appsettings.json:
```json
{
  "AWS": {
    "KMS": {
      "KeyId": "arn:aws:kms:region:account:key/key-id"
    }
  }
}
```

### TLS/HTTPS
TLS 1.3 is enforced automatically. Ensure AWS Certificate Manager is configured for your domain.

## Usage

### Registering Services
```csharp
// In Program.cs
builder.Services.AddEncryptionServices();
builder.Services.AddTlsConfiguration();
builder.Services.AddMalwareScanningServices();
builder.Services.AddDataDeletionServices();
```

### Using Encryption
```csharp
public class UserService
{
    private readonly IEncryptionService _encryption;

    public async Task SaveUserAsync(User user)
    {
        // Encrypt sensitive fields
        user.Name = await _encryption.EncryptAsync(user.Name);
        user.PhoneNumber = await _encryption.EncryptAsync(user.PhoneNumber);
        
        // Save to database
        await _repository.SaveAsync(user);
    }
}
```

## Security Best Practices

1. **Encryption at Rest**: All personal data must be encrypted before storage
2. **Encryption in Transit**: All API communications use TLS 1.3
3. **Key Rotation**: Rotate KMS keys annually
4. **Access Control**: Use IAM policies for least privilege access
5. **Audit Logging**: All security operations are logged to CloudWatch
6. **Malware Scanning**: All uploads are scanned before processing
7. **Data Deletion**: Honor deletion requests within 30 days

## Compliance

This implementation supports:
- Indian data protection regulations
- GDPR-style data deletion requirements
- Industry-standard encryption (AES-256)
- Secure communication (TLS 1.3)
