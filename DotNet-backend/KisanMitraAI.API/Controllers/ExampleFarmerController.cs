using KisanMitraAI.Core.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KisanMitraAI.API.Controllers;

/// <summary>
/// Example controller demonstrating the use of authorization attributes.
/// This controller shows how to protect endpoints with farmer-specific authorization.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ExampleFarmerController : ControllerBase
{
    private readonly ILogger<ExampleFarmerController> _logger;

    public ExampleFarmerController(ILogger<ExampleFarmerController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Example endpoint that requires farmer authentication.
    /// Only authenticated farmers can access this endpoint, and they can only access their own data.
    /// </summary>
    [HttpGet("profile")]
    [RequiresFarmer]
    public IActionResult GetProfile()
    {
        var farmerId = User.GetFarmerId();
        
        _logger.LogInformation("Farmer {FarmerId} accessed their profile", farmerId);
        
        return Ok(new
        {
            FarmerId = farmerId,
            Message = "This is your farmer profile"
        });
    }

    /// <summary>
    /// Example endpoint that requires farmer authentication with explicit farmer ID in route.
    /// The authorization middleware will ensure the farmer ID in the route matches the authenticated user.
    /// </summary>
    [HttpGet("{farmerId}/data")]
    [RequiresFarmer]
    public IActionResult GetFarmerData(string farmerId)
    {
        var authenticatedFarmerId = User.GetFarmerId();
        
        // The middleware already checked that farmerId matches authenticatedFarmerId
        // If we reach here, the farmer is authorized to access this data
        
        _logger.LogInformation("Farmer {FarmerId} accessed their data", farmerId);
        
        return Ok(new
        {
            FarmerId = farmerId,
            Message = "This is your farmer data",
            IsOwner = farmerId == authenticatedFarmerId
        });
    }

    /// <summary>
    /// Example admin-only endpoint.
    /// Only users with the Admin role can access this endpoint.
    /// </summary>
    [HttpGet("admin/all-farmers")]
    [RequiresAdmin]
    public IActionResult GetAllFarmers()
    {
        _logger.LogInformation("Admin accessed all farmers list");
        
        return Ok(new
        {
            Message = "This endpoint is only accessible to administrators",
            Farmers = new[] { "farmer1", "farmer2", "farmer3" }
        });
    }
}
