import React, { useState } from 'react';
import { useLanguage } from '@/contexts/LanguageContext';
import type { SoilHealthData, ValidationError } from '@/types';

interface SoilDataFormProps {
  initialData?: Partial<SoilHealthData>;
  validationErrors?: ValidationError[];
  onSubmit: (data: SoilHealthData) => void;
  onCancel?: () => void;
  isLoading?: boolean;
}

export const SoilDataForm: React.FC<SoilDataFormProps> = ({
  initialData,
  validationErrors = [],
  onSubmit,
  onCancel,
  isLoading = false
}) => {
  const { t } = useLanguage();
  
  const [formData, setFormData] = useState<Partial<SoilHealthData>>({
    pH: initialData?.pH || 0,
    organicCarbon: initialData?.organicCarbon || 0,
    nitrogen: initialData?.nitrogen || 0,
    phosphorus: initialData?.phosphorus || 0,
    potassium: initialData?.potassium || 0,
    sulfur: initialData?.sulfur || 0,
    zinc: initialData?.zinc || 0,
    boron: initialData?.boron || 0,
    iron: initialData?.iron || 0,
    manganese: initialData?.manganese || 0,
    copper: initialData?.copper || 0,
    soilTexture: initialData?.soilTexture || '',
    ...initialData
  });

  const getFieldError = (fieldName: string): string | undefined => {
    const error = validationErrors.find(e => e.field === fieldName);
    return error?.message;
  };

  const hasFieldError = (fieldName: string): boolean => {
    return validationErrors.some(e => e.field === fieldName);
  };

  const handleChange = (field: keyof SoilHealthData, value: any) => {
    setFormData(prev => ({ ...prev, [field]: value }));
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    
    // Basic validation
    if (!formData.farmerId || !formData.sampleId || !formData.collectionDate) {
      return;
    }

    onSubmit(formData as SoilHealthData);
  };

  const inputClassName = (fieldName: string) => `
    mt-1 block w-full rounded-md shadow-sm
    ${hasFieldError(fieldName)
      ? 'border-red-300 focus:border-red-500 focus:ring-red-500'
      : 'border-gray-300 focus:border-green-500 focus:ring-green-500'
    }
    disabled:bg-gray-100 disabled:cursor-not-allowed
  `;

  return (
    <form onSubmit={handleSubmit} className="space-y-6">
      {/* Basic Information */}
      <div className="bg-white dark:bg-gray-800 rounded-lg shadow p-6">
        <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100 mb-4">
          {t('soilAnalysis.basicInfo', 'Basic Information')}
        </h3>
        
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 dark:text-gray-300">
              {t('soilAnalysis.sampleId', 'Sample ID')}
            </label>
            <input
              type="text"
              value={formData.sampleId || ''}
              onChange={(e) => handleChange('sampleId', e.target.value)}
              className={inputClassName('sampleId')}
              disabled={isLoading}
            />
            {hasFieldError('sampleId') && (
              <p className="mt-1 text-sm text-red-600">{getFieldError('sampleId')}</p>
            )}
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 dark:text-gray-300">
              {t('soilAnalysis.collectionDate', 'Collection Date')}
            </label>
            <input
              type="date"
              value={formData.collectionDate || ''}
              onChange={(e) => handleChange('collectionDate', e.target.value)}
              className={inputClassName('collectionDate')}
              disabled={isLoading}
            />
            {hasFieldError('collectionDate') && (
              <p className="mt-1 text-sm text-red-600">{getFieldError('collectionDate')}</p>
            )}
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 dark:text-gray-300">
              {t('soilAnalysis.soilTexture', 'Soil Texture')}
            </label>
            <select
              value={formData.soilTexture || ''}
              onChange={(e) => handleChange('soilTexture', e.target.value)}
              className={inputClassName('soilTexture')}
              disabled={isLoading}
            >
              <option value="">{t('common.select', 'Select...')}</option>
              <option value="Sandy">{t('soilAnalysis.sandy', 'Sandy')}</option>
              <option value="Loamy">{t('soilAnalysis.loamy', 'Loamy')}</option>
              <option value="Clay">{t('soilAnalysis.clay', 'Clay')}</option>
              <option value="Silt">{t('soilAnalysis.silt', 'Silt')}</option>
            </select>
            {hasFieldError('soilTexture') && (
              <p className="mt-1 text-sm text-red-600">{getFieldError('soilTexture')}</p>
            )}
          </div>
        </div>
      </div>

      {/* Primary Nutrients */}
      <div className="bg-white dark:bg-gray-800 rounded-lg shadow p-6">
        <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100 mb-4">
          {t('soilAnalysis.primaryNutrients', 'Primary Nutrients')}
        </h3>
        
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 dark:text-gray-300">
              {t('soilAnalysis.ph', 'pH')}
            </label>
            <input
              type="number"
              step="0.1"
              min="0"
              max="14"
              value={formData.pH || ''}
              onChange={(e) => handleChange('pH', parseFloat(e.target.value))}
              className={inputClassName('pH')}
              disabled={isLoading}
            />
            {hasFieldError('pH') && (
              <p className="mt-1 text-sm text-red-600">{getFieldError('pH')}</p>
            )}
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 dark:text-gray-300">
              {t('soilAnalysis.organicCarbon', 'Organic Carbon (%)')}
            </label>
            <input
              type="number"
              step="0.01"
              min="0"
              value={formData.organicCarbon || ''}
              onChange={(e) => handleChange('organicCarbon', parseFloat(e.target.value))}
              className={inputClassName('organicCarbon')}
              disabled={isLoading}
            />
            {hasFieldError('organicCarbon') && (
              <p className="mt-1 text-sm text-red-600">{getFieldError('organicCarbon')}</p>
            )}
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 dark:text-gray-300">
              {t('soilAnalysis.nitrogen', 'Nitrogen (kg/ha)')}
            </label>
            <input
              type="number"
              step="0.1"
              min="0"
              value={formData.nitrogen || ''}
              onChange={(e) => handleChange('nitrogen', parseFloat(e.target.value))}
              className={inputClassName('nitrogen')}
              disabled={isLoading}
            />
            {hasFieldError('nitrogen') && (
              <p className="mt-1 text-sm text-red-600">{getFieldError('nitrogen')}</p>
            )}
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 dark:text-gray-300">
              {t('soilAnalysis.phosphorus', 'Phosphorus (kg/ha)')}
            </label>
            <input
              type="number"
              step="0.1"
              min="0"
              value={formData.phosphorus || ''}
              onChange={(e) => handleChange('phosphorus', parseFloat(e.target.value))}
              className={inputClassName('phosphorus')}
              disabled={isLoading}
            />
            {hasFieldError('phosphorus') && (
              <p className="mt-1 text-sm text-red-600">{getFieldError('phosphorus')}</p>
            )}
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 dark:text-gray-300">
              {t('soilAnalysis.potassium', 'Potassium (kg/ha)')}
            </label>
            <input
              type="number"
              step="0.1"
              min="0"
              value={formData.potassium || ''}
              onChange={(e) => handleChange('potassium', parseFloat(e.target.value))}
              className={inputClassName('potassium')}
              disabled={isLoading}
            />
            {hasFieldError('potassium') && (
              <p className="mt-1 text-sm text-red-600">{getFieldError('potassium')}</p>
            )}
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 dark:text-gray-300">
              {t('soilAnalysis.sulfur', 'Sulfur (ppm)')}
            </label>
            <input
              type="number"
              step="0.1"
              min="0"
              value={formData.sulfur || ''}
              onChange={(e) => handleChange('sulfur', parseFloat(e.target.value))}
              className={inputClassName('sulfur')}
              disabled={isLoading}
            />
            {hasFieldError('sulfur') && (
              <p className="mt-1 text-sm text-red-600">{getFieldError('sulfur')}</p>
            )}
          </div>
        </div>
      </div>

      {/* Micronutrients */}
      <div className="bg-white dark:bg-gray-800 rounded-lg shadow p-6">
        <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100 mb-4">
          {t('soilAnalysis.micronutrients', 'Micronutrients')}
        </h3>
        
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 dark:text-gray-300">
              {t('soilAnalysis.zinc', 'Zinc (ppm)')}
            </label>
            <input
              type="number"
              step="0.01"
              min="0"
              value={formData.zinc || ''}
              onChange={(e) => handleChange('zinc', parseFloat(e.target.value))}
              className={inputClassName('zinc')}
              disabled={isLoading}
            />
            {hasFieldError('zinc') && (
              <p className="mt-1 text-sm text-red-600">{getFieldError('zinc')}</p>
            )}
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 dark:text-gray-300">
              {t('soilAnalysis.boron', 'Boron (ppm)')}
            </label>
            <input
              type="number"
              step="0.01"
              min="0"
              value={formData.boron || ''}
              onChange={(e) => handleChange('boron', parseFloat(e.target.value))}
              className={inputClassName('boron')}
              disabled={isLoading}
            />
            {hasFieldError('boron') && (
              <p className="mt-1 text-sm text-red-600">{getFieldError('boron')}</p>
            )}
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 dark:text-gray-300">
              {t('soilAnalysis.iron', 'Iron (ppm)')}
            </label>
            <input
              type="number"
              step="0.01"
              min="0"
              value={formData.iron || ''}
              onChange={(e) => handleChange('iron', parseFloat(e.target.value))}
              className={inputClassName('iron')}
              disabled={isLoading}
            />
            {hasFieldError('iron') && (
              <p className="mt-1 text-sm text-red-600">{getFieldError('iron')}</p>
            )}
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 dark:text-gray-300">
              {t('soilAnalysis.manganese', 'Manganese (ppm)')}
            </label>
            <input
              type="number"
              step="0.01"
              min="0"
              value={formData.manganese || ''}
              onChange={(e) => handleChange('manganese', parseFloat(e.target.value))}
              className={inputClassName('manganese')}
              disabled={isLoading}
            />
            {hasFieldError('manganese') && (
              <p className="mt-1 text-sm text-red-600">{getFieldError('manganese')}</p>
            )}
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 dark:text-gray-300">
              {t('soilAnalysis.copper', 'Copper (ppm)')}
            </label>
            <input
              type="number"
              step="0.01"
              min="0"
              value={formData.copper || ''}
              onChange={(e) => handleChange('copper', parseFloat(e.target.value))}
              className={inputClassName('copper')}
              disabled={isLoading}
            />
            {hasFieldError('copper') && (
              <p className="mt-1 text-sm text-red-600">{getFieldError('copper')}</p>
            )}
          </div>
        </div>
      </div>

      {/* Form Actions */}
      <div className="flex justify-end space-x-4">
        {onCancel && (
          <button
            type="button"
            onClick={onCancel}
            disabled={isLoading}
            className="px-6 py-2 border border-gray-300 rounded-md text-gray-700 hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed"
          >
            {t('common.cancel', 'Cancel')}
          </button>
        )}
        <button
          type="submit"
          disabled={isLoading}
          className="px-6 py-2 bg-green-600 text-white rounded-md hover:bg-green-700 disabled:opacity-50 disabled:cursor-not-allowed"
        >
          {isLoading ? t('common.saving', 'Saving...') : t('common.save', 'Save')}
        </button>
      </div>
    </form>
  );
};
