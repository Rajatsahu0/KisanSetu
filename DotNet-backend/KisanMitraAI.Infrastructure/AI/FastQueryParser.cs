using KisanMitraAI.Core.AI;
using KisanMitraAI.Core.VoiceIntelligence;
using KisanMitraAI.Core.VoiceIntelligence.Models;
using Microsoft.Extensions.Logging;

namespace KisanMitraAI.Infrastructure.AI;

/// <summary>
/// Dictionary-based query parser with INTENT CLASSIFICATION.
/// 
/// Intent priority (highest first):
///   1. Farming advice — खेती, बोना, उगाना, कहाँ, कब, कैसे → general_question (→ Knowledge Base)
///   2. Price query    — भाव, दाम, कीमत, रेट, मंडी         → price_query (→ fast template)
///   3. Unknown        — no clear intent                     → Gemini fallback
/// 
/// Key rule: If BOTH advice and price keywords are absent but commodity is found,
/// it's ambiguous → route to Gemini for proper understanding.
/// </summary>
public class FastQueryParser : IQueryParser
{
    private readonly IQueryParser _geminiFallback;
    private readonly ILogger<FastQueryParser> _logger;

    private static readonly Dictionary<string, string> CommodityMap = new(StringComparer.OrdinalIgnoreCase)
    {
        // Hindi/English
        { "आलू", "Potato" }, { "aloo", "Potato" }, { "aalu", "Potato" }, { "alu", "Potato" }, { "potato", "Potato" },
        { "टमाटर", "Tomato" }, { "tamatar", "Tomato" }, { "tamater", "Tomato" }, { "tomato", "Tomato" },
        { "प्याज", "Onion" }, { "pyaaz", "Onion" }, { "pyaj", "Onion" }, { "pyaz", "Onion" }, { "onion", "Onion" },
        { "गेहूं", "Wheat" }, { "gehun", "Wheat" }, { "gehu", "Wheat" }, { "wheat", "Wheat" },
        { "चावल", "Rice" }, { "chawal", "Rice" }, { "rice", "Rice" }, { "dhan", "Rice" }, { "धान", "Rice" },
        { "सेब", "Apple" }, { "seb", "Apple" }, { "apple", "Apple" },
        { "केला", "Banana" }, { "kela", "Banana" }, { "banana", "Banana" },
        { "आम", "Mango" }, { "aam", "Mango" }, { "mango", "Mango" },
        { "फूलगोभी", "Cauliflower" }, { "phool gobhi", "Cauliflower" }, { "cauliflower", "Cauliflower" },
        { "पत्तागोभी", "Cabbage" }, { "patta gobhi", "Cabbage" }, { "cabbage", "Cabbage" },
        { "बैंगन", "Brinjal" }, { "baingan", "Brinjal" }, { "brinjal", "Brinjal" },
        { "शिमला मिर्च", "Capsicum" }, { "shimla mirch", "Capsicum" }, { "capsicum", "Capsicum" },
        { "गाजर", "Carrot" }, { "gajar", "Carrot" }, { "carrot", "Carrot" },
        { "मटर", "Green Peas" }, { "matar", "Green Peas" }, { "peas", "Green Peas" },
        { "मक्का", "Corn" }, { "makka", "Corn" }, { "corn", "Corn" }, { "maize", "Corn" },
        { "लहसुन", "Garlic" }, { "lahsun", "Garlic" }, { "garlic", "Garlic" },
        { "अदरक", "Ginger" }, { "adrak", "Ginger" }, { "ginger", "Ginger" },
        { "तूर दाल", "Toor Dal" }, { "toor dal", "Toor Dal" }, { "arhar", "Toor Dal" },
        { "सरसों", "Mustard" }, { "sarson", "Mustard" }, { "mustard", "Mustard" },
        { "कपास", "Cotton" }, { "kapas", "Cotton" }, { "cotton", "Cotton" },
        { "गन्ना", "Sugarcane" }, { "ganna", "Sugarcane" }, { "sugarcane", "Sugarcane" },
        { "सोयाबीन", "Soybean" }, { "soyabean", "Soybean" }, { "soybean", "Soybean" },
        { "चना", "Chickpea" }, { "chana", "Chickpea" }, { "chickpea", "Chickpea" },
        { "मूंग", "Moong" }, { "moong", "Moong" }, { "mung", "Moong" },
        { "उड़द", "Urad" }, { "urad", "Urad" },
        { "बाजरा", "Bajra" }, { "bajra", "Bajra" }, { "millet", "Bajra" },
        { "ज्वार", "Jowar" }, { "jowar", "Jowar" }, { "sorghum", "Jowar" },
        
        // Kannada script commodities
        { "ಆಲೂಗಡ್ಡೆ", "Potato" }, { "ಆಲೂಗೆಡ್ಡೆ", "Potato" },
        { "ಟೊಮೇಟೊ", "Tomato" },
        { "ಈರುಳ್ಳಿ", "Onion" },
        { "ಗೋಧಿ", "Wheat" },
        { "ಅಕ್ಕಿ", "Rice" },
        { "ಸೇಬು", "Apple" },
        { "ಬಾಳೆಹಣ್ಣು", "Banana" },
        { "ಮಾವಿನಹಣ್ಣು", "Mango" },
        { "ಹೂಕೋಸು", "Cauliflower" },
        { "ಎಲೆಕೋಸು", "Cabbage" },
        { "ಬದನೆಕಾಯಿ", "Brinjal" },
        { "ಕ್ಯಾಪ್ಸಿಕಂ", "Capsicum" },
        { "ಕ್ಯಾರೆಟ್", "Carrot" },
        { "ಬಟಾಣಿ", "Green Peas" },
        { "ಮೆಕ್ಕೆಜೋಳ", "Corn" },
        { "ಬೆಳ್ಳುಳ್ಳಿ", "Garlic" },
        { "ಶುಂಠಿ", "Ginger" }, { "ಅಲ್ಲಂ", "Ginger" },
        { "ತೊಗರಿಬೇಳೆ", "Toor Dal" },
        { "ಸಾಸಿವೆ", "Mustard" },
        { "ಹತ್ತಿ", "Cotton" },
        { "ಕಬ್ಬು", "Sugarcane" },
        { "ಸೋಯಾಬೀನ್", "Soybean" },
        { "ಕಡಲೆ", "Chickpea" },
        { "ಹೆಸರುಕಾಳು", "Moong" },
        { "ಉದ್ದು", "Urad" },
        { "ಸಜ್ಜೆ", "Bajra" },
        { "ಜೋಳ", "Jowar" },
    };

    private static readonly Dictionary<string, string> LocationMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "दिल्ली", "Delhi" }, { "delhi", "Delhi" }, { "new delhi", "Delhi" }, { "नई दिल्ली", "Delhi" },
        { "आजादपुर", "Delhi" }, { "azadpur", "Delhi" },
        { "नोएडा", "Noida" }, { "noida", "Noida" },
        { "गुरुग्राम", "Gurugram" }, { "gurugram", "Gurugram" }, { "gurgaon", "Gurugram" },
        { "लखनऊ", "Lucknow" }, { "lucknow", "Lucknow" },
        { "जयपुर", "Jaipur" }, { "jaipur", "Jaipur" },
        { "मुंबई", "Mumbai" }, { "mumbai", "Mumbai" },
        { "पुणे", "Pune" }, { "pune", "Pune" },
        { "नासिक", "Nashik" }, { "nashik", "Nashik" }, { "nasik", "Nashik" },
        { "चेन्नई", "Chennai" }, { "chennai", "Chennai" },
        { "बेंगलुरु", "Bangalore" }, { "बैंगलोर", "Bangalore" }, { "bangalore", "Bangalore" }, { "bengaluru", "Bangalore" },
        { "हैदराबाद", "Hyderabad" }, { "hyderabad", "Hyderabad" },
        { "कोलकाता", "Kolkata" }, { "kolkata", "Kolkata" },
        { "पटना", "Patna" }, { "patna", "Patna" },
        { "लुधियाना", "Ludhiana" }, { "ludhiana", "Ludhiana" },
        { "अमृतसर", "Amritsar" }, { "amritsar", "Amritsar" },
        { "चंडीगढ़", "Chandigarh" }, { "chandigarh", "Chandigarh" },
        { "अहमदाबाद", "Ahmedabad" }, { "ahmedabad", "Ahmedabad" },
        { "नागपुर", "Nagpur" }, { "nagpur", "Nagpur" },
        { "इंदौर", "Indore" }, { "indore", "Indore" },
        { "भोपाल", "Bhopal" }, { "bhopal", "Bhopal" },
        { "ग्वालियर", "Gwalior" }, { "gwalior", "Gwalior" },
        { "कानपुर", "Kanpur" }, { "kanpur", "Kanpur" },
        { "वाराणसी", "Varanasi" }, { "varanasi", "Varanasi" },
        { "कोच्चि", "Kochi" }, { "kochi", "Kochi" },
        { "मैसूर", "Mysore" }, { "mysore", "Mysore" }, { "mysuru", "Mysore" },
        { "कोयंबटूर", "Coimbatore" }, { "coimbatore", "Coimbatore" },
        { "सूरत", "Surat" }, { "surat", "Surat" },
    };

    // WEATHER intent
    private static readonly string[] WeatherKeywords = {
        "मौसम", "वेदर", "weather", "बारिश", "rain", "तापमान", "temperature",
        "धूप", "sunshine", "ठंड", "cold", "गर्मी", "heat", "hot",
        "forecast", "पूर्वानुमान", "barish", "mausam"
    };

    // SOIL intent
    private static readonly string[] SoilKeywords = {
        "मिट्टी का pH", "soil pH", "मृदा", "soil health", "soil data",
        "मिट्टी की जांच", "soil test", "नाइट्रोजन", "nitrogen",
        "फॉस्फोरस", "phosphorus", "पोटैशियम", "potassium",
        "मेरी मिट्टी", "my soil", "soil report", "मृदा रिपोर्ट"
    };

    // PRICE intent
    private static readonly string[] PriceKeywords = {
        "भाव", "दाम", "कीमत", "रेट", "मंडी", "price", "rate", "cost",
        "bhav", "daam", "keemat", "mandi", "kitna", "कितना", "कितने",
        "बिकता", "बिक रहा", "बेचना", "sell", "selling",
        "प्राइस", "रेट्स", "rates", "prices", "market"
    };

    // ADVICE intent
    private static readonly string[] AdviceKeywords = {
        "खेती", "बोना", "बोई", "उगाना", "उगाई", "लगाना", "लगाई", "रोपना", "रोपाई",
        "कहाँ", "कहां", "कब", "कैसे", "क्यों", "कौन", "कितना समय",
        "मिट्टी", "सिंचाई", "खाद", "उर्वरक", "कीटनाशक", "बीज", "फसल",
        "बीमारी", "रोग", "कीट", "pest", "disease",
        "जैविक", "organic", "प्राकृतिक", "natural",
        "उपज", "पैदावार", "yield", "production",
        "farming", "cultivation", "grow", "plant", "sow", "harvest",
        "where", "when", "how", "which", "what soil", "what fertilizer",
        "soil", "irrigation", "fertilizer", "pesticide", "seed", "crop",
        "kheti", "bona", "ugana", "lagana", "ropna", "ropai",
        "kahan", "kab", "kaise", "kyon", "kaun",
        "mitti", "sinchai", "khaad", "keetnashak", "beej", "fasal"
    };

    private static readonly string[] ComparisonKeywords = {
        "और", "aur", "or", "vs", "versus", "ya", "या",
        "compare", "comparison", "तुलना", "दोनों", "dono"
    };

    private static readonly string[] DirectionalWords = {
        "se", "से", "to", "तक", "supply", "सप्लाई", "arrival", "आवक",
        "transport", "ट्रांसपोर्ट", "bhejna", "भेजना", "lana", "लाना"
    };

    public FastQueryParser(
        GeminiService geminiService,
        GeminiModelConfig modelConfig,
        ILogger<FastQueryParser> logger)
    {
        _geminiFallback = new QueryParser(geminiService, modelConfig,
            Microsoft.Extensions.Logging.LoggerFactory.Create(b => b.AddConsole())
                .CreateLogger<QueryParser>());
        _logger = logger;
    }

    public async Task<ParsedQuery> ParseQueryAsync(string transcribedText, string context, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(transcribedText))
            return new ParsedQuery("", "", "unknown", true, "Please repeat your question", 0f);

        var text = transcribedText.Trim();

        var hasWeatherIntent = WeatherKeywords.Any(k => text.Contains(k, StringComparison.OrdinalIgnoreCase));
        var hasSoilIntent = SoilKeywords.Any(k => text.Contains(k, StringComparison.OrdinalIgnoreCase));
        var hasAdviceIntent = AdviceKeywords.Any(k => text.Contains(k, StringComparison.OrdinalIgnoreCase));
        var hasPriceIntent = PriceKeywords.Any(k => text.Contains(k, StringComparison.OrdinalIgnoreCase));

        _logger.LogInformation(
            "Intent classification: WeatherIntent={Weather}, SoilIntent={Soil}, AdviceIntent={Advice}, PriceIntent={Price}, Text={Text}",
            hasWeatherIntent, hasSoilIntent, hasAdviceIntent, hasPriceIntent, text);

        if (hasWeatherIntent)
        {
            var location = ExtractEntity(text, LocationMap);
            return new ParsedQuery("", location ?? "", "weather_query", false, null, 0.95f);
        }

        if (hasSoilIntent)
            return new ParsedQuery("", "", "soil_query", false, null, 0.9f);

        if (hasAdviceIntent && !hasPriceIntent)
            return new ParsedQuery("", "", "general_question", true, null, 0.9f);

        if (hasPriceIntent)
        {
            var allCommodities = ExtractAllCommodities(text);
            var allLocations = ExtractAllLocations(text);
            var commodity = allCommodities.Count > 0 ? allCommodities[0] : null;
            var location = allLocations.Count > 0 ? allLocations[0] : ExtractEntity(text, LocationMap);

            var multiCommodity = allCommodities.Count >= 2;
            var multiLocation = allLocations.Count >= 2 && IsLikelyComparison(text, allLocations);

            if (!string.IsNullOrEmpty(commodity) && (multiCommodity || multiLocation))
            {
                var intent = (multiCommodity && multiLocation) ? "multi_commodity_comparison"
                           : multiCommodity ? "multi_commodity_query"
                           : "price_comparison";

                return new ParsedQuery(commodity, location ?? allLocations.FirstOrDefault() ?? "Delhi", intent, false, null, 0.95f)
                {
                    Commodities = allCommodities,
                    Locations = allLocations.Count > 0 ? allLocations : new List<string> { location ?? "Delhi" }
                };
            }

            if (!string.IsNullOrEmpty(commodity))
            {
                location ??= "Delhi";
                return new ParsedQuery(commodity, location, "price_query", false, null, 0.95f);
            }

            return await _geminiFallback.ParseQueryAsync(transcribedText, context, cancellationToken);
        }

        return await _geminiFallback.ParseQueryAsync(transcribedText, context, cancellationToken);
    }

    private static bool IsLikelyComparison(string text, List<string> locations)
    {
        if (locations.Count < 2) return false;
        if (DirectionalWords.Any(w => text.Contains(w, StringComparison.OrdinalIgnoreCase)))
            return false;
        var meinCount = System.Text.RegularExpressions.Regex.Matches(
            text, @"\b(mein|में)\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase).Count;
        if (meinCount >= 2) return true;
        if (ComparisonKeywords.Any(k => text.Contains(k, StringComparison.OrdinalIgnoreCase)))
            return true;
        return true;
    }

    private static List<string> ExtractAllCommodities(string text)
    {
        var commodities = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var kvp in CommodityMap.OrderByDescending(k => k.Key.Length))
        {
            if (seen.Contains(kvp.Value)) continue;
            var pattern = @"(?<=^|\s|[,।?])" + System.Text.RegularExpressions.Regex.Escape(kvp.Key) + @"(?=$|\s|[,।?])";
            if (System.Text.RegularExpressions.Regex.IsMatch(text, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase))
            {
                commodities.Add(kvp.Value);
                seen.Add(kvp.Value);
            }
        }
        return commodities;
    }

    private static List<string> ExtractAllLocations(string text)
    {
        var locations = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var kvp in LocationMap.OrderByDescending(k => k.Key.Length))
        {
            if (text.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase) && !seen.Contains(kvp.Value))
            {
                locations.Add(kvp.Value);
                seen.Add(kvp.Value);
            }
        }
        return locations;
    }

    private static string? ExtractEntity(string text, Dictionary<string, string> map)
    {
        foreach (var kvp in map.OrderByDescending(k => k.Key.Length))
        {
            if (text.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase))
                return kvp.Value;
        }
        return null;
    }
}
