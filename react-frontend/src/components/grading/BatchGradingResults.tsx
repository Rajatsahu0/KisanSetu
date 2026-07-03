import React, { useState } from 'react';
import { useLanguage } from '@/contexts/LanguageContext';
import type { BatchGradingResult } from '@/types';
import { QualityGrade } from '@/types';
import { GradingResultDisplay } from './GradingResultDisplay';

interface BatchGradingResultsProps {
  batchResult: BatchGradingResult;
  imageUrls?: string[];
}

const gradeColors: Record<string, { bg: string; text: string }> = {
  [QualityGrade.A]:      { bg: 'bg-emerald-100 dark:bg-emerald-900/30', text: 'text-emerald-800 dark:text-emerald-200' },
  [QualityGrade.B]:      { bg: 'bg-blue-100 dark:bg-blue-900/30', text: 'text-blue-800 dark:text-blue-200' },
  [QualityGrade.FAQ]:    { bg: 'bg-sky-100 dark:bg-sky-900/30', text: 'text-sky-800 dark:text-sky-200' },
  [QualityGrade.C]:      { bg: 'bg-yellow-100 dark:bg-yellow-900/30', text: 'text-yellow-800 dark:text-yellow-200' },
  [QualityGrade.NonFAQ]: { bg: 'bg-orange-100 dark:bg-orange-900/30', text: 'text-orange-800 dark:text-orange-200' },
  [QualityGrade.Reject]: { bg: 'bg-red-100 dark:bg-red-900/30', text: 'text-red-800 dark:text-red-200' },
};

const getColors = (grade: string) => gradeColors[grade] || gradeColors[QualityGrade.FAQ];

export const BatchGradingResults: React.FC<BatchGradingResultsProps> = ({
  batchResult,
  imageUrls = []
}) => {
  const { t } = useLanguage();
  const [selectedIndex, setSelectedIndex] = useState<number | null>(null);

  const results = batchResult.individualResults || [];
  const colors = getColors(batchResult.aggregatedGrade);

  const gradeDistribution = results.reduce((acc, result) => {
    const g = result.gradeLabel || result.grade;
    acc[g] = (acc[g] || 0) + 1;
    return acc;
  }, {} as Record<string, number>);

  return (
    <div className="space-y-6">
      {/* Summary */}
      <div className="bg-white dark:bg-gray-800 rounded-lg shadow-md p-6">
        <h2 className="text-2xl font-bold text-gray-900 dark:text-gray-100 mb-6">
          {t('grading.batchResults', 'Batch Grading Results')}
        </h2>

        <div className="grid grid-cols-1 md:grid-cols-3 gap-4 mb-6">
          <div className="bg-gray-50 dark:bg-gray-700/50 rounded-lg p-4">
            <p className="text-sm text-gray-500 dark:text-gray-400 mb-1">
              {t('grading.processed', 'Processed')}
            </p>
            <p className="text-2xl font-bold text-gray-900 dark:text-gray-100">
              {batchResult.processedCount} {t('grading.images', 'images')}
            </p>
          </div>

          <div className={`rounded-lg p-4 ${colors.bg}`}>
            <p className="text-sm mb-1 opacity-80">
              {t('grading.aggregatedGrade', 'Batch Grade')}
            </p>
            <p className={`text-2xl font-bold ${colors.text}`}>
              {batchResult.aggregatedGrade}
            </p>
          </div>

          <div className="bg-green-50 dark:bg-green-900/20 rounded-lg p-4">
            <p className="text-sm text-green-700 dark:text-green-300 mb-1">
              {t('grading.batchPrice', 'Batch Price')}
            </p>
            <p className="text-2xl font-bold text-green-600 dark:text-green-400">
              ₹{batchResult.batchCertifiedPrice?.toFixed(0) || '0'}
            </p>
            <p className="text-xs text-green-600 dark:text-green-400">
              {t('grading.perQuintal', 'per quintal (100 kg)')}
            </p>
          </div>
        </div>

        {/* Grade Distribution */}
        {Object.keys(gradeDistribution).length > 0 && (
          <div>
            <h3 className="text-sm font-semibold text-gray-900 dark:text-gray-100 mb-3">
              {t('grading.gradeDistribution', 'Grade Distribution')}
            </h3>
            <div className="flex flex-wrap gap-3">
              {Object.entries(gradeDistribution).map(([grade, count]) => {
                const gc = getColors(grade);
                return (
                  <div key={grade} className={`rounded-lg px-4 py-2 ${gc.bg}`}>
                    <span className={`text-sm font-bold ${gc.text}`}>
                      {grade}: {count}
                    </span>
                    <span className={`text-xs ml-1 ${gc.text} opacity-75`}>
                      ({((count / results.length) * 100).toFixed(0)}%)
                    </span>
                  </div>
                );
              })}
            </div>
          </div>
        )}
      </div>

      {/* Individual Results */}
      <div>
        <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100 mb-4">
          {t('grading.individualResults', 'Individual Results')}
        </h3>

        {selectedIndex !== null ? (
          <div className="space-y-4">
            <button
              onClick={() => setSelectedIndex(null)}
              className="flex items-center text-green-600 dark:text-green-400 hover:underline"
            >
              <svg className="w-5 h-5 mr-1" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 19l-7-7 7-7" />
              </svg>
              {t('grading.backToList', 'Back to list')}
            </button>
            <GradingResultDisplay
              result={results[selectedIndex]}
              imageUrl={imageUrls[selectedIndex]}
            />
          </div>
        ) : (
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
            {results.map((result, index) => {
              const rc = getColors(result.grade);
              return (
                <div
                  key={result.recordId}
                  onClick={() => setSelectedIndex(index)}
                  className="bg-white dark:bg-gray-800 rounded-lg shadow-md overflow-hidden cursor-pointer hover:shadow-lg transition-shadow"
                >
                  {imageUrls[index] && (
                    <div className="relative h-40 bg-gray-100 dark:bg-gray-900">
                      <img src={imageUrls[index]} alt={`Produce ${index + 1}`} className="w-full h-full object-cover" />
                      <div className={`absolute top-2 right-2 px-3 py-1 rounded-full text-xs font-bold ${rc.bg} ${rc.text}`}>
                        {result.gradeLabel || result.grade}
                      </div>
                    </div>
                  )}
                  <div className="p-4">
                    <div className="flex items-center justify-between mb-1">
                      <span className="text-sm font-medium text-gray-700 dark:text-gray-300">
                        {t('grading.image', 'Image')} {index + 1}
                      </span>
                      <span className="text-sm font-bold text-green-600 dark:text-green-400">
                        ₹{result.certifiedPrice.toFixed(0)}/q
                      </span>
                    </div>
                    {result.analysis.visionReasoning && (
                      <p className="text-xs text-gray-500 dark:text-gray-400 truncate">
                        {result.analysis.visionReasoning}
                      </p>
                    )}
                  </div>
                </div>
              );
            })}
          </div>
        )}
      </div>
    </div>
  );
};
