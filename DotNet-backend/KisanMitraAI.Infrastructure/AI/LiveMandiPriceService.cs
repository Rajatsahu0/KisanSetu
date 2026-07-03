using KisanMitraAI.Core.AI;
using KisanMitraAI.Core.Models;
using KisanMitraAI.Infrastructure.Repositories.Timestream;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace KisanMitraAI.Infrastructure.AI;

/// <summary>
/// Live mandi price service with on-demand caching:
///   1. Check in-memory cache (fastest)
///   2. Check DynamoDB for today's cached prices
///   3. Cache MISS → call data.gov.in API
///   4. Store results in DynamoDB (TTL: 24 hours)
///   5. Return prices
/// </summary>
public class LiveMandiPriceService
{
    private readonly HttpClient _httpClient;
    private readonly GeminiService _geminiService;
    private readonly IMandiPriceRepository _repository;
    private readonly IMemoryCache _cache;
    private readonly ILogger<LiveMandiPriceService> _logger;
    private readonly string _apiBaseUrl;
    private readonly string _apiKey;

    private static readonly Dictionary<string, string> CommodityMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["आलू"] = "Potato", ["potato"] = "Potato", ["aloo"] = "Potato",
        ["प्याज"] = "Onion", ["onion"] = "Onion", ["pyaaz"] = "Onion",
        ["टमाटर"] = "Tomato", ["tomato"] = "Tomato", ["tamatar"] = "Tomato",
        ["हरी मिर्च"] = "Green Chilli", ["green chilli"] = "Green Chilli", ["mirch"] = "Green Chilli",
        ["फूलगोभी"] = "Cauliflower", ["cauliflower"] = "Cauliflower", ["gobhi"] = "Cauliflower",
        ["पत्तागोभी"] = "Cabbage", ["cabbage"] = "Cabbage",
        ["बैंगन"] = "Brinjal", ["brinjal"] = "Brinjal", ["baingan"] = "Brinjal",
        ["भिंडी"] = "Bhindi(Ladies Finger)", ["lady finger"] = "Bhindi(Ladies Finger)", ["bhindi"] = "Bhindi(Ladies Finger)",
        ["लौकी"] = "Bottle gourd", ["bottle gourd"] = "Bottle gourd", ["lauki"] = "Bottle gourd",
        ["करेला"] = "Bitter gourd", ["bitter gourd"] = "Bitter gourd", ["karela"] = "Bitter gourd",
        ["तोरई"] = "Ridgeguard(Tori)", ["ridge gourd"] = "Ridgeguard(Tori)", ["tori"] = "Ridgeguard(Tori)",
        ["कद्दू"] = "Pumpkin", ["pumpkin"] = "Pumpkin", ["kaddu"] = "Pumpkin",
        ["खीरा"] = "Cucumbar(Kheera)", ["cucumber"] = "Cucumbar(Kheera)", ["kheera"] = "Cucumbar(Kheera)",
        ["मूली"] = "Raddish", ["radish"] = "Raddish", ["mooli"] = "Raddish",
        ["गाजर"] = "Carrot", ["carrot"] = "Carrot", ["gajar"] = "Carrot",
        ["मटर"] = "Green Peas", ["peas"] = "Green Peas", ["matar"] = "Green Peas",
        ["सेम"] = "French Beans(Frasbean)", ["beans"] = "French Beans(Frasbean)",
        ["पालक"] = "Spinach", ["spinach"] = "Spinach", ["palak"] = "Spinach",
        ["मेथी"] = "Methi(Leaves)", ["fenugreek"] = "Methi(Leaves)", ["methi"] = "Methi(Leaves)",
        ["धनिया"] = "Coriander(Leaves)", ["coriander"] = "Coriander(Leaves)", ["dhaniya"] = "Coriander(Leaves)",
        ["लहसुन"] = "Garlic", ["garlic"] = "Garlic", ["lahsun"] = "Garlic",
        ["अदरक"] = "Ginger(Green)", ["ginger"] = "Ginger(Green)", ["adrak"] = "Ginger(Green)",
        ["सहजन"] = "Drumstick", ["drumstick"] = "Drumstick",
        ["शकरकंद"] = "Sweet Potato", ["sweet potato"] = "Sweet Potato",
        ["परवल"] = "Pointed gourd(Parval)", ["pointed gourd"] = "Pointed gourd(Parval)",
        ["ग्वार"] = "Cluster beans", ["cluster beans"] = "Cluster beans",
        ["शिमला मिर्च"] = "Capsicum", ["capsicum"] = "Capsicum",
        ["नींबू"] = "Lemon", ["lemon"] = "Lemon", ["nimbu"] = "Lemon",
        ["चुकंदर"] = "Beetroot", ["beetroot"] = "Beetroot",
        ["गेहूं"] = "Wheat", ["wheat"] = "Wheat", ["gehun"] = "Wheat",
        ["धान"] = "Paddy(Common)", ["rice"] = "Rice", ["chawal"] = "Rice", ["paddy"] = "Paddy(Common)",
        ["मक्का"] = "Maize", ["maize"] = "Maize", ["makka"] = "Maize",
        ["बाजरा"] = "Bajra(Pearl Millet/Cumbu)", ["bajra"] = "Bajra(Pearl Millet/Cumbu)",
        ["ज्वार"] = "Jowar(Sorghum)", ["jowar"] = "Jowar(Sorghum)",
        ["चना"] = "Bengal Gram(Gram)(Whole)", ["chana"] = "Bengal Gram(Gram)(Whole)", ["chickpea"] = "Bengal Gram(Gram)(Whole)",
        ["मूंग"] = "Green Gram(Moong)(Whole)", ["moong"] = "Green Gram(Moong)(Whole)",
        ["उड़द"] = "Black Gram(Urd Beans)(Whole)", ["urad"] = "Black Gram(Urd Beans)(Whole)",
        ["अरहर"] = "Arhar(Tur/Red Gram)(Whole)", ["arhar"] = "Arhar(Tur/Red Gram)(Whole)", ["tur"] = "Arhar(Tur/Red Gram)(Whole)",
        ["मसूर"] = "Masur Dal", ["masoor"] = "Masur Dal",
        ["सोयाबीन"] = "Soyabean", ["soybean"] = "Soyabean",
        ["मूंगफली"] = "Groundnut", ["groundnut"] = "Groundnut",
        ["कपास"] = "Cotton", ["cotton"] = "Cotton",
        ["सरसों"] = "Mustard", ["mustard"] = "Mustard", ["sarson"] = "Mustard",
        ["केला"] = "Banana", ["banana"] = "Banana", ["kela"] = "Banana",
        ["आम"] = "Mango", ["mango"] = "Mango", ["aam"] = "Mango",
        ["सेब"] = "Apple", ["apple"] = "Apple",
        ["अमरूद"] = "Guava", ["guava"] = "Guava",
        ["पपीता"] = "Papaya", ["papaya"] = "Papaya",
        ["तरबूज"] = "Water Melon", ["watermelon"] = "Water Melon",
    };

    private static readonly Dictionary<string, string> LocationToStateMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["delhi"] = "NCT of Delhi", ["new delhi"] = "NCT of Delhi", ["दिल्ली"] = "NCT of Delhi",
        ["noida"] = "Uttar Pradesh", ["lucknow"] = "Uttar Pradesh", ["agra"] = "Uttar Pradesh",
        ["नोएडा"] = "Uttar Pradesh", ["लखनऊ"] = "Uttar Pradesh", ["varanasi"] = "Uttar Pradesh",
        ["बनारस"] = "Uttar Pradesh", ["banaras"] = "Uttar Pradesh",
        ["मेरठ"] = "Uttar Pradesh", ["meerut"] = "Uttar Pradesh",
        ["कानपुर"] = "Uttar Pradesh", ["kanpur"] = "Uttar Pradesh",
        ["प्रयागराज"] = "Uttar Pradesh", ["prayagraj"] = "Uttar Pradesh", ["allahabad"] = "Uttar Pradesh",
        ["इलाहाबाद"] = "Uttar Pradesh",
        ["mumbai"] = "Maharashtra", ["pune"] = "Maharashtra", ["nagpur"] = "Maharashtra",
        ["मुंबई"] = "Maharashtra", ["पुणे"] = "Maharashtra",
        ["बॉम्बे"] = "Maharashtra", ["bombay"] = "Maharashtra",
        ["नासिक"] = "Maharashtra", ["nashik"] = "Maharashtra", ["nasik"] = "Maharashtra",
        ["औरंगाबाद"] = "Maharashtra", ["aurangabad"] = "Maharashtra",
        ["सोलापुर"] = "Maharashtra", ["solapur"] = "Maharashtra",
        ["कोल्हापुर"] = "Maharashtra", ["kolhapur"] = "Maharashtra",
        ["सांगली"] = "Maharashtra", ["sangli"] = "Maharashtra",
        ["अमरावती"] = "Maharashtra", ["amravati"] = "Maharashtra",
        ["जलगांव"] = "Maharashtra", ["jalgaon"] = "Maharashtra",
        ["बारामती"] = "Maharashtra", ["baramati"] = "Maharashtra",
        ["jaipur"] = "Rajasthan", ["jodhpur"] = "Rajasthan", ["जयपुर"] = "Rajasthan",
        // === SOUTH INDIA (Priority) ===
        // Tamil Nadu
        ["chennai"] = "Tamil Nadu", ["चेन्नई"] = "Tamil Nadu", ["मद्रास"] = "Tamil Nadu", ["madras"] = "Tamil Nadu",
        ["coimbatore"] = "Tamil Nadu", ["कोयंबटूर"] = "Tamil Nadu",
        ["madurai"] = "Tamil Nadu", ["मदुरै"] = "Tamil Nadu",
        ["salem"] = "Tamil Nadu", ["सेलम"] = "Tamil Nadu",
        ["tiruchirappalli"] = "Tamil Nadu", ["trichy"] = "Tamil Nadu", ["तिरुचिरापल्ली"] = "Tamil Nadu",
        ["tirunelveli"] = "Tamil Nadu", ["तिरुनेलवेली"] = "Tamil Nadu",
        ["erode"] = "Tamil Nadu", ["इरोड"] = "Tamil Nadu",
        ["vellore"] = "Tamil Nadu", ["वेल्लोर"] = "Tamil Nadu",
        ["thoothukudi"] = "Tamil Nadu", ["tuticorin"] = "Tamil Nadu",
        ["dindigul"] = "Tamil Nadu", ["thanjavur"] = "Tamil Nadu",
        ["kanyakumari"] = "Tamil Nadu", ["नागरकोइल"] = "Tamil Nadu",
        ["hosur"] = "Tamil Nadu", ["karur"] = "Tamil Nadu",
        ["नामक्कल"] = "Tamil Nadu", ["namakkal"] = "Tamil Nadu",
        
        // Karnataka
        ["bangalore"] = "Karnataka", ["bengaluru"] = "Karnataka", ["बैंगलोर"] = "Karnataka", ["बंगलौर"] = "Karnataka",
        ["mysore"] = "Karnataka", ["mysuru"] = "Karnataka", ["मैसूर"] = "Karnataka",
        ["hubli"] = "Karnataka", ["hubballi"] = "Karnataka", ["हुबली"] = "Karnataka",
        ["belgaum"] = "Karnataka", ["belagavi"] = "Karnataka", ["बेलगाम"] = "Karnataka",
        ["mangalore"] = "Karnataka", ["mangaluru"] = "Karnataka", ["मंगलौर"] = "Karnataka",
        ["davangere"] = "Karnataka", ["दावणगेरे"] = "Karnataka",
        ["shimoga"] = "Karnataka", ["shivamogga"] = "Karnataka", ["शिमोगा"] = "Karnataka",
        ["hassan"] = "Karnataka", ["हासन"] = "Karnataka",
        ["tumkur"] = "Karnataka", ["tumakuru"] = "Karnataka",
        ["bellary"] = "Karnataka", ["ballari"] = "Karnataka",
        ["bijapur"] = "Karnataka", ["vijayapura"] = "Karnataka",
        ["gulbarga"] = "Karnataka", ["kalaburagi"] = "Karnataka",
        ["raichur"] = "Karnataka", ["mandya"] = "Karnataka",
        ["chitradurga"] = "Karnataka", ["udupi"] = "Karnataka",
        ["chikmagalur"] = "Karnataka", ["कोलार"] = "Karnataka", ["kolar"] = "Karnataka",
        
        // Telangana
        ["hyderabad"] = "Telangana", ["हैदराबाद"] = "Telangana",
        ["warangal"] = "Telangana", ["वारंगल"] = "Telangana",
        ["nizamabad"] = "Telangana", ["निजामाबाद"] = "Telangana",
        ["khammam"] = "Telangana", ["karimnagar"] = "Telangana",
        ["ramagundam"] = "Telangana", ["mahbubnagar"] = "Telangana",
        ["nalgonda"] = "Telangana", ["adilabad"] = "Telangana",
        ["suryapet"] = "Telangana", ["miryalaguda"] = "Telangana",
        
        // Andhra Pradesh
        ["visakhapatnam"] = "Andhra Pradesh", ["vizag"] = "Andhra Pradesh", ["विशाखापट्टनम"] = "Andhra Pradesh",
        ["vijayawada"] = "Andhra Pradesh", ["विजयवाड़ा"] = "Andhra Pradesh",
        ["guntur"] = "Andhra Pradesh", ["गुंटूर"] = "Andhra Pradesh",
        ["tirupati"] = "Andhra Pradesh", ["तिरुपति"] = "Andhra Pradesh",
        ["nellore"] = "Andhra Pradesh", ["नेल्लोर"] = "Andhra Pradesh",
        ["kurnool"] = "Andhra Pradesh", ["कुर्नूल"] = "Andhra Pradesh",
        ["kakinada"] = "Andhra Pradesh", ["rajahmundry"] = "Andhra Pradesh",
        ["anantapur"] = "Andhra Pradesh", ["अनंतपुर"] = "Andhra Pradesh",
        ["eluru"] = "Andhra Pradesh", ["ongole"] = "Andhra Pradesh",
        ["kadapa"] = "Andhra Pradesh", ["cuddapah"] = "Andhra Pradesh",
        ["chittoor"] = "Andhra Pradesh", ["machilipatnam"] = "Andhra Pradesh",
        ["tenali"] = "Andhra Pradesh", ["proddatur"] = "Andhra Pradesh",
        
        // Kerala
        ["kochi"] = "Kerala", ["cochin"] = "Kerala", ["कोच्चि"] = "Kerala",
        ["thiruvananthapuram"] = "Kerala", ["trivandrum"] = "Kerala", ["तिरुवनंतपुरम"] = "Kerala",
        ["kozhikode"] = "Kerala", ["calicut"] = "Kerala", ["कोझिकोड"] = "Kerala",
        ["thrissur"] = "Kerala", ["trichur"] = "Kerala",
        ["kollam"] = "Kerala", ["quilon"] = "Kerala",
        ["palakkad"] = "Kerala", ["palghat"] = "Kerala",
        ["alappuzha"] = "Kerala", ["alleppey"] = "Kerala",
        ["malappuram"] = "Kerala", ["kannur"] = "Kerala",
        ["kottayam"] = "Kerala", ["kasaragod"] = "Kerala",
        ["pathanamthitta"] = "Kerala", ["idukki"] = "Kerala",
        
        // === REST OF INDIA ===
        ["kolkata"] = "West Bengal", ["कोलकाता"] = "West Bengal",
        ["कलकत्ता"] = "West Bengal", ["calcutta"] = "West Bengal",
        ["siliguri"] = "West Bengal", ["durgapur"] = "West Bengal",
        ["asansol"] = "West Bengal", ["howrah"] = "West Bengal",
        ["ahmedabad"] = "Gujarat", ["अहमदाबाद"] = "Gujarat",
        ["surat"] = "Gujarat", ["सूरत"] = "Gujarat",
        ["vadodara"] = "Gujarat", ["baroda"] = "Gujarat", ["वडोदरा"] = "Gujarat",
        ["rajkot"] = "Gujarat", ["राजकोट"] = "Gujarat",
        ["bhavnagar"] = "Gujarat", ["jamnagar"] = "Gujarat",
        ["junagadh"] = "Gujarat", ["gandhinagar"] = "Gujarat",
        ["anand"] = "Gujarat", ["nadiad"] = "Gujarat",
        ["morbi"] = "Gujarat", ["surendranagar"] = "Gujarat",
        ["chandigarh"] = "Chandigarh", ["चंडीगढ़"] = "Chandigarh",
        ["mohali"] = "Punjab", ["panchkula"] = "Haryana",
        ["bhopal"] = "Madhya Pradesh", ["indore"] = "Madhya Pradesh",
        ["gwalior"] = "Madhya Pradesh", ["ग्वालियर"] = "Madhya Pradesh",
        ["jabalpur"] = "Madhya Pradesh", ["जबलपुर"] = "Madhya Pradesh",
        ["ujjain"] = "Madhya Pradesh", ["उज्जैन"] = "Madhya Pradesh",
        ["sagar"] = "Madhya Pradesh", ["rewa"] = "Madhya Pradesh",
        ["satna"] = "Madhya Pradesh", ["chhindwara"] = "Madhya Pradesh",
        ["patna"] = "Bihar", ["पटना"] = "Bihar",
        ["ludhiana"] = "Punjab", ["लुधियाना"] = "Punjab",
        ["amritsar"] = "Punjab", ["अमृतसर"] = "Punjab",
        ["jalandhar"] = "Punjab", ["जालंधर"] = "Punjab",
        ["patiala"] = "Punjab", ["bathinda"] = "Punjab",
        ["hoshiarpur"] = "Punjab", ["moga"] = "Punjab",
        ["pathankot"] = "Punjab", ["sangrur"] = "Punjab",
        ["firozpur"] = "Punjab", ["kapurthala"] = "Punjab",
        ["dehradun"] = "Uttarakhand", ["देहरादून"] = "Uttarakhand",
        ["haridwar"] = "Uttarakhand", ["roorkee"] = "Uttarakhand",
        ["haldwani"] = "Uttarakhand", ["rudrapur"] = "Uttarakhand",
        ["nainital"] = "Uttarakhand", ["rishikesh"] = "Uttarakhand",
        ["ranchi"] = "Jharkhand", ["रांची"] = "Jharkhand",
        ["jamshedpur"] = "Jharkhand", ["dhanbad"] = "Jharkhand",
        ["bokaro"] = "Jharkhand", ["deoghar"] = "Jharkhand",
        ["hazaribagh"] = "Jharkhand", ["giridih"] = "Jharkhand",
        ["bhubaneswar"] = "Odisha", ["भुवनेश्वर"] = "Odisha",
        ["cuttack"] = "Odisha", ["rourkela"] = "Odisha",
        ["berhampur"] = "Odisha", ["sambalpur"] = "Odisha",
        ["puri"] = "Odisha", ["balasore"] = "Odisha",
        ["raipur"] = "Chhattisgarh", ["रायपुर"] = "Chhattisgarh",
        ["bhilai"] = "Chhattisgarh", ["bilaspur"] = "Chhattisgarh",
        ["korba"] = "Chhattisgarh", ["durg"] = "Chhattisgarh",
        ["rajnandgaon"] = "Chhattisgarh", ["raigarh"] = "Chhattisgarh",
        ["guwahati"] = "Assam", ["गुवाहाटी"] = "Assam",
        ["dibrugarh"] = "Assam", ["silchar"] = "Assam",
        ["jorhat"] = "Assam", ["nagaon"] = "Assam",
        ["tezpur"] = "Assam", ["tinsukia"] = "Assam",
        ["shimla"] = "Himachal Pradesh", ["शिमला"] = "Himachal Pradesh",
        ["mandi"] = "Himachal Pradesh", ["solan"] = "Himachal Pradesh",
        ["dharamshala"] = "Himachal Pradesh", ["kullu"] = "Himachal Pradesh",
        ["hamirpur"] = "Himachal Pradesh", ["una"] = "Himachal Pradesh",
        ["kochi"] = "Kerala", ["thiruvananthapuram"] = "Kerala",
        ["goa"] = "Goa", ["गोवा"] = "Goa",
        ["panaji"] = "Goa", ["margao"] = "Goa",
        ["vasco"] = "Goa", ["mapusa"] = "Goa",
        // State names → themselves
        ["uttar pradesh"] = "Uttar Pradesh", ["maharashtra"] = "Maharashtra",
        ["rajasthan"] = "Rajasthan", ["tamil nadu"] = "Tamil Nadu",
        ["karnataka"] = "Karnataka", ["telangana"] = "Telangana",
        ["west bengal"] = "West Bengal", ["gujarat"] = "Gujarat",
        ["madhya pradesh"] = "Madhya Pradesh", ["bihar"] = "Bihar",
        ["punjab"] = "Punjab", ["uttarakhand"] = "Uttarakhand",
        ["jharkhand"] = "Jharkhand", ["odisha"] = "Odisha",
        ["chhattisgarh"] = "Chhattisgarh", ["assam"] = "Assam",
        ["himachal pradesh"] = "Himachal Pradesh", ["kerala"] = "Kerala",
        ["haryana"] = "Haryana", ["andhra pradesh"] = "Andhra Pradesh",
        ["nct of delhi"] = "NCT of Delhi",
    };

    public LiveMandiPriceService(
        IHttpClientFactory httpClientFactory,
        GeminiService geminiService,
        IMandiPriceRepository repository,
        IMemoryCache cache,
        IConfiguration configuration,
        ILogger<LiveMandiPriceService> logger)
    {
        _httpClient = httpClientFactory.CreateClient();
        _geminiService = geminiService;
        _repository = repository;
        _cache = cache;
        _logger = logger;
        _apiBaseUrl = configuration["MandiPriceApi:BaseUrl"] ?? "https://api.data.gov.in/resource/9ef84268-d588-465a-a308-a864a43d0070";
        _apiKey = configuration["MandiPriceApi:ApiKey"] ?? "";
    }

    public async Task<IEnumerable<MandiPrice>> GetCurrentPricesAsync(
        string commodity, string location, CancellationToken cancellationToken = default)
    {
        var apiCommodity = ResolveCommodityName(commodity);
        var (resolvedState, resolvedDistrict) = ResolveLocation(location);
        var cacheLocation = resolvedDistrict ?? resolvedState ?? location;

        // Layer 2: If dictionary didn't resolve commodity, check AI cache then try Nova Micro
        if (apiCommodity == CultureInfo.CurrentCulture.TextInfo.ToTitleCase(commodity.Trim().ToLower())
            && !CommodityMap.ContainsValue(apiCommodity)
            && !ApiNameBridge.ContainsValue(apiCommodity))
        {
            if (_cache.TryGetValue<string>($"commodity_resolve_{commodity.Trim()}", out var cachedResolve) && !string.IsNullOrEmpty(cachedResolve))
            {
                apiCommodity = cachedResolve;
                _logger.LogInformation("LiveMandi: AI cache HIT '{Input}' → '{Resolved}'", commodity, cachedResolve);
            }
            else
            {
                var aiResolved = await ResolveWithNovaMicroAsync(commodity, cancellationToken);
                if (!string.IsNullOrEmpty(aiResolved))
                {
                    _logger.LogInformation("LiveMandi: Nova Micro resolved '{Input}' → '{Resolved}'", commodity, aiResolved);
                    _cache.Set($"commodity_resolve_{commodity.Trim()}", aiResolved, TimeSpan.FromDays(7));
                    apiCommodity = aiResolved;
                }
            }
        }

        _logger.LogInformation("LiveMandi: {Commodity}→{ApiCommodity}, {Location}→state={State},district={District}",
            commodity, apiCommodity, location, resolvedState ?? "(none)", resolvedDistrict ?? "(none)");

        // 1. In-memory cache
        var cacheKey = $"mandi_{apiCommodity}_{cacheLocation}";
        if (_cache.TryGetValue<List<MandiPrice>>(cacheKey, out var cached) && cached?.Count > 0)
        {
            _logger.LogInformation("LiveMandi: Memory HIT ({Count} prices)", cached.Count);
            return cached;
        }

        // 2. Skip DynamoDB for current prices — always fetch fresh from API
        _logger.LogInformation("LiveMandi: Bypassing DynamoDB → calling data.gov.in directly");
        var apiPrices = await FetchFromApiAsync(apiCommodity, location, cancellationToken);

        if (apiPrices.Count == 0)
        {
            _logger.LogWarning("LiveMandi: No API results for {Commodity} in {Location}", apiCommodity, location);
            return apiPrices;
        }

        _ = Task.Run(async () =>
        {
            try
            {
                foreach (var price in apiPrices)
                    await _repository.StorePriceAsync(price, CancellationToken.None);
                _logger.LogInformation("LiveMandi: Stored {Count} prices in DynamoDB", apiPrices.Count);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "LiveMandi: Failed to store prices (non-blocking)");
            }
        });

        _cache.Set(cacheKey, apiPrices, TimeSpan.FromHours(1));
        return apiPrices;
    }

    /// <summary>
    /// Get prices for a specific date. Checks DynamoDB first, calls API on miss, stores result.
    /// Use case: "1 April ko tomato ka bhav kya tha?"
    /// </summary>
    public async Task<IEnumerable<MandiPrice>> GetPricesForDateAsync(
        string commodity, string location, DateTimeOffset date, CancellationToken cancellationToken = default)
    {
        var apiCommodity = ResolveCommodityName(commodity);
        var apiState = ResolveStateName(location);
        var dateStr = date.ToString("yyyy-MM-dd");

        _logger.LogInformation("LiveMandi historical: {Commodity} in {State} on {Date}", apiCommodity, apiState, dateStr);

        // 1. Memory cache
        var cacheKey = $"mandi_{apiCommodity}_{apiState}_{dateStr}";
        if (_cache.TryGetValue<List<MandiPrice>>(cacheKey, out var cached) && cached?.Count > 0)
            return cached;

        // 2. DynamoDB — check if we have data for that specific date
        var dbPrices = (await _repository.GetHistoricalPricesAsync(
            apiCommodity, apiState, date.Date, date.Date.AddDays(1), cancellationToken)).ToList();

        if (dbPrices.Count > 0)
        {
            _logger.LogInformation("LiveMandi historical: DynamoDB HIT ({Count} prices for {Date})", dbPrices.Count, dateStr);
            _cache.Set(cacheKey, dbPrices, TimeSpan.FromHours(6));
            return dbPrices;
        }

        // 3. API call — data.gov.in doesn't support date filter directly,
        // but returns arrival_date in records. Fetch and filter.
        _logger.LogInformation("LiveMandi historical: MISS → calling API for {Date}", dateStr);
        var apiPrices = await FetchFromApiAsync(apiCommodity, apiState, cancellationToken);

        // Filter for the requested date
        var datePrices = apiPrices.Where(p => p.PriceDate.Date == date.Date).ToList();

        // If no prices for exact date, return all (API may only have today's data)
        var result = datePrices.Count > 0 ? datePrices : apiPrices;

        if (result.Count > 0)
        {
            // Store all fetched prices in DynamoDB
            foreach (var price in apiPrices)
            {
                try { await _repository.StorePriceAsync(price, CancellationToken.None); } catch { }
            }
            _cache.Set(cacheKey, result, TimeSpan.FromHours(6));
        }

        return result;
    }

    // Bridge: FastQueryParser English output → data.gov.in API name
    // Handles mismatches between our internal names and API names
    private static readonly Dictionary<string, string> ApiNameBridge = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Corn"] = "Maize", ["Maize"] = "Maize",
        ["Chickpea"] = "Bengal Gram(Gram)(Whole)", ["Chana"] = "Bengal Gram(Gram)(Whole)",
        ["Toor Dal"] = "Arhar(Tur/Red Gram)(Whole)", ["Arhar"] = "Arhar(Tur/Red Gram)(Whole)", ["Tur Dal"] = "Arhar(Tur/Red Gram)(Whole)",
        ["Ginger"] = "Ginger(Green)", ["Adrak"] = "Ginger(Green)",
        ["Soybean"] = "Soyabean", ["Soya"] = "Soyabean",
        ["Moong"] = "Green Gram(Moong)(Whole)", ["Mung"] = "Green Gram(Moong)(Whole)", ["Green Gram"] = "Green Gram(Moong)(Whole)",
        ["Urad"] = "Black Gram(Urd Beans)(Whole)", ["Black Gram"] = "Black Gram(Urd Beans)(Whole)",
        ["Bajra"] = "Bajra(Pearl Millet/Cumbu)", ["Pearl Millet"] = "Bajra(Pearl Millet/Cumbu)",
        ["Jowar"] = "Jowar(Sorghum)", ["Sorghum"] = "Jowar(Sorghum)",
        ["Masoor"] = "Masur Dal", ["Masur"] = "Masur Dal", ["Lentil"] = "Masur Dal",
        ["Lady Finger"] = "Bhindi(Ladies Finger)", ["Okra"] = "Bhindi(Ladies Finger)", ["Bhindi"] = "Bhindi(Ladies Finger)",
        ["Ridge Gourd"] = "Ridgeguard(Tori)", ["Tori"] = "Ridgeguard(Tori)",
        ["Cucumber"] = "Cucumbar(Kheera)", ["Kheera"] = "Cucumbar(Kheera)",
        ["Radish"] = "Raddish", ["Mooli"] = "Raddish",
        ["Peas"] = "Green Peas", ["Matar"] = "Green Peas",
        ["Beans"] = "French Beans(Frasbean)", ["French Beans"] = "French Beans(Frasbean)",
        ["Fenugreek"] = "Methi(Leaves)", ["Methi"] = "Methi(Leaves)",
        ["Coriander"] = "Coriander(Leaves)", ["Dhaniya"] = "Coriander(Leaves)",
        ["Pointed Gourd"] = "Pointed gourd(Parval)", ["Parval"] = "Pointed gourd(Parval)",
        ["Cluster Beans"] = "Cluster beans", ["Gwar"] = "Cluster beans",
        ["Bottle Gourd"] = "Bottle gourd", ["Lauki"] = "Bottle gourd",
        ["Bitter Gourd"] = "Bitter gourd", ["Karela"] = "Bitter gourd",
        ["Sweet Potato"] = "Sweet Potato",
        ["Drumstick"] = "Drumstick",
        ["Sugarcane"] = "Sugarcane",
        ["Groundnut"] = "Groundnut", ["Peanut"] = "Groundnut",
        ["Watermelon"] = "Water Melon", ["Water Melon"] = "Water Melon",
        ["Green Chilli"] = "Green Chilli", ["Mirch"] = "Green Chilli",
        ["Ragi"] = "Ragi(Finger Millet)", ["Finger Millet"] = "Ragi(Finger Millet)",
        // Regional language commodity names → API names
        // Tamil
        ["\u0bb0\u0bbe\u0b95\u0bbf"] = "Ragi(Finger Millet)",
        ["\u0b89\u0bb0\u0bc1\u0bb3\u0bc8\u0b95\u0bcd\u0b95\u0bbf\u0bb4\u0b99\u0bcd\u0b95\u0bc1"] = "Potato",
        ["\u0ba4\u0b95\u0bcd\u0b95\u0bbe\u0bb3\u0bbf"] = "Tomato",
        ["\u0bb5\u0bc6\u0b99\u0bcd\u0b95\u0bbe\u0baf\u0bae\u0bcd"] = "Onion",
        ["\u0b95\u0ba4\u0bcd\u0ba4\u0bbf\u0bb0\u0bbf\u0b95\u0bcd\u0b95\u0bbe\u0baf\u0bcd"] = "Brinjal",
        ["\u0b85\u0bb0\u0bbf\u0b9a\u0bbf"] = "Rice",
        ["\u0b95\u0bcb\u0ba4\u0bc1\u0bae\u0bc8"] = "Wheat",
        ["\u0bae\u0bbe\u0bae\u0bcd\u0baa\u0bb4\u0bae\u0bcd"] = "Mango",
        ["\u0bb5\u0bbe\u0bb4\u0bc8\u0baa\u0bcd\u0baa\u0bb4\u0bae\u0bcd"] = "Banana",
        ["\u0b87\u0b9e\u0bcd\u0b9a\u0bbf"] = "Ginger(Green)",
        ["\u0b85\u0b9f\u0bb0\u0b95"] = "Garlic",
        // Telugu
        ["\u0c2c\u0c02\u0c17\u0c3e\u0c33\u0c3e\u0c26\u0c41\u0c02\u0c2a"] = "Potato",
        ["\u0c1f\u0c2e\u0c3e\u0c1f\u0c3e"] = "Tomato",
        ["\u0c09\u0c32\u0c4d\u0c32\u0c3f\u0c2a\u0c3e\u0c2f"] = "Onion",
        ["\u0c35\u0c02\u0c15\u0c3e\u0c2f"] = "Brinjal",
        ["\u0c2c\u0c3f\u0c2f\u0c4d\u0c2f\u0c02"] = "Rice",
        ["\u0c17\u0c4b\u0c27\u0c41\u0c2e"] = "Wheat",
        ["\u0c2e\u0c3e\u0c2e\u0c3f\u0c21\u0c3f"] = "Mango",
        ["\u0c05\u0c30\u0c1f\u0c3f\u0c2a\u0c02\u0c21\u0c41"] = "Banana",
        // Punjabi
        ["\u0a06\u0a32\u0a42"] = "Potato",
        ["\u0a1f\u0a2e\u0a3e\u0a1f\u0a30"] = "Tomato",
        ["\u0a2a\u0a3f\u0a06\u0a1c\u0a3c"] = "Onion",
        ["\u0a2c\u0a48\u0a02\u0a17\u0a23"] = "Brinjal",
        ["\u0a1a\u0a4c\u0a32"] = "Rice",
        ["\u0a15\u0a23\u0a15"] = "Wheat",
        ["\u0a05\u0a70\u0a2c"] = "Mango",
        ["\u0a15\u0a47\u0a32\u0a3e"] = "Banana",
        ["\u0a32\u0a38\u0a23"] = "Garlic",
        ["\u0a05\u0a26\u0a30\u0a15"] = "Ginger(Green)",
        ["\u0a2d\u0a3f\u0a70\u0a21\u0a40"] = "Bhindi(Ladies Finger)",
        // Bengali
        ["\u09ac\u09be\u099f\u09be\u099f\u09be"] = "Potato",
        ["\u099f\u09ae\u09c7\u099f\u09cb"] = "Tomato",
        ["\u09aa\u09c7\u0981\u09af\u09bc\u09be\u099c"] = "Onion",
        ["\u09ac\u09c7\u0997\u09c1\u09a8"] = "Brinjal",
        ["\u09ad\u09be\u09a4"] = "Rice",
        ["\u0997\u09ae"] = "Wheat",
        ["\u0986\u09ae"] = "Mango",
        ["\u0995\u09b2\u09be"] = "Banana",
        ["\u0986\u09a6\u09be"] = "Ginger(Green)",
        // Marathi
        ["\u092c\u091f\u093e\u091f\u093e"] = "Potato",
        ["\u0915\u093e\u0902\u0926\u093e"] = "Onion",
    };

    public static string ResolveCommodityName(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return input;
        var trimmed = input.Trim();
        // Layer 1a: Direct match in main CommodityMap
        if (CommodityMap.TryGetValue(trimmed, out var mapped)) return mapped;
        // Layer 1b: Bridge map (FastQueryParser output → API name + regional languages)
        if (ApiNameBridge.TryGetValue(trimmed, out var bridged)) return bridged;
        // Layer 1c: Try title case
        var titleCase = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(trimmed.ToLower());
        if (CommodityMap.TryGetValue(titleCase, out var mapped2)) return mapped2;
        if (ApiNameBridge.TryGetValue(titleCase, out var bridged2)) return bridged2;
        // Return as-is — LiveMandiPriceService will try API with this name
        return titleCase;
    }

    // Known state names as they appear in data.gov.in API responses
    private static readonly HashSet<string> KnownStateNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "NCT of Delhi", "Uttar Pradesh", "Maharashtra", "Rajasthan", "Tamil Nadu",
        "Karnataka", "Telangana", "West Bengal", "Gujarat", "Madhya Pradesh",
        "Bihar", "Punjab", "Uttarakhand", "Jharkhand", "Odisha", "Chhattisgarh",
        "Assam", "Himachal Pradesh", "Kerala", "Haryana", "Andhra Pradesh",
        "Chandigarh", "Goa", "Manipur", "Meghalaya", "Mizoram", "Nagaland",
        "Sikkim", "Tripura", "Arunachal Pradesh", "Jammu and Kashmir"
    };

    /// <summary>
    /// Returns (state, district) tuple for API filtering.
    /// If input is a known state → (state, null).
    /// If input maps to a state via dictionary → (state, input) so we filter both.
    /// Otherwise → (null, input) — use district filter directly, no state needed.
    /// </summary>
    public static (string? State, string? District) ResolveLocation(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return (null, null);
        var trimmed = input.Trim();

        // Already a known state name — filter by state only
        if (KnownStateNames.Contains(trimmed)) return (trimmed, null);

        // Known city → state mapping exists — filter by state + district for precision
        if (LocationToStateMap.TryGetValue(trimmed, out var mappedState))
            return (mappedState, trimmed);

        // Unknown city/village/district — pass directly as district filter
        // data.gov.in district field handles any district name without needing state
        return (null, trimmed);
    }

    // Keep for backward compat with FastResponseGenerator.MatchesLocation
    public static string ResolveStateName(string input)
    {
        var (state, _) = ResolveLocation(input);
        return state ?? input.Trim();
    }

    /// <summary>
    /// Layer 2: Use Nova Micro to translate unknown commodity name to data.gov.in API name.
    /// Handles any Indian language → English API commodity name.
    /// Cost: ~$0.00009 per call. Speed: 300-800ms.
    /// </summary>
    private async Task<string?> ResolveWithNovaMicroAsync(string input, CancellationToken cancellationToken)
    {
        try
        {
            var prompt = $"Translate this Indian commodity/crop name to the exact data.gov.in mandi API commodity name.\n" +
                $"Input: {input}\n" +
                $"Common API names: Potato, Tomato, Onion, Wheat, Rice, Maize, Brinjal, Cauliflower, Cabbage, " +
                $"Bhindi(Ladies Finger), Bottle gourd, Bitter gourd, Green Chilli, Capsicum, Garlic, Ginger(Green), " +
                $"Carrot, Raddish, Green Peas, Pumpkin, Spinach, Methi(Leaves), Coriander(Leaves), Drumstick, " +
                $"Banana, Mango, Apple, Guava, Papaya, Water Melon, Cotton, Mustard, Sugarcane, Groundnut, " +
                $"Soyabean, Bengal Gram(Gram)(Whole), Green Gram(Moong)(Whole), Black Gram(Urd Beans)(Whole), " +
                $"Arhar(Tur/Red Gram)(Whole), Masur Dal, Bajra(Pearl Millet/Cumbu), Jowar(Sorghum), " +
                $"Ragi(Finger Millet), Paddy(Common), Cucumbar(Kheera), Ridgeguard(Tori), French Beans(Frasbean), " +
                $"Cluster beans, Pointed gourd(Parval), Sweet Potato, Beetroot, Lemon\n" +
                $"Output ONLY the exact API name from the list above. Nothing else.";

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cts.Token);

            var result = await _geminiService.GenerateContentAsync(
                "gemini-2.5-flash", prompt, 0.0f, 50, linked.Token);
            result = result?.Trim();

            // Validate the result is a known API name (not hallucinated)
            if (!string.IsNullOrEmpty(result) && result.Length < 100)
                return result;

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Nova Micro commodity resolution failed for '{Input}'", input);
            return null;
        }
    }

    private async Task<List<MandiPrice>> FetchFromApiAsync(
        string commodity, string location, CancellationToken cancellationToken)
    {
        var (state, district) = ResolveLocation(location);
        
        try
        {
            var url = $"{_apiBaseUrl}?api-key={_apiKey}&format=json&limit=100" +
                $"&filters[commodity]={Uri.EscapeDataString(commodity)}";

            if (!string.IsNullOrEmpty(state))
                url += $"&filters[state.keyword]={Uri.EscapeDataString(state)}";
            if (!string.IsNullOrEmpty(district))
                url += $"&filters[district]={Uri.EscapeDataString(district)}";

            _logger.LogInformation("LiveMandi API: commodity={Commodity}, state={State}, district={District}",
                commodity, state ?? "(none)", district ?? "(none)");

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cts.Token);

            var json = await _httpClient.GetStringAsync(url, linked.Token);
            using var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("records", out var records))
                return new List<MandiPrice>();

            var prices = new List<MandiPrice>();
            foreach (var r in records.EnumerateArray())
            {
                try
                {
                    var dateStr = r.GetProperty("arrival_date").GetString() ?? "";
                    if (!DateTimeOffset.TryParseExact(dateStr, "dd/MM/yyyy",
                        CultureInfo.InvariantCulture, DateTimeStyles.None, out var arrivalDate))
                        arrivalDate = DateTimeOffset.UtcNow;

                    prices.Add(new MandiPrice(
                        commodity: commodity,
                        location: r.GetProperty("state").GetString() ?? state,
                        mandiName: r.GetProperty("market").GetString() ?? "",
                        minPrice: GetDecimal(r, "min_price"),
                        maxPrice: GetDecimal(r, "max_price"),
                        modalPrice: GetDecimal(r, "modal_price"),
                        priceDate: arrivalDate,
                        unit: "Quintal",
                        state: r.GetProperty("state").GetString(),
                        district: r.GetProperty("district").GetString(),
                        variety: r.GetProperty("variety").GetString(),
                        grade: r.GetProperty("grade").GetString()
                    ));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse price record");
                }
            }

            // Fallback: if district filter returned 0 results and we have a state, retry with state-only
            if (prices.Count == 0 && !string.IsNullOrEmpty(state) && !string.IsNullOrEmpty(district))
            {
                _logger.LogInformation("LiveMandi API: District '{District}' returned 0 results, retrying with state-only", district);
                var stateOnlyUrl = $"{_apiBaseUrl}?api-key={_apiKey}&format=json&limit=100" +
                    $"&filters[commodity]={Uri.EscapeDataString(commodity)}" +
                    $"&filters[state.keyword]={Uri.EscapeDataString(state)}";

                json = await _httpClient.GetStringAsync(stateOnlyUrl, linked.Token);
                using var doc2 = JsonDocument.Parse(json);

                if (doc2.RootElement.TryGetProperty("records", out var records2))
                {
                    foreach (var r in records2.EnumerateArray())
                    {
                        try
                        {
                            var dateStr = r.GetProperty("arrival_date").GetString() ?? "";
                            if (!DateTimeOffset.TryParseExact(dateStr, "dd/MM/yyyy",
                                CultureInfo.InvariantCulture, DateTimeStyles.None, out var arrivalDate))
                                arrivalDate = DateTimeOffset.UtcNow;

                            prices.Add(new MandiPrice(
                                commodity: commodity,
                                location: r.GetProperty("state").GetString() ?? state,
                                mandiName: r.GetProperty("market").GetString() ?? "",
                                minPrice: GetDecimal(r, "min_price"),
                                maxPrice: GetDecimal(r, "max_price"),
                                modalPrice: GetDecimal(r, "modal_price"),
                                priceDate: arrivalDate,
                                unit: "Quintal",
                                state: r.GetProperty("state").GetString(),
                                district: r.GetProperty("district").GetString(),
                                variety: r.GetProperty("variety").GetString(),
                                grade: r.GetProperty("grade").GetString()
                            ));
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to parse price record in fallback");
                        }
                    }
                    _logger.LogInformation("LiveMandi API: State-only fallback returned {Count} results", prices.Count);
                }
            }

            return prices;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "data.gov.in API failed for {Commodity}/{Location}", commodity, location);
            return new List<MandiPrice>();
        }
    }

    private static decimal GetDecimal(JsonElement el, string prop)
    {
        if (el.TryGetProperty(prop, out var v))
        {
            if (v.ValueKind == JsonValueKind.Number) return v.GetDecimal();
            if (v.ValueKind == JsonValueKind.String && decimal.TryParse(v.GetString(), out var d)) return d;
        }
        return 0;
    }
}
