import React, { useState } from 'react';
import { useLanguage } from '@/contexts/LanguageContext';
import type { PlantingWindow, SeedRecommendation } from '@/services/plantingAdvisoryService';

interface RecommendationDisplayProps {
  plantingWindows: PlantingWindow[];
  seedRecommendations: SeedRecommendation[];
  weatherFetchedAt?: string;
  soilDataDate?: string;
  usedPlanId?: string;
  usedDefaultSoil?: boolean;
  dataSources?: {
    realForecastDays?: number;
    historicalProjectionDays?: number;
    soilMoistureSource?: string;
    soilNutrientSource?: string;
    disclaimer?: string;
  };
  onSave?: () => void;
}

export const RecommendationDisplay: React.FC<RecommendationDisplayProps> = ({
  plantingWindows,
  seedRecommendations,
  weatherFetchedAt,
  soilDataDate,
  usedPlanId,
  usedDefaultSoil,
  dataSources,
  onSave
}) => {
  const { currentLanguage } = useLanguage();
  const hi = currentLanguage.code === 'hi';
  const [expandedSeed, setExpandedSeed] = useState<number | null>(null);

  const formatDate = (dateString: string) => {
    try {
      const date = new Date(dateString);
      return date.toLocaleDateString(hi ? 'hi-IN' : 'en-IN', { day: 'numeric', month: 'short', year: 'numeric' });
    } catch { return dateString; }
  };

  const getConfidenceColor = (score: number) => {
    if (score >= 75) return { bg: 'bg-green-500', text: 'text-green-700', light: 'bg-green-50 border-green-200', label: hi ? 'उच्च' : 'High' };
    if (score >= 50) return { bg: 'bg-yellow-500', text: 'text-yellow-700', light: 'bg-yellow-50 border-yellow-200', label: hi ? 'मध्यम' : 'Medium' };
    return { bg: 'bg-red-500', text: 'text-red-700', light: 'bg-red-50 border-red-200', label: hi ? 'कम' : 'Low' };
  };

  const getDaysCount = (start: string, end: string) => {
    try {
      return Math.ceil((new Date(end).getTime() - new Date(start).getTime()) / 86400000);
    } catch { return 0; }
  };

  return (
    <div className="space-y-8">

      {/* ─── Soil Data Missing Banner ─── */}
      {usedDefaultSoil && (
        <div className="bg-amber-50 dark:bg-amber-900/20 border-2 border-amber-300 dark:border-amber-700 rounded-xl p-4">
          <div className="flex items-start gap-3">
            <span className="text-2xl flex-shrink-0">🧪</span>
            <div>
              <h4 className="font-bold text-amber-800 dark:text-amber-200 mb-1">
                {hi ? 'मृदा स्वास्थ्य कार्ड अपलोड करें' : 'Upload Soil Health Card for Accurate Results'}
              </h4>
              <p className="text-sm text-amber-700 dark:text-amber-300 mb-2">
                {hi
                  ? 'अभी सिफारिशें अनुमानित मिट्टी डेटा पर आधारित हैं। अपना मृदा स्वास्थ्य कार्ड अपलोड करने से आपकी मिट्टी के अनुसार सटीक बीज और खाद की सिफारिश मिलेगी।'
                  : 'Current recommendations use estimated soil data. Upload your Soil Health Card to get precise seed and fertilizer recommendations tailored to your actual soil conditions.'}
              </p>
              <a
                href="/soil-analysis"
                className="inline-flex items-center gap-1.5 px-4 py-2 bg-amber-600 hover:bg-amber-700 text-white text-sm font-medium rounded-lg transition-colors"
              >
                📄 {hi ? 'मृदा कार्ड अपलोड करें' : 'Upload Soil Card'}
              </a>
            </div>
          </div>
        </div>
      )}

      {/* ─── Data Disclaimer ─── */}
      {dataSources?.disclaimer && (
        <div className="text-xs text-gray-500 dark:text-gray-400 bg-gray-50 dark:bg-gray-800/50 rounded-lg px-3 py-2 flex items-center gap-2">
          <span>ℹ️</span>
          <span>{dataSources.disclaimer}</span>
        </div>
      )}

      {/* ─── Data Sources ─── */}
      <div className="flex flex-wrap gap-3">
        {weatherFetchedAt && (
          <div className="flex items-center gap-2 px-3 py-1.5 bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 rounded-full text-xs text-blue-700 dark:text-blue-300">
            <span>🌤️</span>
            <span>{hi ? 'मौसम' : 'Weather'}: {formatDate(weatherFetchedAt)}</span>
          </div>
        )}
        {soilDataDate && (
          <div className="flex items-center gap-2 px-3 py-1.5 bg-amber-50 dark:bg-amber-900/20 border border-amber-200 dark:border-amber-800 rounded-full text-xs text-amber-700 dark:text-amber-300">
            <span>🌱</span>
            <span>{hi ? 'मिट्टी' : 'Soil'}: {formatDate(soilDataDate)}</span>
          </div>
        )}
        {usedPlanId && (
          <div className="flex items-center gap-2 px-3 py-1.5 bg-purple-50 dark:bg-purple-900/20 border border-purple-200 dark:border-purple-800 rounded-full text-xs text-purple-700 dark:text-purple-300">
            <span>📋</span>
            <span>{hi ? 'योजना' : 'Plan'}: #{usedPlanId.slice(0, 8)}</span>
          </div>
        )}
      </div>

      {/* ─── Planting Windows ─── */}
      <div>
        <h3 className="text-lg font-bold text-gray-900 dark:text-gray-100 mb-1">
          📅 {hi ? 'बुवाई का सही समय' : 'Best Time to Sow'}
        </h3>
        <p className="text-xs text-gray-500 dark:text-gray-400 mb-4">
          {hi ? 'मौसम और मिट्टी के आधार पर सबसे अच्छा समय' : 'Based on weather forecast and soil analysis'}
        </p>

        <div className="space-y-4">
          {plantingWindows.map((window, index) => {
            const conf = getConfidenceColor(window.confidenceScore);
            const days = getDaysCount(window.startDate, window.endDate);
            const isBest = index === 0;

            return (
              <div key={index} className={`rounded-xl border-2 overflow-hidden transition-all ${
                isBest ? 'border-green-400 dark:border-green-600 shadow-md' : 'border-gray-200 dark:border-gray-700'
              }`}>
                {/* Window Header */}
                <div className={`px-4 py-3 flex items-center justify-between ${
                  isBest ? 'bg-green-50 dark:bg-green-900/30' : 'bg-gray-50 dark:bg-gray-800'
                }`}>
                  <div className="flex items-center gap-3">
                    {isBest && (
                      <span className="px-2 py-0.5 bg-green-600 text-white text-xs font-bold rounded-full">
                        ⭐ {hi ? 'सर्वोत्तम' : 'BEST'}
                      </span>
                    )}
                    <div>
                      <div className="font-bold text-gray-900 dark:text-gray-100">
                        {formatDate(window.startDate)} → {formatDate(window.endDate)}
                      </div>
                      <div className="text-xs text-gray-500 dark:text-gray-400">
                        {days} {hi ? 'दिन की अवधि' : 'day window'}
                      </div>
                    </div>
                  </div>

                  {/* Confidence Badge */}
                  <div className="text-right">
                    <div className="flex items-center gap-2">
                      <div className="w-16 h-2 bg-gray-200 dark:bg-gray-600 rounded-full overflow-hidden">
                        <div className={`h-full rounded-full ${conf.bg}`} style={{ width: `${window.confidenceScore}%` }} />
                      </div>
                      <span className={`text-sm font-bold ${conf.text}`}>{window.confidenceScore}%</span>
                    </div>
                    <div className={`text-xs ${conf.text}`}>{conf.label} {hi ? 'विश्वसनीयता' : 'confidence'}</div>
                  </div>
                </div>

                {/* Window Body */}
                <div className="px-4 py-3">
                  {/* Risk Alerts — shown FIRST, prominently */}
                  {window.riskFactors && window.riskFactors.length > 0 && (
                    <div className="mb-3 flex flex-wrap gap-2">
                      {window.riskFactors.map((risk, idx) => (
                        <span key={idx} className="inline-flex items-center gap-1 px-2 py-1 bg-orange-50 dark:bg-orange-900/20 border border-orange-200 dark:border-orange-800 rounded-lg text-xs text-orange-700 dark:text-orange-300">
                          ⚠️ {risk}
                        </span>
                      ))}
                    </div>
                  )}

                  {/* Rationale */}
                  <p className="text-sm text-gray-700 dark:text-gray-300 leading-relaxed">
                    {window.rationale}
                  </p>
                </div>
              </div>
            );
          })}
        </div>
      </div>

      {/* ─── Seed Recommendations ─── */}
      {seedRecommendations && seedRecommendations.length > 0 && (
        <div>
          <h3 className="text-lg font-bold text-gray-900 dark:text-gray-100 mb-1">
            🌱 {hi ? 'बीज की सिफारिशें' : 'Recommended Seeds'}
          </h3>
          <p className="text-xs text-gray-500 dark:text-gray-400 mb-4">
            {hi ? 'आपकी मिट्टी और मौसम के लिए सबसे उपयुक्त किस्में' : 'Best varieties for your soil and weather conditions'}
          </p>

          <div className="space-y-3">
            {seedRecommendations.map((seed, index) => {
              const isExpanded = expandedSeed === index;
              const isTop = index === 0;

              return (
                <div key={index} className={`rounded-xl border overflow-hidden transition-all ${
                  isTop ? 'border-green-300 dark:border-green-700' : 'border-gray-200 dark:border-gray-700'
                }`}>
                  {/* Seed Header — always visible */}
                  <button
                    onClick={() => setExpandedSeed(isExpanded ? null : index)}
                    className={`w-full px-4 py-3 flex items-center justify-between text-left transition-colors ${
                      isTop ? 'bg-green-50 dark:bg-green-900/20 hover:bg-green-100 dark:hover:bg-green-900/30'
                            : 'bg-white dark:bg-gray-800 hover:bg-gray-50 dark:hover:bg-gray-750'
                    }`}
                  >
                    <div className="flex items-center gap-3 flex-1 min-w-0">
                      {isTop && (
                        <span className="px-1.5 py-0.5 bg-green-600 text-white text-[10px] font-bold rounded">
                          {hi ? 'शीर्ष' : 'TOP'}
                        </span>
                      )}
                      <div className="min-w-0">
                        <div className="font-bold text-gray-900 dark:text-gray-100 truncate">
                          {seed.varietyName}
                        </div>
                        <div className="text-xs text-gray-500 dark:text-gray-400 truncate">
                          {seed.seedCompany}
                        </div>
                      </div>
                    </div>

                    {/* Quick Stats */}
                    <div className="flex items-center gap-4 flex-shrink-0 ml-3">
                      <div className="text-center hidden sm:block">
                        <div className="text-sm font-bold text-gray-900 dark:text-gray-100">{seed.maturityDays}</div>
                        <div className="text-[10px] text-gray-500">{hi ? 'दिन' : 'days'}</div>
                      </div>
                      <div className="text-center hidden sm:block">
                        <div className="text-sm font-bold text-green-600 dark:text-green-400">{seed.yieldPotential}</div>
                        <div className="text-[10px] text-gray-500">{hi ? 'टन/हे.' : 't/ha'}</div>
                      </div>
                      <svg className={`w-5 h-5 text-gray-400 transition-transform ${isExpanded ? 'rotate-180' : ''}`} fill="none" viewBox="0 0 24 24" stroke="currentColor">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 9l-7 7-7-7" />
                      </svg>
                    </div>
                  </button>

                  {/* Seed Details — expandable */}
                  {isExpanded && (
                    <div className="px-4 py-3 border-t border-gray-100 dark:border-gray-700 bg-white dark:bg-gray-800">
                      {/* Stats Row */}
                      <div className="grid grid-cols-2 gap-3 mb-3">
                        <div className="bg-blue-50 dark:bg-blue-900/20 rounded-lg p-2.5 text-center">
                          <div className="text-lg font-bold text-blue-700 dark:text-blue-300">{seed.maturityDays}</div>
                          <div className="text-[10px] text-blue-600 dark:text-blue-400">{hi ? 'परिपक्वता (दिन)' : 'Maturity (days)'}</div>
                        </div>
                        <div className="bg-green-50 dark:bg-green-900/20 rounded-lg p-2.5 text-center">
                          <div className="text-lg font-bold text-green-700 dark:text-green-300">{seed.yieldPotential}</div>
                          <div className="text-[10px] text-green-600 dark:text-green-400">{hi ? 'उपज (टन/हेक्टेयर)' : 'Yield (tons/ha)'}</div>
                        </div>
                      </div>

                      {/* Suitability */}
                      <div className="mb-3">
                        <div className="text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wide mb-1">
                          {hi ? 'आपकी मिट्टी के लिए क्यों उपयुक्त' : 'Why suitable for your soil'}
                        </div>
                        <p className="text-sm text-gray-700 dark:text-gray-300 leading-relaxed">
                          {seed.suitabilityReason}
                        </p>
                      </div>

                      {/* Key Characteristics as tags */}
                      {seed.keyCharacteristics && seed.keyCharacteristics.length > 0 && (
                        <div className="flex flex-wrap gap-1.5">
                          {seed.keyCharacteristics.map((char, idx) => (
                            <span key={idx} className="px-2 py-0.5 bg-gray-100 dark:bg-gray-700 text-gray-700 dark:text-gray-300 text-xs rounded-full">
                              {char}
                            </span>
                          ))}
                        </div>
                      )}
                    </div>
                  )}
                </div>
              );
            })}
          </div>
        </div>
      )}

      {/* ─── Save Button ─── */}
      {onSave && (
        <div className="flex justify-center pt-2">
          <button
            onClick={onSave}
            className="px-8 py-3 bg-green-600 hover:bg-green-700 text-white rounded-xl font-medium transition-colors shadow-sm hover:shadow-md flex items-center gap-2"
          >
            💾 {hi ? 'सिफारिश सहेजें' : 'Save Recommendation'}
          </button>
        </div>
      )}
    </div>
  );
};
