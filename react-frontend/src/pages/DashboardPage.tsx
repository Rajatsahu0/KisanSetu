import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '@/contexts/AuthContext';
import { useLanguage } from '@/contexts/LanguageContext';
import { HeroCarousel, KpiMetrics, ActivityFeed, ActionCenter } from '@/components/dashboard';
import type { KpiCard } from '@/components/dashboard';
import type { ActivityItem } from '@/components/dashboard';
import { qualityGradingService } from '@/services/qualityGradingService';
import { voiceQueryService } from '@/services/voiceQueryService';
import { soilAnalysisService } from '@/services/soilAnalysisService';

export const DashboardPage: React.FC = () => {
  const { isAuthenticated, user } = useAuth();
  const navigate = useNavigate();
  const { t } = useLanguage();
  const [kpis, setKpis] = useState<KpiCard[]>([]);
  const [activities, setActivities] = useState<ActivityItem[]>([]);
  const [isLoading, setIsLoading] = useState(false);

  const FEATURES = [
    {
      category: t('dashboard.catMarket', 'Market Intelligence'),
      items: [
        { icon: '🎤', title: t('dashboard.featVoice', 'Voice Queries'), desc: t('dashboard.featVoiceDesc', 'Ask market prices in your local language') },
        { icon: '💰', title: t('dashboard.featMandi', 'Live Mandi Prices'), desc: t('dashboard.featMandiDesc', 'Real-time prices from mandis across India') },
      ],
    },
    {
      category: t('dashboard.catCrop', 'Crop Advisory'),
      items: [
        { icon: '📅', title: t('dashboard.featPlanting', 'Planting Windows'), desc: t('dashboard.featPlantingDesc', 'AI-powered optimal sowing dates based on weather') },
        { icon: '🌱', title: t('dashboard.featSeed', 'Seed Recommendations'), desc: t('dashboard.featSeedDesc', 'Best varieties matched to your soil and climate') },
      ],
    },
    {
      category: t('dashboard.catSoil', 'Soil & Quality'),
      items: [
        { icon: '🧪', title: t('dashboard.featSoilOCR', 'Soil Health Card OCR'), desc: t('dashboard.featSoilOCRDesc', 'Upload card photo → instant digital nutrient analysis') },
        { icon: '⭐', title: t('dashboard.featGrading', 'Produce Grading'), desc: t('dashboard.featGradingDesc', 'AI quality grading with certified price estimation') },
      ],
    },
    {
      category: t('dashboard.catAnalytics', 'Analytics'),
      items: [
        { icon: '📊', title: t('dashboard.featTrends', 'Historical Trends'), desc: t('dashboard.featTrendsDesc', 'Track soil health, prices, and grades over time') },
        { icon: '📋', title: t('dashboard.featPlans', 'Regenerative Plans'), desc: t('dashboard.featPlansDesc', '12-month AI-generated farming improvement plans') },
      ],
    },
  ];

  const HOW_IT_WORKS = [
    { step: '1', icon: '🎤', title: t('dashboard.step1Title', 'Ask or Upload'), desc: t('dashboard.step1Desc', 'Speak your question or upload Soil Health Card') },
    { step: '2', icon: '🤖', title: t('dashboard.step2Title', 'AI Processes'), desc: t('dashboard.step2Desc', 'AI analyzes with real weather & market data') },
    { step: '3', icon: '📋', title: t('dashboard.step3Title', 'Get Advice'), desc: t('dashboard.step3Desc', 'Receive actionable recommendations in your language') },
    { step: '4', icon: '🌾', title: t('dashboard.step4Title', 'Take Action'), desc: t('dashboard.step4Desc', 'Plant the right crop at the right time') },
  ];

  useEffect(() => {
    if (!isAuthenticated) return;
    setIsLoading(true);

    Promise.allSettled([
      qualityGradingService.getGradingHistory(),
      voiceQueryService.getHistory(100),
      soilAnalysisService.getSoilHistory(),
      soilAnalysisService.getSavedPlans(),
    ]).then(([grading, voice, soil, plans]) => {
      const g = grading.status === 'fulfilled' && Array.isArray(grading.value) ? grading.value : [];
      const v = voice.status === 'fulfilled' && Array.isArray(voice.value) ? voice.value : [];
      const s = soil.status === 'fulfilled' && Array.isArray(soil.value) ? soil.value : [];
      const p = plans.status === 'fulfilled' && Array.isArray(plans.value) ? plans.value : [];

      setKpis([
        { icon: '🎤', label: t('dashboard.kpiVoice', 'Voice Queries'), value: v.length, trend: v.length > 0 ? 'up' : 'stable' },
        { icon: '🌱', label: t('dashboard.kpiSoil', 'Soil Tests'), value: Math.max(s.length, p.length), trend: s.length > 0 ? 'up' : 'stable' },
        { icon: '⭐', label: t('dashboard.kpiGrades', 'Quality Grades'), value: g.length, trend: g.length > 0 ? 'up' : 'stable' },
        { icon: '📋', label: t('dashboard.kpiPlans', 'Saved Plans'), value: p.length, trend: p.length > 0 ? 'up' : 'stable' },
      ]);

      const acts: ActivityItem[] = [];
      v.slice(0, 5).forEach((item: any, i: number) => acts.push({
        id: `v-${i}`, icon: '🎤', title: t('dashboard.actVoice', 'Voice Query'),
        description: item.transcription || t('dashboard.actQuery', 'Query'), timestamp: item.timestamp || new Date().toISOString(), type: 'user',
      }));
      g.slice(0, 3).forEach((item: any, i: number) => acts.push({
        id: `g-${i}`, icon: '⭐', title: t('dashboard.actGrading', 'Quality Grading'),
        description: `${t('grading.grade', 'Grade')} ${item.grade || '?'} — ₹${item.certifiedPrice || 0}`, timestamp: item.timestamp || new Date().toISOString(), type: 'user',
      }));
      s.slice(0, 2).forEach((item: any, i: number) => acts.push({
        id: `s-${i}`, icon: '🌱', title: t('dashboard.actSoil', 'Soil Analysis'),
        description: `pH: ${item.pH || '?'}, OC: ${item.organicCarbon || '?'}%`, timestamp: item.testDate || new Date().toISOString(), type: 'system',
      }));
      acts.sort((a, b) => new Date(b.timestamp).getTime() - new Date(a.timestamp).getTime());
      setActivities(acts.slice(0, 10));
    }).finally(() => setIsLoading(false));
  }, [isAuthenticated, t]);

  if (!isAuthenticated) {
    return (
      <div className="space-y-10">
        <HeroCarousel />

        <section>
          <h2 className="text-2xl font-bold text-gray-900 dark:text-white mb-6 text-center">{t('dashboard.everythingFarmerNeeds', 'Everything a Farmer Needs')}</h2>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            {FEATURES.map(cat => (
              <div key={cat.category} className="bg-white dark:bg-gray-800 rounded-xl shadow-sm border border-gray-100 dark:border-gray-700 p-5">
                <h3 className="text-sm font-bold text-green-600 dark:text-green-400 uppercase tracking-wide mb-3">{cat.category}</h3>
                <div className="space-y-3">
                  {cat.items.map(item => (
                    <div key={item.title} className="flex items-start gap-3">
                      <span className="text-2xl flex-shrink-0">{item.icon}</span>
                      <div>
                        <div className="text-sm font-semibold text-gray-900 dark:text-white">{item.title}</div>
                        <div className="text-xs text-gray-500 dark:text-gray-400">{item.desc}</div>
                      </div>
                    </div>
                  ))}
                </div>
              </div>
            ))}
          </div>
        </section>

        <section className="bg-white dark:bg-gray-800 rounded-2xl shadow-sm border border-gray-100 dark:border-gray-700 p-6 sm:p-8">
          <h2 className="text-2xl font-bold text-gray-900 dark:text-white mb-8 text-center">{t('dashboard.howItWorks', 'How It Works')}</h2>
          <div className="grid grid-cols-2 sm:grid-cols-4 gap-6">
            {HOW_IT_WORKS.map((s, i) => (
              <div key={i} className="text-center relative">
                <div className="w-14 h-14 bg-green-100 dark:bg-green-900/30 rounded-2xl flex items-center justify-center mx-auto mb-3">
                  <span className="text-2xl">{s.icon}</span>
                </div>
                <div className="text-[10px] font-bold text-green-600 dark:text-green-400 mb-1">{t('dashboard.step', 'STEP')} {s.step}</div>
                <div className="text-sm font-semibold text-gray-900 dark:text-white mb-1">{s.title}</div>
                <div className="text-xs text-gray-500 dark:text-gray-400">{s.desc}</div>
                {i < HOW_IT_WORKS.length - 1 && (
                  <div className="hidden sm:block absolute top-7 -right-3 text-gray-300 dark:text-gray-600 text-xl">→</div>
                )}
              </div>
            ))}
          </div>
        </section>

        <section className="bg-gradient-to-r from-green-600 to-emerald-600 rounded-2xl p-8 text-center text-white shadow-xl">
          <h2 className="text-2xl sm:text-3xl font-bold mb-2">{t('dashboard.ctaTitle', 'Ready to Transform Your Farming?')}</h2>
          <p className="text-green-100 mb-6 max-w-lg mx-auto">{t('dashboard.ctaDesc', 'Join farmers across India using AI-powered tools. Free forever — no credit card needed.')}</p>
          <div className="flex flex-wrap gap-3 justify-center">
            <button onClick={() => navigate('/register')}
              className="px-8 py-3 bg-white text-green-700 font-bold rounded-xl hover:bg-green-50 transition-colors shadow-lg">
              🚀 {t('dashboard.ctaRegister', 'Get Started Free')}
            </button>
            <button onClick={() => navigate('/voice-queries')}
              className="px-8 py-3 bg-white/15 text-white font-medium rounded-xl hover:bg-white/25 transition-colors border border-white/30">
              🎤 {t('dashboard.ctaDemo', 'Try Demo')}
            </button>
          </div>
        </section>
      </div>
    );
  }

  const getSeason = () => {
    const m = new Date().getMonth();
    if (m >= 5 && m <= 8) return t('dashboard.kharif', 'Kharif season active') + ' 🌧️';
    if (m >= 10 || m <= 1) return t('dashboard.rabi', 'Rabi season active') + ' ❄️';
    return t('dashboard.zaid', 'Zaid season') + ' 🌤️';
  };

  return (
    <div className="space-y-6">
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-2">
        <div className="min-w-0">
          <h1 className="text-xl sm:text-2xl lg:text-3xl font-bold text-gray-900 dark:text-white truncate">
            🙏 {t('dashboard.welcome', 'Welcome, {{name}}!', { name: user?.name || t('dashboard.farmer', 'Farmer') })}
          </h1>
          <p className="text-sm text-gray-500 dark:text-gray-400 mt-0.5">{t('dashboard.farmingDashboard', "Here's your farming dashboard")}</p>
        </div>
      </div>

      <KpiMetrics cards={kpis} isLoading={isLoading} />
      <ActionCenter />

      <div className="grid grid-cols-1 lg:grid-cols-[1fr_minmax(0,380px)] gap-6">
        <div className="bg-white dark:bg-gray-800 rounded-xl shadow-sm border border-gray-100 dark:border-gray-700 p-5">
          <h3 className="text-base font-semibold text-gray-900 dark:text-white mb-4">{t('dashboard.quickSummary', 'Quick Summary')}</h3>
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div className="bg-gradient-to-br from-blue-50 to-blue-100 dark:from-blue-900/20 dark:to-blue-900/10 rounded-xl p-4">
              <div className="text-sm font-medium text-blue-700 dark:text-blue-300 mb-1">🎤 {t('dashboard.latestVoice', 'Latest Voice Query')}</div>
              <p className="text-xs text-blue-600 dark:text-blue-400 truncate">
                {activities.find(a => a.icon === '🎤')?.description || t('dashboard.noQueries', 'No queries yet')}
              </p>
            </div>
            <div className="bg-gradient-to-br from-green-50 to-green-100 dark:from-green-900/20 dark:to-green-900/10 rounded-xl p-4">
              <div className="text-sm font-medium text-green-700 dark:text-green-300 mb-1">🌱 {t('dashboard.latestSoil', 'Latest Soil Test')}</div>
              <p className="text-xs text-green-600 dark:text-green-400 truncate">
                {activities.find(a => a.icon === '🌱')?.description || t('dashboard.noTests', 'No tests yet')}
              </p>
            </div>
            <div className="bg-gradient-to-br from-yellow-50 to-yellow-100 dark:from-yellow-900/20 dark:to-yellow-900/10 rounded-xl p-4">
              <div className="text-sm font-medium text-yellow-700 dark:text-yellow-300 mb-1">⭐ {t('dashboard.latestGrading', 'Latest Grading')}</div>
              <p className="text-xs text-yellow-600 dark:text-yellow-400 truncate">
                {activities.find(a => a.icon === '⭐')?.description || t('dashboard.noGradings', 'No gradings yet')}
              </p>
            </div>
            <div className="bg-gradient-to-br from-purple-50 to-purple-100 dark:from-purple-900/20 dark:to-purple-900/10 rounded-xl p-4">
              <div className="text-sm font-medium text-purple-700 dark:text-purple-300 mb-1">📅 {t('dashboard.plantingSeason', 'Planting Season')}</div>
              <p className="text-xs text-purple-600 dark:text-purple-400">{getSeason()}</p>
            </div>
          </div>
        </div>

        <ActivityFeed items={activities} isLoading={isLoading} onItemClick={(item) => {
          if (item.icon === '🎤') navigate('/voice-queries');
          else if (item.icon === '🌱') navigate('/soil-analysis');
          else if (item.icon === '⭐') navigate('/quality-grading');
        }} />
      </div>
    </div>
  );
};
