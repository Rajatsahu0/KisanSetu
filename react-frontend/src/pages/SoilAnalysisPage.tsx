import React, { useState, useEffect } from 'react';
import { useLanguage } from '@/contexts/LanguageContext';
import { useAuth } from '@/contexts/AuthContext';
import { FileUploadZone } from '@/components/upload/FileUploadZone';
import { UploadProgress } from '@/components/upload/UploadProgress';
import { SoilDataForm } from '@/components/soil/SoilDataForm';
import { SoilDataDisplay } from '@/components/soil/SoilDataDisplay';
import { RegenerativePlanDisplay } from '@/components/soil/RegenerativePlanDisplay';
import { ConfidenceScoreDisplay } from '@/components/soil/ConfidenceScoreDisplay';
import { SavedPlansView } from '@/components/soil/SavedPlansView';
import { soilAnalysisService, type RegenerativePlan, type FarmProfile } from '@/services/soilAnalysisService';
import type { SoilHealthCardResponse, SoilHealthData } from '@/types';
import { LoadingSpinner } from '@/components/common/LoadingSpinner';
import { useNotifications } from '@/hooks/useNotifications';
import { profileService } from '@/services/profileService';
import { validateSoilData, normalizeSoilData, calculateConfidenceScore } from '@/utils/soilDataValidation';

type ViewMode = 'upload' | 'review' | 'plan' | 'saved';

export const SoilAnalysisPage: React.FC = () => {
  const { t, currentLanguage } = useLanguage();
  const { user } = useAuth();
  const { showNotification } = useNotifications();
  
  const [viewMode, setViewMode] = useState<ViewMode>('upload');
  const [uploadProgress, setUploadProgress] = useState<number>(0);
  const [isUploading, setIsUploading] = useState(false);
  const [isProcessing, setIsProcessing] = useState(false);
  const [uploadError, setUploadError] = useState<string | null>(null);
  
  const [soilCardResponse, setSoilCardResponse] = useState<SoilHealthCardResponse | null>(null);
  const [regenerativePlan, setRegenerativePlan] = useState<RegenerativePlan | null>(null);
  const [planFormData, setPlanFormData] = useState({
    farmSize: 5,
    primaryCrops: '',
    irrigationType: '',
  });
  const [isGeneratingPlan, setIsGeneratingPlan] = useState(false);
  const [farmerProfile, setFarmerProfile] = useState<{ state: string; city: string }>({ state: '', city: '' });

  // Load profile on mount
  useEffect(() => {
    profileService.getProfile()
      .then(p => setFarmerProfile({ state: p.state || '', city: p.city || '' }))
      .catch(() => {});
  }, []);

  const handleFileUpload = async (files: File[]) => {
    if (files.length === 0) return;

    const file = files[0];
    setIsUploading(true);
    setUploadError(null);
    setUploadProgress(0);

    try {
      const response = await soilAnalysisService.uploadSoilHealthCard(
        file,
        (progress) => setUploadProgress(progress)
      );

      setSoilCardResponse(response);
      
      if (response.isValid) {
        setViewMode('review');
      } else {
        // Show form for manual correction
        setViewMode('review');
      }

    } catch (error) {
      setUploadError(error instanceof Error ? error.message : 'Upload failed');
    } finally {
      setIsUploading(false);
      setUploadProgress(0);
    }
  };

  const handleManualDataSubmit = async (data: SoilHealthData) => {
    setIsProcessing(true);
    try {
      // Normalize the data
      const normalizedData = normalizeSoilData(data) as SoilHealthData;
      
      // Validate the data
      const validationErrors = validateSoilData(normalizedData);
      
      if (validationErrors.length > 0) {
        // Update with validation errors
        setSoilCardResponse({
          soilData: normalizedData,
          isValid: false,
          validationErrors,
          message: t('soilAnalysis.validationFailed', 'Please correct the validation errors'),
          requiresManualVerification: true
        });
        return;
      }

      // Update the soil card response with corrected data
      setSoilCardResponse({
        soilData: normalizedData,
        isValid: true,
        validationErrors: [],
        message: t('soilAnalysis.dataUpdated', 'Soil data updated successfully'),
        requiresManualVerification: false
      });

    } catch (error) {
      console.error('Failed to save soil data:', error);
    } finally {
      setIsProcessing(false);
    }
  };

  const handleGeneratePlan = async () => {
    if (!soilCardResponse?.soilData || !user) return;

    setIsGeneratingPlan(true);
    try {
      const farmProfile: FarmProfile = {
        farmerId: user?.id || '',
        farmName: user.name || 'My Farm',
        location: {
          state: farmerProfile.state,
          district: farmerProfile.city,
          block: '',
          village: ''
        },
        farmSize: planFormData.farmSize,
        primaryCrops: planFormData.primaryCrops ? planFormData.primaryCrops.split(',').map(c => c.trim()).filter(Boolean) : [],
        soilType: soilCardResponse.soilData.soilTexture || 'Unknown'
      };

      const plan = await soilAnalysisService.generateRegenerativePlan({
        soilData: soilCardResponse.soilData,
        farmProfile,
        language: currentLanguage?.code || 'en'
      });

      setRegenerativePlan(plan);
      setViewMode('plan');
    } catch (error) {
      console.error('Failed to generate plan:', error);
      showNotification({ type: 'error', title: 'Error', message: error instanceof Error ? error.message : 'Failed to generate plan' });
    } finally {
      setIsGeneratingPlan(false);
    }
  };

  const handleSavePlan = async () => {
    if (!regenerativePlan) return;
    
    try {
      await soilAnalysisService.savePlan(regenerativePlan);
      showNotification({ type: 'success', title: 'Saved', message: 'Plan saved successfully!' });
    } catch (error) {
      showNotification({ type: 'warning', title: 'Saved Locally', message: 'Failed to save to server. Saved locally as fallback.' });
    }
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="bg-white dark:bg-gray-800 rounded-lg shadow-md p-6">
        <h1 className="text-xl sm:text-3xl font-bold text-gray-900 dark:text-gray-100 mb-2">
          {t('nav.soilAnalysis', 'Soil Analysis')}
        </h1>
        <p className="text-gray-600 dark:text-gray-300">
          {t('soilAnalysis.description', 'Upload your Soil Health Card to get digital analysis and regenerative farming recommendations')}
        </p>
      </div>

      {/* Navigation Tabs */}
      <div className="bg-white dark:bg-gray-800 rounded-lg shadow-md">
        <nav className="flex overflow-x-auto border-b border-gray-200 dark:border-gray-700 -mx-px">
          <button
            onClick={() => setViewMode('upload')}
            className={`px-4 sm:px-6 py-4 text-sm font-medium border-b-2 transition-colors whitespace-nowrap ${
              viewMode === 'upload'
                ? 'border-green-500 text-green-600 dark:text-green-400'
                : 'border-transparent text-gray-500 hover:text-gray-700 dark:text-gray-400'
            }`}
          >
            {t('soilAnalysis.uploadCard', 'Upload Card')}
          </button>
          <button
            onClick={() => setViewMode('review')}
            disabled={!soilCardResponse}
            className={`px-4 sm:px-6 py-4 text-sm font-medium border-b-2 transition-colors whitespace-nowrap ${
              viewMode === 'review'
                ? 'border-green-500 text-green-600 dark:text-green-400'
                : 'border-transparent text-gray-500 hover:text-gray-700 dark:text-gray-400'
            } disabled:opacity-50 disabled:cursor-not-allowed`}
          >
            {t('soilAnalysis.reviewData', 'Review Data')}
          </button>
          <button
            onClick={() => setViewMode('plan')}
            disabled={!regenerativePlan}
            className={`px-4 sm:px-6 py-4 text-sm font-medium border-b-2 transition-colors whitespace-nowrap ${
              viewMode === 'plan'
                ? 'border-green-500 text-green-600 dark:text-green-400'
                : 'border-transparent text-gray-500 hover:text-gray-700 dark:text-gray-400'
            } disabled:opacity-50 disabled:cursor-not-allowed`}
          >
            {t('soilAnalysis.regenerativePlan', 'Regenerative Plan')}
          </button>
          <button
            onClick={() => setViewMode('saved')}
            className={`px-4 sm:px-6 py-4 text-sm font-medium border-b-2 transition-colors whitespace-nowrap ${
              viewMode === 'saved'
                ? 'border-green-500 text-green-600 dark:text-green-400'
                : 'border-transparent text-gray-500 hover:text-gray-700 dark:text-gray-400'
            }`}
          >
            {t('soilAnalysis.savedPlans', 'Saved Plans')}
          </button>
        </nav>
      </div>

      {/* Content Area */}
      <div>
        {/* Upload View */}
        {viewMode === 'upload' && (
          <div className="space-y-6">
            <div className="bg-white dark:bg-gray-800 rounded-lg shadow-md p-6">
              <h2 className="text-xl font-semibold text-gray-900 dark:text-gray-100 mb-4">
                {t('soilAnalysis.uploadSoilHealthCard', 'Upload Soil Health Card')}
              </h2>
              
              {!isUploading && !uploadError && (
                <FileUploadZone
                  accept={['image/jpeg', 'image/png', 'application/pdf', 'text/plain']}
                  maxSize={10 * 1024 * 1024}
                  maxFiles={1}
                  onUpload={handleFileUpload}
                  onProgress={setUploadProgress}
                  onError={setUploadError}
                />
              )}

              {isUploading && (
                <UploadProgress
                  progress={uploadProgress}
                  status="uploading"
                />
              )}

              {uploadError && (
                <div className="bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-lg p-4">
                  <p className="text-red-800 dark:text-red-200">{uploadError}</p>
                  <button
                    onClick={() => {
                      setUploadError(null);
                      setUploadProgress(0);
                    }}
                    className="mt-2 text-sm text-red-600 dark:text-red-400 hover:underline"
                  >
                    {t('common.tryAgain', 'Try Again')}
                  </button>
                </div>
              )}
            </div>

            <div className="bg-blue-50 dark:bg-blue-900/20 rounded-lg p-6">
              <h3 className="text-lg font-semibold text-blue-900 dark:text-blue-100 mb-2">
                {t('soilAnalysis.tips', 'Tips for Best Results')}
              </h3>
              <ul className="space-y-2 text-sm text-blue-800 dark:text-blue-200">
                <li className="flex items-start">
                  <svg className="w-5 h-5 mr-2 mt-0.5 flex-shrink-0" fill="currentColor" viewBox="0 0 20 20">
                    <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clipRule="evenodd" />
                  </svg>
                  {t('soilAnalysis.tip1', 'Take a clear photo in good lighting')}
                </li>
                <li className="flex items-start">
                  <svg className="w-5 h-5 mr-2 mt-0.5 flex-shrink-0" fill="currentColor" viewBox="0 0 20 20">
                    <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clipRule="evenodd" />
                  </svg>
                  {t('soilAnalysis.tip2', 'Ensure all text is visible and readable')}
                </li>
                <li className="flex items-start">
                  <svg className="w-5 h-5 mr-2 mt-0.5 flex-shrink-0" fill="currentColor" viewBox="0 0 20 20">
                    <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clipRule="evenodd" />
                  </svg>
                  {t('soilAnalysis.tip3', 'Supported formats: JPEG, PNG, PDF (max 10MB)')}
                </li>
              </ul>
            </div>
          </div>
        )}

        {/* Review View */}
        {viewMode === 'review' && soilCardResponse && (
          <div className="space-y-6">
            {/* Confidence Score Display */}
            {soilCardResponse.soilData && (
              <ConfidenceScoreDisplay
                score={calculateConfidenceScore(
                  soilCardResponse.soilData,
                  soilCardResponse.validationErrors
                )}
                showDetails={true}
              />
            )}

            {soilCardResponse.requiresManualVerification && (
              <div className="bg-yellow-50 dark:bg-yellow-900/20 border border-yellow-200 dark:border-yellow-800 rounded-lg p-4">
                <p className="text-yellow-800 dark:text-yellow-200 mb-2">
                  {soilCardResponse.message}
                </p>
                <p className="text-sm text-yellow-700 dark:text-yellow-300">
                  {t('soilAnalysis.pleaseReview', 'Please review and correct the data below')}
                </p>
              </div>
            )}

            {soilCardResponse.soilData && !soilCardResponse.requiresManualVerification && (
              <>
                <SoilDataDisplay soilData={soilCardResponse.soilData} />
                
                {/* Farm Details — always visible after soil extraction */}
                <div className="bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-800 rounded-lg p-5">
                  <h3 className="text-sm font-bold text-green-900 dark:text-green-100 mb-1">
                    {t('soilAnalysis.farmDetails', 'Farm Details')}
                  </h3>
                  <p className="text-xs text-green-700 dark:text-green-400 mb-4">
                    {t('soilAnalysis.farmDetailsDescription', 'Fill in your farm details for better recommendations. Leave blank if not applicable.')}
                  </p>
                  <div className="grid grid-cols-1 md:grid-cols-3 gap-4 mb-4">
                    <div>
                      <label className="block text-xs font-medium text-gray-700 dark:text-gray-300 mb-1">{t('soilAnalysis.farmSize', 'Farm Size (acres)')}</label>
                      <input type="number" value={planFormData.farmSize}
                        onChange={e => setPlanFormData(p => ({ ...p, farmSize: Number(e.target.value) || 1 }))}
                        className="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700 text-gray-900 dark:text-white text-sm" />
                    </div>
                    <div>
                      <label className="block text-xs font-medium text-gray-700 dark:text-gray-300 mb-1">
                        {t('soilAnalysis.currentCrops', 'Current Crops')} <span className="text-gray-400">({t('soilAnalysis.optional', 'optional')})</span>
                      </label>
                      <input type="text" value={planFormData.primaryCrops}
                        onChange={e => setPlanFormData(p => ({ ...p, primaryCrops: e.target.value }))}
                        placeholder={t('soilAnalysis.cropsPlaceholder', 'e.g. Wheat, Rice, Cotton')}
                        className="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700 text-gray-900 dark:text-white text-sm" />
                      <p className="text-[10px] text-gray-400 mt-1">{t('soilAnalysis.currentCropsHint', 'Leave blank if no crops currently planted')}</p>
                    </div>
                    <div>
                      <label className="block text-xs font-medium text-gray-700 dark:text-gray-300 mb-1">
                        {t('soilAnalysis.irrigationLabel', 'Irrigation')} <span className="text-gray-400">({t('soilAnalysis.optional', 'optional')})</span>
                      </label>
                      <select value={planFormData.irrigationType}
                        onChange={e => setPlanFormData(p => ({ ...p, irrigationType: e.target.value }))}
                        className="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700 text-gray-900 dark:text-white text-sm">
                        <option value="">{t('soilAnalysis.notSureNone', '-- Not sure / None --')}</option>
                        <option value="Rainfed">{t('soilAnalysis.rainfed', 'Rainfed')}</option>
                        <option value="Drip">{t('soilAnalysis.dripIrrigation', 'Drip Irrigation')}</option>
                        <option value="Sprinkler">{t('soilAnalysis.sprinkler', 'Sprinkler')}</option>
                        <option value="Flood">{t('soilAnalysis.floodIrrigation', 'Flood Irrigation')}</option>
                        <option value="Canal">{t('soilAnalysis.canal', 'Canal')}</option>
                        <option value="Borewell">{t('soilAnalysis.borewell', 'Borewell / Tubewell')}</option>
                      </select>
                    </div>
                  </div>

                  <div className="flex items-center gap-3">
                    <button onClick={handleGeneratePlan} disabled={isGeneratingPlan}
                      className="px-6 py-2.5 bg-green-600 text-white rounded-lg hover:bg-green-700 disabled:opacity-50 disabled:cursor-not-allowed text-sm font-medium flex items-center gap-2">
                      {isGeneratingPlan ? (
                        <><LoadingSpinner size="sm" /> {t('soilAnalysis.generatingPlan', 'Generating Plan...')}</>
                      ) : (
                        `🌱 ${t('soilAnalysis.generatePlan', 'Generate Regenerative Plan')}`
                      )}
                    </button>
                    <button onClick={() => setViewMode('upload')}
                      className="px-5 py-2.5 border border-gray-300 dark:border-gray-600 text-gray-700 dark:text-gray-300 rounded-lg hover:bg-gray-50 dark:hover:bg-gray-700 text-sm">
                      {t('soilAnalysis.uploadAnotherCard', 'Upload Another Card')}
                    </button>
                  </div>
                </div>
              </>
            )}

            {soilCardResponse.requiresManualVerification && soilCardResponse.soilData && (
              <SoilDataForm
                initialData={soilCardResponse.soilData}
                validationErrors={soilCardResponse.validationErrors}
                onSubmit={handleManualDataSubmit}
                onCancel={() => setViewMode('upload')}
                isLoading={isProcessing}
              />
            )}
          </div>
        )}

        {/* Plan View */}
        {viewMode === 'plan' && regenerativePlan && (
          <RegenerativePlanDisplay
            plan={regenerativePlan}
            onSave={handleSavePlan}
          />
        )}

        {/* Saved Plans View */}
        {viewMode === 'saved' && (
          <SavedPlansView
            onSelectPlan={(plan) => {
              setRegenerativePlan(plan);
              setViewMode('plan');
            }}
          />
        )}
      </div>
    </div>
  );
};
