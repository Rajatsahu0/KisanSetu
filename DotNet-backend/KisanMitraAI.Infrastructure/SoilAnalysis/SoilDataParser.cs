using KisanMitraAI.Core.Models;
using KisanMitraAI.Core.SoilAnalysis;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text.RegularExpressions;

namespace KisanMitraAI.Infrastructure.SoilAnalysis;

public class SoilDataParser : ISoilDataParser
{
    private readonly ILogger<SoilDataParser> _logger;

    // Field name patterns for different state formats
    private static readonly Dictionary<string, string[]> FieldPatterns = new()
    {
        ["FarmerId"] = new[] { "farmer id", "farmer code", "kisan id", "registration no", "reg no", "client name", "sample id" },
        ["Location"] = new[] { "location", "village", "district", "taluka", "block", "sample location" },
        ["Nitrogen"] = new[] { "nitrogen", "n", "available n", "n (kg/ha)", "nitrogen (n)" },
        ["Phosphorus"] = new[] { "phosphorus", "p", "available p", "p2o5", "p (kg/ha)", "phosphorus (p)" },
        ["Potassium"] = new[] { "potassium", "k", "available k", "k2o", "k (kg/ha)", "potassium (k)" },
        ["pH"] = new[] { "ph", "ph value", "soil ph", "reaction", "soil ph:" },
        ["OrganicCarbon"] = new[] { "organic carbon", "oc", "o.c", "carbon", "oc %", "organic carbon:" },
        ["OrganicMatter"] = new[] { "organic matter", "om", "o.m", "om %", "organic matter (om)", "organic matter (om):" },
        ["Sulfur"] = new[] { "sulfur", "sulphur", "s", "available s" },
        ["Zinc"] = new[] { "zinc", "zn", "available zn" },
        ["Boron"] = new[] { "boron", "b", "available b" },
        ["Iron"] = new[] { "iron", "fe", "available fe" },
        ["Manganese"] = new[] { "manganese", "mn", "available mn" },
        ["Copper"] = new[] { "copper", "cu", "available cu" },
        ["Magnesium"] = new[] { "magnesium", "mg", "available mg", "magnesium (mg)" },
        ["TestDate"] = new[] { "test date", "date of test", "sampling date", "analysis date", "date issued" },
        ["LabId"] = new[] { "lab id", "laboratory", "lab code", "testing lab", "report number" }
    };

    public SoilDataParser(ILogger<SoilDataParser> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

        public async Task<SoilHealthData> ParseSoilDataAsync(
        TextExtractionResult extraction,
        CancellationToken cancellationToken)
    {
        if (extraction == null)
            throw new ArgumentNullException(nameof(extraction));

        _logger.LogInformation("Parsing soil data from extraction result. DocumentS3Key: {DocumentS3Key}",
            extraction.DocumentS3Key);

        // Combine fields from both key-value pairs and tables
        var allFields = new Dictionary<string, string>(extraction.ExtractedFields, StringComparer.OrdinalIgnoreCase);
        
        // Extract data from tables and add to fields
        foreach (var table in extraction.ExtractedTables.Values)
        {
            ExtractFieldsFromTable(table, allFields);
        }

        // Extract fields using pattern matching
        var farmerId = ExtractField(allFields, "FarmerId") ?? "UNKNOWN";
        var location = ExtractField(allFields, "Location") ?? "UNKNOWN";
        
        var nitrogen = ParseFloat(ExtractField(allFields, "Nitrogen"));
        var phosphorus = ParseFloat(ExtractField(allFields, "Phosphorus"));
        var potassium = ParseFloat(ExtractField(allFields, "Potassium"));
        var pH = ParseFloat(ExtractField(allFields, "pH"));
        
        // Try to extract Organic Carbon, or convert from Organic Matter if available
        var organicCarbonStr = ExtractField(allFields, "OrganicCarbon");
        var organicMatterStr = ExtractField(allFields, "OrganicMatter");
        float organicCarbon;
        
        if (!string.IsNullOrWhiteSpace(organicCarbonStr))
        {
            organicCarbon = ParseFloat(organicCarbonStr);
        }
        else if (!string.IsNullOrWhiteSpace(organicMatterStr))
        {
            // Convert Organic Matter to Organic Carbon (OC ≈ OM × 0.58)
            var organicMatter = ParseFloat(organicMatterStr);
            organicCarbon = organicMatter * 0.58f;
            _logger.LogInformation("Converted Organic Matter {OM}% to Organic Carbon {OC}%", organicMatter, organicCarbon);
        }
        else
        {
            organicCarbon = 0f;
        }
        
        // Ensure organic carbon is within valid range (0-100%)
        if (organicCarbon > 100f)
        {
            _logger.LogWarning("Organic carbon value {OC} exceeds 100%, capping at 100%", organicCarbon);
            organicCarbon = 100f;
        }
        else if (organicCarbon < 0f)
        {
            _logger.LogWarning("Organic carbon value {OC} is negative, setting to 0%", organicCarbon);
            organicCarbon = 0f;
        }
        
        var sulfur = ParseFloat(ExtractField(allFields, "Sulfur"));
        var zinc = ParseFloat(ExtractField(allFields, "Zinc"));
        var boron = ParseFloat(ExtractField(allFields, "Boron"));
        var iron = ParseFloat(ExtractField(allFields, "Iron"));
        var manganese = ParseFloat(ExtractField(allFields, "Manganese"));
        var copper = ParseFloat(ExtractField(allFields, "Copper"));
        
        var testDateStr = ExtractField(allFields, "TestDate");
        var testDate = ParseDate(testDateStr)?.UtcDateTime ?? DateTime.UtcNow;
        
        var labId = ExtractField(allFields, "LabId") ?? "UNKNOWN";

        var soilData = new SoilHealthData(
            farmerId,
            location,
            nitrogen,
            phosphorus,
            potassium,
            pH,
            organicCarbon,
            sulfur,
            zinc,
            boron,
            iron,
            manganese,
            copper,
            testDate,
            labId);

        // LOG PARSED SOIL DATA TO CONSOLE FOR DEBUGGING
        Console.WriteLine("========== PARSED SOIL DATA ==========");
        Console.WriteLine($"FarmerId: {farmerId}");
        Console.WriteLine($"Location: {location}");
        Console.WriteLine($"Nitrogen: {nitrogen} kg/ha");
        Console.WriteLine($"Phosphorus: {phosphorus} kg/ha");
        Console.WriteLine($"Potassium: {potassium} kg/ha");
        Console.WriteLine($"pH: {pH}");
        Console.WriteLine($"Organic Carbon: {organicCarbon}%");
        Console.WriteLine($"Sulfur: {sulfur} ppm");
        Console.WriteLine($"Zinc: {zinc} ppm");
        Console.WriteLine($"Boron: {boron} ppm");
        Console.WriteLine($"Iron: {iron} ppm");
        Console.WriteLine($"Manganese: {manganese} ppm");
        Console.WriteLine($"Copper: {copper} ppm");
        Console.WriteLine($"Test Date: {testDate:yyyy-MM-dd}");
        Console.WriteLine($"Lab ID: {labId}");
        Console.WriteLine("======================================\n");

        _logger.LogInformation(
            "Soil data parsed. FarmerId: {FarmerId}, Location: {Location}, pH: {pH}, OC: {OC}",
            farmerId, location, pH, organicCarbon);

        return await Task.FromResult(soilData);
    }

    public async Task<ValidationResult> ValidateSoilDataAsync(
        SoilHealthData data,
        CancellationToken cancellationToken)
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data));

        var errors = new List<ValidationError>();

        // Validate farmer ID
        if (string.IsNullOrWhiteSpace(data.FarmerId) || data.FarmerId == "UNKNOWN")
        {
            errors.Add(new ValidationError("FarmerId", "Farmer ID is missing or could not be extracted", data.FarmerId));
        }

        // Validate location
        if (string.IsNullOrWhiteSpace(data.Location) || data.Location == "UNKNOWN")
        {
            errors.Add(new ValidationError("Location", "Location is missing or could not be extracted", data.Location));
        }

        // Validate pH (0-14 range)
        if (data.pH < 0 || data.pH > 14)
        {
            errors.Add(new ValidationError("pH", "pH value must be between 0 and 14", data.pH.ToString(CultureInfo.InvariantCulture)));
        }

        // Validate organic carbon (0-100% range)
        if (data.OrganicCarbon < 0 || data.OrganicCarbon > 100)
        {
            errors.Add(new ValidationError("OrganicCarbon", "Organic carbon must be between 0 and 100%", data.OrganicCarbon.ToString(CultureInfo.InvariantCulture)));
        }

        // Validate nitrogen (0-1000 kg/ha range)
        if (data.Nitrogen < 0 || data.Nitrogen > 1000)
        {
            errors.Add(new ValidationError("Nitrogen", "Nitrogen value must be between 0 and 1000 kg/ha", data.Nitrogen.ToString(CultureInfo.InvariantCulture)));
        }

        // Validate phosphorus (0-1000 kg/ha range)
        if (data.Phosphorus < 0 || data.Phosphorus > 1000)
        {
            errors.Add(new ValidationError("Phosphorus", "Phosphorus value must be between 0 and 1000 kg/ha", data.Phosphorus.ToString(CultureInfo.InvariantCulture)));
        }

        // Validate potassium (0-1000 kg/ha range)
        if (data.Potassium < 0 || data.Potassium > 1000)
        {
            errors.Add(new ValidationError("Potassium", "Potassium value must be between 0 and 1000 kg/ha", data.Potassium.ToString(CultureInfo.InvariantCulture)));
        }

        // Validate sulfur (0-100 ppm range)
        if (data.Sulfur < 0 || data.Sulfur > 100)
        {
            errors.Add(new ValidationError("Sulfur", "Sulfur value must be between 0 and 100 ppm", data.Sulfur.ToString(CultureInfo.InvariantCulture)));
        }

        // Validate micronutrients (0-100 ppm range)
        if (data.Zinc < 0 || data.Zinc > 100)
        {
            errors.Add(new ValidationError("Zinc", "Zinc value must be between 0 and 100 ppm", data.Zinc.ToString(CultureInfo.InvariantCulture)));
        }

        if (data.Boron < 0 || data.Boron > 100)
        {
            errors.Add(new ValidationError("Boron", "Boron value must be between 0 and 100 ppm", data.Boron.ToString(CultureInfo.InvariantCulture)));
        }

        if (data.Iron < 0 || data.Iron > 100)
        {
            errors.Add(new ValidationError("Iron", "Iron value must be between 0 and 100 ppm", data.Iron.ToString(CultureInfo.InvariantCulture)));
        }

        if (data.Manganese < 0 || data.Manganese > 100)
        {
            errors.Add(new ValidationError("Manganese", "Manganese value must be between 0 and 100 ppm", data.Manganese.ToString(CultureInfo.InvariantCulture)));
        }

        if (data.Copper < 0 || data.Copper > 100)
        {
            errors.Add(new ValidationError("Copper", "Copper value must be between 0 and 100 ppm", data.Copper.ToString(CultureInfo.InvariantCulture)));
        }

        // Validate test date
        if (data.TestDate > DateTime.UtcNow)
        {
            errors.Add(new ValidationError("TestDate", "Test date cannot be in the future", data.TestDate.ToString("O")));
        }

        // Validate lab ID
        if (string.IsNullOrWhiteSpace(data.LabId) || data.LabId == "UNKNOWN")
        {
            errors.Add(new ValidationError("LabId", "Lab ID is missing or could not be extracted", data.LabId));
        }

        var isValid = errors.Count == 0;

        if (!isValid)
        {
            _logger.LogWarning(
                "Soil data validation failed with {ErrorCount} errors. FarmerId: {FarmerId}",
                errors.Count, data.FarmerId);
        }

        return await Task.FromResult(new ValidationResult(isValid, errors));
    }

    private string? ExtractField(Dictionary<string, string> fields, string fieldType)
    {
        if (!FieldPatterns.TryGetValue(fieldType, out var patterns))
            return null;

        foreach (var pattern in patterns)
        {
            foreach (var kvp in fields)
            {
                if (kvp.Key.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                {
                    // Special handling for fields where value might be in the key
                    // Example: "Organic Carbon (OC) 0.65 %: 0.5-0.75"
                    // We want to extract "0.65" from the key, not "0.5-0.75" from the value
                    
                    var key = kvp.Key;
                    var value = kvp.Value;
                    
                    // Try to extract value from key first (common Textract pattern)
                    // Pattern: "Field Name 123.45 unit: optimal_range"
                    
                    // For percentage values (Organic Carbon, Organic Matter)
                    if (fieldType == "OrganicCarbon" || fieldType == "OrganicMatter")
                    {
                        // Try to extract number from key first (e.g., "0.65 %" from "Organic Carbon (OC) 0.65 %")
                        var keyMatch = Regex.Match(key, @"(\d+\.?\d*)\s*%");
                        if (keyMatch.Success)
                        {
                            _logger.LogDebug("Extracted {FieldType} value {Value}% from field key", fieldType, keyMatch.Groups[1].Value);
                            return keyMatch.Groups[1].Value;
                        }
                    }
                    
                    // For pH (no unit, just a number)
                    if (fieldType == "pH")
                    {
                        // Try to extract pH value from key (e.g., "6.8" from "pH 6.8" or "pH: 6.8")
                        var keyMatch = Regex.Match(key, @"ph\s*:?\s*(\d+\.?\d*)", RegexOptions.IgnoreCase);
                        if (keyMatch.Success)
                        {
                            _logger.LogDebug("Extracted pH value {Value} from field key", keyMatch.Groups[1].Value);
                            return keyMatch.Groups[1].Value;
                        }
                        
                        // Also try extracting any number after "pH" in the key
                        keyMatch = Regex.Match(key, @"(\d+\.?\d*)\s*$");
                        if (keyMatch.Success && !string.IsNullOrWhiteSpace(value))
                        {
                            // If value looks like a range (e.g., "6.0-7.5"), use the number from key
                            if (value.Contains("-") || value.Contains("to", StringComparison.OrdinalIgnoreCase))
                            {
                                _logger.LogDebug("Extracted pH value {Value} from field key (value is range)", keyMatch.Groups[1].Value);
                                return keyMatch.Groups[1].Value;
                            }
                        }
                    }
                    
                    // For nutrients with kg/ha or ppm units
                    if (fieldType == "Nitrogen" || fieldType == "Phosphorus" || fieldType == "Potassium" ||
                        fieldType == "Sulfur" || fieldType == "Zinc" || fieldType == "Boron" || 
                        fieldType == "Iron" || fieldType == "Manganese" || fieldType == "Copper")
                    {
                        // Try to extract number from key (e.g., "280.5" from "Nitrogen (N) 280.5 kg/ha")
                        var keyMatch = Regex.Match(key, @"(\d+\.?\d*)\s*(kg/ha|ppm)", RegexOptions.IgnoreCase);
                        if (keyMatch.Success)
                        {
                            _logger.LogDebug("Extracted {FieldType} value {Value} from field key", fieldType, keyMatch.Groups[1].Value);
                            return keyMatch.Groups[1].Value;
                        }
                    }
                    
                    // Default: return the value field
                    // But skip if value looks like an optimal range
                    if (!string.IsNullOrWhiteSpace(value) && 
                        !value.Contains("-") && 
                        !value.Contains("to", StringComparison.OrdinalIgnoreCase) &&
                        !value.Contains("optimal", StringComparison.OrdinalIgnoreCase))
                    {
                        return value;
                    }
                    
                    // Last resort: try to extract any number from the key
                    var lastResortMatch = Regex.Match(key, @"(\d+\.?\d*)");
                    if (lastResortMatch.Success)
                    {
                        _logger.LogDebug("Extracted {FieldType} value {Value} from field key (last resort)", fieldType, lastResortMatch.Groups[1].Value);
                        return lastResortMatch.Groups[1].Value;
                    }
                    
                    return value;
                }
            }
        }

        return null;
    }

    private void ExtractFieldsFromTable(TableData table, Dictionary<string, string> fields)
    {
        // Process each row in the table
        for (int i = 0; i < table.Rows.Count; i++)
        {
            var row = table.Rows[i];
            
            // Skip empty rows
            if (row.All(string.IsNullOrWhiteSpace))
                continue;

            // Try to find key-value pairs in the row
            // Common patterns: [Label, Value] or [Label, Value, Unit] or [Label, Value, Unit, Range, Status]
            if (row.Count >= 2)
            {
                var key = row[0]?.Trim();
                var value = row[1]?.Trim();

                if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(value))
                {
                    // Store the key-value pair
                    fields[key] = value;

                    // If there's a unit in the third column, append it to the value
                    if (row.Count >= 3 && !string.IsNullOrWhiteSpace(row[2]))
                    {
                        var unit = row[2].Trim();
                        // Don't append if it's a range or status indicator
                        if (!unit.Contains("-") && !unit.Equals("Optimal", StringComparison.OrdinalIgnoreCase) 
                            && !unit.Equals("Low", StringComparison.OrdinalIgnoreCase)
                            && !unit.Equals("High", StringComparison.OrdinalIgnoreCase))
                        {
                            fields[$"{key}_with_unit"] = $"{value} {unit}";
                        }
                    }
                }
            }
        }

        // Also check headers if they contain field names
        if (table.Headers.Count >= 2)
        {
            for (int i = 0; i < table.Headers.Count; i++)
            {
                var header = table.Headers[i]?.Trim();
                if (!string.IsNullOrWhiteSpace(header) && header.Equals("Field", StringComparison.OrdinalIgnoreCase))
                {
                    // This table has a "Field" and "Value" structure
                    var valueIndex = table.Headers.FindIndex(h => h?.Contains("Value", StringComparison.OrdinalIgnoreCase) == true);
                    if (valueIndex >= 0)
                    {
                        foreach (var row in table.Rows)
                        {
                            if (row.Count > Math.Max(i, valueIndex))
                            {
                                var fieldName = row[i]?.Trim();
                                var fieldValue = row[valueIndex]?.Trim();
                                if (!string.IsNullOrWhiteSpace(fieldName) && !string.IsNullOrWhiteSpace(fieldValue))
                                {
                                    fields[fieldName] = fieldValue;
                                }
                            }
                        }
                    }
                }
            }
        }
        
        // Special handling for "Property | Value | Unit | Optimal Range" table format
        // This handles cases like "pH | 6.8 | - | 6.0-7.5" or "Organic Carbon (OC) | 0.65 | % | 0.5-0.75"
        if (table.Headers.Count >= 2)
        {
            var propertyIndex = table.Headers.FindIndex(h => 
                h?.Contains("Property", StringComparison.OrdinalIgnoreCase) == true ||
                h?.Contains("Nutrient", StringComparison.OrdinalIgnoreCase) == true ||
                h?.Contains("Micronutrient", StringComparison.OrdinalIgnoreCase) == true);
            
            var valueIndex = table.Headers.FindIndex(h => h?.Contains("Value", StringComparison.OrdinalIgnoreCase) == true);
            
            if (propertyIndex >= 0 && valueIndex >= 0)
            {
                foreach (var row in table.Rows)
                {
                    if (row.Count > Math.Max(propertyIndex, valueIndex))
                    {
                        var propertyName = row[propertyIndex]?.Trim();
                        var propertyValue = row[valueIndex]?.Trim();
                        
                        if (!string.IsNullOrWhiteSpace(propertyName) && !string.IsNullOrWhiteSpace(propertyValue))
                        {
                            fields[propertyName] = propertyValue;
                            
                            // Also add with unit if available
                            if (row.Count > valueIndex + 1)
                            {
                                var unit = row[valueIndex + 1]?.Trim();
                                if (!string.IsNullOrWhiteSpace(unit) && unit != "-")
                                {
                                    fields[$"{propertyName}_with_unit"] = $"{propertyValue} {unit}";
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    private float ParseFloat(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return 0f;

        // Remove common units and extra text
        value = Regex.Replace(value, @"[^\d\.\-]", "");

        if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
            return result;

        return 0f;
    }

    private DateTimeOffset? ParseDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        // Try various date formats
        var formats = new[]
        {
            "dd/MM/yyyy",
            "dd-MM-yyyy",
            "yyyy-MM-dd",
            "dd/MM/yy",
            "dd-MM-yy",
            "MM/dd/yyyy",
            "yyyy/MM/dd"
        };

        foreach (var format in formats)
        {
            if (DateTimeOffset.TryParseExact(value, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
            {
                return result;
            }
        }

        // Try general parsing
        if (DateTimeOffset.TryParse(value, out var generalResult))
        {
            return generalResult;
        }

        return null;
    }
}
