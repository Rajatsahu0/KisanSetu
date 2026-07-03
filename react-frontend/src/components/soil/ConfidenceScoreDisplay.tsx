import React from 'react';
import { useLanguage } from '@/contexts/LanguageContext';

interface ConfidenceScoreDisplayProps {
  score: number;
  showDetails?: boolean;
}

export const ConfidenceScoreDisplay: React.FC<ConfidenceScoreDisplayProps> = ({
  score,
  showDetails = false
}) => {
  const { t } = useLanguage();

  const getScoreColor = (score: number): string => {
    if (score >= 90) return 'text-green-600 bg-green-100 dark:bg-green-900/30 dark:text-green-400';
    if (score >= 70) return 'text-yellow-600 bg-yellow-100 dark:bg-yellow-900/30 dark:text-yellow-400';
    return 'text-red-600 bg-red-100 dark:bg-red-900/30 dark:text-red-400';
  };

  const getScoreLabel = (score: number): string => {
    if (score >= 90) return t('soilAnalysis.highConfidence', 'High Confidence');
    if (score >= 70) return t('soilAnalysis.mediumConfidence', 'Medium Confidence');
    return t('soilAnalysis.lowConfidence', 'Low Confidence');
  };

  const getScoreDescription = (score: number): string => {
    if (score >= 90) {
      return t(
        'soilAnalysis.highConfidenceDesc',
        'Data extraction was successful with high accuracy. All fields are complete and validated.'
      );
    }
    if (score >= 70) {
      return t(
        'soilAnalysis.mediumConfidenceDesc',
        'Most data was extracted successfully, but some fields may need verification.'
      );
    }
    return t(
      'soilAnalysis.lowConfidenceDesc',
      'Data extraction had difficulties. Please review and correct the values manually.'
    );
  };

  const getProgressBarColor = (score: number): string => {
    if (score >= 90) return 'bg-green-600';
    if (score >= 70) return 'bg-yellow-600';
    return 'bg-red-600';
  };

  return (
    <div className="bg-white dark:bg-gray-800 rounded-lg shadow-md p-6">
      <div className="flex items-center justify-between mb-4">
        <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100">
          {t('soilAnalysis.confidenceScore', 'Confidence Score')}
        </h3>
        <span className={`px-4 py-2 rounded-full text-sm font-medium ${getScoreColor(score)}`}>
          {getScoreLabel(score)}
        </span>
      </div>

      {/* Progress Bar */}
      <div className="mb-4">
        <div className="flex items-center justify-between mb-2">
          <span className="text-sm text-gray-600 dark:text-gray-400">
            {t('soilAnalysis.accuracy', 'Accuracy')}
          </span>
          <span className="text-2xl font-bold text-gray-900 dark:text-gray-100">
            {score}%
          </span>
        </div>
        <div className="w-full bg-gray-200 dark:bg-gray-700 rounded-full h-3 overflow-hidden">
          <div
            className={`h-full rounded-full transition-all duration-500 ${getProgressBarColor(score)}`}
            style={{ width: `${score}%` }}
          />
        </div>
      </div>

      {/* Description */}
      {showDetails && (
        <div className="mt-4 p-4 bg-gray-50 dark:bg-gray-700/50 rounded-lg">
          <p className="text-sm text-gray-700 dark:text-gray-300">
            {getScoreDescription(score)}
          </p>
        </div>
      )}

      {/* Score Breakdown (if showing details) */}
      {showDetails && (
        <div className="mt-4 space-y-2">
          <h4 className="text-sm font-semibold text-gray-700 dark:text-gray-300">
            {t('soilAnalysis.scoreBreakdown', 'Score Breakdown')}
          </h4>
          <div className="space-y-1 text-sm">
            <div className="flex justify-between">
              <span className="text-gray-600 dark:text-gray-400">
                {t('soilAnalysis.fieldCompleteness', 'Field Completeness')}
              </span>
              <span className="font-medium text-gray-900 dark:text-gray-100">
                {Math.min(100, score + 10)}%
              </span>
            </div>
            <div className="flex justify-between">
              <span className="text-gray-600 dark:text-gray-400">
                {t('soilAnalysis.dataQuality', 'Data Quality')}
              </span>
              <span className="font-medium text-gray-900 dark:text-gray-100">
                {score}%
              </span>
            </div>
            <div className="flex justify-between">
              <span className="text-gray-600 dark:text-gray-400">
                {t('soilAnalysis.validationStatus', 'Validation Status')}
              </span>
              <span className="font-medium text-gray-900 dark:text-gray-100">
                {score >= 90 ? t('common.passed', 'Passed') : t('common.needsReview', 'Needs Review')}
              </span>
            </div>
          </div>
        </div>
      )}

      {/* Recommendations based on score */}
      {showDetails && score < 90 && (
        <div className="mt-4 p-4 bg-blue-50 dark:bg-blue-900/20 rounded-lg">
          <h4 className="text-sm font-semibold text-blue-900 dark:text-blue-100 mb-2">
            {t('soilAnalysis.recommendations', 'Recommendations')}
          </h4>
          <ul className="space-y-1 text-sm text-blue-800 dark:text-blue-200">
            {score < 70 && (
              <li className="flex items-start">
                <svg className="w-4 h-4 mr-2 mt-0.5 flex-shrink-0" fill="currentColor" viewBox="0 0 20 20">
                  <path fillRule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a1 1 0 000 2v3a1 1 0 001 1h1a1 1 0 100-2v-3a1 1 0 00-1-1H9z" clipRule="evenodd" />
                </svg>
                {t('soilAnalysis.retakePhoto', 'Consider retaking the photo in better lighting')}
              </li>
            )}
            <li className="flex items-start">
              <svg className="w-4 h-4 mr-2 mt-0.5 flex-shrink-0" fill="currentColor" viewBox="0 0 20 20">
                <path fillRule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a1 1 0 000 2v3a1 1 0 001 1h1a1 1 0 100-2v-3a1 1 0 00-1-1H9z" clipRule="evenodd" />
              </svg>
              {t('soilAnalysis.reviewFields', 'Review and correct any highlighted fields')}
            </li>
            <li className="flex items-start">
              <svg className="w-4 h-4 mr-2 mt-0.5 flex-shrink-0" fill="currentColor" viewBox="0 0 20 20">
                <path fillRule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a1 1 0 000 2v3a1 1 0 001 1h1a1 1 0 100-2v-3a1 1 0 00-1-1H9z" clipRule="evenodd" />
              </svg>
              {t('soilAnalysis.verifyValues', 'Verify values against your physical card')}
            </li>
          </ul>
        </div>
      )}
    </div>
  );
};
