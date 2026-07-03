import { useState, useCallback } from 'react';
import { useAuth } from '@/contexts/AuthContext';

interface TrialQuota {
  voiceQueries: number;
  plantingQueries: number;
  firstUsed: string;
}

const STORAGE_KEY = 'kisanmitra_trial';
const VOICE_LIMIT = 5;
const PLANTING_LIMIT = 3;

const getQuota = (): TrialQuota => {
  try {
    const stored = localStorage.getItem(STORAGE_KEY);
    if (stored) return JSON.parse(stored);
  } catch {}
  return { voiceQueries: 0, plantingQueries: 0, firstUsed: new Date().toISOString() };
};

const saveQuota = (quota: TrialQuota) => {
  localStorage.setItem(STORAGE_KEY, JSON.stringify(quota));
};

export const useTrialQuota = () => {
  const { isAuthenticated } = useAuth();
  const [quota, setQuota] = useState<TrialQuota>(getQuota);

  const canUseVoice = isAuthenticated || quota.voiceQueries < VOICE_LIMIT;
  const canUsePlanting = isAuthenticated || quota.plantingQueries < PLANTING_LIMIT;
  const voiceRemaining = isAuthenticated ? Infinity : Math.max(0, VOICE_LIMIT - quota.voiceQueries);
  const plantingRemaining = isAuthenticated ? Infinity : Math.max(0, PLANTING_LIMIT - quota.plantingQueries);

  const consumeVoice = useCallback(() => {
    if (isAuthenticated) return true;
    const current = getQuota();
    if (current.voiceQueries >= VOICE_LIMIT) return false;
    const updated = { ...current, voiceQueries: current.voiceQueries + 1 };
    saveQuota(updated);
    setQuota(updated);
    return true;
  }, [isAuthenticated]);

  const consumePlanting = useCallback(() => {
    if (isAuthenticated) return true;
    const current = getQuota();
    if (current.plantingQueries >= PLANTING_LIMIT) return false;
    const updated = { ...current, plantingQueries: current.plantingQueries + 1 };
    saveQuota(updated);
    setQuota(updated);
    return true;
  }, [isAuthenticated]);

  return {
    canUseVoice,
    canUsePlanting,
    voiceRemaining,
    plantingRemaining,
    voiceLimit: VOICE_LIMIT,
    plantingLimit: PLANTING_LIMIT,
    consumeVoice,
    consumePlanting,
    isAuthenticated,
  };
};
