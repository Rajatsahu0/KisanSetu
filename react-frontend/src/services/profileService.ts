import { apiClient } from './apiClient';
import type { UserProfile } from '@/types';

class ProfileService {
  async getProfile(): Promise<UserProfile> {
    return apiClient.get<UserProfile>('/api/v1/profile');
  }

  async updateProfile(profile: UserProfile): Promise<UserProfile> {
    const updateData = {
      name: profile.name,
      email: profile.email,
      city: profile.city,
      state: profile.state,
      pincode: profile.pincode,
    };
    return apiClient.post<UserProfile>('/api/v1/profile', updateData);
  }
}

export const profileService = new ProfileService();
