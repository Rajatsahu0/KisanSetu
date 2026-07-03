/**
 * Planting Advisory Service
 * Handles API calls for planting recommendations based on weather and soil data
 */

import { apiClient } from './apiClient';

export interface PlantingRecommendationRequest {
  cropType: string;
  location: string;
  forecastDays?: number;
  planId?: string;
  language?: string;
}

export interface PlantingWindow {
  startDate: string;
  endDate: string;
  rationale: string;
  confidenceScore: number;
  riskFactors: string[];
}

export interface SeedRecommendation {
  varietyName: string;
  seedCompany: string;
  maturityDays: number;
  suitabilityReason: string;
  yieldPotential: number;
  keyCharacteristics: string[];
}

export interface PlantingRecommendationResponse {
  plantingWindows: PlantingWindow[];
  seedRecommendations: SeedRecommendation[];
  message: string;
  hasRecommendations: boolean;
  weatherFetchedAt?: string;
  soilDataDate?: string;
  usedPlanId?: string;
  usedDefaultSoil?: boolean;
  dataSources?: {
    realForecastDays?: number;
    historicalProjectionDays?: number;
    soilMoistureSource?: string;
    soilNutrientSource?: string;
    disclaimer?: string;
  };
}

export interface SavedRecommendation extends PlantingRecommendationResponse {
  recommendationId: string;
  cropType: string;
  location: string;
  savedAt: string;
}

class PlantingAdvisoryService {
  /**
   * Get planting recommendations for a specific crop and location
   */
  async getPlantingRecommendation(
    request: PlantingRecommendationRequest
  ): Promise<PlantingRecommendationResponse> {
    const endpoint = request.planId 
      ? '/api/v1/plantingadvisory/recommend-from-plan'
      : '/api/v1/plantingadvisory/recommend';
    
    return apiClient.post<PlantingRecommendationResponse>(endpoint, request);
  }

  /**
   * Save a recommendation to S3
   */
  async saveRecommendation(
    recommendation: PlantingRecommendationResponse,
    cropType: string,
    location: string
  ): Promise<void> {
    const recommendationId = `rec-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;
    
    const saveRequest = {
      recommendationId,
      cropType,
      location,
      plantingWindows: recommendation.plantingWindows,
      seedRecommendations: recommendation.seedRecommendations,
      weatherFetchedAt: recommendation.weatherFetchedAt,
      soilDataDate: recommendation.soilDataDate,
      savedAt: new Date().toISOString()
    };

    try {
      await apiClient.post('/api/v1/plantingadvisory/recommendations/save', saveRequest);
    } catch (error) {
      console.error('Failed to save recommendation to S3:', error);
      // Fallback to local storage
      this.saveRecommendationLocally(saveRequest);
      throw new Error('Failed to save recommendation to server, saved locally instead');
    }
  }

  /**
   * Get saved recommendations from S3
   */
  async getSavedRecommendations(): Promise<SavedRecommendation[]> {
    try {
      const recommendations = await apiClient.get<SavedRecommendation[]>(
        '/api/v1/plantingadvisory/recommendations'
      );
      return recommendations;
    } catch (error) {
      console.error('Failed to retrieve saved recommendations from S3:', error);
      // Fallback to local storage
      return this.getSavedRecommendationsLocally();
    }
  }

  /**
   * Delete a saved recommendation from S3
   */
  async deleteSavedRecommendation(recommendationId: string): Promise<void> {
    try {
      await apiClient.delete(`/api/v1/plantingadvisory/recommendations/${recommendationId}`);
    } catch (error) {
      console.error('Failed to delete recommendation from S3:', error);
      // Fallback to local storage
      this.deleteSavedRecommendationLocally(recommendationId);
      throw new Error('Failed to delete recommendation from server, deleted locally instead');
    }
  }

  /**
   * Save a recommendation locally (fallback)
   */
  private saveRecommendationLocally(recommendation: SavedRecommendation): void {
    try {
      const saved = this.getSavedRecommendationsLocally();
      saved.push(recommendation);
      localStorage.setItem('savedPlantingRecommendations', JSON.stringify(saved));
    } catch (error) {
      console.error('Failed to save recommendation locally:', error);
      throw new Error('Failed to save recommendation');
    }
  }

  /**
   * Get saved recommendations from local storage (fallback)
   */
  private getSavedRecommendationsLocally(): SavedRecommendation[] {
    try {
      const saved = localStorage.getItem('savedPlantingRecommendations');
      return saved ? JSON.parse(saved) : [];
    } catch (error) {
      console.error('Failed to retrieve saved recommendations:', error);
      return [];
    }
  }

  /**
   * Delete a saved recommendation locally (fallback)
   */
  private deleteSavedRecommendationLocally(recommendationId: string): void {
    try {
      const saved = this.getSavedRecommendationsLocally();
      const filtered = saved.filter(r => r.recommendationId !== recommendationId);
      localStorage.setItem('savedPlantingRecommendations', JSON.stringify(filtered));
    } catch (error) {
      console.error('Failed to delete recommendation:', error);
      throw new Error('Failed to delete recommendation');
    }
  }
}

// Export singleton instance
export const plantingAdvisoryService = new PlantingAdvisoryService();

export default plantingAdvisoryService;
