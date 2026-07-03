import React, { useState } from 'react';
import type { RegenerativePlan } from '@/services/soilAnalysisService';
import { useLanguage } from '@/contexts/LanguageContext';

interface RegenerativePlanDisplayProps {
  plan: RegenerativePlan;
  onSave?: () => void;
}

const PRIORITY_CONFIG = {
  high: { border: 'border-l-red-500', badge: 'bg-red-100 text-red-800 dark:bg-red-900/40 dark:text-red-300', labelKey: 'soilAnalysis.priorityHigh' },
  medium: { border: 'border-l-yellow-500', badge: 'bg-yellow-100 text-yellow-800 dark:bg-yellow-900/40 dark:text-yellow-300', labelKey: 'soilAnalysis.priorityMedium' },
  low: { border: 'border-l-green-500', badge: 'bg-green-100 text-green-800 dark:bg-green-900/40 dark:text-green-300', labelKey: 'soilAnalysis.priorityLow' },
};

export const RegenerativePlanDisplay: React.FC<RegenerativePlanDisplayProps> = ({ plan, onSave }) => {
  const [selectedMonth, setSelectedMonth] = useState<number | null>(null);
  const { t } = useLanguage();

  // Use the plan's first month as "current" — the AI generates starting from the month it was created
  const currentMonthNum = plan.monthlyActions?.[0]?.month || (new Date().getMonth() + 1);
  const currentMonth = plan.monthlyActions?.[0];
  const nextMonth = plan.monthlyActions?.[1];
  const selectedMonthData = selectedMonth ? plan.monthlyActions?.find(m => m.month === selectedMonth) : null;

  return (
    <div className="space-y-6">

      {/* ─── Plan Header ─── */}
      <div className="bg-gradient-to-r from-green-600 to-emerald-700 rounded-xl p-5 text-white shadow-lg">
        <div className="flex items-center justify-between mb-3">
          <h2 className="text-xl font-bold">{t('soilAnalysis.regenerativePlan', 'Regenerative Farming Plan')}</h2>
          {onSave && (
            <button onClick={onSave} className="px-4 py-2 bg-white/20 hover:bg-white/30 rounded-lg text-sm font-medium transition-colors backdrop-blur-sm">
              💾 {t('soilAnalysis.savePlanButton', 'Save Plan')}
            </button>
          )}
        </div>
        <div className="grid grid-cols-3 gap-3">
          <div className="bg-white/10 rounded-lg p-3 text-center">
            <div className="text-2xl font-bold">{plan.carbonEstimate?.totalCarbonTonnesPerYear?.toFixed(1) || '0'}</div>
            <div className="text-xs text-green-100">{t('soilAnalysis.carbonPerYear', 't CO₂/year')}</div>
          </div>
          <div className="bg-white/10 rounded-lg p-3 text-center">
            <div className="text-2xl font-bold">{plan.recommendations?.length || 0}</div>
            <div className="text-xs text-green-100">{t('soilAnalysis.recommendations', 'Recommendations')}</div>
          </div>
          <div className="bg-white/10 rounded-lg p-3 text-center">
            <div className="text-2xl font-bold">12</div>
            <div className="text-xs text-green-100">{t('soilAnalysis.monthPlan', 'Month Plan')}</div>
          </div>
        </div>
      </div>

      {/* ─── Current Month + Calendar (Side by Side) ─── */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">

        {/* Left: Current Month Action Card */}
        {currentMonth && (
          <div className="bg-blue-50 dark:bg-blue-900/20 border-2 border-blue-300 dark:border-blue-700 rounded-xl p-5">
            <div className="flex items-center gap-2 mb-3">
              <span className="text-2xl">📅</span>
              <div>
                <h3 className="text-lg font-bold text-blue-900 dark:text-blue-100">
                  {currentMonth.monthName} — {t('soilAnalysis.whatToDoNow', 'What to Do Now')}
                </h3>
                <p className="text-xs text-blue-600 dark:text-blue-400">{t('soilAnalysis.priorityActions', 'Priority actions for this month')}</p>
              </div>
            </div>

            <div className="space-y-2 mb-4">
              {currentMonth.practices?.map((practice, i) => (
                <div key={i} className="flex items-start gap-2">
                  <span className="text-green-500 mt-0.5">✅</span>
                  <span className="text-sm text-blue-800 dark:text-blue-200">{practice}</span>
                </div>
              ))}
            </div>

            {currentMonth.rationale && (
              <p className="text-xs text-blue-600 dark:text-blue-400 italic mb-3">
                {t('soilAnalysis.whyLabel', 'Why')}: {currentMonth.rationale}
              </p>
            )}

            {nextMonth && (
              <div className="pt-3 border-t border-blue-200 dark:border-blue-700">
                <p className="text-xs text-blue-500 dark:text-blue-400">
                  <span className="font-medium">{t('soilAnalysis.nextMonthLabel', 'Next month')} ({nextMonth.monthName}):</span>{' '}
                  {nextMonth.practices?.slice(0, 2).join(', ')}
                </p>
              </div>
            )}
          </div>
        )}

        {/* Right: 12-Month Calendar */}
        <div className="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-xl p-5">
          <h3 className="text-base font-bold text-gray-900 dark:text-white mb-4">{t('soilAnalysis.timelineTitle', '12-Month Timeline')}</h3>

          {/* Month grid */}
          <div className="grid grid-cols-3 sm:grid-cols-4 gap-2 mb-4">
            {plan.monthlyActions?.map((m) => {
              const isCurrent = m.month === currentMonthNum;
              const isSelected = selectedMonth === m.month;
              return (
                <button key={m.month} onClick={() => setSelectedMonth(isSelected ? null : m.month)}
                  className={`px-2 py-2 rounded-lg text-xs font-medium transition-all text-center ${
                    isCurrent
                      ? 'bg-blue-600 text-white shadow-md ring-2 ring-blue-300 dark:ring-blue-500'
                      : isSelected
                      ? 'bg-green-600 text-white shadow-md'
                      : 'bg-gray-100 dark:bg-gray-700 text-gray-600 dark:text-gray-400 hover:bg-gray-200 dark:hover:bg-gray-600'
                  }`}>
                  {m.monthName}
                  {isCurrent && <div className="text-[8px] mt-0.5">{t('soilAnalysis.nowBadge', 'Now')}</div>}
                </button>
              );
            })}
          </div>

          {/* Selected month detail */}
          {selectedMonthData ? (
            <div className={`rounded-lg border p-4 transition-all ${
              selectedMonthData.month === currentMonthNum
                ? 'border-blue-300 dark:border-blue-700 bg-blue-50/50 dark:bg-blue-900/10'
                : 'border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-800/50'
            }`}>
              <div className="flex items-center justify-between mb-3">
                <h4 className="text-sm font-bold text-gray-900 dark:text-white">
                  📅 {selectedMonthData.monthName}
                  {selectedMonthData.month === currentMonthNum && (
                    <span className="ml-2 text-[10px] bg-blue-600 text-white px-1.5 py-0.5 rounded-full">{t('soilAnalysis.current', 'Current')}</span>
                  )}
                </h4>
                <button onClick={() => setSelectedMonth(null)} className="text-gray-400 hover:text-gray-600 text-xs">✕</button>
              </div>

              {/* Tasks */}
              <div className="mb-3">
                <div className="text-[10px] font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wide mb-1.5">{t('soilAnalysis.tasks', 'Tasks')}</div>
                <div className="space-y-1">
                  {selectedMonthData.practices?.map((p, i) => (
                    <div key={i} className="flex items-start gap-1.5">
                      <span className="text-green-500 text-xs mt-0.5">✅</span>
                      <span className="text-xs text-gray-800 dark:text-gray-200">{p}</span>
                    </div>
                  ))}
                </div>
              </div>

              {/* Why */}
              {selectedMonthData.rationale && (
                <div className="mb-3">
                  <div className="text-[10px] font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wide mb-1">{t('soilAnalysis.whyThisMonth', 'Why This Month')}</div>
                  <p className="text-xs text-gray-600 dark:text-gray-300">{selectedMonthData.rationale}</p>
                </div>
              )}

              {/* Results */}
              {selectedMonthData.expectedOutcomes && selectedMonthData.expectedOutcomes.length > 0 && (
                <div>
                  <div className="text-[10px] font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wide mb-1.5">{t('soilAnalysis.expectedResults', 'Expected Results')}</div>
                  <div className="flex flex-wrap gap-1.5">
                    {selectedMonthData.expectedOutcomes.map((o, i) => (
                      <span key={i} className="px-2 py-0.5 bg-green-100 dark:bg-green-900/30 text-green-700 dark:text-green-300 text-[10px] rounded-full">
                        {o}
                      </span>
                    ))}
                  </div>
                </div>
              )}
            </div>
          ) : (
            <p className="text-xs text-gray-400 dark:text-gray-500 text-center py-6">
              {t('soilAnalysis.clickMonth', 'Click any month to see tasks and outcomes')}
            </p>
          )}
        </div>
      </div>

      {/* ─── Recommendations (All Expanded) ─── */}
      {plan.recommendations && plan.recommendations.length > 0 && (
        <div>
          <h3 className="text-lg font-bold text-gray-900 dark:text-white mb-4">
            {t('soilAnalysis.recommendationsForSoil', 'Recommendations for Your Soil')}
          </h3>
          <div className="space-y-4">
            {plan.recommendations.slice(0, 6).map((rec, i) => {
              const p = PRIORITY_CONFIG[rec.priority as keyof typeof PRIORITY_CONFIG] || PRIORITY_CONFIG.medium;

              return (
                <div key={i} className={`bg-white dark:bg-gray-800 rounded-xl border-l-4 ${p.border} shadow-sm border border-gray-100 dark:border-gray-700 p-5`}>
                  {/* Header */}
                  <div className="flex items-start justify-between mb-3">
                    <div className="flex items-center gap-3">
                      <span className="text-lg font-bold text-gray-300 dark:text-gray-600">#{i + 1}</span>
                      <div>
                        <h4 className="font-semibold text-gray-900 dark:text-white">{rec.title}</h4>
                        <span className="text-xs text-gray-500 dark:text-gray-400">{rec.category}</span>
                      </div>
                    </div>
                    <span className={`px-2.5 py-0.5 rounded-full text-[10px] font-bold ${p.badge}`}>{t(p.labelKey, 'Medium')}</span>
                  </div>

                  {/* Description */}
                  <p className="text-sm text-gray-700 dark:text-gray-300 mb-4">{rec.description}</p>

                  {/* Cost + Benefit */}
                  <div className="grid grid-cols-2 gap-3 mb-4">
                    <div className="bg-gray-50 dark:bg-gray-700/50 rounded-lg p-3">
                      <div className="text-[10px] text-gray-500 dark:text-gray-400 uppercase mb-1">{t('soilAnalysis.estimatedCost', 'Estimated Cost')}</div>
                      <div className="text-lg font-bold text-gray-900 dark:text-white">₹{rec.estimatedCost?.toLocaleString()}</div>
                    </div>
                    <div className="bg-green-50 dark:bg-green-900/20 rounded-lg p-3">
                      <div className="text-[10px] text-gray-500 dark:text-gray-400 uppercase mb-1">{t('soilAnalysis.expectedBenefit', 'Expected Benefit')}</div>
                      <div className="text-sm font-medium text-green-700 dark:text-green-300">{rec.expectedBenefit}</div>
                    </div>
                  </div>

                  {/* Steps */}
                  {rec.implementationSteps && rec.implementationSteps.length > 0 && (
                    <div>
                      <div className="text-[10px] font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wide mb-2">{t('soilAnalysis.implementationSteps', 'Implementation Steps')}</div>
                      <div className="space-y-1.5">
                        {rec.implementationSteps.map((step, si) => (
                          <div key={si} className="flex items-start gap-2">
                            <span className="w-5 h-5 bg-green-100 dark:bg-green-900/30 text-green-700 dark:text-green-300 rounded-full flex items-center justify-center text-[10px] font-bold flex-shrink-0 mt-0.5">
                              {si + 1}
                            </span>
                            <span className="text-sm text-gray-700 dark:text-gray-300">{step}</span>
                          </div>
                        ))}
                      </div>
                    </div>
                  )}
                </div>
              );
            })}
          </div>
        </div>
      )}
    </div>
  );
};
