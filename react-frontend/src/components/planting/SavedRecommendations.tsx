import React, { useState, useEffect, useCallback } from 'react';
import { useLanguage } from '@/contexts/LanguageContext';
import { useAuth } from '@/contexts/AuthContext';
import { plantingAdvisoryService, type SavedRecommendation } from '@/services/plantingAdvisoryService';
import { RecommendationDisplay } from './RecommendationDisplay';
import { ToastContainer } from '@/components/notifications/ToastContainer';
import type { ToastNotification } from '@/components/notifications/Toast';

export const SavedRecommendations: React.FC = () => {
  const { currentLanguage } = useLanguage();
  const { isAuthenticated } = useAuth();
  const language = currentLanguage.code;
  const [savedRecommendations, setSavedRecommendations] = useState<SavedRecommendation[]>([]);
  const [selectedRecommendation, setSelectedRecommendation] = useState<SavedRecommendation | null>(null);
  const [toasts, setToasts] = useState<ToastNotification[]>([]);

  const showToast = useCallback((type: ToastNotification['type'], title: string, message: string) => {
    const id = Date.now().toString();
    setToasts(prev => [...prev, { id, type, title, message, duration: 4000 }]);
  }, []);

  const dismissToast = useCallback((id: string) => {
    setToasts(prev => prev.filter(t => t.id !== id));
  }, []);

  useEffect(() => {
    loadSavedRecommendations();
  }, []);

  const loadSavedRecommendations = async () => {
    if (!isAuthenticated) {
      setSavedRecommendations([]);
      return;
    }
    const recommendations = await plantingAdvisoryService.getSavedRecommendations();
    setSavedRecommendations(recommendations);
  };

  const handleDelete = async (recommendationId: string) => {
    if (!confirm(language === 'hi' 
      ? 'क्या आप वाकई इस सिफारिश को हटाना चाहते हैं?' 
      : 'Are you sure you want to delete this recommendation?')) {
      return;
    }
    try {
      await plantingAdvisoryService.deleteSavedRecommendation(recommendationId);
      loadSavedRecommendations();
      showToast('success', 
        language === 'hi' ? 'हटाया गया' : 'Deleted', 
        language === 'hi' ? 'सिफारिश सफलतापूर्वक हटाई गई' : 'Recommendation deleted successfully');
      if (selectedRecommendation?.recommendationId === recommendationId) {
        setSelectedRecommendation(null);
      }
    } catch (err) {
      console.error('Failed to delete recommendation:', err);
      showToast('error',
        language === 'hi' ? 'त्रुटि' : 'Error',
        language === 'hi' ? 'सिफारिश हटाने में विफल' : 'Failed to delete recommendation');
    }
  };

  const formatDate = (dateString: string) => {
    const date = new Date(dateString);
    return date.toLocaleDateString(language === 'hi' ? 'hi-IN' : 'en-IN', {
      year: 'numeric',
      month: 'short',
      day: 'numeric'
    });
  };

  if (savedRecommendations.length === 0) {
    return (
      <div className="text-center py-12">
        <div className="text-6xl mb-4">📋</div>
        <h3 className="text-lg font-medium text-gray-900 mb-2">
          {language === 'hi' ? 'कोई सहेजी गई सिफारिशें नहीं' : 'No Saved Recommendations'}
        </h3>
        <p className="text-gray-600">
          {language === 'hi' 
            ? 'आपकी सहेजी गई रोपण सिफारिशें यहां दिखाई देंगी' 
            : 'Your saved planting recommendations will appear here'}
        </p>
      </div>
    );
  }

  if (selectedRecommendation) {
    return (
      <div>
        <button
          onClick={() => setSelectedRecommendation(null)}
          className="mb-4 text-green-600 hover:text-green-700 font-medium flex items-center gap-2"
        >
          ← {language === 'hi' ? 'वापस जाएं' : 'Back to List'}
        </button>
        
        <div className="bg-gray-50 rounded-lg p-4 mb-4">
          <div className="flex justify-between items-start">
            <div>
              <h3 className="text-lg font-semibold text-gray-900">
                {selectedRecommendation.cropType}
              </h3>
              <p className="text-sm text-gray-600">
                📍 {selectedRecommendation.location}
              </p>
              <p className="text-xs text-gray-500 mt-1">
                {language === 'hi' ? 'सहेजा गया: ' : 'Saved: '}
                {formatDate(selectedRecommendation.savedAt)}
              </p>
            </div>
            <button
              onClick={() => handleDelete(selectedRecommendation.recommendationId)}
              className="text-red-600 hover:text-red-700 text-sm font-medium"
            >
              {language === 'hi' ? 'हटाएं' : 'Delete'}
            </button>
          </div>
        </div>

        <RecommendationDisplay
          plantingWindows={selectedRecommendation.plantingWindows}
          seedRecommendations={selectedRecommendation.seedRecommendations}
          weatherFetchedAt={selectedRecommendation.weatherFetchedAt}
          soilDataDate={selectedRecommendation.soilDataDate}
        />
      </div>
    );
  }

  return (
    <div>
      <ToastContainer notifications={toasts} onDismiss={dismissToast} position="top-right" />
      <h3 className="text-xl font-semibold text-gray-900 mb-4">
        {language === 'hi' ? 'सहेजी गई सिफारिशें' : 'Saved Recommendations'}
      </h3>
      <div className="space-y-3">
        {savedRecommendations.map((recommendation) => (
          <div
            key={recommendation.recommendationId}
            className="bg-white border border-gray-200 rounded-lg p-4 hover:shadow-md transition-shadow"
          >
            <div className="flex justify-between items-start">
              <div className="flex-1">
                <h4 className="text-lg font-semibold text-gray-900 mb-1">
                  {recommendation.cropType}
                </h4>
                <p className="text-sm text-gray-600 mb-2">
                  📍 {recommendation.location}
                </p>
                <div className="flex gap-4 text-xs text-gray-500">
                  <span>
                    {language === 'hi' ? 'खिड़कियां: ' : 'Windows: '}
                    {recommendation.plantingWindows.length}
                  </span>
                  <span>
                    {language === 'hi' ? 'किस्में: ' : 'Varieties: '}
                    {recommendation.seedRecommendations.length}
                  </span>
                  <span>
                    {language === 'hi' ? 'सहेजा गया: ' : 'Saved: '}
                    {formatDate(recommendation.savedAt)}
                  </span>
                </div>
              </div>
              <div className="flex gap-2">
                <button
                  onClick={() => setSelectedRecommendation(recommendation)}
                  className="px-4 py-2 bg-green-600 text-white text-sm rounded-lg hover:bg-green-700 transition-colors"
                >
                  {language === 'hi' ? 'देखें' : 'View'}
                </button>
                <button
                  onClick={() => handleDelete(recommendation.recommendationId)}
                  className="px-4 py-2 bg-red-600 text-white text-sm rounded-lg hover:bg-red-700 transition-colors"
                >
                  {language === 'hi' ? 'हटाएं' : 'Delete'}
                </button>
              </div>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
};
