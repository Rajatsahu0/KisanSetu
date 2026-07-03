import React from 'react';
import { AlertCircle, TrendingUp, Calendar, Upload } from 'lucide-react';

interface InsufficientDataMessageProps {
  dataType: string;
  minDataPoints?: number;
  currentDataPoints?: number;
  suggestions?: string[];
}

export const InsufficientDataMessage: React.FC<InsufficientDataMessageProps> = ({
  dataType,
  minDataPoints = 5,
  currentDataPoints = 0,
  suggestions,
}) => {
  const defaultSuggestions = [
    'Upload more soil health cards to track soil nutrient trends',
    'Grade more produce images to analyze quality patterns',
    'Use voice queries regularly to build price history',
    'Try selecting a longer time period to include more data',
  ];

  const displaySuggestions = suggestions || defaultSuggestions;

  return (
    <div className="bg-white dark:bg-gray-800 rounded-lg shadow-md p-8">
      <div className="text-center">
        {/* Icon */}
        <div className="inline-flex items-center justify-center w-16 h-16 bg-yellow-100 dark:bg-yellow-900/20 rounded-full mb-4">
          <AlertCircle className="w-8 h-8 text-yellow-600 dark:text-yellow-400" />
        </div>

        {/* Title */}
        <h3 className="text-xl font-bold text-gray-900 dark:text-gray-100 mb-2">
          Insufficient Data for {dataType}
        </h3>

        {/* Description */}
        <p className="text-gray-600 dark:text-gray-400 mb-6 max-w-md mx-auto">
          We need at least {minDataPoints} data points to generate meaningful insights and trends.
          {currentDataPoints > 0 && (
            <span className="block mt-2">
              Currently, you have {currentDataPoints} data point{currentDataPoints !== 1 ? 's' : ''}.
            </span>
          )}
        </p>

        {/* Progress Bar */}
        {currentDataPoints > 0 && (
          <div className="max-w-md mx-auto mb-6">
            <div className="w-full bg-gray-200 dark:bg-gray-700 rounded-full h-2">
              <div
                className="bg-blue-600 h-2 rounded-full transition-all duration-300"
                style={{ width: `${Math.min((currentDataPoints / minDataPoints) * 100, 100)}%` }}
              />
            </div>
            <p className="text-xs text-gray-500 dark:text-gray-400 mt-2">
              {currentDataPoints} of {minDataPoints} minimum data points
            </p>
          </div>
        )}

        {/* Suggestions */}
        <div className="bg-blue-50 dark:bg-blue-900/20 rounded-lg p-6 max-w-2xl mx-auto">
          <h4 className="text-sm font-semibold text-gray-900 dark:text-gray-100 mb-4 flex items-center justify-center">
            <TrendingUp className="w-4 h-4 mr-2" />
            How to Build Your Data History
          </h4>
          <div className="space-y-3 text-left">
            {displaySuggestions.map((suggestion, index) => (
              <div key={index} className="flex items-start space-x-3">
                <div className="flex-shrink-0 mt-0.5">
                  {index === 0 && <Upload className="w-4 h-4 text-blue-600" />}
                  {index === 1 && <TrendingUp className="w-4 h-4 text-blue-600" />}
                  {index === 2 && <Calendar className="w-4 h-4 text-blue-600" />}
                  {index === 3 && <Calendar className="w-4 h-4 text-blue-600" />}
                </div>
                <p className="text-sm text-gray-700 dark:text-gray-300">
                  {suggestion}
                </p>
              </div>
            ))}
          </div>
        </div>

        {/* Action Buttons */}
        <div className="mt-6 flex flex-col sm:flex-row gap-3 justify-center">
          <button
            onClick={() => window.location.href = '/soil-analysis'}
            className="px-6 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors"
          >
            Upload Soil Data
          </button>
          <button
            onClick={() => window.location.href = '/quality-grading'}
            className="px-6 py-2 bg-gray-200 dark:bg-gray-700 text-gray-700 dark:text-gray-300 rounded-lg hover:bg-gray-300 dark:hover:bg-gray-600 transition-colors"
          >
            Grade Produce
          </button>
        </div>
      </div>
    </div>
  );
};
