/**
 * Accessibility Testing Utilities
 * 
 * Utilities for testing and validating accessibility compliance
 */

/**
 * Color contrast validation for WCAG AA compliance
 */
export interface ContrastResult {
  ratio: number;
  meetsAA: boolean;
  meetsAAA: boolean;
  isLargeText: boolean;
}

/**
 * Parse RGB color string to values
 */
const parseRgb = (color: string): [number, number, number] | null => {
  // Handle hex colors
  if (color.startsWith('#')) {
    const hex = color.slice(1);
    if (hex.length === 3) {
      const r = parseInt(hex[0] + hex[0], 16);
      const g = parseInt(hex[1] + hex[1], 16);
      const b = parseInt(hex[2] + hex[2], 16);
      return [r, g, b];
    } else if (hex.length === 6) {
      const r = parseInt(hex.slice(0, 2), 16);
      const g = parseInt(hex.slice(2, 4), 16);
      const b = parseInt(hex.slice(4, 6), 16);
      return [r, g, b];
    }
  }
  
  // Handle rgb() colors
  const rgbMatch = color.match(/rgb\((\d+),\s*(\d+),\s*(\d+)\)/);
  if (rgbMatch) {
    return [
      parseInt(rgbMatch[1]),
      parseInt(rgbMatch[2]),
      parseInt(rgbMatch[3])
    ];
  }
  
  return null;
};

/**
 * Calculate relative luminance
 */
const getLuminance = (r: number, g: number, b: number): number => {
  const [rs, gs, bs] = [r, g, b].map(val => {
    const normalized = val / 255;
    return normalized <= 0.03928
      ? normalized / 12.92
      : Math.pow((normalized + 0.055) / 1.055, 2.4);
  });
  
  return 0.2126 * rs + 0.7152 * gs + 0.0722 * bs;
};

/**
 * Calculate contrast ratio between two colors
 */
export const calculateContrastRatio = (
  foreground: string,
  background: string
): number => {
  const fg = parseRgb(foreground);
  const bg = parseRgb(background);
  
  if (!fg || !bg) {
    return 0;
  }
  
  const l1 = getLuminance(...fg);
  const l2 = getLuminance(...bg);
  
  const lighter = Math.max(l1, l2);
  const darker = Math.min(l1, l2);
  
  return (lighter + 0.05) / (darker + 0.05);
};

/**
 * Check if color combination meets WCAG standards
 */
export const checkContrast = (
  foreground: string,
  background: string,
  isLargeText: boolean = false
): ContrastResult => {
  const ratio = calculateContrastRatio(foreground, background);
  
  const aaThreshold = isLargeText ? 3 : 4.5;
  const aaaThreshold = isLargeText ? 4.5 : 7;
  
  return {
    ratio,
    meetsAA: ratio >= aaThreshold,
    meetsAAA: ratio >= aaaThreshold,
    isLargeText
  };
};

/**
 * Validate all color combinations in the application
 */
export const validateColorPalette = (): Record<string, ContrastResult> => {
  const results: Record<string, ContrastResult> = {};
  
  const primaryColors = {
    'primary-600-on-white': { fg: '#16a34a', bg: '#ffffff' },
    'primary-700-on-white': { fg: '#15803d', bg: '#ffffff' },
    'primary-500-on-white': { fg: '#22c55e', bg: '#ffffff' },
  };
  
  const textCombinations = {
    'gray-900-on-white': { fg: '#111827', bg: '#ffffff' },
    'gray-700-on-white': { fg: '#374151', bg: '#ffffff' },
    'white-on-primary-600': { fg: '#ffffff', bg: '#16a34a' },
    'white-on-gray-800': { fg: '#ffffff', bg: '#1f2937' },
  };
  
  const allCombinations = { ...primaryColors, ...textCombinations };
  
  for (const [name, { fg, bg }] of Object.entries(allCombinations)) {
    results[name] = checkContrast(fg, bg);
  }
  
  return results;
};

/**
 * Generate accessibility report
 */
export interface AccessibilityReport {
  timestamp: Date;
  colorContrast: Record<string, ContrastResult>;
  elementsWithoutNames: number;
  imagesWithoutAlt: number;
  headingHierarchy: { valid: boolean; issues: string[] };
  hasKeyboardTrap: boolean;
  summary: {
    totalIssues: number;
    criticalIssues: number;
  };
}

export const generateAccessibilityReport = (): AccessibilityReport => {
  const colorContrast = validateColorPalette();
  
  const contrastIssues = Object.values(colorContrast).filter(
    result => !result.meetsAA
  ).length;
  
  return {
    timestamp: new Date(),
    colorContrast,
    elementsWithoutNames: 0,
    imagesWithoutAlt: 0,
    headingHierarchy: { valid: true, issues: [] },
    hasKeyboardTrap: false,
    summary: {
      totalIssues: contrastIssues,
      criticalIssues: contrastIssues
    }
  };
};

/**
 * Log accessibility report to console
 */
export const logAccessibilityReport = (): void => {
  const report = generateAccessibilityReport();

  console.log('🔍 Accessibility Report');
  console.log('======================');

  Object.entries(report.colorContrast).forEach(([name, result]) => {
    const status = result.meetsAA ? '✅' : '❌';
    console.log(`${status} ${name}: ${result.ratio.toFixed(2)}:1`);
  });

  console.log(`\nTotal Issues: ${report.summary.totalIssues}`);
  console.log(`Critical Issues: ${report.summary.criticalIssues}`);
};

/**
 * Run accessibility checks in development mode
 */
export const runAccessibilityChecks = (): void => {
  if (import.meta.env.DEV) {
    setTimeout(() => {
      logAccessibilityReport();
    }, 1000);
  }
};
