import React, { useState } from 'react';
import { useLanguage } from '@/contexts/LanguageContext';
import { useNotifications } from '@/hooks/useNotifications';
import { useTrialQuota } from '@/hooks/useTrialQuota';
import { TrialLimitModal } from '@/components/auth/TrialLimitModal';
import { plantingAdvisoryService, type PlantingRecommendationResponse } from '@/services/plantingAdvisoryService';
import { PlantingAdvisoryForm } from '@/components/planting/PlantingAdvisoryForm';
import { RecommendationDisplay } from '@/components/planting/RecommendationDisplay';
import { SavedRecommendations } from '@/components/planting/SavedRecommendations';
import { LoadingSpinner } from '@/components/common/LoadingSpinner';
import { ErrorDisplay } from '@/components/error/ErrorDisplay';

type ViewMode = 'form' | 'results' | 'saved';

export const PlantingAdvisoryPage: React.FC = () => {
  const { currentLanguage, t } = useLanguage();
  const language = currentLanguage.code;
  const { canUsePlanting, plantingRemaining, plantingLimit, consumePlanting, isAuthenticated } = useTrialQuota();
  const [showTrialModal, setShowTrialModal] = useState(false);
  const { showNotification } = useNotifications();
  const [viewMode, setViewMode] = useState<ViewMode>('form');
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [recommendation, setRecommendation] = useState<PlantingRecommendationResponse | null>(null);
  const [currentCropType, setCurrentCropType] = useState<string>('');
  const [currentLocation, setCurrentLocation] = useState<string>('');

  const handleGetRecommendations = async (
    cropType: string, location: string, forecastDays: number, planId?: string
  ) => {
    if (!consumePlanting()) { setShowTrialModal(true); return; }
    setIsLoading(true);
    setError(null);
    setCurrentCropType(cropType);
    setCurrentLocation(location);

    try {
      const response = await plantingAdvisoryService.getPlantingRecommendation({ 
        cropType, location, forecastDays, planId, language 
      });
      setRecommendation(response);

      if (response.hasRecommendations) {
        setViewMode('results');
        showNotification({ type: 'success', title: t('app.success', 'Success'), message: response.message });
      } else {
        showNotification({ type: 'warning', title: t('plantingAdvisory.noSuitableWindows', 'No Recommendations'), message: response.message });
      }
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : t('plantingAdvisory.failedToGet', 'Failed to get recommendations');
      setError(errorMessage);

      if (errorMessage.includes('SOIL_DATA_NOT_FOUND') || errorMessage.includes('soil data')) {
        showNotification({
          type: 'error',
          title: t('plantingAdvisory.soilDataRequired', 'Soil Data Required'),
          message: t('plantingAdvisory.uploadSoilFirst', 'Please upload your Soil Health Card first or select a saved plan')
        });
      } else {
        showNotification({ type: 'error', title: t('app.error', 'Error'), message: errorMessage });
      }
    } finally {
      setIsLoading(false);
    }
  };

  const handleSaveRecommendation = async () => {
    if (recommendation && currentCropType && currentLocation) {
      try {
        await plantingAdvisoryService.saveRecommendation(recommendation, currentCropType, currentLocation);
        showNotification({ type: 'success', title: t('app.save', 'Saved'), message: t('plantingAdvisory.recommendationSaved', 'Recommendation saved successfully') });
      } catch (err) {
        showNotification({ type: 'error', title: t('app.error', 'Error'), message: t('plantingAdvisory.saveFailed', 'Failed to save recommendation') });
      }
    }
  };

  const handleRetry = () => { setError(null); setViewMode('form'); };
  const handleNewRecommendation = () => { setRecommendation(null); setError(null); setViewMode('form'); };

  return (
    <div className="min-h-screen bg-gray-50 py-4 sm:py-8 px-2 sm:px-6 lg:px-8">
      <div className="max-w-4xl mx-auto">
        {showTrialModal && <TrialLimitModal feature="planting" limit={plantingLimit} onClose={() => setShowTrialModal(false)} />}

        {!isAuthenticated && (
          <div className="mb-4 bg-purple-50 dark:bg-purple-900/20 border border-purple-200 dark:border-purple-800 rounded-lg p-3 flex items-center justify-between">
            <span className="text-sm text-purple-700 dark:text-purple-300">
              🎁 {t('plantingAdvisory.trialRemaining', 'Free trial')}: <span className="font-bold">{plantingRemaining}</span> / {plantingLimit}
            </span>
            <a href="/register" className="text-xs font-medium text-purple-600 dark:text-purple-400 hover:underline">{t('auth.registerFree', 'Register for unlimited')} →</a>
          </div>
        )}

        <div className="mb-8">
          <h1 className="text-xl sm:text-3xl font-bold text-gray-900 mb-2">🌱 {t('plantingAdvisory.title', 'Planting Advisory')}</h1>
          <p className="text-gray-600">{t('plantingAdvisory.subtitle', 'Get planting recommendations based on weather and soil data')}</p>
        </div>

        <div className="mb-6 flex gap-2 border-b border-gray-200 overflow-x-auto">
          <button onClick={() => setViewMode('form')}
            className={`px-4 py-2 font-medium transition-colors ${viewMode === 'form' ? 'text-green-600 border-b-2 border-green-600' : 'text-gray-600 hover:text-gray-900'}`}>
            {t('plantingAdvisory.newRecommendation', 'New Recommendation')}
          </button>
          <button onClick={() => setViewMode('saved')}
            className={`px-4 py-2 font-medium transition-colors ${viewMode === 'saved' ? 'text-green-600 border-b-2 border-green-600' : 'text-gray-600 hover:text-gray-900'}`}>
            {t('plantingAdvisory.savedRecommendations', 'Saved Recommendations')}
          </button>
        </div>

        <div className="bg-white rounded-lg shadow-md p-6">
          {viewMode === 'form' && (
            <>
              <div className="mb-6 bg-blue-50 border border-blue-200 rounded-lg p-4">
                <h3 className="text-sm font-medium text-blue-900 mb-2">ℹ️ {t('plantingAdvisory.prerequisites', 'Prerequisites')}</h3>
                <p className="text-sm text-blue-700">
                  {t('plantingAdvisory.prerequisitesDesc', 'To get planting recommendations, you can either select a saved soil plan or upload a new Soil Health Card. Saved plans are available in the dropdown above.')}
                </p>
              </div>

              {error && (
                <div className="mb-6">
                  <ErrorDisplay error={{ code: 'ERROR', message: error, userFriendlyMessage: error }} onRetry={handleRetry} />
                </div>
              )}

              <PlantingAdvisoryForm onSubmit={handleGetRecommendations} isLoading={isLoading} />
              {isLoading && <div className="mt-6 flex justify-center"><LoadingSpinner /></div>}
            </>
          )}

          {viewMode === 'results' && recommendation && (
            <>
              <div className="mb-6 flex justify-between items-center">
                <button onClick={handleNewRecommendation} className="text-green-600 hover:text-green-700 font-medium flex items-center gap-2">
                  ← {t('plantingAdvisory.newRecommendation', 'New Recommendation')}
                </button>
              </div>

              {recommendation.hasRecommendations ? (
                <RecommendationDisplay
                  plantingWindows={recommendation.plantingWindows}
                  seedRecommendations={recommendation.seedRecommendations}
                  weatherFetchedAt={recommendation.weatherFetchedAt}
                  soilDataDate={recommendation.soilDataDate}
                  usedPlanId={recommendation.usedPlanId}
                  usedDefaultSoil={recommendation.usedDefaultSoil}
                  dataSources={recommendation.dataSources}
                  onSave={handleSaveRecommendation}
                />
              ) : (
                <div className="text-center py-12">
                  <div className="text-6xl mb-4">🌾</div>
                  <h3 className="text-lg font-medium text-gray-900 mb-2">{t('plantingAdvisory.noSuitableWindows', 'No Suitable Planting Windows')}</h3>
                  <p className="text-gray-600 mb-4">{recommendation.message}</p>
                  <button onClick={handleNewRecommendation} className="px-6 py-2 bg-green-600 text-white rounded-lg hover:bg-green-700 transition-colors">
                    {t('app.tryAgain', 'Try Again')}
                  </button>
                </div>
              )}
            </>
          )}

          {viewMode === 'saved' && <SavedRecommendations />}
        </div>
      </div>
    </div>
  );
};

export default PlantingAdvisoryPage;
