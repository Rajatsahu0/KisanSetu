import React, { useState, useEffect, useCallback } from 'react';
import { useLanguage } from '@/contexts/LanguageContext';
import { soilAnalysisService, type RegenerativePlan } from '@/services/soilAnalysisService';
import { RegenerativePlanDisplay } from './RegenerativePlanDisplay';
import { ToastContainer } from '@/components/notifications/ToastContainer';
import type { ToastNotification } from '@/components/notifications/Toast';

interface SavedPlan extends RegenerativePlan {
  savedAt?: string;
  carbonSequestrationPotential?: number;
  generatedDate?: string;
}

interface SavedPlansViewProps {
  onSelectPlan?: (plan: RegenerativePlan) => void;
}

export const SavedPlansView: React.FC<SavedPlansViewProps> = ({ onSelectPlan }) => {
  const { t } = useLanguage();
  const [savedPlans, setSavedPlans] = useState<SavedPlan[]>([]);
  const [selectedPlan, setSelectedPlan] = useState<SavedPlan | null>(null);
  const [compareMode, setCompareMode] = useState(false);
  const [comparisonPlans, setComparisonPlans] = useState<SavedPlan[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [toasts, setToasts] = useState<ToastNotification[]>([]);

  const showToast = useCallback((type: ToastNotification['type'], title: string, message: string) => {
    const id = Date.now().toString();
    setToasts(prev => [...prev, { id, type, title, message, duration: 4000 }]);
  }, []);

  const dismissToast = useCallback((id: string) => {
    setToasts(prev => prev.filter(t => t.id !== id));
  }, []);

  useEffect(() => {
    loadSavedPlans();
  }, []);

  const loadSavedPlans = async () => {
    try {
      setLoading(true);
      setError(null);
      const plans = await soilAnalysisService.getSavedPlans();
      setSavedPlans(plans as SavedPlan[]);
    } catch (err) {
      console.error('Failed to load saved plans:', err);
      setError('Failed to load saved plans');
    } finally {
      setLoading(false);
    }
  };

  const handleDeletePlan = async (planId: string) => {
    if (confirm(t('soilAnalysis.confirmDelete', 'Are you sure you want to delete this plan?'))) {
      try {
        await soilAnalysisService.deleteSavedPlan(planId);
        await loadSavedPlans();
        showToast('success', t('common.deleted', 'Deleted'), t('soilAnalysis.planDeleted', 'Plan deleted successfully'));
        if (selectedPlan?.planId === planId) {
          setSelectedPlan(null);
        }
      } catch (err) {
        console.error('Failed to delete plan:', err);
        showToast('error', t('common.error', 'Error'), t('soilAnalysis.deleteFailed', 'Failed to delete plan. Please try again.'));
      }
    }
  };

  const handleToggleComparison = (plan: SavedPlan) => {
    if (comparisonPlans.find(p => p.planId === plan.planId)) {
      setComparisonPlans(comparisonPlans.filter(p => p.planId !== plan.planId));
    } else if (comparisonPlans.length < 2) {
      setComparisonPlans([...comparisonPlans, plan]);
    }
  };

  const PlanCard: React.FC<{ plan: SavedPlan }> = ({ plan }) => (
    <div className="bg-white dark:bg-gray-800 rounded-lg shadow-md p-6 border-l-4 border-green-500">
      <div className="flex items-start justify-between mb-4">
        <div className="flex-1">
          <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100 mb-1">
            {t('soilAnalysis.plan', 'Plan')} #{plan.planId.slice(0, 8)}
          </h3>
          <p className="text-sm text-gray-500 dark:text-gray-400">
            {t('soilAnalysis.saved', 'Saved')}: {plan.savedAt ? new Date(plan.savedAt).toLocaleDateString() : new Date(plan.createdAt).toLocaleDateString()}
          </p>
          <p className="text-sm text-gray-500 dark:text-gray-400">
            {t('soilAnalysis.generated', 'Generated')}: {plan.generatedDate ? new Date(plan.generatedDate).toLocaleDateString() : new Date(plan.createdAt).toLocaleDateString()}
          </p>
        </div>
        {compareMode && (
          <input
            type="checkbox"
            checked={comparisonPlans.some(p => p.planId === plan.planId)}
            onChange={() => handleToggleComparison(plan)}
            disabled={!comparisonPlans.some(p => p.planId === plan.planId) && comparisonPlans.length >= 2}
            className="w-5 h-5 text-green-600 rounded focus:ring-green-500"
          />
        )}
      </div>

      <div className="grid grid-cols-2 gap-4 mb-4">
        <div className="bg-gray-50 dark:bg-gray-700/50 rounded-lg p-3">
          <p className="text-xs text-gray-500 dark:text-gray-400 mb-1">
            {t('soilAnalysis.carbonSequestration', 'Carbon Sequestration')}
          </p>
          <p className="text-lg font-semibold text-gray-900 dark:text-gray-100">
            {(plan.carbonSequestrationPotential || plan.carbonEstimate?.totalCarbonTonnesPerYear || 0).toFixed(2)} t
          </p>
        </div>
        <div className="bg-gray-50 dark:bg-gray-700/50 rounded-lg p-3">
          <p className="text-xs text-gray-500 dark:text-gray-400 mb-1">
            {t('soilAnalysis.costSavings', 'Cost Savings')}
          </p>
          <p className="text-lg font-semibold text-gray-900 dark:text-gray-100">
            ₹{(plan.estimatedCostSavings || 0).toLocaleString()}
          </p>
        </div>
      </div>

      <div className="flex space-x-2">
        <button
          onClick={() => {
            setSelectedPlan(plan);
            onSelectPlan?.(plan);
          }}
          className="flex-1 px-4 py-2 bg-green-600 text-white rounded-md hover:bg-green-700 text-sm font-medium"
        >
          {t('soilAnalysis.viewDetails', 'View Details')}
        </button>
        <button
          onClick={() => handleDeletePlan(plan.planId)}
          className="px-4 py-2 border border-red-300 text-red-600 rounded-md hover:bg-red-50 dark:hover:bg-red-900/20 text-sm font-medium"
        >
          {t('common.delete', 'Delete')}
        </button>
      </div>
    </div>
  );

  const ComparisonView: React.FC = () => {
    if (comparisonPlans.length < 2) {
      return (
        <div className="bg-white dark:bg-gray-800 rounded-lg shadow-md p-12 text-center">
          <p className="text-gray-500 dark:text-gray-400">
            {t('soilAnalysis.selectTwoPlans', 'Select two plans to compare')}
          </p>
        </div>
      );
    }

    const [plan1, plan2] = comparisonPlans;

    return (
      <div className="space-y-6">
        <div className="bg-white dark:bg-gray-800 rounded-lg shadow-md p-6">
          <h3 className="text-xl font-semibold text-gray-900 dark:text-gray-100 mb-6">
            {t('soilAnalysis.planComparison', 'Plan Comparison')}
          </h3>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            {/* Plan 1 */}
            <div>
              <h4 className="text-lg font-semibold text-gray-900 dark:text-gray-100 mb-4">
                {t('soilAnalysis.plan', 'Plan')} #{plan1.planId.slice(0, 8)}
              </h4>
              <div className="space-y-3">
                <div className="flex justify-between">
                  <span className="text-sm text-gray-600 dark:text-gray-400">
                    {t('soilAnalysis.generated', 'Generated')}
                  </span>
                  <span className="text-sm font-medium text-gray-900 dark:text-gray-100">
                    {plan1.generatedDate ? new Date(plan1.generatedDate).toLocaleDateString() : new Date(plan1.createdAt).toLocaleDateString()}
                  </span>
                </div>
                <div className="flex justify-between">
                  <span className="text-sm text-gray-600 dark:text-gray-400">
                    {t('soilAnalysis.carbonSequestration', 'Carbon Sequestration')}
                  </span>
                  <span className="text-sm font-medium text-gray-900 dark:text-gray-100">
                    {(plan1.carbonSequestrationPotential || plan1.carbonEstimate?.totalCarbonTonnesPerYear || 0).toFixed(2)} t
                  </span>
                </div>
                <div className="flex justify-between">
                  <span className="text-sm text-gray-600 dark:text-gray-400">
                    {t('soilAnalysis.costSavings', 'Cost Savings')}
                  </span>
                  <span className="text-sm font-medium text-gray-900 dark:text-gray-100">
                    ₹{(plan1.estimatedCostSavings || 0).toLocaleString()}
                  </span>
                </div>
                <div className="flex justify-between">
                  <span className="text-sm text-gray-600 dark:text-gray-400">
                    {t('soilAnalysis.recommendations', 'Recommendations')}
                  </span>
                  <span className="text-sm font-medium text-gray-900 dark:text-gray-100">
                    {plan1.recommendations?.length || 0}
                  </span>
                </div>
              </div>
            </div>

            {/* Plan 2 */}
            <div>
              <h4 className="text-lg font-semibold text-gray-900 dark:text-gray-100 mb-4">
                {t('soilAnalysis.plan', 'Plan')} #{plan2.planId.slice(0, 8)}
              </h4>
              <div className="space-y-3">
                <div className="flex justify-between">
                  <span className="text-sm text-gray-600 dark:text-gray-400">
                    {t('soilAnalysis.generated', 'Generated')}
                  </span>
                  <span className="text-sm font-medium text-gray-900 dark:text-gray-100">
                    {plan2.generatedDate ? new Date(plan2.generatedDate).toLocaleDateString() : new Date(plan2.createdAt).toLocaleDateString()}
                  </span>
                </div>
                <div className="flex justify-between">
                  <span className="text-sm text-gray-600 dark:text-gray-400">
                    {t('soilAnalysis.carbonSequestration', 'Carbon Sequestration')}
                  </span>
                  <span className="text-sm font-medium text-gray-900 dark:text-gray-100">
                    {(plan2.carbonSequestrationPotential || plan2.carbonEstimate?.totalCarbonTonnesPerYear || 0).toFixed(2)} t
                  </span>
                </div>
                <div className="flex justify-between">
                  <span className="text-sm text-gray-600 dark:text-gray-400">
                    {t('soilAnalysis.costSavings', 'Cost Savings')}
                  </span>
                  <span className="text-sm font-medium text-gray-900 dark:text-gray-100">
                    ₹{(plan2.estimatedCostSavings || 0).toLocaleString()}
                  </span>
                </div>
                <div className="flex justify-between">
                  <span className="text-sm text-gray-600 dark:text-gray-400">
                    {t('soilAnalysis.recommendations', 'Recommendations')}
                  </span>
                  <span className="text-sm font-medium text-gray-900 dark:text-gray-100">
                    {plan2.recommendations?.length || 0}
                  </span>
                </div>
              </div>
            </div>
          </div>

          {/* Differences */}
          <div className="mt-6 pt-6 border-t border-gray-200 dark:border-gray-700">
            <h5 className="text-sm font-semibold text-gray-700 dark:text-gray-300 mb-3">
              {t('soilAnalysis.keyDifferences', 'Key Differences')}
            </h5>
            <div className="space-y-2 text-sm">
              <div className="flex items-center justify-between">
                <span className="text-gray-600 dark:text-gray-400">
                  {t('soilAnalysis.carbonDifference', 'Carbon Sequestration Difference')}
                </span>
                <span className={`font-medium ${
                  (plan2.carbonSequestrationPotential || plan2.carbonEstimate?.totalCarbonTonnesPerYear || 0) > 
                  (plan1.carbonSequestrationPotential || plan1.carbonEstimate?.totalCarbonTonnesPerYear || 0)
                    ? 'text-green-600 dark:text-green-400'
                    : 'text-red-600 dark:text-red-400'
                }`}>
                  {((plan2.carbonSequestrationPotential || plan2.carbonEstimate?.totalCarbonTonnesPerYear || 0) - 
                    (plan1.carbonSequestrationPotential || plan1.carbonEstimate?.totalCarbonTonnesPerYear || 0)).toFixed(2)} t
                </span>
              </div>
              <div className="flex items-center justify-between">
                <span className="text-gray-600 dark:text-gray-400">
                  {t('soilAnalysis.savingsDifference', 'Cost Savings Difference')}
                </span>
                <span className={`font-medium ${
                  (plan2.estimatedCostSavings || 0) > (plan1.estimatedCostSavings || 0)
                    ? 'text-green-600 dark:text-green-400'
                    : 'text-red-600 dark:text-red-400'
                }`}>
                  ₹{((plan2.estimatedCostSavings || 0) - (plan1.estimatedCostSavings || 0)).toLocaleString()}
                </span>
              </div>
            </div>
          </div>
        </div>
      </div>
    );
  };

  if (selectedPlan && !compareMode) {
    return (
      <div className="space-y-6">
        <button
          onClick={() => setSelectedPlan(null)}
          className="flex items-center text-green-600 hover:text-green-700 dark:text-green-400"
        >
          <svg className="w-5 h-5 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 19l-7-7 7-7" />
          </svg>
          {t('common.back', 'Back to List')}
        </button>
        <RegenerativePlanDisplay plan={selectedPlan} />
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <ToastContainer notifications={toasts} onDismiss={dismissToast} position="top-right" />
      {/* Header */}
      <div className="flex items-center justify-between">
        <h2 className="text-2xl font-bold text-gray-900 dark:text-gray-100">
          {t('soilAnalysis.savedPlans', 'Saved Plans')}
        </h2>
        <button
          onClick={() => {
            setCompareMode(!compareMode);
            setComparisonPlans([]);
          }}
          className={`px-4 py-2 rounded-md text-sm font-medium ${
            compareMode
              ? 'bg-gray-200 text-gray-700 dark:bg-gray-700 dark:text-gray-300'
              : 'bg-green-600 text-white hover:bg-green-700'
          }`}
        >
          {compareMode
            ? t('common.cancel', 'Cancel')
            : t('soilAnalysis.comparePlans', 'Compare Plans')
          }
        </button>
      </div>

      {/* Content */}
      {loading ? (
        <div className="bg-white dark:bg-gray-800 rounded-lg shadow-md p-12 text-center">
          <p className="text-gray-500 dark:text-gray-400">
            {t('common.loading', 'Loading...')}
          </p>
        </div>
      ) : error ? (
        <div className="bg-white dark:bg-gray-800 rounded-lg shadow-md p-12 text-center">
          <p className="text-red-500 dark:text-red-400">{error}</p>
          <button
            onClick={loadSavedPlans}
            className="mt-4 px-4 py-2 bg-green-600 text-white rounded-md hover:bg-green-700"
          >
            {t('common.retry', 'Retry')}
          </button>
        </div>
      ) : compareMode ? (
        <>
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
            {savedPlans.map(plan => (
              <PlanCard key={plan.planId} plan={plan} />
            ))}
          </div>
          {comparisonPlans.length > 0 && <ComparisonView />}
        </>
      ) : savedPlans.length > 0 ? (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {savedPlans.map(plan => (
            <PlanCard key={plan.planId} plan={plan} />
          ))}
        </div>
      ) : (
        <div className="bg-white dark:bg-gray-800 rounded-lg shadow-md p-12 text-center">
          <p className="text-gray-500 dark:text-gray-400">
            {t('soilAnalysis.noSavedPlans', 'No saved plans yet')}
          </p>
        </div>
      )}
    </div>
  );
};
