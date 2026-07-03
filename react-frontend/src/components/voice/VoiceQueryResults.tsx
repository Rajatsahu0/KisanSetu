import React from 'react';
import { TrendingUp, MapPin, Calendar, DollarSign, Play, Square } from 'lucide-react';
import { useLanguage } from '@/contexts/LanguageContext';
import { VoiceQueryResponse } from '@/types';
import { AudioPlayer } from './AudioPlayer';
import { ToastContainer } from '@/components/notifications/ToastContainer';
import type { ToastNotification } from '@/components/notifications/Toast';

// Enterprise markdown renderer with smart section detection
// Handles: markdown syntax (###, **, -, 1.) AND plain text with "heading:" patterns
const simpleMarkdownToHtml = (text: string): string => {
  if (!text) return '';
  
  const lines = text.split('\n').map(l => l.trim()).filter(l => l.length > 0);
  const html: string[] = [];
  let inList = false;
  let listType = '';

  const closeList = () => {
    if (inList) { html.push(`</${listType}>`); inList = false; }
  };

  const openList = (type: string) => {
    if (inList && listType !== type) closeList();
    if (!inList) {
      html.push(`<${type} class="my-0.5 ml-4 ${type === 'ul' ? 'list-disc' : 'list-decimal'}">`); 
      inList = true; listType = type;
    }
  };

  for (let i = 0; i < lines.length; i++) {
    const line = lines[i];
    const fmt = applyInlineFormatting(line);

    // Markdown headers: ### text
    const headerMatch = fmt.match(/^(#{1,4})\s+(.+)/);
    if (headerMatch) {
      closeList();
      html.push(`<div class="font-bold mt-3 mb-0.5">${headerMatch[2]}</div>`);
      continue;
    }

    // Markdown bullet: - text
    const bulletMatch = fmt.match(/^[-*+]\s+(.+)/);
    if (bulletMatch) {
      openList('ul');
      html.push(`<li class="mb-0.5">${bulletMatch[1]}</li>`);
      continue;
    }

    // Markdown numbered: 1. text
    const numMatch = fmt.match(/^(\d+)\.\s+(.+)/);
    if (numMatch) {
      openList('ol');
      html.push(`<li class="mb-0.5">${numMatch[2]}</li>`);
      continue;
    }

    // Smart detection: line ending with ":" = section header
    // Next lines that don't end with ":" are its sub-points
    if (fmt.endsWith(':') || fmt.endsWith(':।')) {
      closeList();
      html.push(`<div class="font-semibold mt-2 mb-0">${fmt}</div>`);
      continue;
    }

    // Check if previous line was a section header (ends with ":")
    // If so, this line is a sub-point — render as indented bullet
    const prevLine = i > 0 ? lines[i - 1].trim() : '';
    const prevWasHeader = prevLine.endsWith(':') || prevLine.endsWith(':।');
    const prevWasSubPoint = i > 0 && html.length > 0 && inList;
    
    if (prevWasHeader || prevWasSubPoint) {
      // Check if this line looks like a sub-point (starts with a term followed by ":")
      const subPointMatch = fmt.match(/^(.+?:\s*.+)/);
      if (subPointMatch && !fmt.endsWith(':')) {
        openList('ul');
        html.push(`<li class="mb-0.5">${fmt}</li>`);
        continue;
      }
    }

    // Regular paragraph
    closeList();
    html.push(`<p class="mb-1.5">${fmt}</p>`);
  }

  closeList();
  return html.join('');
};

const applyInlineFormatting = (text: string): string => {
  return text
    .replace(/\*\*(.+?)\*\*/g, '<strong>$1</strong>')
    .replace(/__(.+?)__/g, '<strong>$1</strong>')
    .replace(/(?<!\*)\*(?!\*)(.+?)(?<!\*)\*(?!\*)/g, '<em>$1</em>')
    .replace(/`([^`]+)`/g, '<code class="bg-gray-100 dark:bg-gray-700 px-1 rounded text-sm">$1</code>');
};

interface VoiceQueryResultsProps {
  result: VoiceQueryResponse;
  onAddToFavorites?: () => void;
}

export const VoiceQueryResults: React.FC<VoiceQueryResultsProps> = ({
  result,
  onAddToFavorites
}) => {
  const { t } = useLanguage();
  const [isSpeaking, setIsSpeaking] = React.useState(false);
  const speechSynthesisRef = React.useRef<SpeechSynthesisUtterance | null>(null);
  const [toasts, setToasts] = React.useState<ToastNotification[]>([]);

  const showToast = React.useCallback((type: ToastNotification['type'], title: string, message: string) => {
    const id = Date.now().toString();
    setToasts(prev => [...prev, { id, type, title, message, duration: 4000 }]);
  }, []);

  const dismissToast = React.useCallback((id: string) => {
    setToasts(prev => prev.filter(t => t.id !== id));
  }, []);

  const [voicesLoaded, setVoicesLoaded] = React.useState(false);

  // Load voices on mount (some browsers need this)
  React.useEffect(() => {
    if ('speechSynthesis' in window) {
      const loadVoices = () => {
        const voices = window.speechSynthesis.getVoices();
        if (voices.length > 0) setVoicesLoaded(true);
      };

      // Chrome loads voices asynchronously
      if (window.speechSynthesis.onvoiceschanged !== undefined) {
        window.speechSynthesis.onvoiceschanged = loadVoices;
      }
      
      loadVoices();
    }
  }, []);

  // Stop TTS when result changes (user selects different history item or new query)
  React.useEffect(() => {
    return () => {
      if ('speechSynthesis' in window) {
        window.speechSynthesis.cancel();
      }
      setIsSpeaking(false);
      speechSynthesisRef.current = null;
    };
  }, [result]);

  // Cleanup speech synthesis on unmount + handle tab visibility
  React.useEffect(() => {
    const handleVisibilityChange = () => {
      if (document.hidden && isSpeaking) {
        window.speechSynthesis.cancel();
        setIsSpeaking(false);
        speechSynthesisRef.current = null;
      }
    };
    document.addEventListener('visibilitychange', handleVisibilityChange);

    return () => {
      document.removeEventListener('visibilitychange', handleVisibilityChange);
      if ('speechSynthesis' in window) {
        window.speechSynthesis.cancel();
      }
      speechSynthesisRef.current = null;
    };
  }, [isSpeaking]);

  // Clean markdown formatting from text for speech
  const cleanTextForSpeech = (text: string, dialect?: string): string => {
    if (!text) return '';
    return text
      .replace(/^#{1,6}\s+/gm, '')           // ### headers
      .replace(/(\*\*|__)(.*?)\1/g, '$2')     // **bold**
      .replace(/(\*|_)(.*?)\1/g, '$2')         // *italic*
      .replace(/~~(.*?)~~/g, '$1')             // ~~strike~~
      .replace(/`([^`]+)`/g, '$1')             // `code`
      .replace(/```[\s\S]*?```/g, '')          // ```code blocks```
      .replace(/\[([^\]]+)\]\([^)]+\)/g, '$1') // [link](url)
      .replace(/!\[([^\]]*)\]\([^)]+\)/g, '')  // ![img](url)
      .replace(/^[-*_]{3,}\s*$/gm, '')         // --- hr
      .replace(/^>\s+/gm, '')                  // > blockquote
      .replace(/^[\s]*[-*+]\s+/gm, '')         // - bullet
      .replace(/^[\s]*\d+\.\s+/gm, '')         // 1. numbered
      .replace(/^\s+/gm, '')                   // leading whitespace
      .replace(/\n{3,}/g, '\n\n')              // collapse blanks
      .trim();
  };

  // Detect the actual language of response text by checking for script/patterns
  const detectResponseLanguage = (text: string): string => {
    if (!text) return 'en-IN';

    // Check for native scripts first (most reliable)
    const scriptPatterns: { pattern: RegExp; lang: string }[] = [
      { pattern: /[\u0900-\u097F]/, lang: 'hi-IN' },   // Devanagari (Hindi/Marathi)
      { pattern: /[\u0A00-\u0A7F]/, lang: 'pa-IN' },   // Gurmukhi (Punjabi)
      { pattern: /[\u0A80-\u0AFF]/, lang: 'gu-IN' },   // Gujarati
      { pattern: /[\u0B80-\u0BFF]/, lang: 'ta-IN' },   // Tamil
      { pattern: /[\u0C00-\u0C7F]/, lang: 'te-IN' },   // Telugu
      { pattern: /[\u0C80-\u0CFF]/, lang: 'kn-IN' },   // Kannada
      { pattern: /[\u0D00-\u0D7F]/, lang: 'ml-IN' },   // Malayalam
      { pattern: /[\u0980-\u09FF]/, lang: 'bn-IN' },   // Bengali
    ];

    for (const { pattern, lang } of scriptPatterns) {
      if (pattern.test(text)) return lang;
    }

    // Detect Romanized Indian languages (Hinglish, etc.)
    // Common Hindi/Hinglish words in Roman script
    const hindiRomanWords = /\b(hai|hain|ka|ki|ke|ko|mein|se|aur|ya|nahi|kya|yeh|woh|hota|kare|karein|iska|uska|jaise|lekin|agar|toh|bhi|bahut|accha|achha|theek|sahi|galat|hona|karna|rakhein|chahiye|aap|tum|hum|unka|inka|sabhi|kuch|bahut|zyada|kam|pehle|baad|saath|liye|wala|wali|wale|matil|matla|kheti|fasal|beej|paani|mitti|khet|kisaan|sabzi|phal|phool)\b/gi;
    const hindiMatches = text.match(hindiRomanWords) || [];
    const words = text.split(/\s+/).length;
    const hindiRatio = hindiMatches.length / words;

    // If more than 15% of words are Hindi/Hinglish, use Hindi voice
    if (hindiRatio > 0.15) return 'hi-IN';

    return 'en-IN';
  };

  const handleTextToSpeech = () => {
    if (!result.responseText) return;

    if (!('speechSynthesis' in window)) {
      showToast('warning', t('voice.unsupported', 'Not Supported'), t('voice.ttsNotSupported', 'Text-to-speech is not supported in your browser'));
      return;
    }

    // If already speaking, stop immediately
    if (isSpeaking) {
      window.speechSynthesis.cancel();
      setIsSpeaking(false);
      speechSynthesisRef.current = null;
      return;
    }

    // Set speaking state IMMEDIATELY so button changes to Stop
    setIsSpeaking(true);

    const cleanedText = cleanTextForSpeech(result.responseText, result.dialect);
    if (!cleanedText) {
      setIsSpeaking(false);
      return;
    }

    // Get voices SYNCHRONOUSLY — no await, preserves iOS Safari gesture chain
    let voices = window.speechSynthesis.getVoices();

    // Auto-detect response language
    const detectedLang = detectResponseLanguage(cleanedText);
    const marathiWords = /\b(आहे|नाही|करा|करावे|आणि|किंवा|असते|होते|म्हणून|तुम्ही|आम्ही|त्यांचे)\b/;
    const effectiveLang = (detectedLang === 'hi-IN' && marathiWords.test(cleanedText)) ? 'mr-IN' : detectedLang;

    const selectVoice = (v: SpeechSynthesisVoice[]) =>
      v.find(x => x.lang === effectiveLang)
      || v.find(x => x.lang.startsWith(effectiveLang.split('-')[0]))
      || v.find(x => x.lang.startsWith('en-IN'))
      || v.find(x => x.lang.startsWith('en'))
      || v[0] || null;

    // Cancel any pending speech SYNCHRONOUSLY
    window.speechSynthesis.cancel();

    const chunks = splitTextIntoChunks(cleanedText, 200);
    let currentChunk = 0;
    let stopped = false;

    const speakNext = () => {
      if (stopped || currentChunk >= chunks.length) {
        setIsSpeaking(false);
        speechSynthesisRef.current = null;
        return;
      }

      // Re-fetch voices each chunk (Chrome loads them async, may be available now)
      if (voices.length === 0) voices = window.speechSynthesis.getVoices();
      const selectedVoice = selectVoice(voices);

      const utterance = new SpeechSynthesisUtterance(chunks[currentChunk]);
      speechSynthesisRef.current = utterance;

      if (selectedVoice) {
        utterance.voice = selectedVoice;
        utterance.lang = selectedVoice.lang;
      } else {
        utterance.lang = effectiveLang;
      }

      utterance.rate = 0.95;
      utterance.pitch = 1.0;
      utterance.volume = 1.0;

      utterance.onend = () => {
        currentChunk++;
        speakNext();
      };

      utterance.onerror = (event) => {
        if (event.error === 'interrupted' || event.error === 'canceled') {
          stopped = true;
        } else {
          console.error('Speech synthesis error:', event.error);
        }
        setIsSpeaking(false);
        speechSynthesisRef.current = null;
      };

      window.speechSynthesis.speak(utterance);
    };

    // Use requestAnimationFrame so React re-renders the button to "Stop" BEFORE speaking starts
    // This fixes the button state lag on both desktop and mobile
    requestAnimationFrame(() => {
      requestAnimationFrame(() => {
        speakNext();
      });
    });
  };

  // Split text into chunks at sentence boundaries (।, ., !, ?, newline)
  // Chrome SpeechSynthesis has a ~15s/~200 char limit per utterance
  const splitTextIntoChunks = (text: string, maxLen: number): string[] => {
    const chunks: string[] = [];
    // Split on Hindi purna viram (।), period, newline
    const sentences = text.split(/(?<=[।\.!\?\n])\s*/).filter(s => s.trim());
    let current = '';

    for (const sentence of sentences) {
      if ((current + ' ' + sentence).length > maxLen && current) {
        chunks.push(current.trim());
        current = sentence;
      } else {
        current = current ? current + ' ' + sentence : sentence;
      }
    }
    if (current.trim()) chunks.push(current.trim());
    return chunks.length > 0 ? chunks : [text];
  };

  const formatPrice = (price: number): string => {
    return new Intl.NumberFormat('en-IN', {
      style: 'currency',
      currency: 'INR',
      maximumFractionDigits: 0
    }).format(price);
  };

  const formatDate = (dateString: string): string => {
    try {
      const date = new Date(dateString);
      
      // Check if date is valid
      if (isNaN(date.getTime())) {
        return t('voice.unknownDate', 'Unknown date');
      }
      
      return new Intl.DateTimeFormat('en-IN', {
        day: 'numeric',
        month: 'short',
        year: 'numeric'
      }).format(date);
    } catch (error) {
      console.error('Error formatting date:', dateString, error);
      return t('voice.unknownDate', 'Unknown date');
    }
  };

  return (
    <div className="space-y-6">
      <ToastContainer notifications={toasts} onDismiss={dismissToast} position="top-right" />
      {/* Transcription */}
      <div className="bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 rounded-lg p-4">
        <h3 className="text-sm font-medium text-blue-900 dark:text-blue-100 mb-2">
          {t('voice.yourQuery', 'Your Query')}
        </h3>
        <p className="text-blue-800 dark:text-blue-200 break-words">
          "{result.transcription}"
        </p>
        <div className="mt-2 flex items-center justify-between">
          <span className="text-xs text-blue-600 dark:text-blue-400">
            {t('voice.confidence', 'Confidence')}: {Math.round(result.confidence * 100)}%
          </span>
          <span className="text-xs text-blue-600 dark:text-blue-400">
            {t('voice.dialect', 'Dialect')}: {result.dialect}
          </span>
        </div>
      </div>

      {/* Audio Response - Only show if valid URL exists */}
      {result.audioResponseUrl && 
       typeof result.audioResponseUrl === 'string' && 
       result.audioResponseUrl.trim() !== '' && 
       result.audioResponseUrl.startsWith('http') && (
        <div>
          <h3 className="text-sm font-medium text-gray-900 dark:text-gray-100 mb-3">
            {t('voice.audioResponse', 'Audio Response')}
          </h3>
          <AudioPlayer 
            audioUrl={result.audioResponseUrl} 
            autoPlay={false}
            hideOnError={true}
          />
        </div>
      )}

      {/* AI Response Text (for general questions) */}
      {result.responseText && (
        <div className="bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-800 rounded-lg p-4">
          <div className="flex items-center justify-between mb-2">
            <h3 className="text-sm font-medium text-green-900 dark:text-green-100">
              {t('voice.aiResponse', 'AI Response')}
            </h3>
            <button
              onClick={handleTextToSpeech}
              className="flex items-center gap-1 px-3 py-1 text-sm text-green-700 dark:text-green-300 hover:bg-green-100 dark:hover:bg-green-800/50 rounded-md transition-colors"
              aria-label={isSpeaking ? t('voice.stopSpeech', 'Stop speech') : t('voice.playSpeech', 'Play speech')}
            >
              {isSpeaking ? (
                <>
                  <Square className="w-4 h-4" />
                  <span>{t('voice.stop', 'Stop')}</span>
                </>
              ) : (
                <>
                  <Play className="w-4 h-4" />
                  <span>{t('voice.play', 'Play')}</span>
                </>
              )}
            </button>
          </div>
          <div className="text-green-800 dark:text-green-200 text-sm leading-relaxed break-words overflow-hidden"
            dangerouslySetInnerHTML={{ __html: simpleMarkdownToHtml(result.responseText) }}
          />
        </div>
      )}

      {/* Market Prices */}
      {result.prices && result.prices.length > 0 && (
        <div>
          <div className="flex items-center justify-between mb-3">
            <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100">
              {t('voice.marketPrices', 'Market Prices')}
            </h3>
            {onAddToFavorites && (
              <button
                onClick={onAddToFavorites}
                className="text-sm text-green-600 dark:text-green-400 hover:text-green-700 dark:hover:text-green-300"
              >
                {t('voice.addToFavorites', 'Add to Favorites')}
              </button>
            )}
          </div>

          <div className="grid gap-4 sm:grid-cols-2">
            {result.prices.map((price, index) => (
              <div
                key={index}
                className="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg p-4 hover:shadow-md transition-shadow"
              >
                {/* Commodity Name */}
                <div className="flex items-start justify-between mb-3">
                  <h4 className="text-lg font-semibold text-gray-900 dark:text-gray-100">
                    {price.commodity}
                  </h4>
                  <div className="flex items-center text-green-600 dark:text-green-400">
                    <TrendingUp className="w-4 h-4 mr-1" />
                  </div>
                </div>

                {/* Price */}
                <div className="mb-3">
                  <div className="flex items-baseline">
                    <span className="text-3xl font-bold text-gray-900 dark:text-gray-100">
                      {formatPrice(price.price)}
                    </span>
                    <span className="ml-2 text-sm text-gray-500 dark:text-gray-400">
                      / {price.unit}
                    </span>
                  </div>
                </div>

                {/* Market Info */}
                <div className="space-y-2">
                  <div className="flex items-center text-sm text-gray-600 dark:text-gray-400">
                    <MapPin className="w-4 h-4 mr-2" />
                    <span>{price.market}</span>
                  </div>
                  <div className="flex items-center text-sm text-gray-600 dark:text-gray-400">
                    <Calendar className="w-4 h-4 mr-2" />
                    <span>{formatDate(price.date)}</span>
                  </div>
                  <div className="flex items-center text-sm text-gray-600 dark:text-gray-400">
                    <DollarSign className="w-4 h-4 mr-2" />
                    <span>{t('voice.source', 'Source')}: {price.source}</span>
                  </div>
                </div>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* No Results */}
      {(!result.prices || result.prices.length === 0) && !result.responseText && (
        <div className="bg-yellow-50 dark:bg-yellow-900/20 border border-yellow-200 dark:border-yellow-800 rounded-lg p-4">
          <p className="text-yellow-800 dark:text-yellow-200">
            {t('voice.noPricesFound', 'No market prices found for your query. Please try again with a different query.')}
          </p>
        </div>
      )}
    </div>
  );
};
