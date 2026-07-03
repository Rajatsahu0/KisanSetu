import React from 'react';
import { Lightbulb, TrendingUp, AlertTriangle, CheckCircle } from 'lucide-react';
import { useLanguage } from '@/contexts/LanguageContext';

interface Insight { type: 'positive' | 'negative' | 'neutral' | 'warning'; title: string; description: string; recommendation?: string; }
interface InsightsPanelProps { insights: string[]; loading?: boolean; }

export const InsightsPanel: React.FC<InsightsPanelProps> = ({ insights, loading = false }) => {
  const { t } = useLanguage();

  const parseInsights = (insights: string[]): Insight[] => {
    return insights.map((insight) => {
      const lower = insight.toLowerCase();
      let type: Insight['type'] = 'neutral';
      if (lower.includes('increase') || lower.includes('improve') || lower.includes('better')) type = 'positive';
      else if (lower.includes('decrease') || lower.includes('decline') || lower.includes('worse')) type = 'negative';
      else if (lower.includes('warning') || lower.includes('caution') || lower.includes('alert')) type = 'warning';
      return { type, title: insight.split('.')[0] || insight, description: insight };
    });
  };

  const getIcon = (type: Insight['type']) => {
    switch (type) {
      case 'positive': return <CheckCircle className="w-5 h-5 text-green-600" />;
      case 'negative': return <TrendingUp className="w-5 h-5 text-red-600 rotate-180" />;
      case 'warning': return <AlertTriangle className="w-5 h-5 text-yellow-600" />;
      default: return <Lightbulb className="w-5 h-5 text-blue-600" />;
    }
  };

  const getBg = (type: Insight['type']) => {
    switch (type) {
      case 'positive': return 'bg-green-50 dark:bg-green-900/20 border-green-200 dark:border-green-800';
      case 'negative': return 'bg-red-50 dark:bg-red-900/20 border-red-200 dark:border-red-800';
      case 'warning': return 'bg-yellow-50 dark:bg-yellow-900/20 border-yellow-200 dark:border-yellow-800';
      default: return 'bg-blue-50 dark:bg-blue-900/20 border-blue-200 dark:border-blue-800';
    }
  };

  const parsed = parseInsights(insights);

  if (loading) {
    return (
      <div className="bg-white dark:bg-gray-800 rounded-lg shadow-md p-6">
        <div className="flex items-center space-x-2 mb-4">
          <Lightbulb className="w-5 h-5 text-blue-600 animate-pulse" />
          <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100">{t('history.aiInsights', 'AI Insights')}</h3>
        </div>
        <div className="space-y-3">{[1, 2, 3].map((i) => (<div key={i} className="animate-pulse"><div className="h-4 bg-gray-200 dark:bg-gray-700 rounded w-3/4 mb-2" /><div className="h-3 bg-gray-200 dark:bg-gray-700 rounded w-full" /></div>))}</div>
      </div>
    );
  }

  if (insights.length === 0) {
    return (
      <div className="bg-white dark:bg-gray-800 rounded-lg shadow-md p-6">
        <div className="flex items-center space-x-2 mb-4">
          <Lightbulb className="w-5 h-5 text-blue-600" />
          <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100">{t('history.aiInsights', 'AI Insights')}</h3>
        </div>
        <div className="text-center py-8">
          <AlertTriangle className="w-12 h-12 text-gray-400 mx-auto mb-3" />
          <p className="text-gray-600 dark:text-gray-400">{t('history.noInsights', 'No insights available for the selected data range.')}</p>
          <p className="text-sm text-gray-500 dark:text-gray-500 mt-2">{t('history.tryDifferentPeriod', 'Try selecting a different time period or data type.')}</p>
        </div>
      </div>
    );
  }

  return (
    <div className="bg-white dark:bg-gray-800 rounded-lg shadow-md p-6">
      <div className="flex items-center space-x-2 mb-4">
        <Lightbulb className="w-5 h-5 text-blue-600" />
        <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100">{t('history.aiInsights', 'AI Insights')}</h3>
        <span className="ml-auto text-xs text-gray-500 dark:text-gray-400">
          {t('history.insightCount', '{{count}} insights', { count: insights.length })}
        </span>
      </div>
      <div className="space-y-3">
        {parsed.map((insight, index) => (
          <div key={index} className={`p-4 rounded-lg border ${getBg(insight.type)}`}>
            <div className="flex items-start space-x-3">
              <div className="flex-shrink-0 mt-0.5">{getIcon(insight.type)}</div>
              <div className="flex-1">
                <p className="text-sm text-gray-900 dark:text-gray-100">{insight.description}</p>
                {insight.recommendation && (
                  <p className="text-xs text-gray-600 dark:text-gray-400 mt-2">
                    <span className="font-semibold">{t('history.recommendation', 'Recommendation:')}</span> {insight.recommendation}
                  </p>
                )}
              </div>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
};
