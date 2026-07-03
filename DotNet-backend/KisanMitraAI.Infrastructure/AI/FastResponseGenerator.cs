using KisanMitraAI.Core.AI;
using KisanMitraAI.Core.Models;
using KisanMitraAI.Core.VoiceIntelligence;
using KisanMitraAI.Core.VoiceIntelligence.Models;
using Microsoft.Extensions.Logging;
using System.Text;

namespace KisanMitraAI.Infrastructure.AI;

public class FastResponseGenerator : IResponseGenerator
{
    private readonly IResponseGenerator _geminiFallback;
    private readonly ILogger<FastResponseGenerator> _logger;

    // English → Hindi commodity names for display
    private static readonly Dictionary<string, string> HindiCommodityNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Potato"] = "आलू", ["Tomato"] = "टमाटर", ["Onion"] = "प्याज",
        ["Wheat"] = "गेहूं", ["Rice"] = "चावल", ["Apple"] = "सेब",
        ["Banana"] = "केला", ["Mango"] = "आम", ["Cauliflower"] = "फूलगोभी",
        ["Cabbage"] = "पत्तागोभी", ["Brinjal"] = "बैंगन", ["Capsicum"] = "शिमला मिर्च",
        ["Carrot"] = "गाजर", ["Green Peas"] = "मटर", ["Corn"] = "मक्का",
        ["Garlic"] = "लहसुन", ["Ginger"] = "अदरक", ["Toor Dal"] = "तूर दाल",
        ["Mustard"] = "सरसों", ["Cotton"] = "कपास", ["Sugarcane"] = "गन्ना",
        ["Soybean"] = "सोयाबीन", ["Chickpea"] = "चना", ["Moong"] = "मूंग",
        ["Urad"] = "उड़द", ["Bajra"] = "बाजरा", ["Jowar"] = "ज्वार",
    };

    // English → Kannada commodity names for display
    private static readonly Dictionary<string, string> KannadaCommodityNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Potato"] = "ಆಲೂಗಡ್ಡೆ", ["Tomato"] = "ಟೊಮೇಟೊ", ["Onion"] = "ಈರುಳ್ಳಿ",
        ["Wheat"] = "ಗೋಧಿ", ["Rice"] = "ಅಕ್ಕಿ", ["Apple"] = "ಸೇಬು",
        ["Banana"] = "ಬಾಳೆಹಣ್ಣು", ["Mango"] = "ಮಾವಿನಹಣ್ಣು", ["Cauliflower"] = "ಹೂಕೋಸು",
        ["Cabbage"] = "ಎಲೆಕೋಸು", ["Brinjal"] = "ಬದನೆಕಾಯಿ", ["Capsicum"] = "ಕ್ಯಾಪ್ಸಿಕಂ",
        ["Carrot"] = "ಕ್ಯಾರೆಟ್", ["Green Peas"] = "ಬಟಾಣಿ", ["Corn"] = "ಜೋಳ",
        ["Garlic"] = "ಬೆಳ್ಳುಳ್ಳಿ", ["Ginger"] = "ಅಲ್ಲಂ", ["Toor Dal"] = "ತೊಗರಿಬೇಳೆ",
        ["Mustard"] = "ಸಾಸಿವೆ", ["Cotton"] = "ಹತ್ತಿ", ["Sugarcane"] = "ಕಬ್ಬು",
        ["Soybean"] = "ಸೋಯಾಬೀನ್", ["Chickpea"] = "ಕಡಲೆ", ["Moong"] = "ಹೆಸರುಕಾಳು",
        ["Urad"] = "ಉದ್ದು", ["Bajra"] = "ಸಜ್ಜೆ", ["Jowar"] = "ಜೋಳ",
    };

    // English → Tamil commodity names for display
    private static readonly Dictionary<string, string> TamilCommodityNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Potato"] = "உருளைக்கிழங்கு", ["Tomato"] = "தக்காளி", ["Onion"] = "வெங்காயம்",
        ["Wheat"] = "கோதுமை", ["Rice"] = "அரிசி", ["Apple"] = "ஆப்பிள்",
        ["Banana"] = "வாழைப்பழம்", ["Mango"] = "மாம்பழம்", ["Cauliflower"] = "காலிஃப்ளவர்",
        ["Cabbage"] = "முட்டைகோஸ்", ["Brinjal"] = "கத்தரிக்காய்", ["Capsicum"] = "குடமிளகாய்",
        ["Carrot"] = "கேரட்", ["Green Peas"] = "பட்டாணி", ["Corn"] = "சோளம்",
        ["Garlic"] = "பூண்டு", ["Ginger"] = "இஞ்சி", ["Toor Dal"] = "துவரம் பருப்பு",
        ["Mustard"] = "கடுகு", ["Cotton"] = "பருத்தி", ["Sugarcane"] = "கரும்பு",
        ["Soybean"] = "சோயா பீன்ஸ்", ["Chickpea"] = "கொண்டைக்கடலை", ["Moong"] = "பாசிப்பருப்பு",
        ["Urad"] = "உளுந்து", ["Bajra"] = "கம்பு", ["Jowar"] = "சோளம்",
    };

    // English → Hindi location names for display
    private static readonly Dictionary<string, string> HindiLocationNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Delhi"] = "दिल्ली", ["Noida"] = "नोएडा", ["Gurugram"] = "गुरुग्राम",
        ["Lucknow"] = "लखनऊ", ["Jaipur"] = "जयपुर", ["Mumbai"] = "मुंबई",
        ["Pune"] = "पुणे", ["Chandigarh"] = "चंडीगढ़", ["Agra"] = "आगरा",
        ["Dehradun"] = "देहरादून", ["Gwalior"] = "ग्वालियर", ["Bhopal"] = "भोपाल",
        ["Indore"] = "इंदौर", ["Jabalpur"] = "जबलपुर", ["Ujjain"] = "उज्जैन",
        ["Patna"] = "पटना", ["Kolkata"] = "कोलकाता",
        ["Ahmedabad"] = "अहमदाबाद", ["Kanpur"] = "कानपुर", ["Varanasi"] = "वाराणसी",
        ["Nagpur"] = "नागपुर", ["Surat"] = "सूरत",
        // South India
        ["Chennai"] = "चेन्नई", ["Coimbatore"] = "कोयंबटूर", ["Madurai"] = "मदुरै",
        ["Salem"] = "सेलम", ["Trichy"] = "तिरुचि",
        ["Bangalore"] = "बेंगलुरु", ["Mysore"] = "मैसूर", ["Hubli"] = "हुबली",
        ["Mangalore"] = "मंगलौर", ["Belgaum"] = "बेलगाम",
        ["Hyderabad"] = "हैदराबाद", ["Warangal"] = "वारंगल",
        ["Visakhapatnam"] = "विशाखापट्टनम", ["Vijayawada"] = "विजयवाड़ा",
        ["Guntur"] = "गुंटूर", ["Tirupati"] = "तिरुपति",
        ["Kochi"] = "कोच्चि", ["Thiruvananthapuram"] = "तिरुवनंतपुरम",
        ["Kozhikode"] = "कोझिकोड",
    };

    // English → Tamil location names for display
    private static readonly Dictionary<string, string> TamilLocationNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Chennai"] = "சென்னை", ["Coimbatore"] = "கோயம்புத்தூர்", ["Madurai"] = "மதுரை",
        ["Salem"] = "சேலம்", ["Trichy"] = "திருச்சி", ["Tiruchirappalli"] = "திருச்சிராப்பள்ளி",
        ["Tirunelveli"] = "திருநெல்வேலி", ["Erode"] = "ஈரோடு", ["Vellore"] = "வேலூர்",
        ["Thoothukudi"] = "தூத்துக்குடி", ["Dindigul"] = "திண்டுக்கல்", ["Thanjavur"] = "தஞ்சாவூர்",
        ["Kanyakumari"] = "கன்னியாகுமரி", ["Hosur"] = "ஓசூர்", ["Karur"] = "கரூர்",
        ["Namakkal"] = "நாமக்கல்", ["Bangalore"] = "பெங்களூரு", ["Bengaluru"] = "பெங்களூரு",
        ["Mysore"] = "மைசூர்", ["Hubli"] = "ஹூப்ளி", ["Mangalore"] = "மங்களூர்",
        ["Hyderabad"] = "ஹைதராபாத்", ["Visakhapatnam"] = "விசாகப்பட்டினம்",
        ["Vijayawada"] = "விஜயவாடா", ["Guntur"] = "குந்தூர்", ["Tirupati"] = "திருப்பதி",
        ["Kochi"] = "கொச்சி", ["Thiruvananthapuram"] = "திருவனந்தபுரம்", ["Kozhikode"] = "கோழிக்கோடு",
        ["Delhi"] = "டெல்லி", ["Mumbai"] = "மும்பை", ["Kolkata"] = "கொல்கத்தா",
        ["Pune"] = "புனே", ["Ahmedabad"] = "அகமதாபாத்",
    };

    // Hindi unit names
    private static readonly Dictionary<string, string> HindiUnitNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Quintal"] = "क्विंटल", ["Kg"] = "किलो", ["Ton"] = "टन",
    };

    // Tamil unit names
    private static readonly Dictionary<string, string> TamilUnitNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Quintal"] = "குவிண்டால்", ["Kg"] = "கிலோ", ["Ton"] = "டன்",
    };

    // English → Telugu commodity names
    private static readonly Dictionary<string, string> TeluguCommodityNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Potato"] = "బంగాళాదుంప", ["Tomato"] = "టమాటా", ["Onion"] = "ఉల్లిపాయ",
        ["Wheat"] = "గోధుమ", ["Rice"] = "బియ్యం", ["Apple"] = "ఆపిల్",
        ["Banana"] = "అరటిపండు", ["Mango"] = "మామిడిపండు", ["Cauliflower"] = "గోబీపువ్వు",
        ["Cabbage"] = "క్యాబేజీ", ["Brinjal"] = "వంకాయ", ["Capsicum"] = "క్యాప్సికం",
        ["Carrot"] = "క్యారెట్", ["Green Peas"] = "బఠాణీ", ["Corn"] = "మొక్కజొన్న",
        ["Garlic"] = "వెల్లుల్లి", ["Ginger"] = "అల్లం", ["Toor Dal"] = "కందిపప్పు",
        ["Mustard"] = "ఆవాలు", ["Cotton"] = "పత్తి", ["Sugarcane"] = "చెరకు",
        ["Soybean"] = "సోయాబీన్", ["Chickpea"] = "శనగలు", ["Moong"] = "పెసలు",
        ["Urad"] = "మినుములు", ["Bajra"] = "సజ్జలు", ["Jowar"] = "జొన్నలు",
    };

    // English → Telugu location names
    private static readonly Dictionary<string, string> TeluguLocationNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Hyderabad"] = "హైదరాబాద్", ["Warangal"] = "వరంగల్",
        ["Visakhapatnam"] = "విశాఖపట్నం", ["Vijayawada"] = "విజయవాడ",
        ["Guntur"] = "గుంటూరు", ["Tirupati"] = "తిరుపతి",
        ["Karimnagar"] = "కరీంనగర్", ["Nizamabad"] = "నిజామాబాద్",
        ["Khammam"] = "ఖమ్మం", ["Rajahmundry"] = "రాజమహేంద్రవరం",
        ["Nellore"] = "నెల్లూరు", ["Kurnool"] = "కర్నూలు",
        ["Delhi"] = "ఢిల్లీ", ["Mumbai"] = "ముంబై", ["Bangalore"] = "బెంగళూరు",
        ["Chennai"] = "చెన్నై", ["Kolkata"] = "కోల్‌కతా", ["Pune"] = "పుణె",
    };

    // Telugu unit names
    private static readonly Dictionary<string, string> TeluguUnitNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Quintal"] = "క్వింటాల్", ["Kg"] = "కిలో", ["Ton"] = "టన్ను",
    };

    // English → Malayalam commodity names
    private static readonly Dictionary<string, string> MalayalamCommodityNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Potato"] = "ഉരുളക്കിഴങ്ങ്", ["Tomato"] = "തക്കാളി", ["Onion"] = "സവള",
        ["Wheat"] = "ഗോതമ്പ്", ["Rice"] = "അരി", ["Apple"] = "ആപ്പിൾ",
        ["Banana"] = "വാഴപ്പഴം", ["Mango"] = "മാങ്ങ", ["Cauliflower"] = "കോളിഫ്ളവർ",
        ["Cabbage"] = "കാബേജ്", ["Brinjal"] = "വഴുതനങ്ങ", ["Capsicum"] = "കാപ്സിക്കം",
        ["Carrot"] = "ക്യാററ്റ്", ["Green Peas"] = "പട്ടാണി", ["Corn"] = "ചോളം",
        ["Garlic"] = "വെളുത്തുള്ളി", ["Ginger"] = "ഇഞ്ചി", ["Toor Dal"] = "തുവരപ്പരിപ്പ്",
        ["Mustard"] = "കടുക്", ["Cotton"] = "പരുത്തി", ["Sugarcane"] = "കരിമ്പ്",
        ["Soybean"] = "സോയാബീൻ", ["Chickpea"] = "കടല", ["Moong"] = "ചെറുപയർ",
        ["Urad"] = "ഉഴുന്ന്", ["Bajra"] = "കമ്പം", ["Jowar"] = "ചോളം",
    };

    // English → Malayalam location names
    private static readonly Dictionary<string, string> MalayalamLocationNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Kochi"] = "കൊച്ചി", ["Thiruvananthapuram"] = "തിരുവനന്തപുരം",
        ["Kozhikode"] = "കോഴിക്കോട്", ["Thrissur"] = "തൃശ്ശൂർ",
        ["Kollam"] = "കൊല്ലം", ["Palakkad"] = "പാലക്കാട്",
        ["Alappuzha"] = "ആലപ്പുഴ", ["Kannur"] = "കണ്ണൂർ",
        ["Ernakulam"] = "എറണാകുളം", ["Malappuram"] = "മലപ്പുറം",
        ["Delhi"] = "ഡൽഹി", ["Mumbai"] = "മുംബൈ", ["Bangalore"] = "ബെംഗളൂരു",
        ["Chennai"] = "ചെന്നൈ", ["Kolkata"] = "കോൽക്കത്ത", ["Hyderabad"] = "ഹൈദരാബാദ്",
    };

    // Malayalam unit names
    private static readonly Dictionary<string, string> MalayalamUnitNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Quintal"] = "ക്വിന്റൽ", ["Kg"] = "കിലോ", ["Ton"] = "ടൺ",
    };

    // English → Bengali commodity names
    private static readonly Dictionary<string, string> BengaliCommodityNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Potato"] = "আলু", ["Tomato"] = "টমেটো", ["Onion"] = "পেঁয়াজ",
        ["Wheat"] = "গম", ["Rice"] = "চাল", ["Apple"] = "আপেল",
        ["Banana"] = "কলা", ["Mango"] = "আম", ["Cauliflower"] = "ফুলকপি",
        ["Cabbage"] = "বাঁধাকপি", ["Brinjal"] = "বেগুন", ["Capsicum"] = "ক্যাপসিকাম",
        ["Carrot"] = "গাজর", ["Green Peas"] = "মটরশুটি", ["Corn"] = "ভুট্টা",
        ["Garlic"] = "রসুন", ["Ginger"] = "আদা", ["Toor Dal"] = "অরহর ডাল",
        ["Mustard"] = "সরিষা", ["Cotton"] = "তুলা", ["Sugarcane"] = "আখ",
        ["Soybean"] = "সোয়াবিন", ["Chickpea"] = "ছোলা", ["Moong"] = "মুগ ডাল",
        ["Urad"] = "বিরি কালাই", ["Bajra"] = "বাজরা", ["Jowar"] = "জোয়ার",
    };

    // English → Bengali location names
    private static readonly Dictionary<string, string> BengaliLocationNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Kolkata"] = "কলকাতা", ["Siliguri"] = "শিলিগুড়ি",
        ["Howrah"] = "হাওড়া", ["Asansol"] = "আসানসোল",
        ["Durgapur"] = "দুর্গাপুর", ["Bardhaman"] = "বর্ধমান",
        ["Malda"] = "মালদা", ["Murshidabad"] = "মুর্শিদাবাদ",
        ["Delhi"] = "দিল্লী", ["Mumbai"] = "মুম্বাই", ["Patna"] = "পাটনা",
        ["Chennai"] = "চেন্নাই", ["Bangalore"] = "বেঙ্গালুরু",
        ["Hyderabad"] = "হায়দরাবাদ", ["Pune"] = "পুণে",
    };

    // Bengali unit names
    private static readonly Dictionary<string, string> BengaliUnitNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Quintal"] = "কুইন্টাল", ["Kg"] = "কেজি", ["Ton"] = "টন",
    };

    // English → Marathi commodity names
    private static readonly Dictionary<string, string> MarathiCommodityNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Potato"] = "बटाटा", ["Tomato"] = "टोमॅटो", ["Onion"] = "कांदा",
        ["Wheat"] = "गहू", ["Rice"] = "तांदूळ", ["Apple"] = "सफरचंद",
        ["Banana"] = "केळे", ["Mango"] = "आंबा", ["Cauliflower"] = "फ्लॉवर",
        ["Cabbage"] = "कोबी", ["Brinjal"] = "वांगे", ["Capsicum"] = "ढोबळी मिरची",
        ["Carrot"] = "गाजर", ["Green Peas"] = "वाटाणा", ["Corn"] = "मका",
        ["Garlic"] = "लसूण", ["Ginger"] = "आले", ["Toor Dal"] = "तूर डाळ",
        ["Mustard"] = "मोहरी", ["Cotton"] = "कापूस", ["Sugarcane"] = "ऊस",
        ["Soybean"] = "सोयाबीन", ["Chickpea"] = "हरभरा", ["Moong"] = "मूग",
        ["Urad"] = "उडीद", ["Bajra"] = "बाजरी", ["Jowar"] = "ज्वारी",
    };

    // English → Marathi location names
    private static readonly Dictionary<string, string> MarathiLocationNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Mumbai"] = "मुंबई", ["Pune"] = "पुणे", ["Nagpur"] = "नागपूर",
        ["Nashik"] = "नाशिक", ["Aurangabad"] = "औरंगाबाद",
        ["Solapur"] = "सोलापूर", ["Kolhapur"] = "कोल्हापूर",
        ["Sangli"] = "सांगली", ["Satara"] = "सातारा",
        ["Ahmednagar"] = "अहमदनगर", ["Jalgaon"] = "जळगाव",
        ["Delhi"] = "दिल्ली", ["Bangalore"] = "बेंगळूरु",
        ["Hyderabad"] = "हैदराबाद", ["Chennai"] = "चेन्नई",
        ["Kolkata"] = "कोलकाता", ["Ahmedabad"] = "अहमदाबाद",
        ["Indore"] = "इंदोर", ["Surat"] = "सुरत",
    };

    // Marathi unit names
    private static readonly Dictionary<string, string> MarathiUnitNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Quintal"] = "क्विंटल", ["Kg"] = "किलो", ["Ton"] = "टन",
    };

    // English → Gujarati commodity names
    private static readonly Dictionary<string, string> GujaratiCommodityNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Potato"] = "બટાટા", ["Tomato"] = "ટમેટા", ["Onion"] = "કાંદા",
        ["Wheat"] = "ઘઉં", ["Rice"] = "ચોખા", ["Apple"] = "સફરજન",
        ["Banana"] = "કેળા", ["Mango"] = "કેરી", ["Cauliflower"] = "ફૂલકોબી",
        ["Cabbage"] = "કોબીજ", ["Brinjal"] = "રિંગણ", ["Capsicum"] = "શિમલા મરચા",
        ["Carrot"] = "ગાજર", ["Green Peas"] = "વટાણા", ["Corn"] = "મકાઈ",
        ["Garlic"] = "લસણ", ["Ginger"] = "આદુ", ["Toor Dal"] = "તુવેર દાળ",
        ["Mustard"] = "રાઈ", ["Cotton"] = "કપાસ", ["Sugarcane"] = "શેરડી",
        ["Soybean"] = "સોયાબીન", ["Chickpea"] = "ચણા", ["Moong"] = "મગ",
        ["Urad"] = "અડદ", ["Bajra"] = "બાજરી", ["Jowar"] = "જુવાર",
    };

    // English → Gujarati location names
    private static readonly Dictionary<string, string> GujaratiLocationNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Ahmedabad"] = "અમદાવાદ", ["Surat"] = "સુરત",
        ["Vadodara"] = "વડોદરા", ["Rajkot"] = "રાજકોટ",
        ["Bhavnagar"] = "ભાવનગર", ["Jamnagar"] = "જામનગર",
        ["Junagadh"] = "જૂનાગઢ", ["Gandhinagar"] = "ગાંધીનગર",
        ["Anand"] = "આણંદ", ["Mehsana"] = "મહેસાણા",
        ["Delhi"] = "દિલ્લી", ["Mumbai"] = "મુંબઈ", ["Pune"] = "પુણે",
        ["Bangalore"] = "બેંગળૂરુ", ["Hyderabad"] = "હૈદરાબાદ",
        ["Indore"] = "ઇંદોર", ["Nagpur"] = "નાગપુર",
    };

    // Gujarati unit names
    private static readonly Dictionary<string, string> GujaratiUnitNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Quintal"] = "ક્વિંટલ", ["Kg"] = "કિલો", ["Ton"] = "ટન",
    };

    // English → Punjabi commodity names
    private static readonly Dictionary<string, string> PunjabiCommodityNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Potato"] = "ਆਲੂ", ["Tomato"] = "ਟਮਾਟਰ", ["Onion"] = "ਗੰਢਾ",
        ["Wheat"] = "ਕਣਕ", ["Rice"] = "ਚੌਲ", ["Apple"] = "ਸੇਬ",
        ["Banana"] = "ਕੇਲਾ", ["Mango"] = "ਅੰਬ", ["Cauliflower"] = "ਫੁੱਲਗੋਭੀ",
        ["Cabbage"] = "ਬੰਦਗੋਭੀ", ["Brinjal"] = "ਬੈਂਗਣ", ["Capsicum"] = "ਸ਼ਿਮਲਾ ਮਿਰਚ",
        ["Carrot"] = "ਗਾਜਰ", ["Green Peas"] = "ਮਟਰ", ["Corn"] = "ਮੱਕੀ",
        ["Garlic"] = "ਲਸਣ", ["Ginger"] = "ਅਦਰਕ", ["Toor Dal"] = "ਅਰਹਰ ਦਾਲ",
        ["Mustard"] = "ਸਰ੍ਹੋਂ", ["Cotton"] = "ਕਪਾਹ", ["Sugarcane"] = "ਗੰਨਾ",
        ["Soybean"] = "ਸੋਆਬੀਨ", ["Chickpea"] = "ਛੋਲੇ", ["Moong"] = "ਮੂੰਗੀ",
        ["Urad"] = "ਉੜਦ", ["Bajra"] = "ਬਾਜਰਾ", ["Jowar"] = "ਜੋਵਾਰ",
    };

    // English → Punjabi location names
    private static readonly Dictionary<string, string> PunjabiLocationNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Chandigarh"] = "ਚੰਡੀਗੜ੍ਹ", ["Ludhiana"] = "ਲੁਧਿਆਣਾ",
        ["Amritsar"] = "ਅੰਮ੍ਰਿਤਸਰ", ["Jalandhar"] = "ਜਲੰਧਰ",
        ["Patiala"] = "ਪਟਿਆਲਾ", ["Bathinda"] = "ਬਠਿੰਡਾ",
        ["Mohali"] = "ਮੋਹਾਲੀ", ["Pathankot"] = "ਪਠਾਨਕੋਟ",
        ["Moga"] = "ਮੋਗਾ", ["Hoshiarpur"] = "ਹੁਸ਼ਿਆਰਪੁਰ",
        ["Delhi"] = "ਦਿੱਲੀ", ["Mumbai"] = "ਮੁੰਬਈ", ["Jaipur"] = "ਜੈਪੁਰ",
        ["Gurugram"] = "ਗੁਰੂਗ੍ਰਾਮ", ["Kolkata"] = "ਕੋਲਕਾਤਾ",
    };

    // Punjabi unit names
    private static readonly Dictionary<string, string> PunjabiUnitNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Quintal"] = "ਕੁਇੰਟਲ", ["Kg"] = "ਕਿਲੋ", ["Ton"] = "ਟਨ",
    };

    private static readonly Dictionary<string, PriceTemplate> DialectTemplates = new(StringComparer.OrdinalIgnoreCase)
    {
        // === Hindi (standard) ===
        ["Hindi"] = new PriceTemplate(
        Single: "भाई साहब, {commodity} का आज का भाव {location} मंडी में ₹{modal} प्रति {unit} चल रहा है। कम से कम ₹{min} और ज़्यादा से ज़्यादा ₹{max} तक मिल रहा है।",
        NoPrices: "भाई साहब, {commodity} का भाव {location} मंडी में अभी नहीं आया है। थोड़ी देर बाद फिर से पूछिए।",
        Multiple: "भाई साहब, {location} की मंडियों में {commodity} का भाव कुछ इस तरह चल रहा है — {priceList}।",
        Comparison: "भाई साहब, {commodity} के भाव की तुलना — {comparisonList}।"),
        ["hi-IN"] = new PriceTemplate(
        Single: "भाई साहब, {commodity} का आज का भाव {location} मंडी में ₹{modal} प्रति {unit} चल रहा है। कम से कम ₹{min} और ज़्यादा से ज़्यादा ₹{max} तक मिल रहा है।",
        NoPrices: "भाई साहब, {commodity} का भाव {location} मंडी में अभी नहीं आया है। थोड़ी देर बाद फिर से पूछिए।",
        Multiple: "भाई साहब, {location} की मंडियों में {commodity} का भाव कुछ इस तरह चल रहा है — {priceList}।",
        Comparison: "भाई साहब, {commodity} के भाव की तुलना — {comparisonList}।"),

        // === English ===
        ["English"] = new PriceTemplate(
        Single: "Today in {location} mandi, {commodity} is going at ₹{modal} per {unit}. The rate is between ₹{min} and ₹{max}.",
        NoPrices: "Sorry, {commodity} prices in {location} are not available right now. Please check back in some time.",
        Multiple: "Here are today's {commodity} rates in {location} — {priceList}.",
        Comparison: "Here's the {commodity} price comparison — {comparisonList}."),
        ["en-IN"] = new PriceTemplate(
        Single: "Today in {location} mandi, {commodity} is going at ₹{modal} per {unit}. The rate is between ₹{min} and ₹{max}.",
        NoPrices: "Sorry, {commodity} prices in {location} are not available right now. Please check back in some time.",
        Multiple: "Here are today's {commodity} rates in {location} — {priceList}.",
        Comparison: "Here's the {commodity} price comparison — {comparisonList}."),

        // === Bhojpuri ===
        ["Bhojpuri"] = new PriceTemplate(
        Single: "भइया, {location} मंडी में {commodity} के भाव आज ₹{modal} प्रति {unit} बा। कम में ₹{min} आ बढ़िया में ₹{max} तक जात बा।",
        NoPrices: "भइया, {commodity} के भाव {location} में अभी नइखे आइल। थोड़ा रुकीं, बाद में देखीं।",
        Multiple: "भइया, {location} के मंडी में {commodity} के भाव अइसन बा — {priceList}।"),

        // === Bundelkhandi ===
        ["Bundelkhandi"] = new PriceTemplate(
        Single: "भइया, {location} मंडी में {commodity} को भाव आज ₹{modal} प्रति {unit} चल रओ है। कम में ₹{min} और बढ़ के ₹{max} तक जा रओ है।",
        NoPrices: "भइया, {commodity} को भाव {location} में अभी नईं आओ है। थोरी देर बाद पूछो।",
        Multiple: "भइया, {location} की मंडी में {commodity} को भाव कछु अइसो चल रओ है — {priceList}।"),

        // === Marwari ===
        ["Marwari"] = new PriceTemplate(
        Single: "भाईसा, {location} मंडी में {commodity} रो भाव आज ₹{modal} प्रति {unit} चाल रह्यो है। कम सूं कम ₹{min} अर बेसी सूं बेसी ₹{max} तक मिल रह्यो है।",
        NoPrices: "भाईसा, {commodity} रो भाव {location} में अभी नीं आयो है। थोड़ी वार पछै पूछो।",
        Multiple: "भाईसा, {location} री मंडी में {commodity} रो भाव कीं इस तरियां चाल रह्यो है — {priceList}।"),

        // === Awadhi ===
        ["Awadhi"] = new PriceTemplate(
        Single: "भइया, {location} मंडी में {commodity} का भाव आज ₹{modal} प्रति {unit} चलत अहै। कम से कम ₹{min} अउर बेसी से बेसी ₹{max} तक मिलत अहै।",
        NoPrices: "भइया, {commodity} का भाव {location} में अबहीं नाहीं आवा है। थोरी देर बाद पूछा।",
        Multiple: "भइया, {location} की मंडी में {commodity} का भाव कछु अइसे चलत अहै — {priceList}।"),

        // === Braj ===
        ["Braj"] = new PriceTemplate(
        Single: "भाई, {location} मंडी में {commodity} को भाव आज ₹{modal} प्रति {unit} चल रह्यो है। कम सें कम ₹{min} और अधिक सें अधिक ₹{max} तक मिल रह्यो है।",
        NoPrices: "भाई, {commodity} को भाव {location} में अभी नाय आयो है। थोरी देर बाद पूछियो।",
        Multiple: "भाई, {location} की मंडी में {commodity} को भाव कछु ऐसो चल रह्यो है — {priceList}।"),

        // === Magahi ===
        ["Magahi"] = new PriceTemplate(
        Single: "भइया, {location} मंडी में {commodity} के भाव आज ₹{modal} प्रति {unit} हइ। कम से कम ₹{min} आ बेसी से बेसी ₹{max} तक मिलत हइ।",
        NoPrices: "भइया, {commodity} के भाव {location} में अभी नइखे। थोड़ा रुकीं, बाद में देखीं।",
        Multiple: "भइया, {location} के मंडी में {commodity} के भाव कुछ अइसन हइ — {priceList}।"),

        // === Maithili ===
        ["Maithili"] = new PriceTemplate(
        Single: "भाय, {location} मंडी में {commodity} केर भाव आइ ₹{modal} प्रति {unit} चलि रहल अछि। कम सँ कम ₹{min} आ बेसी सँ बेसी ₹{max} तक भेटि रहल अछि।",
        NoPrices: "भाय, {commodity} केर भाव {location} में अखन नहि आयल अछि। किछु काल बाद पुछू।",
        Multiple: "भाय, {location} केर मंडी में {commodity} केर भाव किछु एहन चलि रहल अछि — {priceList}।"),

        // === Chhattisgarhi ===
        ["Chhattisgarhi"] = new PriceTemplate(
        Single: "भइया, {location} मंडी में {commodity} के भाव आज ₹{modal} प्रति {unit} चलत हे। कम से कम ₹{min} अउ जादा से जादा ₹{max} तक मिलत हे।",
        NoPrices: "भइया, {commodity} के भाव {location} में अभी नइ आय हे। थोरिक देर बाद पूछव।",
        Multiple: "भइया, {location} के मंडी में {commodity} के भाव कुछ अइसन चलत हे — {priceList}।"),

        // === Haryanvi ===
        ["Haryanvi"] = new PriceTemplate(
        Single: "भाई, {location} मंडी में {commodity} का भाव आज ₹{modal} प्रति {unit} चाल रहा सै। कम तै कम ₹{min} अर बढ़ तै बढ़ ₹{max} तक मिल रहा सै।",
        NoPrices: "भाई, {commodity} का भाव {location} में अभी कोनी आया। थोड़ी देर बाद बुझ लियो।",
        Multiple: "भाई, {location} की मंडी में {commodity} का भाव कुछ इसा चाल रहा सै — {priceList}।"),

        // === Rajasthani ===
        ["Rajasthani"] = new PriceTemplate(
        Single: "भाईसा, {location} मंडी में {commodity} रो भाव आज ₹{modal} प्रति {unit} चाल रह्यो है। कम सूं कम ₹{min} अर बेसी सूं बेसी ₹{max} तक मिल रह्यो है।",
        NoPrices: "भाईसा, {commodity} रो भाव {location} में अभी नीं आयो है। थोड़ी वार पछै पूछो।",
        Multiple: "भाईसा, {location} री मंडी में {commodity} रो भाव कीं इस तरियां चाल रह्यो है — {priceList}।"),

        // === Punjabi ===
        ["Punjabi"] = new PriceTemplate(
        Single: "ਵੀਰੇ, {location} ਮੰਡੀ ਵਿੱਚ {commodity} ਦਾ ਅੱਜ ਦਾ ਭਾਅ ₹{modal} ਪ੍ਰਤੀ {unit} ਚੱਲ ਰਿਹਾ ਹੈ। ਘੱਟੋ ਘੱਟ ₹{min} ਅਤੇ ਵੱਧ ਤੋਂ ਵੱਧ ₹{max} ਤੱਕ ਮਿਲ ਰਿਹਾ ਹੈ।",
        NoPrices: "ਵੀਰੇ, {commodity} ਦਾ ਭਾਅ {location} ਵਿੱਚ ਹਾਲੇ ਨਹੀਂ ਆਇਆ। ਥੋੜੀ ਦੇਰ ਬਾਅਦ ਪੁੱਛੋ।",
        Multiple: "ਵੀਰੇ, {location} ਦੀਆਂ ਮੰਡੀਆਂ ਵਿੱਚ {commodity} ਦਾ ਭਾਅ ਕੁਝ ਇਸ ਤਰ੍ਹਾਂ ਚੱਲ ਰਿਹਾ ਹੈ — {priceList}।"),
        ["pa-IN"] = new PriceTemplate(
        Single: "ਵੀਰੇ, {location} ਮੰਡੀ ਵਿੱਚ {commodity} ਦਾ ਅੱਜ ਦਾ ਭਾਅ ₹{modal} ਪ੍ਰਤੀ {unit} ਚੱਲ ਰਿਹਾ ਹੈ। ਘੱਟੋ ਘੱਟ ₹{min} ਅਤੇ ਵੱਧ ਤੋਂ ਵੱਧ ₹{max} ਤੱਕ ਮਿਲ ਰਿਹਾ ਹੈ।",
        NoPrices: "ਵੀਰੇ, {commodity} ਦਾ ਭਾਅ {location} ਵਿੱਚ ਹਾਲੇ ਨਹੀਂ ਆਇਆ। ਥੋੜੀ ਦੇਰ ਬਾਅਦ ਪੁੱਛੋ।",
        Multiple: "ਵੀਰੇ, {location} ਦੀਆਂ ਮੰਡੀਆਂ ਵਿੱਚ {commodity} ਦਾ ਭਾਅ ਕੁਝ ਇਸ ਤਰ੍ਹਾਂ ਚੱਲ ਰਿਹਾ ਹੈ — {priceList}।"),

        // === Marathi ===
        ["Marathi"] = new PriceTemplate(
        Single: "दादा, {location} बाजारात {commodity} चा आजचा भाव ₹{modal} प्रति {unit} चालू आहे। कमीत कमी ₹{min} आणि जास्तीत जास्त ₹{max} पर्यंत मिळतोय।",
        NoPrices: "दादा, {commodity} चा भाव {location} मध्ये अजून आलेला नाही. थोड्या वेळाने परत विचारा.",
        Multiple: "दादा, {location} च्या बाजारात {commodity} चा भाव असा चालू आहे — {priceList}."),
        ["mr-IN"] = new PriceTemplate(
        Single: "दादा, {location} बाजारात {commodity} चा आजचा भाव ₹{modal} प्रति {unit} चालू आहे। कमीत कमी ₹{min} आणि जास्तीत जास्त ₹{max} पर्यंत मिळतोय।",
        NoPrices: "दादा, {commodity} चा भाव {location} मध्ये अजून आलेला नाही. थोड्या वेळाने परत विचारा.",
        Multiple: "दादा, {location} च्या बाजारात {commodity} चा भाव असा चालू आहे — {priceList}."),

        // === Gujarati ===
        ["Gujarati"] = new PriceTemplate(
        Single: "ભાઈ, {location} માર્કેટમાં {commodity} નો આજનો ભાવ ₹{modal} પ્રતિ {unit} ચાલે છે। ઓછામાં ઓછો ₹{min} અને વધુમાં વધુ ₹{max} સુધી મળે છે.",
        NoPrices: "ભાઈ, {commodity} નો ભાવ {location} માં હજુ આવ્યો નથી. થોડી વાર પછી પૂછો.",
        Multiple: "ભાઈ, {location} ના માર્કેટમાં {commodity} નો ભાવ કંઈક આ રીતે ચાલે છે — {priceList}."),
        ["gu-IN"] = new PriceTemplate(
        Single: "ભાઈ, {location} માર્કેટમાં {commodity} નો આજનો ભાવ ₹{modal} પ્રતિ {unit} ચાલે છે। ઓછામાં ઓછો ₹{min} અને વધુમાં વધુ ₹{max} સુધી મળે છે.",
        NoPrices: "ભાઈ, {commodity} નો ભાવ {location} માં હજુ આવ્યો નથી. થોડી વાર પછી પૂછો.",
        Multiple: "ભાઈ, {location} ના માર્કેટમાં {commodity} નો ભાવ કંઈક આ રીતે ચાલે છે — {priceList}."),

        // === Tamil ===
        ["Tamil"] = new PriceTemplate(
        Single: "அண்ணா, {location} சந்தையில் {commodity} இன்றைய விலை ₹{modal} ஒரு {unit} க்கு போகுது. குறைந்தபட்சம் ₹{min}, அதிகபட்சம் ₹{max} வரை கிடைக்குது.",
        NoPrices: "அண்ணா, {commodity} விலை {location} ல இப்போ வரல. கொஞ்ச நேரம் கழிச்சு கேளுங்க.",
        Multiple: "அண்ணா, {location} சந்தைகளில் {commodity} விலை இப்படி போகுது — {priceList}."),
        ["ta-IN"] = new PriceTemplate(
        Single: "அண்ணா, {location} சந்தையில் {commodity} இன்றைய விலை ₹{modal} ஒரு {unit} க்கு போகுது. குறைந்தபட்சம் ₹{min}, அதிகபட்சம் ₹{max} வரை கிடைக்குது.",
        NoPrices: "அண்ணா, {commodity} விலை {location} ல இப்போ வரல. கொஞ்ச நேரம் கழிச்சு கேளுங்க.",
        Multiple: "அண்ணா, {location} சந்தைகளில் {commodity} விலை இப்படி போகுது — {priceList}."),

        // === Telugu ===
        ["Telugu"] = new PriceTemplate(
        Single: "అన్నా, {location} మార్కెట్లో {commodity} ఈరోజు ధర ₹{modal} ప్రతి {unit} కి ఉంది. తక్కువలో ₹{min}, ఎక్కువలో ₹{max} వరకు వస్తోంది.",
        NoPrices: "అన్నా, {commodity} ధర {location} లో ఇప్పుడు రాలేదు. కొంచెం సేపు తర్వాత అడగండి.",
        Multiple: "అన్నా, {location} మార్కెట్లలో {commodity} ధర ఇలా ఉంది — {priceList}."),
        ["te-IN"] = new PriceTemplate(
        Single: "అన్నా, {location} మార్కెట్లో {commodity} ఈరోజు ధర ₹{modal} ప్రతి {unit} కి ఉంది. తక్కువలో ₹{min}, ఎక్కువలో ₹{max} వరకు వస్తోంది.",
        NoPrices: "అన్నా, {commodity} ధర {location} లో ఇప్పుడు రాలేదు. కొంచెం సేపు తర్వాత అడగండి.",
        Multiple: "అన్నా, {location} మార్కెట్లలో {commodity} ధర ఇలా ఉంది — {priceList}."),

        // === Kannada ===
        ["Kannada"] = new PriceTemplate(
        Single: "ಅಣ್ಣ, {location} ಮಾರುಕಟ್ಟೆಯಲ್ಲಿ {commodity} ಇವತ್ತಿನ ಬೆಲೆ ₹{modal} ಪ್ರತಿ {unit} ಗೆ ಇದೆ. ಕಡಿಮೆ ₹{min} ಮತ್ತು ಹೆಚ್ಚು ₹{max} ವರೆಗೆ ಸಿಗ್ತಿದೆ.",
        NoPrices: "ಅಣ್ಣ, {commodity} ಬೆಲೆ {location} ನಲ್ಲಿ ಈಗ ಬಂದಿಲ್ಲ. ಸ್ವಲ್ಪ ಹೊತ್ತು ಬಿಟ್ಟು ಕೇಳಿ.",
        Multiple: "ಅಣ್ಣ, {location} ಮಾರುಕಟ್ಟೆಗಳಲ್ಲಿ {commodity} ಬೆಲೆ ಹೀಗಿದೆ — {priceList}."),
        ["kn-IN"] = new PriceTemplate(
        Single: "ಅಣ್ಣ, {location} ಮಾರುಕಟ್ಟೆಯಲ್ಲಿ {commodity} ಇವತ್ತಿನ ಬೆಲೆ ₹{modal} ಪ್ರತಿ {unit} ಗೆ ಇದೆ. ಕಡಿಮೆ ₹{min} ಮತ್ತು ಹೆಚ್ಚು ₹{max} ವರೆಗೆ ಸಿಗ್ತಿದೆ.",
        NoPrices: "ಅಣ್ಣ, {commodity} ಬೆಲೆ {location} ನಲ್ಲಿ ಈಗ ಬಂದಿಲ್ಲ. ಸ್ವಲ್ಪ ಹೊತ್ತು ಬಿಟ್ಟು ಕೇಳಿ.",
        Multiple: "ಅಣ್ಣ, {location} ಮಾರುಕಟ್ಟೆಗಳಲ್ಲಿ {commodity} ಬೆಲೆ ಹೀಗಿದೆ — {priceList}."),

        // === Malayalam ===
        ["Malayalam"] = new PriceTemplate(
        Single: "ചേട്ടാ, {location} മാർക്കറ്റിൽ {commodity} ന്റെ ഇന്നത്തെ വില ₹{modal} ഒരു {unit} ന് ആണ്. കുറഞ്ഞത് ₹{min}, കൂടിയത് ₹{max} വരെ കിട്ടുന്നുണ്ട്.",
        NoPrices: "ചേട്ടാ, {commodity} ന്റെ വില {location} ൽ ഇപ്പോൾ വന്നിട്ടില്ല. കുറച്ച് കഴിഞ്ഞ് ചോദിക്കൂ.",
        Multiple: "ചേട്ടാ, {location} മാർക്കറ്റുകളിൽ {commodity} ന്റെ വില ഇങ്ങനെ പോകുന്നു — {priceList}."),
        ["ml-IN"] = new PriceTemplate(
        Single: "ചേട്ടാ, {location} മാർക്കറ്റിൽ {commodity} ന്റെ ഇന്നത്തെ വില ₹{modal} ഒരു {unit} ന് ആണ്. കുറഞ്ഞത് ₹{min}, കൂടിയത് ₹{max} വരെ കിട്ടുന്നുണ്ട്.",
        NoPrices: "ചേട്ടാ, {commodity} ന്റെ വില {location} ൽ ഇപ്പോൾ വന്നിട്ടില്ല. കുറച്ച് കഴിഞ്ഞ് ചോദിക്കൂ.",
        Multiple: "ചേട്ടാ, {location} മാർക്കറ്റുകളിൽ {commodity} ന്റെ വില ഇങ്ങനെ പോകുന്നു — {priceList}."),

        // === Bengali ===
        ["Bengali"] = new PriceTemplate(
        Single: "দাদা, {location} বাজারে {commodity} র আজকের দাম ₹{modal} প্রতি {unit} চলছে। কম করে ₹{min} আর বেশি করে ₹{max} পর্যন্ত পাওয়া যাচ্ছে।",
        NoPrices: "দাদা, {commodity} র দাম {location} তে এখনো আসেনি। একটু পরে আবার জিজ্ঞেস করুন।",
        Multiple: "দাদা, {location} র বাজারে {commodity} র দাম এরকম চলছে — {priceList}।"),
        ["bn-IN"] = new PriceTemplate(
        Single: "দাদা, {location} বাজারে {commodity} র আজকের দাম ₹{modal} প্রতি {unit} চলছে। কম করে ₹{min} আর বেশি করে ₹{max} পর্যন্ত পাওয়া যাচ্ছে।",
        NoPrices: "দাদা, {commodity} র দাম {location} তে এখনো আসেনি। একটু পরে আবার জিজ্ঞেস করুন।",
        Multiple: "দাদা, {location} র বাজারে {commodity} র দাম এরকম চলছে — {priceList}।"),
    };


    public FastResponseGenerator(
        GeminiService geminiService,
        GeminiModelConfig modelConfig,
        ILogger<FastResponseGenerator> logger)
    {
        _geminiFallback = new ResponseGenerator(geminiService, modelConfig,
            Microsoft.Extensions.Logging.LoggerFactory.Create(b => b.AddConsole())
                .CreateLogger<ResponseGenerator>());
        _logger = logger;
    }

    public async Task<string> GenerateResponseAsync(
        ParsedQuery query, IEnumerable<MandiPrice> prices, string dialect, CancellationToken cancellationToken)
    {
        var priceList = prices.ToList();

        // Multi-entity intents (any combination of multi-commodity / multi-location)
        var isMultiIntent = query.Intent is "price_comparison" or "multi_commodity_query" or "multi_commodity_comparison";
        if (isMultiIntent)
        {
            var commodities = query.Commodities.Count > 0 ? query.Commodities : new List<string> { query.Commodity };
            var locations = query.Locations.Count > 0 ? query.Locations : new List<string> { query.Location };
            var response = FormatMultiEntityResponse(commodities, locations, priceList, dialect);
            _logger.LogInformation("FastResponse: Multi-entity for {Commodities} across {Locations}",
                string.Join(",", commodities), string.Join(",", locations));
            return response;
        }

        if (query.Intent == "price_query" && !string.IsNullOrEmpty(query.Commodity))
        {
            var response = FormatPriceResponse(query.Commodity, query.Location, priceList, dialect);
            _logger.LogInformation("FastResponse: Template response for {Commodity} in dialect {Dialect}", query.Commodity, dialect);
            return response;
        }

        _logger.LogInformation("FastResponse: Gemini fallback for non-price query");
        return await _geminiFallback.GenerateResponseAsync(query, prices, dialect, cancellationToken);
    }

    /// <summary>
    /// Unified handler for all multi-entity queries:
    ///   - 2 commodities, 1 location: "आलू aur प्याज ka bhav bangalore mein"
    ///   - 1 commodity, 2 locations: "आलू ka bhav bangalore aur nashik mein"
    ///   - 2 commodities, 2 locations: "आलू aur प्याज ka bhav delhi aur mumbai mein"
    /// </summary>
    private string FormatMultiEntityResponse(List<string> commodities, List<string> locations, List<MandiPrice> allPrices, string dialect)
    {
        var phrase = MultiEntityPhrases.GetValueOrDefault(dialect) ?? MultiEntityPhrases["Hindi"];

        var displayLocations = locations
            .Select(l => TranslateLocation(l, dialect))
            .ToList();

        var sb = new StringBuilder();

        // Opening line
        sb.AppendLine(phrase.Opening.Replace("{locations}", string.Join($" {phrase.And} ", displayLocations)));
        sb.AppendLine();

        // One row per commodity
        foreach (var commodity in commodities)
        {
            var displayCommodity = TranslateCommodity(commodity, dialect);
            var priceParts = new List<string>();
            var unit = "Quintal";

            foreach (var loc in locations)
            {
                var displayLocation = TranslateLocation(loc, dialect);
                var resolvedState = LiveMandiPriceService.ResolveStateName(loc);

                var locPrices = allPrices.Where(p =>
                    (p.Commodity?.Equals(commodity, StringComparison.OrdinalIgnoreCase) ?? false) &&
                    MatchesLocation(p, loc, resolvedState))
                    .ToList();

                if (locPrices.Any())
                {
                    var best = locPrices.OrderByDescending(p => p.PriceDate).ThenByDescending(p => p.ModalPrice).First();
                    unit = TranslateUnit(best.Unit, dialect);
                    priceParts.Add($"{displayLocation} ₹{best.ModalPrice:N0}");
                }
                else
                {
                    priceParts.Add($"{displayLocation} {phrase.NotAvailable}");
                }
            }

            sb.AppendLine(phrase.Row
                .Replace("{commodity}", displayCommodity)
                .Replace("{prices}", string.Join(" | ", priceParts))
                .Replace("{unit}", unit));
        }

        // Closing advice (only when comparing 2+ locations)
        if (locations.Count >= 2)
        {
            sb.AppendLine();
            sb.Append(phrase.Closing);
        }

        return sb.ToString().TrimEnd();
    }

    /// <summary>
    /// Match a price record against a location — checks city name, resolved state against Location field,
    /// and fuzzy mandi name. DynamoDB stores state name in p.Location (not p.State).
    /// </summary>
    private static bool MatchesLocation(MandiPrice p, string loc, string resolvedState)
    {
        // p.Location from DynamoDB contains the STATE name (e.g., "Karnataka", "Maharashtra")
        // p.State may be null when loaded from DynamoDB cache
        var location = p.Location ?? "";
        var state = p.State ?? "";
        var mandi = p.MandiName ?? "";
        var district = p.District ?? "";

        // Match resolved state against Location (primary match for DynamoDB data)
        if (location.Equals(resolvedState, StringComparison.OrdinalIgnoreCase)) return true;
        if (state.Equals(resolvedState, StringComparison.OrdinalIgnoreCase)) return true;

        // Direct city name match
        if (location.Contains(loc, StringComparison.OrdinalIgnoreCase)) return true;
        if (state.Contains(loc, StringComparison.OrdinalIgnoreCase)) return true;
        if (mandi.Contains(loc, StringComparison.OrdinalIgnoreCase)) return true;
        if (district.Contains(loc, StringComparison.OrdinalIgnoreCase)) return true;

        // Fuzzy match for spelling variants (Nashik/Nasik)
        if (FuzzyContains(mandi, loc)) return true;
        if (FuzzyContains(district, loc)) return true;

        return false;
    }

    /// <summary>
    /// Fuzzy match: handles Nashik/Nasik, Bangalore/Bengaluru, etc.
    /// Strips vowels and compares consonant skeleton.
    /// </summary>
    private static bool FuzzyContains(string? text, string search)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(search)) return false;
        // Simple: check if first 3+ chars match (handles Nashik/Nasik, Pune/Puna)
        var t = text.ToLowerInvariant();
        var s = search.ToLowerInvariant();
        if (s.Length >= 3 && t.Contains(s[..3])) return true;
        // Reverse check
        if (t.Length >= 3 && s.Contains(t.Split(' ')[0][..Math.Min(3, t.Split(' ')[0].Length)])) return true;
        return false;
    }

    private string FormatPriceResponse(string commodity, string location, List<MandiPrice> prices, string dialect)
    {
        var template = DialectTemplates.GetValueOrDefault(dialect) ?? DialectTemplates["Hindi"];

        // Translate to target language based on dialect code
        var displayCommodity = TranslateCommodity(commodity, dialect);
        var displayLocation = TranslateLocation(location, dialect);

        if (!prices.Any())
        {
            return template.NoPrices
                .Replace("{commodity}", displayCommodity)
                .Replace("{location}", displayLocation);
        }

        if (prices.Count == 1)
        {
            var p = prices[0];
            var displayUnit = TranslateUnit(p.Unit, dialect);
            var displayMandi = TranslateMandiName(p.MandiName, location, dialect);

            return template.Single
                .Replace("{commodity}", displayCommodity)
                .Replace("{location}", displayMandi)
                .Replace("{modal}", p.ModalPrice.ToString("N0"))
                .Replace("{min}", p.MinPrice.ToString("N0"))
                .Replace("{max}", p.MaxPrice.ToString("N0"))
                .Replace("{unit}", displayUnit);
        }

        // Multiple prices — show top 3 with translated mandi names
        var sb = new StringBuilder();
        foreach (var p in prices.Take(3))
        {
            var displayUnit = TranslateUnit(p.Unit, dialect);
            var displayMandi = TranslateMandiName(p.MandiName, location, dialect);
            sb.Append($"{displayMandi}: ₹{p.ModalPrice:N0}/{displayUnit}, ");
        }

        return template.Multiple
            .Replace("{commodity}", displayCommodity)
            .Replace("{location}", displayLocation)
            .Replace("{priceList}", sb.ToString().TrimEnd(',', ' '));
    }

    // Simplified translation methods
    private static string TranslateCommodity(string commodity, string dialect)
    {
        return dialect switch
        {
            // Tamil
            "ta-IN" or "Tamil" => TamilCommodityNames?.GetValueOrDefault(commodity, commodity) ?? commodity,
            
            // Kannada
            "kn-IN" or "Kannada" => KannadaCommodityNames?.GetValueOrDefault(commodity, commodity) ?? commodity,
            
            // Hindi + all Hindi dialects (Bhojpuri, Haryanvi, Marwari, etc.)
            "hi-IN" or "Hindi" or "Bhojpuri" or "Bundelkhandi" or "Marwari" or "Awadhi" or "Braj" 
                or "Magahi" or "Maithili" or "Chhattisgarhi" or "Haryanvi" or "Rajasthani" 
                => HindiCommodityNames?.GetValueOrDefault(commodity, commodity) ?? commodity,
            
            // Telugu
            "te-IN" or "Telugu" => TeluguCommodityNames?.GetValueOrDefault(commodity, commodity) ?? commodity,
            
            // Malayalam
            "ml-IN" or "Malayalam" => MalayalamCommodityNames?.GetValueOrDefault(commodity, commodity) ?? commodity,
            
            // Bengali
            "bn-IN" or "Bengali" => BengaliCommodityNames?.GetValueOrDefault(commodity, commodity) ?? commodity,
            
            // Marathi
            "mr-IN" or "Marathi" => MarathiCommodityNames?.GetValueOrDefault(commodity, commodity) ?? commodity,
            
            // Gujarati
            "gu-IN" or "Gujarati" => GujaratiCommodityNames?.GetValueOrDefault(commodity, commodity) ?? commodity,
            
            // Punjabi
            "pa-IN" or "Punjabi" => PunjabiCommodityNames?.GetValueOrDefault(commodity, commodity) ?? commodity,
            
            // English or unknown
            "en-IN" or "English" or _ => commodity
        };
    }

    private static string TranslateLocation(string location, string dialect)
    {
        return dialect switch
        {
            // Tamil
            "ta-IN" or "Tamil" => TamilLocationNames?.GetValueOrDefault(location, location) ?? location,
            
            // Kannada + Hindi + all Hindi dialects (share same location names)
            "kn-IN" or "Kannada" or "hi-IN" or "Hindi" or "Bhojpuri" or "Bundelkhandi" or "Marwari" 
                or "Awadhi" or "Braj" or "Magahi" or "Maithili" or "Chhattisgarhi" or "Haryanvi" or "Rajasthani" 
                => HindiLocationNames?.GetValueOrDefault(location, location) ?? location,
            
            // Telugu
            "te-IN" or "Telugu" => TeluguLocationNames?.GetValueOrDefault(location, location) ?? location,
            
            // Malayalam
            "ml-IN" or "Malayalam" => MalayalamLocationNames?.GetValueOrDefault(location, location) ?? location,
            
            // Bengali
            "bn-IN" or "Bengali" => BengaliLocationNames?.GetValueOrDefault(location, location) ?? location,
            
            // Marathi
            "mr-IN" or "Marathi" => MarathiLocationNames?.GetValueOrDefault(location, location) ?? location,
            
            // Gujarati
            "gu-IN" or "Gujarati" => GujaratiLocationNames?.GetValueOrDefault(location, location) ?? location,
            
            // Punjabi
            "pa-IN" or "Punjabi" => PunjabiLocationNames?.GetValueOrDefault(location, location) ?? location,
            
            // English or unknown
            "en-IN" or "English" or _ => location
        };
    }

    private static string TranslateUnit(string unit, string dialect)
    {
        return dialect switch
        {
            // Tamil
            "ta-IN" or "Tamil" => TamilUnitNames?.GetValueOrDefault(unit, unit) ?? unit,
            
            // Kannada + Hindi + all Hindi dialects (share same unit names)
            "kn-IN" or "Kannada" or "hi-IN" or "Hindi" or "Bhojpuri" or "Bundelkhandi" or "Marwari" 
                or "Awadhi" or "Braj" or "Magahi" or "Maithili" or "Chhattisgarhi" or "Haryanvi" or "Rajasthani" 
                => HindiUnitNames?.GetValueOrDefault(unit, unit) ?? unit,
            
            // Telugu
            "te-IN" or "Telugu" => TeluguUnitNames?.GetValueOrDefault(unit, unit) ?? unit,
            
            // Malayalam
            "ml-IN" or "Malayalam" => MalayalamUnitNames?.GetValueOrDefault(unit, unit) ?? unit,
            
            // Bengali
            "bn-IN" or "Bengali" => BengaliUnitNames?.GetValueOrDefault(unit, unit) ?? unit,
            
            // Marathi
            "mr-IN" or "Marathi" => MarathiUnitNames?.GetValueOrDefault(unit, unit) ?? unit,
            
            // Gujarati
            "gu-IN" or "Gujarati" => GujaratiUnitNames?.GetValueOrDefault(unit, unit) ?? unit,
            
            // Punjabi
            "pa-IN" or "Punjabi" => PunjabiUnitNames?.GetValueOrDefault(unit, unit) ?? unit,
            
            // English or unknown
            "en-IN" or "English" or _ => unit
        };
    }

    private static string TranslateMandiName(string? mandiName, string location, string dialect)
    {
        if (string.IsNullOrEmpty(mandiName))
        {
            var translatedLocation = TranslateLocation(location, dialect);
            return dialect switch
            {
                "ta-IN" or "Tamil" => $"{translatedLocation} சந்தை",
                "kn-IN" or "Kannada" => $"{translatedLocation} ಮಾರುಕಟ್ಟೆ",
                "te-IN" or "Telugu" => $"{translatedLocation} మార్కెట్",
                "ml-IN" or "Malayalam" => $"{translatedLocation} മാർക്കറ്റ്",
                "bn-IN" or "Bengali" => $"{translatedLocation} বাজার",
                "mr-IN" or "Marathi" => $"{translatedLocation} बाजार",
                "gu-IN" or "Gujarati" => $"{translatedLocation} બજાર",
                "pa-IN" or "Punjabi" => $"{translatedLocation} ਮੰਡੀ",
                _ => $"{translatedLocation} मंडी"
            };
        }

        return dialect switch
        {
            "ta-IN" or "Tamil" => TranslateMandiNameTamil(mandiName, location),
            _ => TranslateMandiNameHindi(mandiName, location)
        };
    }

    private static bool IsHindiDialect(string dialect)
    {
        return dialect.Contains("hi", StringComparison.OrdinalIgnoreCase)
            || dialect.Equals("Hindi", StringComparison.OrdinalIgnoreCase)
            || dialect.Equals("Bhojpuri", StringComparison.OrdinalIgnoreCase)
            || dialect.Equals("Bundelkhandi", StringComparison.OrdinalIgnoreCase)
            || dialect.Equals("Marwari", StringComparison.OrdinalIgnoreCase)
            || dialect.Equals("Awadhi", StringComparison.OrdinalIgnoreCase)
            || dialect.Equals("Braj", StringComparison.OrdinalIgnoreCase)
            || dialect.Equals("Magahi", StringComparison.OrdinalIgnoreCase)
            || dialect.Equals("Maithili", StringComparison.OrdinalIgnoreCase)
            || dialect.Equals("Chhattisgarhi", StringComparison.OrdinalIgnoreCase)
            || dialect.Equals("Haryanvi", StringComparison.OrdinalIgnoreCase)
            || dialect.Equals("Rajasthani", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsKannadaDialect(string dialect)
    {
        return dialect.Contains("kn", StringComparison.OrdinalIgnoreCase)
            || dialect.Equals("Kannada", StringComparison.OrdinalIgnoreCase);
    }

    private static string TranslateMandiNameHindi(string? mandiName, string location)
    {
        if (string.IsNullOrEmpty(mandiName)) 
            return HindiLocationNames.GetValueOrDefault(location, location) + " मंडी";

        // "Azadpur Mandi" → "आज़ादपुर मंडी"
        var hindiMandis = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Azadpur Mandi"] = "आज़ादपुर मंडी",
            ["Noida Mandi"] = "नोएडा मंडी",
            ["Lucknow Mandi"] = "लखनऊ मंडी",
            ["Muhana Mandi"] = "मुहाना मंडी",
            ["Vashi APMC"] = "वाशी एपीएमसी",
            ["Market Yard Pune"] = "मार्केट यार्ड पुणे",
            ["Sector 26 Mandi"] = "सेक्टर 26 मंडी",
            ["Agra Mandi"] = "आगरा मंडी",
            ["Karond Mandi"] = "करोंद मंडी",
            ["Devi Ahilya Mandi"] = "देवी अहिल्या मंडी",
            ["Patna Mandi"] = "पटना मंडी",
            ["Koley Market"] = "कोले मार्केट",
            ["Jamalpur Mandi"] = "जमालपुर मंडी",
            ["Kanpur Mandi"] = "कानपुर मंडी",
            ["Varanasi Mandi"] = "वाराणसी मंडी",
            ["Dehradun Mandi"] = "देहरादून मंडी",
            ["Gwalior Mandi"] = "ग्वालियर मंडी",
            ["Gurugram Mandi"] = "गुरुग्राम मंडी",
            ["Kalamna Market"] = "कलमना मार्केट",
            ["Surat APMC"] = "सूरत एपीएमसी",
        };

        return hindiMandis.GetValueOrDefault(mandiName, mandiName);
    }

    private static string TranslateMandiNameTamil(string? mandiName, string location)
    {
        if (string.IsNullOrEmpty(mandiName))
            return TamilLocationNames.GetValueOrDefault(location, location) + " சந்தை";

        // Common mandi names in Tamil
        var tamilMandis = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Doddaballa Pur APMC"] = "தொட்டபல்லபுரம் சந்தை",
            ["Bangalore APMC"] = "பெங்களூரு சந்தை",
            ["Bengaluru APMC"] = "பெங்களூரு சந்தை",
            ["Koyambedu Market"] = "கோயம்பேடு சந்தை",
            ["Chennai Market"] = "சென்னை சந்தை",
            ["Coimbatore Market"] = "கோயம்புத்தூர் சந்தை",
            ["Madurai Market"] = "மதுரை சந்தை",
            ["Salem Market"] = "சேலம் சந்தை",
            ["Trichy Market"] = "திருச்சி சந்தை",
            ["Erode Market"] = "ஈரோடு சந்தை",
            ["Dindigul Market"] = "திண்டுக்கல் சந்தை",
            ["Hosur Market"] = "ஓசூர் சந்தை",
            ["Mysore APMC"] = "மைசூர் சந்தை",
            ["Hubli APMC"] = "ஹூப்ளி சந்தை",
        };

        return tamilMandis.GetValueOrDefault(mandiName, mandiName);
    }

    private record PriceTemplate(
        string Single,
        string NoPrices,
        string Multiple,
        string Comparison = "");

    private record MultiEntityPhrase(string Opening, string Row, string NotAvailable, string Closing, string And);

    // Opening:      "{locations}" placeholder replaced with joined location names
    // Row:          "{commodity}", "{prices}", "{unit}" placeholders
    // NotAvailable: shown per location when no price found
    // Closing:      appended when 2+ locations (sell-where-rate-is-better advice)
    // And:          conjunction used to join commodity/location lists
    private static readonly Dictionary<string, MultiEntityPhrase> MultiEntityPhrases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Hindi"]         = new("{locations} की मंडियों में आज के भाव:",         "{commodity} — {prices} प्रति {unit}",  "उपलब्ध नहीं",          "जहाँ भाव अच्छा मिले, वहाँ बेचना फायदेमंद रहेगा।",                              "और"),
        ["hi-IN"]         = new("{locations} की मंडियों में आज के भाव:",         "{commodity} — {prices} प्रति {unit}",  "उपलब्ध नहीं",          "जहाँ भाव अच्छा मिले, वहाँ बेचना फायदेमंद रहेगा।",                              "और"),
        ["English"]       = new("Today's rates in {locations}:",                  "{commodity} — {prices} per {unit}",    "N/A",                   "Selling where the rate is higher would be more profitable.",                     "and"),
        ["en-IN"]         = new("Today's rates in {locations}:",                  "{commodity} — {prices} per {unit}",    "N/A",                   "Selling where the rate is higher would be more profitable.",                     "and"),
        ["Tamil"]         = new("{locations} சந்தைகளில் இன்றைய விலை:",           "{commodity} — {prices} ஒரு {unit} க்கு", "கிடைக்கவில்லை",     "விலை நல்லா இருக்கற இடத்துல விற்பது லாபகரமாக இருக்கும்.",                       "மற்றும்"),
        ["ta-IN"]         = new("{locations} சந்தைகளில் இன்றைய விலை:",           "{commodity} — {prices} ஒரு {unit} க்கு", "கிடைக்கவில்லை",     "விலை நல்லா இருக்கற இடத்துல விற்பது லாபகரமாக இருக்கும்.",                       "மற்றும்"),
        ["Punjabi"]       = new("{locations} ਮੰਡੀਆਂ ਵਿੱਚ ਅੱਜ ਦੇ ਭਾਅ:",           "{commodity} — {prices} ਪ੍ਰਤੀ {unit}",  "ਉਪਲਬਧ ਨਹੀਂ",          "ਜਿੱਥੇ ਭਾਅ ਵਧੀਆ ਮਿਲੇ, ਉੱਥੇ ਵੇਚਣਾ ਫਾਇਦੇਮੰਦ ਰਹੇਗਾ।",                            "ਅਤੇ"),
        ["pa-IN"]         = new("{locations} ਮੰਡੀਆਂ ਵਿੱਚ ਅੱਜ ਦੇ ਭਾਅ:",           "{commodity} — {prices} ਪ੍ਰਤੀ {unit}",  "ਉਪਲਬਧ ਨਹੀਂ",          "ਜਿੱਥੇ ਭਾਅ ਵਧੀਆ ਮਿਲੇ, ਉੱਥੇ ਵੇਚਣਾ ਫਾਇਦੇਮੰਦ ਰਹੇਗਾ।",                            "ਅਤੇ"),
        ["Marathi"]       = new("{locations} बाजारांमध्ये आजचे भाव:",             "{commodity} — {prices} प्रति {unit}",  "उपलब्ध नाही",          "जिथे भाव चांगला मिळेल, तिथे विकणे फायदेशीर राहील.",                            "आणि"),
        ["mr-IN"]         = new("{locations} बाजारांमध्ये आजचे भाव:",             "{commodity} — {prices} प्रति {unit}",  "उपलब्ध नाही",          "जिथे भाव चांगला मिळेल, तिथे विकणे फायदेशीर राहील.",                            "आणि"),
        ["Telugu"]        = new("{locations} మార్కెట్లలో నేటి ధరలు:",             "{commodity} — {prices} ప్రతి {unit}",  "అందుబాటులో లేదు",      "ధర ఎక్కువగా ఉన్న చోట అమ్మడం లాభదాయకంగా ఉంటుంది.",                             "మరియు"),
        ["te-IN"]         = new("{locations} మార్కెట్లలో నేటి ధరలు:",             "{commodity} — {prices} ప్రతి {unit}",  "అందుబాటులో లేదు",      "ధర ఎక్కువగా ఉన్న చోట అమ్మడం లాభదాయకంగా ఉంటుంది.",                             "మరియు"),
        ["Kannada"]       = new("{locations} ಮಾರುಕಟ್ಟೆಗಳಲ್ಲಿ ಇಂದಿನ ಬೆಲೆಗಳು:",    "{commodity} — {prices} ಪ್ರತಿ {unit}", "ಲಭ್ಯವಿಲ್ಲ",            "ಬೆಲೆ ಹೆಚ್ಚಿರುವ ಕಡೆ ಮಾರಾಟ ಮಾಡುವುದು ಲಾಭದಾಯಕ.",                                  "ಮತ್ತು"),
        ["kn-IN"]         = new("{locations} ಮಾರುಕಟ್ಟೆಗಳಲ್ಲಿ ಇಂದಿನ ಬೆಲೆಗಳು:",    "{commodity} — {prices} ಪ್ರತಿ {unit}", "ಲಭ್ಯವಿಲ್ಲ",            "ಬೆಲೆ ಹೆಚ್ಚಿರುವ ಕಡೆ ಮಾರಾಟ ಮಾಡುವುದು ಲಾಭದಾಯಕ.",                                  "ಮತ್ತು"),
        ["Malayalam"]     = new("{locations} മാർക്കറ്റുകളിൽ ഇന്നത്തെ വിലകൾ:",   "{commodity} — {prices} ഒരു {unit} ന്", "ലഭ്യമല്ല",             "നല്ല വില കിട്ടുന്ന ഇടത്ത് വിൽക്കുന്നത് ലാഭകരമാണ്.",                           "കൂടാതെ"),
        ["ml-IN"]         = new("{locations} മാർക്കറ്റുകളിൽ ഇന്നത്തെ വിലകൾ:",   "{commodity} — {prices} ഒരു {unit} ന്", "ലഭ്യമല്ല",             "നല്ല വില കിട്ടുന്ന ഇടത്ത് വിൽക്കുന്നത് ലാഭകരമാണ്.",                           "കൂടാതെ"),
        ["Bengali"]       = new("{locations} বাজারে আজকের দাম:",                  "{commodity} — {prices} প্রতি {unit}",  "পাওয়া যাচ্ছে না",     "যেখানে দাম ভালো পাওয়া যাবে, সেখানে বিক্রি করা লাভজনক হবে।",                   "এবং"),
        ["bn-IN"]         = new("{locations} বাজারে আজকের দাম:",                  "{commodity} — {prices} প্রতি {unit}",  "পাওয়া যাচ্ছে না",     "যেখানে দাম ভালো পাওয়া যাবে, সেখানে বিক্রি করা লাভজনক হবে।",                   "এবং"),
        ["Gujarati"]      = new("{locations} બજારમાં આજના ભાવ:",                  "{commodity} — {prices} પ્રતિ {unit}",  "ઉપલબ્ધ નથી",          "જ્યાં ભાવ સારો મળે, ત્યાં વેચવું ફાયદાકારક રહેશે.",                            "અને"),
        ["gu-IN"]         = new("{locations} બજારમાં આજના ભાવ:",                  "{commodity} — {prices} પ્રતિ {unit}",  "ઉપલબ્ધ નથી",          "જ્યાં ભાવ સારો મળે, ત્યાં વેચવું ફાયદાકારક રહેશે.",                            "અને"),
        ["Bhojpuri"]      = new("{locations} के मंडी में आज के भाव:",              "{commodity} — {prices} प्रति {unit}",  "उपलब्ध नइखे",          "जहाँ भाव बढ़िया मिले, उहाँ बेचल फायदेमंद रही।",                                "आ"),
        ["Bundelkhandi"]  = new("{locations} की मंडी में आज के भाव:",              "{commodity} — {prices} प्रति {unit}",  "उपलब्ध नईं",           "जहाँ भाव अच्छो मिले, वहाँ बेचनो फायदेमंद रहेगो।",                              "और"),
        ["Marwari"]       = new("{locations} री मंडी में आज रा भाव:",              "{commodity} — {prices} प्रति {unit}",  "उपलब्ध नीं",           "जठे भाव बढ़िया मिले, उठे बेचणो फायदेमंद रहेगो।",                               "अर"),
        ["Awadhi"]        = new("{locations} की मंडी में आज के भाव:",              "{commodity} — {prices} प्रति {unit}",  "उपलब्ध नाहीं",         "जहाँ भाव अच्छा मिले, उहाँ बेचब फायदेमंद रही।",                                 "अउर"),
        ["Braj"]          = new("{locations} की मंडी में आज के भाव:",              "{commodity} — {prices} प्रति {unit}",  "उपलब्ध नाय",           "जहाँ भाव अच्छो मिले, वहाँ बेचनो फायदेमंद रहेगो।",                              "और"),
        ["Magahi"]        = new("{locations} के मंडी में आज के भाव:",              "{commodity} — {prices} प्रति {unit}",  "उपलब्ध नइखे",          "जहाँ भाव बढ़िया मिले, उहाँ बेचल फायदेमंद रही।",                                "आ"),
        ["Maithili"]      = new("{locations} केर मंडी में आइ केर भाव:",            "{commodity} — {prices} प्रति {unit}",  "उपलब्ध नहि",           "जतय भाव नीक भेटय, ओतय बेचब फायदेमंद रहत।",                                    "आ"),
        ["Chhattisgarhi"] = new("{locations} के मंडी में आज के भाव:",              "{commodity} — {prices} प्रति {unit}",  "उपलब्ध नइ",            "जहाँ भाव बढ़िया मिले, उहाँ बेचना फायदेमंद रही।",                               "अउ"),
        ["Haryanvi"]      = new("{locations} की मंडी में आज के भाव:",              "{commodity} — {prices} प्रति {unit}",  "उपलब्ध कोनी",          "जहाँ भाव बढ़िया मिले, उहाँ बेचणा फायदेमंद रहेगा।",                             "अर"),
        ["Rajasthani"]    = new("{locations} री मंडी में आज रा भाव:",              "{commodity} — {prices} प्रति {unit}",  "उपलब्ध नीं",           "जठे भाव बढ़िया मिले, उठे बेचणो फायदेमंद रहेगो।",                               "अर"),
    };
}
