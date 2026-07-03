/**
 * Client-side validation utilities for soil health data
 */

import type { SoilHealthData, ValidationError } from '@/types';

export interface ValidationRules {
  field: keyof SoilHealthData;
  label: string;
  required?: boolean;
  min?: number;
  max?: number;
  type?: 'number' | 'string' | 'date';
}

export const soilDataValidationRules: ValidationRules[] = [
  { field: 'sampleId', label: 'Sample ID', required: true, type: 'string' },
  { field: 'collectionDate', label: 'Collection Date', required: true, type: 'date' },
  { field: 'soilTexture', label: 'Soil Texture', required: true, type: 'string' },
  { field: 'ph', label: 'pH', required: true, type: 'number', min: 0, max: 14 },
  { field: 'organicCarbon', label: 'Organic Carbon', required: true, type: 'number', min: 0, max: 10 },
  { field: 'nitrogen', label: 'Nitrogen', required: true, type: 'number', min: 0, max: 1000 },
  { field: 'phosphorus', label: 'Phosphorus', required: true, type: 'number', min: 0, max: 100 },
  { field: 'potassium', label: 'Potassium', required: true, type: 'number', min: 0, max: 1000 },
  { field: 'sulfur', label: 'Sulfur', required: true, type: 'number', min: 0, max: 100 },
  { field: 'zinc', label: 'Zinc', required: true, type: 'number', min: 0, max: 10 },
  { field: 'boron', label: 'Boron', required: true, type: 'number', min: 0, max: 10 },
  { field: 'iron', label: 'Iron', required: true, type: 'number', min: 0, max: 100 },
  { field: 'manganese', label: 'Manganese', required: true, type: 'number', min: 0, max: 100 },
  { field: 'copper', label: 'Copper', required: true, type: 'number', min: 0, max: 10 }
];

/**
 * Validate soil health data against rules
 */
export function validateSoilData(data: Partial<SoilHealthData>): ValidationError[] {
  const errors: ValidationError[] = [];

  for (const rule of soilDataValidationRules) {
    const value = data[rule.field];

    // Check required fields
    if (rule.required && (value === undefined || value === null || value === '')) {
      errors.push({
        field: rule.field,
        message: `${rule.label} is required`,
        code: 'FIELD_REQUIRED'
      });
      continue;
    }

    // Skip further validation if field is empty and not required
    if (value === undefined || value === null || value === '') {
      continue;
    }

    // Type validation
    if (rule.type === 'number') {
      const numValue = typeof value === 'string' ? parseFloat(value) : value;
      
      if (isNaN(numValue as number)) {
        errors.push({
          field: rule.field,
          message: `${rule.label} must be a valid number`,
          code: 'INVALID_NUMBER'
        });
        continue;
      }

      // Range validation
      if (rule.min !== undefined && (numValue as number) < rule.min) {
        errors.push({
          field: rule.field,
          message: `${rule.label} must be at least ${rule.min}`,
          code: 'VALUE_TOO_LOW'
        });
      }

      if (rule.max !== undefined && (numValue as number) > rule.max) {
        errors.push({
          field: rule.field,
          message: `${rule.label} must not exceed ${rule.max}`,
          code: 'VALUE_TOO_HIGH'
        });
      }
    }

    // Date validation
    if (rule.type === 'date') {
      const dateValue = new Date(value as string);
      if (isNaN(dateValue.getTime())) {
        errors.push({
          field: rule.field,
          message: `${rule.label} must be a valid date`,
          code: 'INVALID_DATE'
        });
      } else if (dateValue > new Date()) {
        errors.push({
          field: rule.field,
          message: `${rule.label} cannot be in the future`,
          code: 'FUTURE_DATE'
        });
      }
    }

    // String validation
    if (rule.type === 'string' && typeof value === 'string') {
      if (value.trim().length === 0) {
        errors.push({
          field: rule.field,
          message: `${rule.label} cannot be empty`,
          code: 'EMPTY_STRING'
        });
      }
    }
  }

  return errors;
}

/**
 * Normalize soil data values
 */
export function normalizeSoilData(data: Partial<SoilHealthData>): Partial<SoilHealthData> {
  const normalized: Partial<SoilHealthData> = { ...data };

  // Normalize numeric fields
  const numericFields: (keyof SoilHealthData)[] = [
    'ph', 'organicCarbon', 'nitrogen', 'phosphorus', 'potassium',
    'sulfur', 'zinc', 'boron', 'iron', 'manganese', 'copper'
  ];

  for (const field of numericFields) {
    const value = normalized[field];
    if (value !== undefined && value !== null) {
      // Convert to number and round to appropriate precision
      const numValue = typeof value === 'string' ? parseFloat(value) : value;
      if (!isNaN(numValue as number)) {
        // Round to 2 decimal places
        normalized[field] = Math.round((numValue as number) * 100) / 100 as any;
      }
    }
  }

  // Normalize string fields
  if (normalized.soilTexture && typeof normalized.soilTexture === 'string') {
    normalized.soilTexture = normalized.soilTexture.trim();
  }

  if (normalized.sampleId && typeof normalized.sampleId === 'string') {
    normalized.sampleId = normalized.sampleId.trim().toUpperCase();
  }

  // Normalize date
  if (normalized.collectionDate) {
    const date = new Date(normalized.collectionDate);
    if (!isNaN(date.getTime())) {
      normalized.collectionDate = date.toISOString().split('T')[0];
    }
  }

  return normalized;
}

/**
 * Calculate confidence score for extracted data
 * This is a simple heuristic - in production, this would come from the backend
 */
export function calculateConfidenceScore(
  data: Partial<SoilHealthData>,
  validationErrors: ValidationError[]
): number {
  const totalFields = soilDataValidationRules.filter(r => r.required).length;
  const filledFields = soilDataValidationRules.filter(r => {
    const value = data[r.field];
    return value !== undefined && value !== null && value !== '';
  }).length;

  // Base score from field completeness
  let score = (filledFields / totalFields) * 100;

  // Reduce score for validation errors
  const errorPenalty = validationErrors.length * 10;
  score = Math.max(0, score - errorPenalty);

  return Math.round(score);
}

/**
 * Get field status based on validation
 */
export function getFieldStatus(
  field: keyof SoilHealthData,
  value: any,
  validationErrors: ValidationError[]
): 'valid' | 'invalid' | 'warning' | 'empty' {
  const hasError = validationErrors.some(e => e.field === field);
  
  if (hasError) {
    return 'invalid';
  }

  if (value === undefined || value === null || value === '') {
    return 'empty';
  }

  // Check if value is within optimal range (this is a simplified check)
  const rule = soilDataValidationRules.find(r => r.field === field);
  if (rule && rule.type === 'number' && typeof value === 'number') {
    if (rule.min !== undefined && value < rule.min * 1.2) {
      return 'warning';
    }
    if (rule.max !== undefined && value > rule.max * 0.8) {
      return 'warning';
    }
  }

  return 'valid';
}

/**
 * Format soil data for display
 */
export function formatSoilDataValue(
  field: keyof SoilHealthData,
  value: any
): string {
  if (value === undefined || value === null || value === '') {
    return '-';
  }

  const rule = soilDataValidationRules.find(r => r.field === field);
  
  if (rule?.type === 'number' && typeof value === 'number') {
    return value.toFixed(2);
  }

  if (rule?.type === 'date') {
    const date = new Date(value);
    if (!isNaN(date.getTime())) {
      return date.toLocaleDateString();
    }
  }

  return String(value);
}

/**
 * Convert data format (e.g., from different units)
 */
export function convertSoilDataUnits(
  data: Partial<SoilHealthData>,
  fromUnit: 'metric' | 'imperial',
  toUnit: 'metric' | 'imperial'
): Partial<SoilHealthData> {
  if (fromUnit === toUnit) {
    return data;
  }

  // For now, we only support metric units
  // This function can be extended to support unit conversions
  return data;
}
