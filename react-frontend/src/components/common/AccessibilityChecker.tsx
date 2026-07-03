import React, { useEffect, useState } from 'react';
import { generateAccessibilityReport, AccessibilityReport } from '@/utils/accessibilityTesting';

/**
 * AccessibilityChecker Component
 * 
 * Development-only component that displays accessibility issues
 * Only renders in development mode
 */
export const AccessibilityChecker: React.FC = () => {
  const [report, setReport] = useState<AccessibilityReport | null>(null);
  const [isVisible, setIsVisible] = useState(false);

  useEffect(() => {
    // Only run in development
    if (import.meta.env.DEV) {
      // Generate report after page loads
      const timer = setTimeout(() => {
        const newReport = generateAccessibilityReport();
        setReport(newReport);
      }, 1000);

      return () => clearTimeout(timer);
    }
  }, []);

  // Don't render in production
  if (!import.meta.env.DEV || !report) {
    return null;
  }

  const hasIssues = report.summary.totalIssues > 0;

  return (
    <div className="fixed bottom-4 right-4 z-50">
      {/* Toggle Button */}
      <button
        onClick={() => setIsVisible(!isVisible)}
        className={`px-4 py-2 rounded-lg shadow-lg font-medium text-white ${
          hasIssues ? 'bg-red-600 hover:bg-red-700' : 'bg-green-600 hover:bg-green-700'
        }`}
        aria-label="Toggle accessibility report"
      >
        A11y {hasIssues && `(${report.summary.totalIssues})`}
      </button>

      {/* Report Panel */}
      {isVisible && (
        <div className="absolute bottom-14 right-0 w-96 max-h-96 overflow-y-auto bg-white dark:bg-gray-800 rounded-lg shadow-xl border border-gray-200 dark:border-gray-700 p-4">
          <div className="flex items-center justify-between mb-4">
            <h3 className="text-lg font-bold text-gray-900 dark:text-white">
              Accessibility Report
            </h3>
            <button
              onClick={() => setIsVisible(false)}
              className="text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-200"
              aria-label="Close report"
            >
              ✕
            </button>
          </div>

          {/* Summary */}
          <div className="mb-4 p-3 bg-gray-50 dark:bg-gray-900 rounded">
            <div className="flex justify-between text-sm">
              <span className="text-gray-600 dark:text-gray-400">Total Issues:</span>
              <span className="font-bold text-gray-900 dark:text-white">
                {report.summary.totalIssues}
              </span>
            </div>
            <div className="flex justify-between text-sm mt-1">
              <span className="text-gray-600 dark:text-gray-400">Critical:</span>
              <span className="font-bold text-red-600">
                {report.summary.criticalIssues}
              </span>
            </div>
          </div>

          {/* Color Contrast */}
          <div className="mb-4">
            <h4 className="font-semibold text-gray-900 dark:text-white mb-2">
              Color Contrast
            </h4>
            <div className="space-y-1">
              {Object.entries(report.colorContrast).map(([name, result]) => (
                <div
                  key={name}
                  className="flex items-center justify-between text-sm"
                >
                  <span className="text-gray-600 dark:text-gray-400 truncate">
                    {name}
                  </span>
                  <span
                    className={`font-mono ${
                      result.meetsAA ? 'text-green-600' : 'text-red-600'
                    }`}
                  >
                    {result.meetsAA ? '✓' : '✗'} {result.ratio.toFixed(2)}:1
                  </span>
                </div>
              ))}
            </div>
          </div>

          {/* Elements Without Names */}
          {report.elementsWithoutNames > 0 && (
            <div className="mb-4 p-3 bg-red-50 dark:bg-red-900/20 rounded">
              <p className="text-sm text-red-800 dark:text-red-200">
                ⚠️ {report.elementsWithoutNames} interactive elements without
                accessible names
              </p>
            </div>
          )}

          {/* Images Without Alt */}
          {report.imagesWithoutAlt > 0 && (
            <div className="mb-4 p-3 bg-red-50 dark:bg-red-900/20 rounded">
              <p className="text-sm text-red-800 dark:text-red-200">
                ⚠️ {report.imagesWithoutAlt} images without alt text
              </p>
            </div>
          )}

          {/* Heading Hierarchy */}
          {!report.headingHierarchy.valid && (
            <div className="mb-4 p-3 bg-yellow-50 dark:bg-yellow-900/20 rounded">
              <p className="text-sm font-semibold text-yellow-800 dark:text-yellow-200 mb-1">
                Heading Hierarchy Issues:
              </p>
              <ul className="text-xs text-yellow-700 dark:text-yellow-300 space-y-1">
                {report.headingHierarchy.issues.map((issue, index) => (
                  <li key={index}>• {issue}</li>
                ))}
              </ul>
            </div>
          )}

          {/* Keyboard Trap */}
          {report.hasKeyboardTrap && (
            <div className="mb-4 p-3 bg-red-50 dark:bg-red-900/20 rounded">
              <p className="text-sm text-red-800 dark:text-red-200">
                🚫 Potential keyboard trap detected
              </p>
            </div>
          )}

          {/* Success Message */}
          {!hasIssues && (
            <div className="p-3 bg-green-50 dark:bg-green-900/20 rounded">
              <p className="text-sm text-green-800 dark:text-green-200">
                ✅ No accessibility issues detected!
              </p>
            </div>
          )}

          {/* Timestamp */}
          <div className="mt-4 pt-3 border-t border-gray-200 dark:border-gray-700">
            <p className="text-xs text-gray-500 dark:text-gray-400">
              Generated: {report.timestamp.toLocaleTimeString()}
            </p>
          </div>
        </div>
      )}
    </div>
  );
};

export default AccessibilityChecker;
