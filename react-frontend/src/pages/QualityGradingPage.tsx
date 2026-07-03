import React, { useState, useCallback } from 'react';
import { useLanguage } from '@/contexts/LanguageContext';
import { FileUploadZone } from '@/components/upload/FileUploadZone';
import { ImageCapture } from '@/components/upload/ImageCapture';
import { UploadProgress } from '@/components/upload/UploadProgress';
import { GradingResultDisplay } from '@/components/grading/GradingResultDisplay';
import { BatchGradingResults } from '@/components/grading/BatchGradingResults';
import { GradingHistory } from '@/components/grading/GradingHistory';
import { ToastContainer } from '@/components/notifications/ToastContainer';
import type { ToastNotification } from '@/components/notifications/Toast';
import { qualityGradingService } from '@/services/qualityGradingService';
import type { GradingResult, BatchGradingResult } from '@/types';

type ViewMode = 'upload' | 'camera' | 'result' | 'batch-result' | 'history';
type UploadMode = 'single' | 'batch';

export const QualityGradingPage: React.FC = () => {
  const { t } = useLanguage();

  const [viewMode, setViewMode] = useState<ViewMode>('upload');
  const [uploadMode, setUploadMode] = useState<UploadMode>('single');
  const [uploadProgress, setUploadProgress] = useState<number>(0);
  const [isUploading, setIsUploading] = useState(false);
  const [uploadError, setUploadError] = useState<string | null>(null);

  const [produceType, setProduceType] = useState('');
  const [location, setLocation] = useState('');
  const [selectedFiles, setSelectedFiles] = useState<File[]>([]);
  const [imagePreviewUrls, setImagePreviewUrls] = useState<string[]>([]);

  const [gradingResult, setGradingResult] = useState<GradingResult | null>(null);
  const [batchGradingResult, setBatchGradingResult] = useState<BatchGradingResult | null>(null);
  const [toasts, setToasts] = useState<ToastNotification[]>([]);

  const showToast = useCallback((type: ToastNotification['type'], title: string, message: string) => {
    const id = Date.now().toString();
    setToasts(prev => [...prev, { id, type, title, message, duration: 4000 }]);
  }, []);

  const dismissToast = useCallback((id: string) => {
    setToasts(prev => prev.filter(t => t.id !== id));
  }, []);

  const handleFileSelect = async (files: File[]) => {
    setSelectedFiles(files);
    
    // Create preview URLs
    const urls = files.map(file => URL.createObjectURL(file));
    setImagePreviewUrls(urls);

    // Auto-detect upload mode based on number of files
    if (files.length > 1) {
      setUploadMode('batch');
    } else {
      setUploadMode('single');
    }
  };

  const handleCameraCapture = (imageBlob: Blob) => {
    const file = new File([imageBlob], `capture-${Date.now()}.jpg`, { type: 'image/jpeg' });
    setSelectedFiles([file]);
    setImagePreviewUrls([URL.createObjectURL(imageBlob)]);
    setUploadMode('single');
    setViewMode('upload');
  };

  const handleGradeSubmit = async () => {
    if (selectedFiles.length === 0 || !produceType || !location) {
      setUploadError(t('grading.fillAllFields', 'Please fill all fields and select images'));
      return;
    }

    setIsUploading(true);
    setUploadError(null);
    setUploadProgress(0);

    try {
      if (uploadMode === 'single') {
        const result = await qualityGradingService.gradeProduct(
          selectedFiles[0],
          produceType,
          location,
          (progress) => setUploadProgress(progress)
        );

        setGradingResult(result);
        setViewMode('result');
      } else {
        const result = await qualityGradingService.gradeBatch(
          selectedFiles,
          produceType,
          location,
          (progress) => setUploadProgress(progress)
        );

        setBatchGradingResult(result);
        setViewMode('batch-result');
      }
    } catch (error) {
      setUploadError(error instanceof Error ? error.message : 'Grading failed');
    } finally {
      setIsUploading(false);
      setUploadProgress(0);
    }
  };

  const handleSaveResult = () => {
    if (gradingResult && imagePreviewUrls[0]) {
      try {
        qualityGradingService.saveGradingLocally(gradingResult, imagePreviewUrls[0]);
        showToast('success', t('grading.saved', 'Saved'), t('grading.resultSaved', 'Grading result saved successfully!'));
      } catch (error) {
        showToast('error', t('grading.error', 'Error'), t('grading.saveFailed', 'Failed to save result'));
      }
    }
  };

  const handleNewGrading = () => {
    setViewMode('upload');
    setSelectedFiles([]);
    setImagePreviewUrls([]);
    setProduceType('');
    setLocation('');
    setGradingResult(null);
    setBatchGradingResult(null);
    setUploadError(null);
  };

  const produceTypes = [
    'Tomato', 'Potato', 'Onion', 'Cauliflower', 'Cabbage', 'Brinjal',
    'Capsicum', 'Carrot', 'Peas', 'Green Chilli', 'Lady Finger',
    'Wheat', 'Rice', 'Maize', 'Bajra', 'Jowar',
    'Mango', 'Banana', 'Apple', 'Grapes', 'Pomegranate', 'Guava',
    'Papaya', 'Lemon', 'Orange', 'Watermelon',
    'Chilli', 'Turmeric', 'Ginger', 'Coriander',
    'Other'
  ];

  const produceLabel = (type: string) => t(`produce.${type.toLowerCase().replace(/ /g, '')}`, type);

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="bg-white dark:bg-gray-800 rounded-lg shadow-md p-6">
        <h1 className="text-xl sm:text-3xl font-bold text-gray-900 dark:text-gray-100 mb-2">
          {t('nav.qualityGrading', 'Quality Grading')}
        </h1>
        <p className="text-gray-600 dark:text-gray-300">
          {t('grading.description', 'Capture or upload produce images to get quality grades and certified prices')}
        </p>
      </div>

      {/* Navigation Tabs */}
      <div className="bg-white dark:bg-gray-800 rounded-lg shadow-md">
        <nav className="flex overflow-x-auto border-b border-gray-200 dark:border-gray-700 -mx-px">
          <button
            onClick={() => setViewMode('upload')}
            className={`px-4 sm:px-6 py-4 text-sm font-medium border-b-2 transition-colors whitespace-nowrap ${
              viewMode === 'upload' || viewMode === 'camera'
                ? 'border-green-500 text-green-600 dark:text-green-400'
                : 'border-transparent text-gray-500 hover:text-gray-700 dark:text-gray-400'
            }`}
          >
            {t('grading.newGrading', 'New Grading')}
          </button>
          <button
            onClick={() => setViewMode('result')}
            disabled={!gradingResult && !batchGradingResult}
            className={`px-4 sm:px-6 py-4 text-sm font-medium border-b-2 transition-colors whitespace-nowrap ${
              viewMode === 'result' || viewMode === 'batch-result'
                ? 'border-green-500 text-green-600 dark:text-green-400'
                : 'border-transparent text-gray-500 hover:text-gray-700 dark:text-gray-400'
            } disabled:opacity-50 disabled:cursor-not-allowed`}
          >
            {t('grading.results', 'Results')}
          </button>
          <button
            onClick={() => setViewMode('history')}
            className={`px-4 sm:px-6 py-4 text-sm font-medium border-b-2 transition-colors whitespace-nowrap ${
              viewMode === 'history'
                ? 'border-green-500 text-green-600 dark:text-green-400'
                : 'border-transparent text-gray-500 hover:text-gray-700 dark:text-gray-400'
            }`}
          >
            {t('grading.history', 'History')}
          </button>
        </nav>
      </div>

      {/* Content Area */}
      <div>
        {/* Upload/Camera View */}
        {(viewMode === 'upload' || viewMode === 'camera') && (
          <div className="space-y-6">
            {/* Produce Type and Location Form */}
            <div className="bg-white dark:bg-gray-800 rounded-lg shadow-md p-6">
              <h2 className="text-xl font-semibold text-gray-900 dark:text-gray-100 mb-4">
                {t('grading.gradeDetails', 'Grading Details')}
              </h2>
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                    {t('grading.produceType', 'Produce Type')} *
                  </label>
                  <select
                    value={produceType}
                    onChange={(e) => setProduceType(e.target.value)}
                    className="w-full px-4 py-2 border border-gray-300 dark:border-gray-600 rounded-md bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100"
                  >
                    <option value="">{t('grading.selectProduce', 'Select produce type')}</option>
                    {produceTypes.map(type => (
                      <option key={type} value={type}>{produceLabel(type)}</option>
                    ))}
                  </select>
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                    {t('grading.location', 'Location')} *
                  </label>
                  <input
                    type="text"
                    value={location}
                    onChange={(e) => setLocation(e.target.value)}
                    placeholder={t('grading.enterLocation', 'Enter location')}
                    className="w-full px-4 py-2 border border-gray-300 dark:border-gray-600 rounded-md bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100"
                  />
                </div>
              </div>
            </div>

            {/* Camera/Upload Toggle */}
            <div className="bg-white dark:bg-gray-800 rounded-lg shadow-md p-6">
              <div className="flex space-x-4 mb-6">
                <button
                  onClick={() => setViewMode('upload')}
                  className={`flex-1 px-4 py-3 rounded-md font-medium transition-colors ${
                    viewMode === 'upload'
                      ? 'bg-green-600 text-white'
                      : 'bg-gray-100 dark:bg-gray-700 text-gray-700 dark:text-gray-300 hover:bg-gray-200 dark:hover:bg-gray-600'
                  }`}
                >
                  <svg className="w-5 h-5 inline-block mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M7 16a4 4 0 01-.88-7.903A5 5 0 1115.9 6L16 6a5 5 0 011 9.9M15 13l-3-3m0 0l-3 3m3-3v12" />
                  </svg>
                  {t('grading.uploadImages', 'Upload Images')}
                </button>
                <button
                  onClick={() => setViewMode('camera')}
                  className={`flex-1 px-4 py-3 rounded-md font-medium transition-colors ${
                    viewMode === 'camera'
                      ? 'bg-green-600 text-white'
                      : 'bg-gray-100 dark:bg-gray-700 text-gray-700 dark:text-gray-300 hover:bg-gray-200 dark:hover:bg-gray-600'
                  }`}
                >
                  <svg className="w-5 h-5 inline-block mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M3 9a2 2 0 012-2h.93a2 2 0 001.664-.89l.812-1.22A2 2 0 0110.07 4h3.86a2 2 0 011.664.89l.812 1.22A2 2 0 0018.07 7H19a2 2 0 012 2v9a2 2 0 01-2 2H5a2 2 0 01-2-2V9z" />
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 13a3 3 0 11-6 0 3 3 0 016 0z" />
                  </svg>
                  {t('grading.useCamera', 'Use Camera')}
                </button>
              </div>

              {viewMode === 'upload' && !isUploading && (
                <>
                  <FileUploadZone
                    accept={['image/jpeg', 'image/png']}
                    maxSize={10 * 1024 * 1024}
                    maxFiles={10}
                    onUpload={handleFileSelect}
                    onProgress={setUploadProgress}
                    onError={setUploadError}
                  />

                  {/* Image Previews */}
                  {imagePreviewUrls.length > 0 && (
                    <div className="mt-6">
                      <div className="flex items-center justify-between mb-3">
                        <h3 className="text-sm font-medium text-gray-700 dark:text-gray-300">
                          {t('grading.selectedImages', 'Selected Images')} ({imagePreviewUrls.length})
                        </h3>
                        <button
                          onClick={() => {
                            setSelectedFiles([]);
                            setImagePreviewUrls([]);
                          }}
                          className="text-sm text-red-600 dark:text-red-400 hover:underline"
                        >
                          {t('grading.clearAll', 'Clear All')}
                        </button>
                      </div>
                      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
                        {imagePreviewUrls.map((url, index) => (
                          <div key={index} className="relative">
                            <img
                              src={url}
                              alt={`Preview ${index + 1}`}
                              className="w-full h-32 object-cover rounded-lg"
                            />
                            <button
                              onClick={() => {
                                const newFiles = selectedFiles.filter((_, i) => i !== index);
                                const newUrls = imagePreviewUrls.filter((_, i) => i !== index);
                                setSelectedFiles(newFiles);
                                setImagePreviewUrls(newUrls);
                              }}
                              className="absolute top-2 right-2 bg-red-600 text-white rounded-full p-1 hover:bg-red-700"
                            >
                              <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                              </svg>
                            </button>
                          </div>
                        ))}
                      </div>
                    </div>
                  )}
                </>
              )}

              {viewMode === 'camera' && (
                <ImageCapture
                  onCapture={handleCameraCapture}
                  onError={setUploadError}
                  quality={0.9}
                  facingMode="environment"
                />
              )}

              {isUploading && (
                <UploadProgress
                  progress={uploadProgress}
                  status="uploading"
                />
              )}

              {uploadError && (
                <div className="mt-4 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-lg p-4">
                  <p className="text-red-800 dark:text-red-200">{uploadError}</p>
                  <button
                    onClick={() => setUploadError(null)}
                    className="mt-2 text-sm text-red-600 dark:text-red-400 hover:underline"
                  >
                    {t('common.dismiss', 'Dismiss')}
                  </button>
                </div>
              )}

              {/* Submit Button */}
              {selectedFiles.length > 0 && !isUploading && (
                <div className="mt-6">
                  <button
                    onClick={handleGradeSubmit}
                    disabled={!produceType || !location}
                    className="w-full px-6 py-3 bg-green-600 text-white rounded-md hover:bg-green-700 disabled:opacity-50 disabled:cursor-not-allowed font-medium"
                  >
                    {uploadMode === 'batch'
                      ? t('grading.gradeBatch', `Grade ${selectedFiles.length} Images`)
                      : t('grading.gradeImage', 'Grade Image')}
                  </button>
                </div>
              )}
            </div>

            {/* Tips */}
            <div className="bg-blue-50 dark:bg-blue-900/20 rounded-lg p-6">
              <h3 className="text-lg font-semibold text-blue-900 dark:text-blue-100 mb-2">
                {t('grading.tips', 'Tips for Best Results')}
              </h3>
              <ul className="space-y-2 text-sm text-blue-800 dark:text-blue-200">
                <li className="flex items-start">
                  <svg className="w-5 h-5 mr-2 mt-0.5 flex-shrink-0" fill="currentColor" viewBox="0 0 20 20">
                    <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clipRule="evenodd" />
                  </svg>
                  {t('grading.tip1', 'Take clear photos in good natural lighting')}
                </li>
                <li className="flex items-start">
                  <svg className="w-5 h-5 mr-2 mt-0.5 flex-shrink-0" fill="currentColor" viewBox="0 0 20 20">
                    <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clipRule="evenodd" />
                  </svg>
                  {t('grading.tip2', 'Ensure produce fills most of the frame')}
                </li>
                <li className="flex items-start">
                  <svg className="w-5 h-5 mr-2 mt-0.5 flex-shrink-0" fill="currentColor" viewBox="0 0 20 20">
                    <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clipRule="evenodd" />
                  </svg>
                  {t('grading.tip3', 'Upload up to 10 images for batch grading')}
                </li>
                <li className="flex items-start">
                  <svg className="w-5 h-5 mr-2 mt-0.5 flex-shrink-0" fill="currentColor" viewBox="0 0 20 20">
                    <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clipRule="evenodd" />
                  </svg>
                  {t('grading.tip4', 'Avoid shadows and reflections on produce')}
                </li>
              </ul>
            </div>
          </div>
        )}

        {/* Single Result View */}
        {viewMode === 'result' && gradingResult && (
          <div className="space-y-4">
            <button
              onClick={handleNewGrading}
              className="flex items-center text-green-600 dark:text-green-400 hover:underline"
            >
              <svg className="w-5 h-5 mr-1" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 19l-7-7 7-7" />
              </svg>
              {t('grading.newGrading', 'New Grading')}
            </button>
            <GradingResultDisplay
              result={gradingResult}
              imageUrl={imagePreviewUrls[0]}
              onSave={handleSaveResult}
            />
          </div>
        )}

        {/* Batch Result View */}
        {viewMode === 'batch-result' && batchGradingResult && (
          <div className="space-y-4">
            <button
              onClick={handleNewGrading}
              className="flex items-center text-green-600 dark:text-green-400 hover:underline"
            >
              <svg className="w-5 h-5 mr-1" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 19l-7-7 7-7" />
              </svg>
              {t('grading.newGrading', 'New Grading')}
            </button>
            <BatchGradingResults
              batchResult={batchGradingResult}
              imageUrls={imagePreviewUrls}
            />
          </div>
        )}

        {/* History View */}
        {viewMode === 'history' && (
          <GradingHistory />
        )}
      </div>
      <ToastContainer notifications={toasts} onDismiss={dismissToast} position="top-right" />
    </div>
  );
};
