// API Response types
export interface ApiResponse<T = any> {
  data: T;
  message: string;
  success: boolean;
  timestamp: string;
}

export interface ApiError {
  code: string;
  message: string;
  details?: any;
  status?: number;
}

// User Profile types
export interface UserProfile {
  name: string;
  phoneNumber: string;
  email?: string;
  city: string;
  state: string;
  pincode: string;
}

// Location types
export interface Location {
  state: string;
  district: string;
  block: string;
  village: string;
  coordinates?: {
    latitude: number;
    longitude: number;
  };
}

// Soil Health types
export interface SoilHealthData {
  farmerId: string;
  sampleId: string;
  collectionDate: string;
  pH: number;  // Capital H to match backend JSON serialization
  organicCarbon: number;
  nitrogen: number;
  phosphorus: number;
  potassium: number;
  sulfur: number;
  zinc: number;
  boron: number;
  iron: number;
  manganese: number;
  copper: number;
  soilTexture: string;
  recommendations: string[];
}

export interface ValidationError {
  field: string;
  message: string;
  code: string;
}

export interface SoilHealthCardResponse {
  soilData: SoilHealthData | null;
  isValid: boolean;
  validationErrors: ValidationError[];
  message: string;
  requiresManualVerification: boolean;
}

// Quality Grading types
export interface DetectedObject {
  label: string;
  confidence: number;
  boundingBox: {
    x: number;
    y: number;
    width: number;
    height: number;
  };
}

export interface QualityIndicator {
  name: string;
  value: number;
  threshold: number;
  status: 'good' | 'fair' | 'poor';
}

export interface ImageAnalysisResult {
  confidenceScore: number;
  visionGrade?: string;
  visionReasoning?: string;
  detectedObjects: DetectedObject[];
  qualityIndicators: QualityIndicator[];
  imageQuality: {
    resolution: string;
    clarity: number;
    lighting: number;
  };
}

export enum QualityGrade {
  A = 'A',
  B = 'B',
  FAQ = 'FAQ',
  C = 'C',
  NonFAQ = 'NonFAQ',
  Reject = 'Reject'
}

export interface GradingResult {
  recordId: string;
  grade: QualityGrade;
  gradeLabel?: string;
  certifiedPrice: number;
  analysis: ImageAnalysisResult;
  timestamp: string;
}

export interface BatchGradingResult {
  batchId: string;
  individualResults: GradingResult[];
  aggregatedGrade: QualityGrade;
  batchCertifiedPrice: number;
  processedCount: number;
  timestamp: string;
}

// Voice Query types
export interface Dialect {
  code: string;
  name: string;
  nativeName: string;
  region: string;
}

export interface MarketPrice {
  commodity: string;
  market: string;
  price: number;
  unit: string;
  date: string;
  source: string;
}

export interface VoiceQueryResponse {
  transcription: string;
  prices: MarketPrice[];
  audioResponseUrl: string;
  responseText: string;
  confidence: number;
  dialect: string;
}

// Chart and Visualization types
export interface ChartPoint {
  label: string;
  value: number;
  timestamp: string;
}

export interface ChartSeries {
  name: string;
  points: ChartPoint[];
  color: string;
  type: 'line' | 'bar' | 'area' | 'scatter';
}

export interface ChartAnnotation {
  x: string | number;
  y: string | number;
  text: string;
  type: 'point' | 'line' | 'area';
}

export interface ChartData {
  title: string;
  xAxisLabel: string;
  yAxisLabel: string;
  series: ChartSeries[];
  annotations: ChartAnnotation[];
}

// File Upload types
export interface UploadProgress {
  loaded: number;
  total: number;
  percentage: number;
}

export interface FileUploadResult {
  fileId: string;
  fileName: string;
  fileSize: number;
  uploadUrl: string;
  status: 'uploading' | 'processing' | 'complete' | 'error';
}

// Planting Advisory types
export interface CropType {
  id: string;
  name: string;
  scientificName: string;
  category: string;
  varieties: string[];
}

export interface PlantingAdvisoryRequest {
  cropType: string;
  location: Location;
  farmSize: number;
  soilType: string;
  previousCrop?: string;
}

export interface PlantingWindow {
  startDate: string;
  endDate: string;
  confidence: number;
  reasoning: string;
}

export interface SeedVarietyRecommendation {
  variety: string;
  suitabilityScore: number;
  characteristics: string[];
  expectedYield: number;
  maturityPeriod: number;
}

export interface PlantingAdvisoryResponse {
  recommendationId: string;
  optimalWindows: PlantingWindow[];
  seedVarieties: SeedVarietyRecommendation[];
  weatherForecast: any; // Will be defined based on weather service
  soilPreparationSteps: string[];
  confidence: number;
  lastUpdated: string;
}