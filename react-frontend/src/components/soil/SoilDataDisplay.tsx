import React from 'react';
import { useLanguage } from '@/contexts/LanguageContext';
import type { SoilHealthData } from '@/types';

interface SoilDataDisplayProps {
  soilData: SoilHealthData;
  showRecommendations?: boolean;
}

export const SoilDataDisplay: React.FC<SoilDataDisplayProps> = ({
  soilData,
  showRecommendations = true
}) => {
  const { t } = useLanguage();

  const getNutrientStatus = (value: number, optimal: { min: number; max: number }): 'low' | 'optimal' | 'high' => {
    if (value < optimal.min) return 'low';
    if (value > optimal.max) return 'high';
    return 'optimal';
  };

  const getStatusColor = (status: 'low' | 'optimal' | 'high'): string => {
    switch (status) {
      case 'low': return 'text-red-600 bg-red-50';
      case 'optimal': return 'text-green-600 bg-green-50';
      case 'high': return 'text-yellow-600 bg-yellow-50';
    }
  };

  const getStatusText = (status: 'low' | 'optimal' | 'high'): string => {
    switch (status) {
      case 'low': return t('soilAnalysis.low', 'Low');
      case 'optimal': return t('soilAnalysis.optimal', 'Optimal');
      case 'high': return t('soilAnalysis.high', 'High');
    }
  };

  // Optimal ranges (these would ideally come from a configuration)
  const optimalRanges = {
    pH: { min: 6.0, max: 7.5 },
    organicCarbon: { min: 0.5, max: 0.75 },
    nitrogen: { min: 280, max: 450 },
    phosphorus: { min: 11, max: 25 },
    potassium: { min: 110, max: 280 },
    sulfur: { min: 10, max: 20 },
    zinc: { min: 0.6, max: 1.2 },
    boron: { min: 0.5, max: 1.0 },
    iron: { min: 4.5, max: 10 },
    manganese: { min: 2, max: 10 },
    copper: { min: 0.2, max: 0.5 }
  };

  const NutrientRow: React.FC<{
    label: string;
    value: number;
    unit: string;
    optimal: { min: number; max: number };
  }> = ({ label, value, unit, optimal }) => {
    const status = getNutrientStatus(value, optimal);
    const statusColor = getStatusColor(status);
    const statusText = getStatusText(status);

    return (
      <div className="flex items-center justify-between py-3 border-b border-gray-200 dark:border-gray-700">
        <div className="flex-1">
          <span className="text-sm font-medium text-gray-700 dark:text-gray-300">{label}</span>
        </div>
        <div className="flex items-center space-x-4">
          <span className="text-sm font-semibold text-gray-900 dark:text-gray-100">
            {(value ?? 0).toFixed(2)} {unit}
          </span>
          <span className={`px-3 py-1 rounded-full text-xs font-medium ${statusColor}`}>
            {statusText}
          </span>
        </div>
      </div>
    );
  };

  return (
    <div className="space-y-6">
      {/* Basic Information */}
      <div className="bg-white dark:bg-gray-800 rounded-lg shadow p-6">
        <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100 mb-4">
          {t('soilAnalysis.sampleInformation', 'Sample Information')}
        </h3>
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          <div>
            <p className="text-sm text-gray-500 dark:text-gray-400">
              {t('soilAnalysis.sampleId', 'Sample ID')}
            </p>
            <p className="text-base font-medium text-gray-900 dark:text-gray-100">
              {soilData.sampleId}
            </p>
          </div>
          <div>
            <p className="text-sm text-gray-500 dark:text-gray-400">
              {t('soilAnalysis.collectionDate', 'Collection Date')}
            </p>
            <p className="text-base font-medium text-gray-900 dark:text-gray-100">
              {new Date(soilData.collectionDate).toLocaleDateString()}
            </p>
          </div>
          <div>
            <p className="text-sm text-gray-500 dark:text-gray-400">
              {t('soilAnalysis.soilTexture', 'Soil Texture')}
            </p>
            <p className="text-base font-medium text-gray-900 dark:text-gray-100">
              {soilData.soilTexture}
            </p>
          </div>
        </div>
      </div>

      {/* Primary Nutrients */}
      <div className="bg-white dark:bg-gray-800 rounded-lg shadow p-6">
        <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100 mb-4">
          {t('soilAnalysis.primaryNutrients', 'Primary Nutrients')}
        </h3>
        <div className="space-y-2">
          <NutrientRow
            label={t('soilAnalysis.ph', 'pH')}
            value={soilData.pH}
            unit=""
            optimal={optimalRanges.pH}
          />
          <NutrientRow
            label={t('soilAnalysis.organicCarbon', 'Organic Carbon')}
            value={soilData.organicCarbon}
            unit="%"
            optimal={optimalRanges.organicCarbon}
          />
          <NutrientRow
            label={t('soilAnalysis.nitrogen', 'Nitrogen')}
            value={soilData.nitrogen}
            unit="kg/ha"
            optimal={optimalRanges.nitrogen}
          />
          <NutrientRow
            label={t('soilAnalysis.phosphorus', 'Phosphorus')}
            value={soilData.phosphorus}
            unit="kg/ha"
            optimal={optimalRanges.phosphorus}
          />
          <NutrientRow
            label={t('soilAnalysis.potassium', 'Potassium')}
            value={soilData.potassium}
            unit="kg/ha"
            optimal={optimalRanges.potassium}
          />
          <NutrientRow
            label={t('soilAnalysis.sulfur', 'Sulfur')}
            value={soilData.sulfur}
            unit="ppm"
            optimal={optimalRanges.sulfur}
          />
        </div>
      </div>

      {/* Micronutrients */}
      <div className="bg-white dark:bg-gray-800 rounded-lg shadow p-6">
        <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100 mb-4">
          {t('soilAnalysis.micronutrients', 'Micronutrients')}
        </h3>
        <div className="space-y-2">
          <NutrientRow
            label={t('soilAnalysis.zinc', 'Zinc')}
            value={soilData.zinc}
            unit="ppm"
            optimal={optimalRanges.zinc}
          />
          <NutrientRow
            label={t('soilAnalysis.boron', 'Boron')}
            value={soilData.boron}
            unit="ppm"
            optimal={optimalRanges.boron}
          />
          <NutrientRow
            label={t('soilAnalysis.iron', 'Iron')}
            value={soilData.iron}
            unit="ppm"
            optimal={optimalRanges.iron}
          />
          <NutrientRow
            label={t('soilAnalysis.manganese', 'Manganese')}
            value={soilData.manganese}
            unit="ppm"
            optimal={optimalRanges.manganese}
          />
          <NutrientRow
            label={t('soilAnalysis.copper', 'Copper')}
            value={soilData.copper}
            unit="ppm"
            optimal={optimalRanges.copper}
          />
        </div>
      </div>

      {/* Recommendations */}
      {showRecommendations && soilData.recommendations && soilData.recommendations.length > 0 && (
        <div className="bg-blue-50 dark:bg-blue-900/20 rounded-lg shadow p-6">
          <h3 className="text-lg font-semibold text-blue-900 dark:text-blue-100 mb-4">
            {t('soilAnalysis.recommendations', 'Recommendations')}
          </h3>
          <ul className="space-y-2">
            {soilData.recommendations.map((recommendation, index) => (
              <li key={index} className="flex items-start">
                <svg
                  className="w-5 h-5 text-blue-600 dark:text-blue-400 mt-0.5 mr-2 flex-shrink-0"
                  fill="currentColor"
                  viewBox="0 0 20 20"
                >
                  <path
                    fillRule="evenodd"
                    d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z"
                    clipRule="evenodd"
                  />
                </svg>
                <span className="text-sm text-blue-900 dark:text-blue-100">
                  {recommendation}
                </span>
              </li>
            ))}
          </ul>
        </div>
      )}
    </div>
  );
};
