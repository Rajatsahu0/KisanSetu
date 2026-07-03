import React from 'react';
import { useLanguage } from '@/contexts/LanguageContext';
import type { GradingResult } from '@/types';
import { QualityGrade } from '@/types';

interface GradingResultDisplayProps {
  result: GradingResult;
  imageUrl?: string;
  onSave?: () => void;
}

const gradeConfig: Record<string, { bg: string; text: string; border: string; emoji: string }> = {
  [QualityGrade.A]:      { bg: 'bg-emerald-100 dark:bg-emerald-900/30', text: 'text-emerald-800 dark:text-emerald-200', border: 'border-emerald-400 dark:border-emerald-600', emoji: '🏆' },
  [QualityGrade.B]:      { bg: 'bg-blue-100 dark:bg-blue-900/30', text: 'text-blue-800 dark:text-blue-200', border: 'border-blue-400 dark:border-blue-600', emoji: '✅' },
  [QualityGrade.FAQ]:    { bg: 'bg-sky-100 dark:bg-sky-900/30', text: 'text-sky-800 dark:text-sky-200', border: 'border-sky-400 dark:border-sky-600', emoji: '📊' },
  [QualityGrade.C]:      { bg: 'bg-yellow-100 dark:bg-yellow-900/30', text: 'text-yellow-800 dark:text-yellow-200', border: 'border-yellow-400 dark:border-yellow-600', emoji: '⚠️' },
  [QualityGrade.NonFAQ]: { bg: 'bg-orange-100 dark:bg-orange-900/30', text: 'text-orange-800 dark:text-orange-200', border: 'border-orange-400 dark:border-orange-600', emoji: '📉' },
  [QualityGrade.Reject]: { bg: 'bg-red-100 dark:bg-red-900/30', text: 'text-red-800 dark:text-red-200', border: 'border-red-400 dark:border-red-600', emoji: '❌' },
};

const getGradeColors = (grade: QualityGrade | string) => {
  return gradeConfig[grade] || gradeConfig[QualityGrade.FAQ];
};

export const GradingResultDisplay: React.FC<GradingResultDisplayProps> = ({
  result,
  imageUrl,
  onSave
}) => {
  const { t } = useLanguage();
  const colors = getGradeColors(result.grade);
  const gradeLabel = t(`grading.grade${result.grade}`, result.gradeLabel || result.grade);

  return (
    <div className="bg-white dark:bg-gray-800 rounded-lg shadow-md overflow-hidden">
      {imageUrl && (
        <div className="relative h-64 bg-gray-100 dark:bg-gray-900">
          <img src={imageUrl} alt="Graded produce" className="w-full h-full object-contain" />
        </div>
      )}

      <div className="p-6 space-y-6">
        {/* Grade + Price Header */}
        <div className="flex flex-col sm:flex-row sm:items-start sm:justify-between gap-4">
          <div>
            <p className="text-sm text-gray-500 dark:text-gray-400 mb-1">
              {t('grading.mandiGrade', 'Mandi Quality Grade')}
            </p>
            <div className={`inline-flex items-center gap-2 px-5 py-3 rounded-lg border-2 ${colors.bg} ${colors.text} ${colors.border}`}>
              <span className="text-2xl">{colors.emoji}</span>
              <span className="text-xl font-bold">{gradeLabel}</span>
            </div>
          </div>
          <div className="sm:text-right">
            <p className="text-sm text-gray-500 dark:text-gray-400 mb-1">
              {t('grading.certifiedPrice', 'Certified Mandi Price')}
            </p>
            <p className="text-3xl font-bold text-green-600 dark:text-green-400">
              ₹{result.certifiedPrice.toFixed(0)}
            </p>
            <p className="text-xs text-gray-500 dark:text-gray-400">
              {t('grading.perQuintal', 'per quintal (100 kg)')}
            </p>
          </div>
        </div>

        {/* AI Reasoning */}
        {result.analysis.visionReasoning && (
          <div className="rounded-lg overflow-hidden border border-gray-200 dark:border-gray-600">
            <div className={`p-4 ${colors.bg}`}>
              <div className="flex items-start gap-2">
                <span className="text-lg mt-0.5">🤖</span>
                <div className="flex-1">
                  <p className={`text-sm font-medium ${colors.text} mb-1`}>
                    {t('grading.aiAssessment', 'AI Quality Assessment')}
                  </p>
                  <p className={`text-sm ${colors.text} leading-relaxed`}>
                    {result.analysis.visionReasoning}
                  </p>
                </div>
              </div>
            </div>
            <div className="bg-gray-100 dark:bg-gray-700 px-4 py-2.5 border-t border-gray-200 dark:border-gray-600">
              <p className="text-xs text-gray-500 dark:text-gray-400 italic">
                ⚠️ {t('grading.aiDisclaimer', 'This assessment is AI-generated based on image analysis. Actual mandi grading may vary based on physical inspection, touch, and smell. Use this as a reference guide before taking your produce to market.')}
              </p>
            </div>
          </div>
        )}

        {/* Quality Indicators */}
        <div>
          <h4 className="text-sm font-semibold text-gray-900 dark:text-gray-100 mb-3">
            {t('grading.qualityIndicators', 'Quality Parameters')}
          </h4>
          <div className="grid grid-cols-2 gap-3">
            {result.analysis.qualityIndicators.map((indicator, index) => {
              const statusColor = indicator.status === 'good'
                ? 'text-green-600 dark:text-green-400 bg-green-50 dark:bg-green-900/20'
                : indicator.status === 'fair'
                ? 'text-yellow-600 dark:text-yellow-400 bg-yellow-50 dark:bg-yellow-900/20'
                : 'text-red-600 dark:text-red-400 bg-red-50 dark:bg-red-900/20';

              const barColor = indicator.status === 'good' ? 'bg-green-500'
                : indicator.status === 'fair' ? 'bg-yellow-500' : 'bg-red-500';

              return (
                <div key={index} className={`rounded-lg p-3 ${statusColor}`}>
                  <div className="flex items-center justify-between mb-1">
                    <span className="text-xs font-medium">{t(`grading.indicator.${indicator.name.toLowerCase().replace(/ /g, '')}`, indicator.name)}</span>
                    <span className="text-xs font-bold">{indicator.value.toFixed(0)}/100</span>
                  </div>
                  <div className="w-full bg-gray-200 dark:bg-gray-600 rounded-full h-1.5">
                    <div
                      className={`${barColor} h-1.5 rounded-full transition-all duration-500`}
                      style={{ width: `${Math.min(indicator.value, 100)}%` }}
                    />
                  </div>
                </div>
              );
            })}
          </div>
        </div>

        {/* Photo Quality (separate from produce grade) */}
        <div className="bg-gray-50 dark:bg-gray-700/50 rounded-lg p-4">
          <h4 className="text-xs font-semibold text-gray-500 dark:text-gray-400 mb-2 uppercase tracking-wide">
            {t('grading.photoQuality', 'Photo Quality')}
          </h4>
          <div className="flex flex-wrap items-center gap-4 sm:gap-6 text-sm">
            <div>
              <span className="text-gray-500 dark:text-gray-400">{t('grading.resolution', 'Resolution')}: </span>
              <span className="font-medium text-gray-900 dark:text-gray-100">
                {result.analysis.imageQuality.resolution}
              </span>
            </div>
            <div>
              <span className="text-gray-500 dark:text-gray-400">{t('grading.clarity', 'Clarity')}: </span>
              <span className="font-medium text-gray-900 dark:text-gray-100">
                {(result.analysis.imageQuality.clarity * 100).toFixed(0)}%
              </span>
            </div>
            <div>
              <span className="text-gray-500 dark:text-gray-400">{t('grading.lighting', 'Lighting')}: </span>
              <span className="font-medium text-gray-900 dark:text-gray-100">
                {(result.analysis.imageQuality.lighting * 100).toFixed(0)}%
              </span>
            </div>
          </div>
          <p className="text-xs text-gray-400 dark:text-gray-500 mt-1 italic">
            {t('grading.photoNote', 'Grade is based on produce quality, not photo conditions')}
          </p>
        </div>

        {/* Detected Defects */}
        {result.analysis.detectedObjects.length > 0 && (
          <div>
            <h4 className="text-sm font-semibold text-gray-900 dark:text-gray-100 mb-2">
              {t('grading.detectedDefects', 'Detected Issues')}
            </h4>
            <div className="flex flex-wrap gap-2">
              {result.analysis.detectedObjects.map((obj, index) => (
                <span key={index} className="inline-flex items-center px-3 py-1 rounded-full text-xs font-medium bg-red-50 text-red-700 dark:bg-red-900/30 dark:text-red-300">
                  {obj.label} ({(obj.confidence * 100).toFixed(0)}%)
                </span>
              ))}
            </div>
          </div>
        )}

        {/* Timestamp & Record */}
        <div className="pt-4 border-t border-gray-200 dark:border-gray-700 flex items-center justify-between">
          <div>
            <p className="text-xs text-gray-400">
              {t('grading.gradedOn', 'Graded')}: {new Date(result.timestamp).toLocaleString()}
            </p>
            <p className="text-xs text-gray-400">
              ID: {result.recordId.slice(0, 8)}...
            </p>
          </div>
          {onSave && (
            <button
              onClick={onSave}
              className="px-4 py-2 bg-green-600 text-white text-sm rounded-md hover:bg-green-700 transition-colors"
            >
              {t('grading.saveResult', 'Save Result')}
            </button>
          )}
        </div>
      </div>
    </div>
  );
};
