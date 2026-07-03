import React, { useState, useEffect } from 'react';
import { useLanguage } from '@/contexts/LanguageContext';
import { soilAnalysisService, type RegenerativePlan } from '@/services/soilAnalysisService';

interface PlantingAdvisoryFormProps {
  onSubmit: (cropType: string, location: string, forecastDays: number, planId?: string) => void;
  isLoading: boolean;
}

// ─── Crop Hierarchy ─────────────────────────────────────────────────

interface SubCrop {
  value: string;
  label: string;
  labelHi: string;
}

interface CropCategory {
  value: string;
  label: string;
  labelHi: string;
  icon: string;
  directCrop?: boolean;    // true = no sub-dropdown, selects directly
  subCrops?: SubCrop[];
}

const CROP_HIERARCHY: CropCategory[] = [
  // ── Cereals (direct selection) ──
  {
    value: 'wheat', label: 'Wheat', labelHi: 'गेहूं', icon: '🌾',
    directCrop: true
  },
  {
    value: 'rice', label: 'Rice', labelHi: 'चावल', icon: '🍚',
    directCrop: true
  },
  {
    value: 'maize', label: 'Maize', labelHi: 'मक्का', icon: '🌽',
    directCrop: true
  },

  // ── Pulses (sub-dropdown) ──
  {
    value: 'pulses', label: 'Pulses', labelHi: 'दालें', icon: '🫘',
    subCrops: [
      { value: 'toor', label: 'Toor / Arhar', labelHi: 'तूर / अरहर' },
      { value: 'moong', label: 'Moong', labelHi: 'मूंग' },
      { value: 'urad', label: 'Urad', labelHi: 'उड़द' },
      { value: 'chana', label: 'Chana / Chickpea', labelHi: 'चना' },
      { value: 'masoor', label: 'Masoor / Lentil', labelHi: 'मसूर' },
      { value: 'rajma', label: 'Rajma / Kidney Bean', labelHi: 'राजमा' },
      { value: 'lobia', label: 'Lobia / Cowpea', labelHi: 'लोबिया' },
    ]
  },

  // ── Oilseeds (sub-dropdown) ──
  {
    value: 'oilseeds', label: 'Oilseeds', labelHi: 'तिलहन', icon: '🌻',
    subCrops: [
      { value: 'mustard', label: 'Mustard', labelHi: 'सरसों' },
      { value: 'soybean', label: 'Soybean', labelHi: 'सोयाबीन' },
      { value: 'groundnut', label: 'Groundnut', labelHi: 'मूंगफली' },
      { value: 'sunflower', label: 'Sunflower', labelHi: 'सूरजमुखी' },
      { value: 'sesame', label: 'Sesame / Til', labelHi: 'तिल' },
      { value: 'linseed', label: 'Linseed', labelHi: 'अलसी' },
    ]
  },

  // ── Millets (sub-dropdown) ──
  {
    value: 'millets', label: 'Millets', labelHi: 'मोटा अनाज', icon: '🌿',
    subCrops: [
      { value: 'bajra', label: 'Bajra / Pearl Millet', labelHi: 'बाजरा' },
      { value: 'jowar', label: 'Jowar / Sorghum', labelHi: 'ज्वार' },
      { value: 'ragi', label: 'Ragi / Finger Millet', labelHi: 'रागी' },
      { value: 'foxtail-millet', label: 'Foxtail Millet', labelHi: 'कंगनी' },
      { value: 'kodo-millet', label: 'Kodo Millet', labelHi: 'कोदो' },
    ]
  },

  // ── Vegetables (sub-dropdown) ──
  {
    value: 'vegetables', label: 'Vegetables', labelHi: 'सब्जियां', icon: '🥬',
    subCrops: [
      { value: 'tomato', label: 'Tomato', labelHi: 'टमाटर' },
      { value: 'potato', label: 'Potato', labelHi: 'आलू' },
      { value: 'onion', label: 'Onion', labelHi: 'प्याज' },
      { value: 'brinjal', label: 'Brinjal', labelHi: 'बैंगन' },
      { value: 'okra', label: 'Okra / Ladyfinger', labelHi: 'भिंडी' },
      { value: 'cauliflower', label: 'Cauliflower', labelHi: 'फूलगोभी' },
      { value: 'cabbage', label: 'Cabbage', labelHi: 'पत्तागोभी' },
      { value: 'capsicum', label: 'Capsicum', labelHi: 'शिमला मिर्च' },
      { value: 'carrot', label: 'Carrot', labelHi: 'गाजर' },
      { value: 'peas', label: 'Green Peas', labelHi: 'मटर' },
      { value: 'spinach', label: 'Spinach', labelHi: 'पालक' },
      { value: 'bottle-gourd', label: 'Bottle Gourd', labelHi: 'लौकी' },
      { value: 'bitter-gourd', label: 'Bitter Gourd', labelHi: 'करेला' },
      { value: 'radish', label: 'Radish', labelHi: 'मूली' },
    ]
  },

  // ── Fruits (sub-dropdown) ──
  {
    value: 'fruits', label: 'Fruits', labelHi: 'फल', icon: '🍎',
    subCrops: [
      { value: 'mango', label: 'Mango', labelHi: 'आम' },
      { value: 'banana', label: 'Banana', labelHi: 'केला' },
      { value: 'guava', label: 'Guava', labelHi: 'अमरूद' },
      { value: 'papaya', label: 'Papaya', labelHi: 'पपीता' },
      { value: 'orange', label: 'Orange', labelHi: 'संतरा' },
      { value: 'lemon', label: 'Lemon', labelHi: 'नींबू' },
      { value: 'watermelon', label: 'Watermelon', labelHi: 'तरबूज' },
      { value: 'pomegranate', label: 'Pomegranate', labelHi: 'अनार' },
    ]
  },

  // ── Spices (sub-dropdown) ──
  {
    value: 'spices', label: 'Spices', labelHi: 'मसाले', icon: '🌶️',
    subCrops: [
      { value: 'turmeric', label: 'Turmeric', labelHi: 'हल्दी' },
      { value: 'chilli', label: 'Chilli', labelHi: 'मिर्च' },
      { value: 'coriander', label: 'Coriander', labelHi: 'धनिया' },
      { value: 'cumin', label: 'Cumin', labelHi: 'जीरा' },
      { value: 'ginger', label: 'Ginger', labelHi: 'अदरक' },
      { value: 'garlic', label: 'Garlic', labelHi: 'लहसुन' },
      { value: 'fenugreek', label: 'Fenugreek', labelHi: 'मेथी' },
    ]
  },

  // ── Cash Crops (direct selection) ──
  {
    value: 'cotton', label: 'Cotton', labelHi: 'कपास', icon: '🏵️',
    directCrop: true
  },
  {
    value: 'sugarcane', label: 'Sugarcane', labelHi: 'गन्ना', icon: '🎋',
    directCrop: true
  },
  {
    value: 'jute', label: 'Jute', labelHi: 'जूट', icon: '🧵',
    directCrop: true
  },
];

// ─── Component ──────────────────────────────────────────────────────

export const PlantingAdvisoryForm: React.FC<PlantingAdvisoryFormProps> = ({
  onSubmit,
  isLoading
}) => {
  const { currentLanguage } = useLanguage();
  const language = currentLanguage.code;

  const [selectedCategory, setSelectedCategory] = useState('');
  const [selectedSubCrop, setSelectedSubCrop] = useState('');
  const [location, setLocation] = useState('');
  const [forecastDays, setForecastDays] = useState(90);
  const [selectedPlanId, setSelectedPlanId] = useState<string>('');
  const [savedPlans, setSavedPlans] = useState<RegenerativePlan[]>([]);
  const [loadingPlans, setLoadingPlans] = useState(false);

  // Derived state
  const activeCategory = CROP_HIERARCHY.find(c => c.value === selectedCategory);
  const hasSubCrops = activeCategory?.subCrops && activeCategory.subCrops.length > 0;
  const finalCropType = hasSubCrops ? selectedSubCrop : selectedCategory;
  const isFormValid = finalCropType && location;

  useEffect(() => {
    loadSavedPlans();
  }, []);

  // Reset sub-crop when category changes
  useEffect(() => {
    setSelectedSubCrop('');
  }, [selectedCategory]);

  const loadSavedPlans = async () => {
    try {
      setLoadingPlans(true);
      const plans = await soilAnalysisService.getSavedPlans();
      setSavedPlans(plans);
    } catch (error) {
      console.error('Failed to load saved plans:', error);
    } finally {
      setLoadingPlans(false);
    }
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (isFormValid) {
      onSubmit(finalCropType, location, forecastDays, selectedPlanId || undefined);
    }
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-6">
      {/* Saved Plans Dropdown */}
      <div>
        <label htmlFor="savedPlan" className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
          {language === 'hi' ? 'सहेजी गई मिट्टी योजना (वैकल्पिक)' : 'Saved Soil Plan (Optional)'}
        </label>
        <select
          id="savedPlan"
          value={selectedPlanId}
          onChange={(e) => setSelectedPlanId(e.target.value)}
          className="w-full px-4 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100 focus:ring-2 focus:ring-green-500 focus:border-transparent"
          disabled={isLoading || loadingPlans}
        >
          <option value="">
            {language === 'hi' ? 'कोई योजना नहीं - नई मिट्टी डेटा का उपयोग करें' : 'No Plan - Use New Soil Data'}
          </option>
          {savedPlans.map((plan) => (
            <option key={plan.planId} value={plan.planId}>
              {language === 'hi' ? 'योजना' : 'Plan'} #{plan.planId.slice(0, 8)} - {new Date(plan.createdAt).toLocaleDateString()}
            </option>
          ))}
        </select>
        {selectedPlanId && (
          <p className="mt-2 text-sm text-green-600 dark:text-green-400">
            {language === 'hi'
              ? '✓ चयनित योजना से मिट्टी डेटा का उपयोग किया जाएगा'
              : '✓ Soil data from selected plan will be used'}
          </p>
        )}
      </div>

      {/* Crop Category */}
      <div>
        <label htmlFor="cropCategory" className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
          {language === 'hi' ? 'फसल श्रेणी' : 'Crop Category'}
          <span className="text-red-500 ml-1">*</span>
        </label>
        <select
          id="cropCategory"
          value={selectedCategory}
          onChange={(e) => setSelectedCategory(e.target.value)}
          className="w-full px-4 py-2.5 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100 focus:ring-2 focus:ring-green-500 focus:border-transparent"
          required
          disabled={isLoading}
        >
          <option value="">
            {language === 'hi' ? '── फसल श्रेणी चुनें ──' : '── Select Crop Category ──'}
          </option>

          {/* Group: Cereals */}
          <optgroup label={language === 'hi' ? '🌾 अनाज' : '🌾 Cereals'}>
            {CROP_HIERARCHY.filter(c => ['wheat', 'rice', 'maize'].includes(c.value)).map(c => (
              <option key={c.value} value={c.value}>
                {c.icon} {language === 'hi' ? c.labelHi : c.label}
              </option>
            ))}
          </optgroup>

          {/* Group: Categories with sub-crops */}
          <optgroup label={language === 'hi' ? '🫘 दालें एवं तिलहन' : '🫘 Pulses & Oilseeds'}>
            {CROP_HIERARCHY.filter(c => ['pulses', 'oilseeds'].includes(c.value)).map(c => (
              <option key={c.value} value={c.value}>
                {c.icon} {language === 'hi' ? c.labelHi : c.label} →
              </option>
            ))}
          </optgroup>

          <optgroup label={language === 'hi' ? '🌿 मोटा अनाज' : '🌿 Millets'}>
            {CROP_HIERARCHY.filter(c => c.value === 'millets').map(c => (
              <option key={c.value} value={c.value}>
                {c.icon} {language === 'hi' ? c.labelHi : c.label} →
              </option>
            ))}
          </optgroup>

          <optgroup label={language === 'hi' ? '🥬 सब्जियां एवं फल' : '🥬 Vegetables & Fruits'}>
            {CROP_HIERARCHY.filter(c => ['vegetables', 'fruits'].includes(c.value)).map(c => (
              <option key={c.value} value={c.value}>
                {c.icon} {language === 'hi' ? c.labelHi : c.label} →
              </option>
            ))}
          </optgroup>

          <optgroup label={language === 'hi' ? '🌶️ मसाले' : '🌶️ Spices'}>
            {CROP_HIERARCHY.filter(c => c.value === 'spices').map(c => (
              <option key={c.value} value={c.value}>
                {c.icon} {language === 'hi' ? c.labelHi : c.label} →
              </option>
            ))}
          </optgroup>

          <optgroup label={language === 'hi' ? '🏵️ नकदी फसलें' : '🏵️ Cash Crops'}>
            {CROP_HIERARCHY.filter(c => ['cotton', 'sugarcane', 'jute'].includes(c.value)).map(c => (
              <option key={c.value} value={c.value}>
                {c.icon} {language === 'hi' ? c.labelHi : c.label}
              </option>
            ))}
          </optgroup>
        </select>
      </div>

      {/* Sub-Crop Dropdown (conditional) */}
      {hasSubCrops && (
        <div className="animate-in slide-in-from-top-2 duration-200">
          <label htmlFor="subCrop" className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
            {language === 'hi'
              ? `${activeCategory?.labelHi} में से चुनें`
              : `Select ${activeCategory?.label}`}
            <span className="text-red-500 ml-1">*</span>
          </label>
          <select
            id="subCrop"
            value={selectedSubCrop}
            onChange={(e) => setSelectedSubCrop(e.target.value)}
            className="w-full px-4 py-2.5 border border-green-300 dark:border-green-600 rounded-lg bg-green-50 dark:bg-green-900/20 text-gray-900 dark:text-gray-100 focus:ring-2 focus:ring-green-500 focus:border-transparent"
            required
            disabled={isLoading}
          >
            <option value="">
              {language === 'hi'
                ? `── ${activeCategory?.labelHi} चुनें ──`
                : `── Select ${activeCategory?.label} ──`}
            </option>
            {activeCategory?.subCrops?.map((sub) => (
              <option key={sub.value} value={sub.value}>
                {language === 'hi' ? sub.labelHi : sub.label}
              </option>
            ))}
          </select>
          {selectedSubCrop && (
            <p className="mt-2 text-sm text-green-600 dark:text-green-400">
              ✓ {language === 'hi' ? 'चयनित:' : 'Selected:'}{' '}
              {language === 'hi'
                ? activeCategory?.subCrops?.find(s => s.value === selectedSubCrop)?.labelHi
                : activeCategory?.subCrops?.find(s => s.value === selectedSubCrop)?.label}
            </p>
          )}
        </div>
      )}

      {/* Location */}
      <div>
        <label htmlFor="location" className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
          {language === 'hi' ? 'स्थान (जिला/शहर)' : 'Location (District/City)'}
          <span className="text-red-500 ml-1">*</span>
        </label>
        <input
          type="text"
          id="location"
          value={location}
          onChange={(e) => setLocation(e.target.value)}
          placeholder={language === 'hi' ? 'उदाहरण: पुणे, महाराष्ट्र' : 'e.g., Pune, Maharashtra'}
          className="w-full px-4 py-2.5 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100 focus:ring-2 focus:ring-green-500 focus:border-transparent"
          required
          disabled={isLoading}
        />
      </div>

      {/* Forecast Period */}
      <div>
        <label htmlFor="forecastDays" className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
          {language === 'hi' ? 'पूर्वानुमान अवधि (दिन)' : 'Forecast Period (Days)'}
        </label>
        <input
          type="number"
          id="forecastDays"
          value={forecastDays}
          onChange={(e) => setForecastDays(parseInt(e.target.value) || 90)}
          min="1"
          max="90"
          className="w-full px-4 py-2.5 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100 focus:ring-2 focus:ring-green-500 focus:border-transparent"
          disabled={isLoading}
        />
        <p className="mt-1 text-sm text-gray-500 dark:text-gray-400">
          {language === 'hi'
            ? '1 से 90 दिनों के बीच (डिफ़ॉल्ट: 90)'
            : 'Between 1 and 90 days (Default: 90)'}
        </p>
      </div>

      {/* Selected Crop Summary */}
      {finalCropType && (
        <div className="bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-800 rounded-lg p-3">
          <p className="text-sm text-green-800 dark:text-green-200">
            {language === 'hi' ? '🌱 चयनित फसल:' : '🌱 Selected Crop:'}{' '}
            <span className="font-semibold">
              {(() => {
                if (hasSubCrops) {
                  const sub = activeCategory?.subCrops?.find(s => s.value === selectedSubCrop);
                  return language === 'hi' ? sub?.labelHi : sub?.label;
                }
                return language === 'hi' ? activeCategory?.labelHi : activeCategory?.label;
              })()}
            </span>
            {location && (
              <>
                {' '}{language === 'hi' ? 'स्थान:' : 'in'}{' '}
                <span className="font-semibold">{location}</span>
              </>
            )}
          </p>
        </div>
      )}

      {/* Submit */}
      <button
        type="submit"
        disabled={isLoading || !isFormValid}
        className="w-full bg-green-600 text-white py-3 px-6 rounded-lg font-medium hover:bg-green-700 disabled:bg-gray-400 disabled:cursor-not-allowed transition-colors"
      >
        {isLoading
          ? (language === 'hi' ? '⏳ सिफारिशें प्राप्त कर रहे हैं...' : '⏳ Getting Recommendations...')
          : (language === 'hi' ? '🌱 सिफारिशें प्राप्त करें' : '🌱 Get Recommendations')}
      </button>
    </form>
  );
};
