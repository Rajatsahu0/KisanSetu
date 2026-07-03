/**
 * Soil Analysis Service
 * Handles API calls for soil health card upload, analysis, and regenerative plan generation
 */

import { apiClient } from './apiClient';
import type {
  SoilHealthCardResponse,
  SoilHealthData,
  ValidationError
} from '@/types';

export interface FarmProfile {
  farmerId: string;
  farmName: string;
  location: {
    state: string;
    district: string;
    block: string;
    village: string;
    coordinates?: {
      latitude: number;
      longitude: number;
    };
  };
  farmSize: number;
  primaryCrops: string[];
  soilType: string;
}

export interface CarbonEstimate {
  totalCarbonTonnesPerYear: number;
  monthlyAverageTonnes: number;
  monthlyBreakdown: {
    month: number;
    estimatedTonnes: number;
    primaryPractice: string;
  }[];
}

export interface RegenerativePlan {
  planId: string;
  farmerId: string;
  soilData?: SoilHealthData;
  recommendations?: PlanRecommendation[];
  monthlyActions: PlanTimeline[];
  carbonEstimate: CarbonEstimate;
  estimatedCostSavings?: number;
  generatedDate?: string;
  validUntil?: string;
  createdAt: string;
}

export interface PlanRecommendation {
  category: string;
  title: string;
  description: string;
  priority: 'high' | 'medium' | 'low';
  estimatedCost: number;
  expectedBenefit: string;
  implementationSteps: string[];
}

export interface PlanTimeline {
  month: number;
  monthName: string;
  practices: string[];
  rationale: string;
  expectedOutcomes: string[];
}

export interface PlanGenerationRequest {
  soilData: SoilHealthData;
  farmProfile: FarmProfile;
  language?: string;
}

class SoilAnalysisService {
  /**
   * Upload and digitize a Soil Health Card
   */
  async uploadSoilHealthCard(
    file: File,
    onProgress?: (progress: number) => void
  ): Promise<SoilHealthCardResponse> {
    return apiClient.uploadFile<SoilHealthCardResponse>(
      '/api/v1/soilanalysis/upload-card',
      file,
      undefined,
      onProgress,
      'cardImage'
    );
  }

  /**
   * Generate a regenerative farming plan
   */
  async generateRegenerativePlan(
    request: PlanGenerationRequest
  ): Promise<RegenerativePlan> {
    return apiClient.post<RegenerativePlan>(
      '/api/v1/soilanalysis/generate-plan',
      request
    );
  }

  /**
   * Get soil data history
   */
  async getSoilHistory(
    startDate?: Date,
    endDate?: Date
  ): Promise<SoilHealthData[]> {
    const params = new URLSearchParams();
    
    if (startDate) {
      params.append('startDate', startDate.toISOString());
    }
    if (endDate) {
      params.append('endDate', endDate.toISOString());
    }

    const queryString = params.toString();
    const endpoint = `/api/v1/soilanalysis/history${queryString ? `?${queryString}` : ''}`;
    
    return apiClient.get<SoilHealthData[]>(endpoint);
  }

  /**
   * Save a regenerative plan to S3
   */
  async savePlan(plan: RegenerativePlan): Promise<void> {
    try {
      await apiClient.post('/api/v1/soilanalysis/plans/save', plan);
    } catch (error) {
      console.error('Failed to save plan to S3:', error);
      // Fallback to local storage
      this.savePlanLocally(plan);
      throw new Error('Failed to save plan to server, saved locally instead');
    }
  }

  /**
   * Get saved plans from S3
   */
  async getSavedPlans(): Promise<(RegenerativePlan & { savedAt?: string })[]> {
    try {
      const plans = await apiClient.get<RegenerativePlan[]>('/api/v1/soilanalysis/plans');
      return plans;
    } catch (error) {
      console.error('Failed to retrieve saved plans from S3:', error);
      // Fallback to local storage
      return this.getSavedPlansLocally();
    }
  }

  /**
   * Get a specific saved plan by ID
   */
  async getSavedPlanById(planId: string): Promise<RegenerativePlan> {
    return apiClient.get<RegenerativePlan>(`/api/v1/soilanalysis/plans/${planId}`);
  }

  /**
   * Delete a saved plan from S3
   */
  async deleteSavedPlan(planId: string): Promise<void> {
    try {
      await apiClient.delete(`/api/v1/soilanalysis/plans/${planId}`);
    } catch (error) {
      console.error('Failed to delete plan from S3:', error);
      // Fallback to local storage
      this.deleteSavedPlanLocally(planId);
      throw new Error('Failed to delete plan from server, deleted locally instead');
    }
  }

  /**
   * Save a regenerative plan locally (fallback)
   */
  savePlanLocally(plan: RegenerativePlan): void {
    try {
      const savedPlans = this.getSavedPlansLocally();
      savedPlans.push({
        ...plan,
        savedAt: new Date().toISOString()
      });
      localStorage.setItem('savedRegenerativePlans', JSON.stringify(savedPlans));
    } catch (error) {
      console.error('Failed to save plan locally:', error);
      throw new Error('Failed to save plan');
    }
  }

  /**
   * Get saved plans from local storage (fallback)
   */
  getSavedPlansLocally(): (RegenerativePlan & { savedAt: string })[] {
    try {
      const saved = localStorage.getItem('savedRegenerativePlans');
      return saved ? JSON.parse(saved) : [];
    } catch (error) {
      console.error('Failed to retrieve saved plans:', error);
      return [];
    }
  }

  /**
   * Delete a saved plan locally (fallback)
   */
  deleteSavedPlanLocally(planId: string): void {
    try {
      const savedPlans = this.getSavedPlansLocally();
      const filtered = savedPlans.filter(p => p.planId !== planId);
      localStorage.setItem('savedRegenerativePlans', JSON.stringify(filtered));
    } catch (error) {
      console.error('Failed to delete plan:', error);
      throw new Error('Failed to delete plan');
    }
  }
}

// Export singleton instance
export const soilAnalysisService = new SoilAnalysisService();

export default soilAnalysisService;
