# Kisan Mitra AI Authorization Middleware

This directory contains the authorization infrastructure for the Kisan Mitra AI platform, implementing role-based access control (RBAC) and farmer data isolation.

## Components

### 1. Custom Authorization Attributes

#### `[RequiresFarmer]`
- Requires the user to be authenticated as a farmer
- Enforces farmer data isolation (farmers can only access their own data)
- Allows admin users to bypass data isolation checks

**Usage:**
```csharp
[HttpGet("profile")]
[RequiresFarmer]
public IActionResult GetProfile()
{
    var farmerId = User.GetFarmerId();
    // Implementation
}
```

#### `[RequiresAdmin]`
- Requires the user to be authenticated as an administrator
- Used for administrative operations that should not be accessible to farmers

**Usage:**
```csharp
[HttpGet("admin/all-farmers")]
[RequiresAdmin]
public IActionResult GetAllFarmers()
{
    // Implementation
}
```

### 2. ClaimsPrincipal Extensions

The `FarmerIdClaimsPrincipalExtensions` class provides extension methods for extracting farmer-specific information from JWT claims:

- `GetFarmerId()` - Extracts the farmer ID from the claims
- `HasFarmerId()` - Checks if a farmer ID claim exists
- `IsFarmer()` - Checks if the user has the Farmer role
- `IsAdmin()` - Checks if the user has the Admin role

**Usage:**
```csharp
var farmerId = User.GetFarmerId();
if (User.IsFarmer())
{
    // Farmer-specific logic
}
```

### 3. Authorization Handlers

#### `FarmerDataIsolationHandler`
Enforces farmer data isolation by comparing the farmer ID from the JWT token with the farmer ID in the request (from route, query, or header).

**How it works:**
1. Extracts farmer ID from JWT claims
2. Extracts farmer ID from request (route values, query parameters, or X-Farmer-Id header)
3. If farmer ID is in the request, it must match the authenticated user's farmer ID
4. Admin users can bypass this check if configured

#### `RoleRequirementHandler`
Validates that the user has the required role (Farmer or Admin).

### 4. Authorization Logging Middleware

The `AuthorizationLoggingMiddleware` logs all authorization failures (403 Forbidden responses) for security auditing purposes.

**Logged information:**
- Farmer ID (if available)
- Request path
- HTTP method

### 5. Service Registration

The `AuthorizationServiceExtensions` class provides a convenient method to register all authorization services:

```csharp
builder.Services.AddKisanMitraAuthorization();
```

This registers:
- Authorization handlers
- HttpContextAccessor
- Authorization policies (RequiresFarmer, RequiresAdmin, FarmerDataIsolation)

## Authorization Policies

### RequiresFarmer Policy
- User must be authenticated
- User must have the "Farmer" role
- Farmer data isolation is enforced (with admin bypass)

### RequiresAdmin Policy
- User must be authenticated
- User must have the "Admin" role

### FarmerDataIsolation Policy
- User must be authenticated
- Farmer data isolation is enforced (without role requirement)

## JWT Token Structure

The authorization system expects JWT tokens with the following claims:

```json
{
  "farmer_id": "farmer123",  // Custom claim for farmer ID
  "sub": "farmer123",        // Subject identifier (fallback)
  "role": "Farmer",          // Role claim
  "name": "John Doe"
}
```

## Data Isolation Mechanism

The farmer data isolation works by checking the farmer ID in three locations:

1. **Route values**: `/api/farmers/{farmerId}/data`
2. **Query parameters**: `/api/data?farmerId=farmer123`
3. **Custom header**: `X-Farmer-Id: farmer123`

If a farmer ID is found in any of these locations, it must match the authenticated user's farmer ID from the JWT token. If no farmer ID is found in the request, the authorization succeeds (the controller can set it from the claims).

## Example Controller

```csharp
[ApiController]
[Route("api/[controller]")]
public class FarmerDataController : ControllerBase
{
    // Only the authenticated farmer can access their own data
    [HttpGet("{farmerId}/soil-data")]
    [RequiresFarmer]
    public IActionResult GetSoilData(string farmerId)
    {
        // The middleware ensures farmerId matches the authenticated user
        var authenticatedFarmerId = User.GetFarmerId();
        
        // Fetch and return soil data for this farmer
        return Ok(soilData);
    }
    
    // Admin can access all farmers' data
    [HttpGet("all-soil-data")]
    [RequiresAdmin]
    public IActionResult GetAllSoilData()
    {
        // Only admins can access this endpoint
        return Ok(allSoilData);
    }
}
```

## Security Considerations

1. **JWT Token Validation**: Ensure JWT tokens are properly validated using AWS Cognito or your authentication provider
2. **HTTPS Only**: All API endpoints should use HTTPS to protect JWT tokens in transit
3. **Token Expiration**: Implement token refresh logic to handle expired tokens
4. **Audit Logging**: All authorization failures are logged for security auditing
5. **Least Privilege**: Use the most restrictive authorization attribute for each endpoint

## Testing

When testing endpoints with authorization:

1. **Unit Tests**: Mock the `ClaimsPrincipal` with appropriate claims
2. **Integration Tests**: Use test JWT tokens with valid farmer IDs and roles
3. **Security Tests**: Verify that farmers cannot access other farmers' data

## Requirements Validation

This implementation satisfies **Requirement 10.3** from the design document:
- ✅ Role-based access control (RBAC) middleware for ASP.NET Core
- ✅ Farmer data isolation checks ensuring farmers can only access their own data
- ✅ Custom authorization attributes: [RequiresFarmer], [RequiresAdmin]
- ✅ FarmerIdClaimsPrincipalExtensions to extract FarmerId from JWT claims
- ✅ Authorization policies added to Program.cs
