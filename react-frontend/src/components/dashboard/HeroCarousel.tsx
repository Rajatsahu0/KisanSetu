import React, { useState, useEffect, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { useLanguage } from '@/contexts/LanguageContext';

interface Slide {
  id: number;
  image: string;
  fallbackGradient: string;
  emoji: string;
  headline: string;
  subtext: string;
  cta?: { label: string; path: string };
  secondary?: { label: string; path: string };
  stats?: { value: string; label: string }[];
}

const AUTO_SLIDE_MS = 5000;

export const HeroCarousel: React.FC = () => {
  const [current, setCurrent] = useState(0);
  const [paused, setPaused] = useState(false);
  const [loadedImages, setLoadedImages] = useState<Set<number>>(new Set());
  const navigate = useNavigate();
  const { t } = useLanguage();

  const SLIDES: Slide[] = [
    {
      id: 1,
      image: '/images/indian farmer field.jpg',
      fallbackGradient: 'from-green-700 via-green-600 to-emerald-700',
      emoji: '🌾',
      headline: t('hero.slide1Title', 'AI-Powered Farming Assistant'),
      subtext: t('hero.slide1Desc', 'Get market prices, planting advice, soil analysis — all in your language'),
      cta: { label: t('hero.slide1Cta', '🎤 Try Voice Query Free'), path: '/voice-queries' },
      secondary: { label: t('hero.slide1Secondary', 'Register Free'), path: '/register' },
    },
    {
      id: 2,
      image: '/images/indian vegetable market.jpg',
      fallbackGradient: 'from-blue-700 via-blue-600 to-indigo-700',
      emoji: '',
      headline: t('hero.slide2Title', 'Real-Time Mandi Prices'),
      subtext: t('hero.slide2Desc', 'Ask "आलू का भाव बताओ" and get live prices from your nearest mandi in seconds'),
      cta: { label: t('hero.slide2Cta', 'Check Prices Now'), path: '/voice-queries' },
    },
    {
      id: 3,
      image: '/images/rice paddy india planting.jpg',
      fallbackGradient: 'from-purple-700 via-purple-600 to-violet-700',
      emoji: '',
      headline: t('hero.slide3Title', 'Smart Planting Advisory'),
      subtext: t('hero.slide3Desc', '90-day weather forecast + AI seed recommendations tailored to your soil and location'),
      cta: { label: t('hero.slide3Cta', 'Get Planting Advice'), path: '/planting-advisory' },
    },
    {
      id: 4,
      image: '/images/indian farmer smartphone.jpg',
      fallbackGradient: 'from-amber-700 via-orange-600 to-red-700',
      emoji: '',
      headline: t('hero.slide4Title', 'Trusted by Indian Farmers'),
      subtext: t('hero.slide4Desc', '10+ languages supported • Free Soil Health Card digitization • Government scheme guidance'),
      stats: [
        { value: '10+', label: t('hero.statLanguages', 'Languages') },
        { value: '50+', label: t('hero.statCrops', 'Crops') },
        { value: '770+', label: t('hero.statDistricts', 'Districts') },
      ],
    },
    {
      id: 5,
      image: '/images/wheat field golden sunrise india.jpg',
      fallbackGradient: 'from-teal-700 via-emerald-600 to-green-700',
      emoji: '',
      headline: t('hero.slide5Title', 'Start Free — No Credit Card'),
      subtext: t('hero.slide5Desc', '5 free voice queries + 3 free planting advisories. Register in 30 seconds for unlimited access.'),
      cta: { label: t('hero.slide5Cta', '🚀 Register Free Now'), path: '/register' },
      secondary: { label: t('hero.slide5Secondary', '🎤 Try Without Registering'), path: '/voice-queries' },
    },
  ];

  const next = useCallback(() => setCurrent(p => (p + 1) % SLIDES.length), []);
  const prev = useCallback(() => setCurrent(p => (p - 1 + SLIDES.length) % SLIDES.length), []);

  useEffect(() => {
    if (paused) return;
    const timer = setInterval(next, AUTO_SLIDE_MS);
    return () => clearInterval(timer);
  }, [paused, next]);

  useEffect(() => {
    SLIDES.forEach((slide, i) => {
      const img = new Image();
      img.onload = () => setLoadedImages(prev => new Set(prev).add(i));
      img.src = slide.image;
    });
  }, []);

  const slide = SLIDES[current];
  const imageLoaded = loadedImages.has(current);

  return (
    <div
      className="relative rounded-2xl shadow-xl overflow-hidden transition-all duration-500"
      onMouseEnter={() => setPaused(true)}
      onMouseLeave={() => setPaused(false)}
      role="region"
      aria-label="Feature carousel"
      aria-roledescription="carousel"
    >
      <div className="absolute inset-0">
        {imageLoaded ? (
          <img
            src={slide.image}
            alt=""
            className="absolute inset-0 w-full h-full object-cover transition-opacity duration-700"
            aria-hidden="true"
          />
        ) : (
          <div className={`absolute inset-0 bg-gradient-to-br ${slide.fallbackGradient}`} />
        )}
        <div className="absolute inset-0 bg-gradient-to-r from-black/70 via-black/50 to-black/30" />
        <div className="absolute inset-x-0 bottom-0 h-20 bg-gradient-to-t from-black/60 to-transparent" />
      </div>

      <div className="relative z-10 px-6 sm:px-10 py-10 sm:py-14 min-h-[300px] sm:min-h-[360px] flex flex-col justify-center text-white">
        {slide.emoji && <div className="text-4xl sm:text-5xl mb-4 drop-shadow-lg">{slide.emoji}</div>}
        <h1 className="text-2xl sm:text-3xl lg:text-4xl font-bold mb-2 leading-tight drop-shadow-md max-w-2xl">
          {slide.headline}
        </h1>
        <p className="text-base sm:text-lg text-white/90 mb-6 max-w-2xl drop-shadow-sm">
          {slide.subtext}
        </p>

        {slide.stats && (
          <div className="flex gap-8 mb-6">
            {slide.stats.map(s => (
              <div key={s.label} className="text-center">
                <div className="text-3xl font-bold drop-shadow-md">{s.value}</div>
                <div className="text-xs text-white/70">{s.label}</div>
              </div>
            ))}
          </div>
        )}

        <div className="flex flex-wrap gap-3">
          {slide.cta && (
            <button onClick={() => navigate(slide.cta!.path)}
              className="px-6 py-3 bg-white text-gray-900 font-bold rounded-xl hover:bg-gray-100 transition-colors shadow-lg text-sm sm:text-base">
              {slide.cta.label}
            </button>
          )}
          {slide.secondary && (
            <button onClick={() => navigate(slide.secondary!.path)}
              className="px-6 py-3 bg-white/15 backdrop-blur-sm text-white font-medium rounded-xl hover:bg-white/25 transition-colors border border-white/30 text-sm sm:text-base">
              {slide.secondary.label}
            </button>
          )}
        </div>
      </div>

      <div className="absolute bottom-4 left-1/2 -translate-x-1/2 flex items-center gap-3 z-20">
        <button onClick={prev}
          className="w-8 h-8 flex items-center justify-center rounded-full bg-white/20 backdrop-blur-sm hover:bg-white/40 transition-colors text-white text-lg"
          aria-label="Previous slide">
          ‹
        </button>
        {SLIDES.map((_, i) => (
          <button key={i} onClick={() => setCurrent(i)}
            className={`h-2.5 rounded-full transition-all duration-300 ${
              i === current ? 'bg-white w-7' : 'bg-white/40 hover:bg-white/60 w-2.5'
            }`}
            aria-label={`Go to slide ${i + 1}`}
            aria-current={i === current ? 'true' : undefined} />
        ))}
        <button onClick={next}
          className="w-8 h-8 flex items-center justify-center rounded-full bg-white/20 backdrop-blur-sm hover:bg-white/40 transition-colors text-white text-lg"
          aria-label="Next slide">
          ›
        </button>
      </div>

      <div className="absolute top-4 right-4 text-xs text-white/50 z-20 bg-black/20 backdrop-blur-sm px-2 py-1 rounded-full">
        {current + 1} / {SLIDES.length}
      </div>
    </div>
  );
};
