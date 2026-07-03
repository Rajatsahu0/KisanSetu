import React from 'react';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { 
  Mic, 
  Camera, 
  Leaf, 
  Sprout, 
  ArrowRight,
  Info
} from 'lucide-react';

const AboutPage: React.FC = () => {
  const { t } = useTranslation();
  const navigate = useNavigate();

  const features = [
    {
      icon: <Leaf className="w-8 h-8" />,
      titleKey: 'about.features.soil.title',
      descKey: 'about.features.soil.description',
      stepsKey: 'about.features.soil.steps',
      route: '/soil-analysis',
      color: 'bg-green-500'
    },
    {
      icon: <Camera className="w-8 h-8" />,
      titleKey: 'about.features.grading.title',
      descKey: 'about.features.grading.description',
      stepsKey: 'about.features.grading.steps',
      route: '/quality-grading',
      color: 'bg-blue-500'
    },
    {
      icon: <Mic className="w-8 h-8" />,
      titleKey: 'about.features.voice.title',
      descKey: 'about.features.voice.description',
      stepsKey: 'about.features.voice.steps',
      route: '/voice-queries',
      color: 'bg-purple-500'
    },
    {
      icon: <Sprout className="w-8 h-8" />,
      titleKey: 'about.features.planting.title',
      descKey: 'about.features.planting.description',
      stepsKey: 'about.features.planting.steps',
      route: '/planting-advisory',
      color: 'bg-amber-500'
    }
  ];

  return (
    <div className="min-h-screen bg-gray-50 py-8 px-4">
      <div className="max-w-6xl mx-auto">
        {/* Header */}
        <div className="bg-white rounded-lg shadow-md p-6 mb-6">
          {/* Project Badge */}
          <div className="inline-flex items-center gap-2 bg-gradient-to-r from-green-50 to-blue-50 border border-green-200 rounded-full px-4 py-2 mb-4">
            <span className="text-2xl font-bold text-black" role="img" aria-label="India flag">🇮🇳</span>
            <span className="text-sm font-semibold text-green-700">
              {t('about.projectBadge')}
            </span>
          </div>

          <div className="flex items-center gap-3 mb-4">
            <Info className="w-8 h-8 text-green-600" />
            <h1 className="text-3xl font-bold text-gray-800">
              {t('about.title')}
            </h1>
          </div>
          <p className="text-gray-600 text-lg">
            {t('about.subtitle')}
          </p>
        </div>

        {/* Features Grid */}
        <div className="grid md:grid-cols-2 gap-6">
          {features.map((feature, index) => (
            <div
              key={index}
              className="bg-white rounded-lg shadow-md hover:shadow-lg transition-shadow cursor-pointer"
              onClick={() => navigate(feature.route)}
            >
              <div className="p-6">
                {/* Feature Header */}
                <div className="flex items-center gap-4 mb-4">
                  <div className={`${feature.color} text-white p-3 rounded-lg`}>
                    {feature.icon}
                  </div>
                  <h2 className="text-xl font-bold text-gray-800">
                    {t(feature.titleKey)}
                  </h2>
                </div>

                {/* Description */}
                <p className="text-gray-600 mb-4">
                  {t(feature.descKey)}
                </p>

                {/* Steps */}
                <div className="space-y-2 mb-4">
                  <h3 className="font-semibold text-gray-700 text-sm">
                    {t('about.howToUse')}:
                  </h3>
                  <ol className="list-decimal list-inside space-y-1 text-sm text-gray-600">
                    {(t(feature.stepsKey, { returnObjects: true }) as string[]).map((step, idx) => (
                      <li key={idx}>{step}</li>
                    ))}
                  </ol>
                </div>

                {/* Navigate Button */}
                <button
                  className="flex items-center gap-2 text-green-600 hover:text-green-700 font-medium"
                  onClick={(e) => {
                    e.stopPropagation();
                    navigate(feature.route);
                  }}
                >
                  {t('about.tryNow')}
                  <ArrowRight className="w-4 h-4" />
                </button>
              </div>
            </div>
          ))}
        </div>

        {/* Additional Info */}
        <div className="bg-white rounded-lg shadow-md p-6 mt-6">
          <h2 className="text-xl font-bold text-gray-800 mb-3">
            {t('about.additionalInfo.title')}
          </h2>
          <ul className="space-y-2 text-gray-600">
            <li>• {t('about.additionalInfo.offline')}</li>
            <li>• {t('about.additionalInfo.multilingual')}</li>
            <li>• {t('about.additionalInfo.history')}</li>
            <li>• {t('about.additionalInfo.secure')}</li>
          </ul>
        </div>
      </div>
    </div>
  );
};

export default AboutPage;
