using KisanMitraAI.Core.Models;
using KisanMitraAI.Core.QualityGrading;
using KisanMitraAI.Core.VoiceIntelligence;
using Microsoft.Extensions.Logging;

namespace KisanMitraAI.Infrastructure.Vision;

public class PriceCalculator : IPriceCalculator
{
    private readonly IPriceRetriever _priceRetriever;
    private readonly ILogger<PriceCalculator> _logger;

    private static readonly Dictionary<string, decimal> FallbackPrices = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Potato"] = 1350m, ["Tomato"] = 1000m, ["Onion"] = 1750m,
        ["Wheat"] = 2100m, ["Rice"] = 2500m, ["Corn"] = 1800m,
        ["Mango"] = 3000m, ["Apple"] = 5000m, ["Banana"] = 1200m,
        ["Cauliflower"] = 1500m, ["Cabbage"] = 1000m, ["Brinjal"] = 1200m,
        ["Chilli"] = 2500m, ["Capsicum"] = 2000m, ["Grapes"] = 4000m,
        ["Pomegranate"] = 5000m, ["Guava"] = 2000m, ["Papaya"] = 1500m,
        ["Lemon"] = 2500m, ["Peas"] = 3000m, ["Carrot"] = 1500m
    };

    public PriceCalculator(IPriceRetriever priceRetriever, ILogger<PriceCalculator> logger)
    {
        _priceRetriever = priceRetriever;
        _logger = logger;
    }

    public async Task<decimal> CalculateCertifiedPriceAsync(
        QualityGrade grade, string commodity, string location,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var allPrices = (await _priceRetriever.GetCurrentPricesAsync(
                commodity, location, cancellationToken)).ToList();

            if (!allPrices.Any())
            {
                _logger.LogWarning("No mandi prices for {Commodity} in {Location}, using fallback", commodity, location);
                var fallbackBase = FallbackPrices.TryGetValue(commodity, out var fp) ? fp : 1500m;
                var fallbackPrice = fallbackBase * grade.GetFallbackMultiplier();
                _logger.LogInformation("Fallback: {Commodity} {Grade} = ₹{Base} × {Mult} = ₹{Price}",
                    commodity, grade.GetDisplayLabel(), fallbackBase, grade.GetFallbackMultiplier(), fallbackPrice);
                return fallbackPrice;
            }

            // Strategy 1: Find prices matching the exact mandi grade
            var mandiGradeFilter = grade.ToMandiGradeFilter();
            var gradePrices = allPrices
                .Where(p => !string.IsNullOrEmpty(p.Grade) &&
                            p.Grade.Equals(mandiGradeFilter, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (gradePrices.Any())
            {
                var gradeModalPrice = gradePrices
                    .OrderByDescending(p => p.PriceDate)
                    .First().ModalPrice;

                _logger.LogInformation(
                    "Grade-specific price for {Commodity} [{Grade}] in {Location}: ₹{Price}/quintal (from {Count} {MandiGrade} records)",
                    commodity, grade.GetDisplayLabel(), location, gradeModalPrice, gradePrices.Count, mandiGradeFilter);

                return gradeModalPrice;
            }

            // Strategy 2: No grade-specific price — use FAQ as base and apply multiplier
            var faqPrices = allPrices
                .Where(p => !string.IsNullOrEmpty(p.Grade) &&
                            p.Grade.Equals("FAQ", StringComparison.OrdinalIgnoreCase))
                .ToList();

            decimal basePrice;
            string priceSource;

            if (faqPrices.Any())
            {
                basePrice = faqPrices.OrderByDescending(p => p.PriceDate).First().ModalPrice;
                priceSource = "FAQ";
            }
            else
            {
                // Strategy 3: No FAQ either — use overall modal price
                basePrice = allPrices.OrderByDescending(p => p.PriceDate).First().ModalPrice;
                priceSource = "overall";
            }

            var multiplier = grade.GetFallbackMultiplier();
            var certifiedPrice = basePrice * multiplier;

            _logger.LogInformation(
                "Derived price for {Commodity} [{Grade}] in {Location}: ₹{Base} ({Source}) × {Mult} = ₹{Price}/quintal",
                commodity, grade.GetDisplayLabel(), location, basePrice, priceSource, multiplier, certifiedPrice);

            return certifiedPrice;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating price for {Commodity} [{Grade}] in {Location}",
                commodity, grade, location);
            throw;
        }
    }
}
