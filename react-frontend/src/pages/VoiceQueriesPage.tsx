import React, { useState, useEffect, useRef } from 'react';
import { Mic, AlertCircle, Wifi, WifiOff, Square, Send } from 'lucide-react';
import { useLanguage } from '@/contexts/LanguageContext';
import { VoiceRecorder } from '@/components/voice/VoiceRecorder';
import type { VoiceRecorderHandle } from '@/components/voice/VoiceRecorder';

import { VoiceQueryResults } from '@/components/voice/VoiceQueryResults';
import { QueryHistory } from '@/components/voice/QueryHistory';
import { voiceQueryService, VoiceQueryHistoryItem } from '@/services/voiceQueryService';
import { uploadQueueService } from '@/services/uploadQueueService';
import { useNotifications } from '@/hooks/useNotifications';
import { useTrialQuota } from '@/hooks/useTrialQuota';
import { TrialLimitModal } from '@/components/auth/TrialLimitModal';
import { Dialect, VoiceQueryResponse } from '@/types';
import { LoadingSpinner } from '@/components/common/LoadingSpinner';

interface QueryHistoryItem {
  id: string;
  query: string;
  response: VoiceQueryResponse;
  timestamp: string;
  isFavorite: boolean;
}

export const VoiceQueriesPage: React.FC = () => {
  const { t, currentLanguage } = useLanguage();
  const { showNotification } = useNotifications();
  
  const [availableDialects, setAvailableDialects] = useState<Dialect[]>([]);
  const [selectedDialect, setSelectedDialect] = useState<string>('hi-IN');
  const [showDialectPicker, setShowDialectPicker] = useState(false);
  const [isProcessing, setIsProcessing] = useState(false);
  const [currentResult, setCurrentResult] = useState<VoiceQueryResponse | null>(null);
  const [queryHistory, setQueryHistory] = useState<QueryHistoryItem[]>([]);
  const [isOnline, setIsOnline] = useState(navigator.onLine);
  const [isLoadingDialects, setIsLoadingDialects] = useState(true);
  const [isLoadingHistory, setIsLoadingHistory] = useState(true);
  const [lastTranscription, setLastTranscription] = useState('');
  const [lastConfidence, setLastConfidence] = useState(0);
  const [liveTranscript, setLiveTranscript] = useState('');
  const [isRecordingActive, setIsRecordingActive] = useState(false);
  const { canUseVoice, voiceRemaining, voiceLimit, consumeVoice, isAuthenticated } = useTrialQuota();
  const [showTrialModal, setShowTrialModal] = useState(false);
  const voiceRecorderRef = useRef<VoiceRecorderHandle>(null);
  // Refs for Speech API transcript — React state is async/batched,
  // so by the time onRecordingComplete reads state, it may not have updated.
  // Refs update synchronously and are always current.
  const speechTranscriptRef = useRef('');
  const speechConfidenceRef = useRef(0);

  // Auto-sync voice dialect with page language
  const langToDialect: Record<string, string> = {
    en: 'en-IN', hi: 'hi-IN', pa: 'pa-IN', bn: 'bn-IN',
    mr: 'mr-IN', te: 'te-IN', ta: 'ta-IN', gu: 'gu-IN',
    kn: 'kn-IN', ml: 'ml-IN',
  };

  useEffect(() => {
    const mapped = langToDialect[currentLanguage.code] || 'hi-IN';
    if (mapped !== selectedDialect) {
      setSelectedDialect(mapped);
    }
  }, [currentLanguage.code]);

  // Stop TTS when leaving the page
  useEffect(() => {
    return () => {
      if ('speechSynthesis' in window) {
        window.speechSynthesis.cancel();
      }
    };
  }, []);

  // Load dialects on mount
  useEffect(() => {
    loadDialects();
    loadHistory();

    // Listen for online/offline events
    const handleOnline = () => {
      setIsOnline(true);
      loadHistory(); // Refresh history when coming back online
    };
    const handleOffline = () => setIsOnline(false);

    window.addEventListener('online', handleOnline);
    window.addEventListener('offline', handleOffline);

    return () => {
      window.removeEventListener('online', handleOnline);
      window.removeEventListener('offline', handleOffline);
    };
  }, []);

  const loadDialects = async () => {
    try {
      setIsLoadingDialects(true);
      const dialects = await voiceQueryService.getSupportedDialects();
      setAvailableDialects(dialects);
    } catch (error) {
      console.error('Failed to load dialects:', error);
      showNotification({
        type: 'error',
        title: t('voice.error', 'Error'),
        message: t('voice.dialectLoadError', 'Failed to load dialects. Using defaults.')
      });
    } finally {
      setIsLoadingDialects(false);
    }
  };

  const loadHistory = async () => {
    try {
      setIsLoadingHistory(true);

      // Don't load history for unauthenticated users — prevents data leakage
      if (!isAuthenticated) {
        setQueryHistory([]);
        return;
      }

      const history = await voiceQueryService.getHistory(50);
      
      // Convert backend format to frontend format
      const formattedHistory: QueryHistoryItem[] = history.map(item => ({
        id: item.queryId,
        query: item.transcription,
        response: {
          transcription: item.transcription,
          responseText: item.responseText,
          audioResponseUrl: '', // Will be generated on demand
          confidence: item.confidence,
          dialect: item.dialect,
          prices: item.prices
        },
        timestamp: item.timestamp,
        isFavorite: item.isFavorite
      }));

      setQueryHistory(formattedHistory);
    } catch (error) {
      console.error('Failed to load history:', error);
      // Don't fall back to localStorage — it may contain another user's data
      setQueryHistory([]);
    } finally {
      setIsLoadingHistory(false);
    }
  };

  const handleRecordingComplete = async (audioBlob: Blob) => {
    try {
      // Check trial quota for unauthenticated users
      if (!consumeVoice()) {
        setShowTrialModal(true);
        return;
      }

      // Capture transcription from REF (not state — state is async/batched)
      const transcriptionText = speechTranscriptRef.current || lastTranscription || liveTranscript;
      const transcriptionConfidence = speechConfidenceRef.current || lastConfidence || 0.85;

      setIsProcessing(true);
      setCurrentResult(null);
      setIsRecordingActive(false);

      if (!isOnline) {
        const audioFile = await voiceQueryService.convertAudioFormat(audioBlob, 'mp3');
        uploadQueueService.addToQueue(audioFile, '/api/voice-query', 'current-user', 'high');
        showNotification({ type: 'info', title: t('voice.offline', 'Offline'), message: t('voice.queuedForSync', 'Queued for processing when online') });
        setIsProcessing(false);
        return;
      }

      if (transcriptionText) {
        console.log('[VoiceQuery] FAST text path — Web Speech API transcript:', transcriptionText, 'confidence:', transcriptionConfidence);
        const result = await voiceQueryService.processTextQuery(
          transcriptionText, selectedDialect, transcriptionConfidence
        );
        setCurrentResult(result);

        // Optimistic update — add to local history without reloading page
        const newItem: QueryHistoryItem = {
          id: Date.now().toString(),
          query: transcriptionText,
          response: result,
          timestamp: new Date().toISOString(),
          isFavorite: false
        };
        setQueryHistory(prev => [newItem, ...prev]);
      } else {
        console.log('[VoiceQuery] SLOW audio fallback — Web Speech API returned nothing');
        showNotification({ type: 'info', title: 'Audio Processing', message: 'Speech recognition unavailable, using server transcription (slower)' });
        const audioFile = await voiceQueryService.convertAudioFormat(audioBlob, 'mp3');
        const validation = voiceQueryService.validateAudioFile(audioFile);
        if (!validation.isValid) {
          showNotification({ type: 'error', title: t('voice.error', 'Error'), message: validation.error || 'Invalid audio' });
          setIsProcessing(false);
          return;
        }
        const result = await voiceQueryService.processVoiceQuery(audioFile, selectedDialect);
        setCurrentResult(result);

        // Optimistic update for audio path too
        const newItem: QueryHistoryItem = {
          id: Date.now().toString(),
          query: result.transcription || 'Voice query',
          response: result,
          timestamp: new Date().toISOString(),
          isFavorite: false
        };
        setQueryHistory(prev => [newItem, ...prev]);
      }

      // Clear transcription state and refs after use
      setLastTranscription('');
      setLastConfidence(0);
      setLiveTranscript('');
      speechTranscriptRef.current = '';
      speechConfidenceRef.current = 0;

      showNotification({ type: 'success', title: t('voice.success', 'Success'), message: t('voice.queryProcessed', 'Query processed successfully') });
    } catch (error) {
      console.error('Voice query failed:', error);
      showNotification({ type: 'error', title: t('voice.error', 'Error'), message: error instanceof Error ? error.message : 'Failed to process query' });
    } finally {
      setIsProcessing(false);
    }
  };

  const handleRecordingError = (error: string) => {
    showNotification({
      type: 'error',
      title: t('voice.error', 'Error'),
      message: error
    });
  };

  const handleSelectQuery = (item: QueryHistoryItem) => {
    setCurrentResult(item.response);
  };

  const handleToggleFavorite = async (id: string) => {
    try {
      const item = queryHistory.find(q => q.id === id);
      if (!item) return;

      const newFavoriteStatus = !item.isFavorite;
      
      // Optimistic update
      const updatedHistory = queryHistory.map(q =>
        q.id === id ? { ...q, isFavorite: newFavoriteStatus } : q
      );
      setQueryHistory(updatedHistory);

      // Update backend
      await voiceQueryService.toggleFavorite(id, newFavoriteStatus);
    } catch (error) {
      console.error('Failed to toggle favorite:', error);
      // Revert on error
      await loadHistory();
      showNotification({
        type: 'error',
        title: t('voice.error', 'Error'),
        message: t('voice.favoriteError', 'Failed to update favorite status')
      });
    }
  };

  const handleDeleteQuery = async (id: string) => {
    try {
      // Optimistic update
      const updatedHistory = queryHistory.filter(item => item.id !== id);
      setQueryHistory(updatedHistory);

      // Clear current result if it's the deleted query
      if (currentResult && queryHistory.find(item => item.id === id)?.response === currentResult) {
        setCurrentResult(null);
      }

      // Delete from backend
      await voiceQueryService.deleteQuery(id);

      showNotification({
        type: 'success',
        title: t('voice.deleted', 'Deleted'),
        message: t('voice.queryDeleted', 'Query deleted from history')
      });
    } catch (error) {
      console.error('Failed to delete query:', error);
      // Revert on error
      await loadHistory();
      showNotification({
        type: 'error',
        title: t('voice.error', 'Error'),
        message: t('voice.deleteError', 'Failed to delete query')
      });
    }
  };

  const handleAddToFavorites = () => {
    if (!currentResult) return;

    const currentQuery = queryHistory.find(item => item.response === currentResult);
    if (currentQuery && !currentQuery.isFavorite) {
      handleToggleFavorite(currentQuery.id);
      showNotification({
        type: 'success',
        title: t('voice.added', 'Added'),
        message: t('voice.addedToFavorites', 'Added to favorites')
      });
    }
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="bg-white dark:bg-gray-800 rounded-lg shadow-md p-4 sm:p-6">
        <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3 mb-4">
          <div className="flex items-center space-x-3 min-w-0">
            <div className="w-10 h-10 sm:w-12 sm:h-12 bg-green-100 dark:bg-green-900/30 rounded-lg flex items-center justify-center flex-shrink-0">
              <Mic className="w-5 h-5 sm:w-6 sm:h-6 text-green-600 dark:text-green-400" />
            </div>
            <div className="min-w-0">
              <h1 className="text-xl sm:text-3xl font-bold text-gray-900 dark:text-gray-100 truncate">
                {t('nav.voiceQueries', 'Voice Queries')}
              </h1>
              <p className="text-xs sm:text-sm text-gray-600 dark:text-gray-400 truncate">
                {t('voice.subtitle', 'Ask about market prices using your voice')}
              </p>
            </div>
          </div>

          {/* Online/Offline Indicator */}
          <div className={`flex items-center space-x-2 px-3 py-1.5 rounded-lg flex-shrink-0 ${
            isOnline
              ? 'bg-green-100 dark:bg-green-900/30 text-green-700 dark:text-green-300'
              : 'bg-red-100 dark:bg-red-900/30 text-red-700 dark:text-red-300'
          }`}>
            {isOnline ? (
              <>
                <Wifi className="w-4 h-4" />
                <span className="text-sm font-medium">{t('voice.online', 'Online')}</span>
              </>
            ) : (
              <>
                <WifiOff className="w-4 h-4" />
                <span className="text-sm font-medium">{t('voice.offline', 'Offline')}</span>
              </>
            )}
          </div>
        </div>

        {/* Offline Warning */}
        {!isOnline && (
          <div className="bg-yellow-50 dark:bg-yellow-900/20 border border-yellow-200 dark:border-yellow-800 rounded-lg p-4 flex items-start space-x-3">
            <AlertCircle className="w-5 h-5 text-yellow-600 dark:text-yellow-400 flex-shrink-0 mt-0.5" />
            <div>
              <p className="text-sm text-yellow-800 dark:text-yellow-200">
                {t('voice.offlineWarning', 'You are offline. Voice queries will be queued and processed when connection is restored.')}
              </p>
            </div>
          </div>
        )}
      </div>

      {/* Trial Limit Modal */}
      {showTrialModal && <TrialLimitModal feature="voice" limit={voiceLimit} onClose={() => setShowTrialModal(false)} />}

      {/* Trial Banner for guests */}
      {!isAuthenticated && (
        <div className="bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 rounded-lg p-3 flex items-center justify-between">
          <span className="text-sm text-blue-700 dark:text-blue-300">
            🎁 Free trial: <span className="font-bold">{voiceRemaining}</span> of {voiceLimit} queries remaining
          </span>
          <a href="/register" className="text-xs font-medium text-blue-600 dark:text-blue-400 hover:underline">Register for unlimited →</a>
        </div>
      )}

      {/* Main Content */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6 items-start">
        {/* Left Column - Recording Interface */}
        <div className="lg:col-span-2 space-y-6">
          {/* Dialect — auto-synced with page language, expandable for manual override */}
          <div className="bg-white dark:bg-gray-800 rounded-lg shadow-md overflow-hidden">
            <button
              onClick={() => setShowDialectPicker(!showDialectPicker)}
              className="w-full px-4 py-3 flex items-center justify-between text-left hover:bg-gray-50 dark:hover:bg-gray-700/50 transition-colors"
            >
              <div className="flex items-center gap-2">
                <span className="text-sm text-gray-500 dark:text-gray-400">{t('voice.speakingIn', 'Speaking in')}:</span>
                <span className="text-sm font-medium text-gray-900 dark:text-gray-100">
                  {availableDialects.find(d => d.code === selectedDialect)?.name || selectedDialect}
                </span>
                <span className="text-xs text-green-600 dark:text-green-400">({t('voice.autoSynced', 'auto-synced')})</span>
              </div>
              <span className="text-xs text-gray-400 dark:text-gray-500">{showDialectPicker ? '▲' : '▼'} {t('voice.changeDialect', 'Change')}</span>
            </button>
            {showDialectPicker && !isLoadingDialects && (
              <div className="border-t border-gray-200 dark:border-gray-700 max-h-64 overflow-y-auto">
                {availableDialects.map((dialect) => (
                  <button
                    key={dialect.code}
                    onClick={() => { setSelectedDialect(dialect.code); setShowDialectPicker(false); }}
                    className={`w-full px-4 py-3 flex items-center justify-between text-left transition-colors ${
                      dialect.code === selectedDialect
                        ? 'bg-green-50 dark:bg-green-900/20'
                        : 'hover:bg-gray-50 dark:hover:bg-gray-700/50'
                    }`}
                  >
                    <div>
                      <span className="text-sm font-medium text-gray-900 dark:text-gray-100">{dialect.name}</span>
                      <span className="text-xs text-gray-500 dark:text-gray-400 ml-2">{dialect.nativeName}</span>
                    </div>
                    {dialect.code === selectedDialect && <span className="text-green-500">✓</span>}
                  </button>
                ))}
              </div>
            )}
          </div>

          {/* Voice Recorder with Web Speech API */}
          <VoiceRecorder
            ref={voiceRecorderRef}
            maxDuration={60}
            onRecordingComplete={handleRecordingComplete}
            onError={handleRecordingError}
            dialect={selectedDialect}
            onRecordingStateChange={(recording) => setIsRecordingActive(recording)}
            onTranscript={(text: string, confidence: number) => {
              setLastTranscription(text);
              setLastConfidence(confidence);
              setLiveTranscript(text);
              speechTranscriptRef.current = text;
              speechConfidenceRef.current = confidence;
            }}
            onInterimTranscript={(text: string) => {
              setLiveTranscript(text);
              setIsRecordingActive(true);
              // Also save interim as fallback — Speech API may not fire 'final'
              // before MediaRecorder.onstop triggers onRecordingComplete
              if (text.length > 0) {
                setLastTranscription(text);
                setLastConfidence(0.85);
                speechTranscriptRef.current = text;
                speechConfidenceRef.current = 0.85;
              }
            }}
          />

          {/* Text Input + Unified Send/Stop Button */}
          <div className="bg-white dark:bg-gray-800 rounded-lg shadow-md p-4">
            <form onSubmit={async (e) => {
              e.preventDefault();

              // If recording, stop recording — onRecordingComplete will handle sending
              if (isRecordingActive) {
                voiceRecorderRef.current?.stopRecording();
                return;
              }

              const input = (e.target as HTMLFormElement).querySelector('input') as HTMLInputElement;
              const text = input?.value?.trim();
              if (!text || isProcessing) return;
              if (!consumeVoice()) { setShowTrialModal(true); return; }
              setIsProcessing(true);
              setCurrentResult(null);
              setLiveTranscript(text);
              try {
                const result = await voiceQueryService.processTextQuery(text, selectedDialect, 0.95);
                setCurrentResult(result);
                input.value = '';
                const newItem: QueryHistoryItem = {
                  id: Date.now().toString(),
                  query: text,
                  response: result,
                  timestamp: new Date().toISOString(),
                  isFavorite: false
                };
                setQueryHistory(prev => [newItem, ...prev]);
              } catch (error) {
                showNotification({ type: 'error', title: t('voice.error', 'Error'), message: error instanceof Error ? error.message : 'Failed' });
              } finally {
                setIsProcessing(false);
              }
            }} className="flex flex-col sm:flex-row gap-2">
              <input
                type="text"
                placeholder={isRecordingActive
                  ? t('voice.recordingHint', '🎤 Recording... press Send to stop & submit')
                  : t('voice.typeQuestion', 'अपना प्रश्न यहाँ टाइप करें / Type your question here...')}
                className="flex-1 min-w-0 px-3 sm:px-4 py-3 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100 focus:ring-2 focus:ring-green-500 focus:border-transparent text-sm"
                disabled={isProcessing || isRecordingActive}
              />
              <button
                type="submit"
                disabled={isProcessing}
                className={`px-6 py-3 rounded-lg font-medium disabled:opacity-50 disabled:cursor-not-allowed transition-colors flex-shrink-0 flex items-center justify-center gap-2 ${
                  isRecordingActive
                    ? 'bg-red-500 hover:bg-red-600 text-white animate-pulse'
                    : 'bg-green-500 hover:bg-green-600 text-white'
                }`}
              >
                {isProcessing ? (
                  t('voice.processing', 'Processing...')
                ) : isRecordingActive ? (
                  <><Square className="w-4 h-4" /> {t('voice.stopSend', 'रोकें और भेजें / Stop & Send')}</>
                ) : (
                  <><Send className="w-4 h-4" /> {t('voice.send', 'भेजें / Send')}</>
                )}
              </button>
            </form>
          </div>

          {/* Live Transcription Preview — shows while recording */}
          {liveTranscript && (
            <div className="bg-blue-50 dark:bg-blue-900/20 rounded-lg p-4 border border-blue-200 dark:border-blue-800">
              <p className="text-sm text-blue-600 dark:text-blue-400 font-medium mb-1">
                🎤 {isRecordingActive ? t('voice.listening', 'Listening...') : t('voice.liveTranscript', 'Your Query:')}
              </p>
              <p className="text-base sm:text-lg text-gray-800 dark:text-gray-200 break-words">"{liveTranscript}"</p>
            </div>
          )}

          {/* Processing Indicator */}
          {isProcessing && (
            <div className="bg-white dark:bg-gray-800 rounded-lg shadow-md p-6">
              <div className="flex items-center justify-center space-x-3">
                <LoadingSpinner size="sm" />
                <p className="text-gray-600 dark:text-gray-400">
                  {t('voice.processing', 'Processing your voice query...')}
                </p>
              </div>
            </div>
          )}

          {/* Results */}
          {currentResult && !isProcessing && (
            <VoiceQueryResults
              result={currentResult}
              onAddToFavorites={handleAddToFavorites}
            />
          )}
        </div>

        {/* Right Column - History */}
        {/* Right Column - History (sticky, stretches with content) */}
        <div className="lg:col-span-1 lg:sticky lg:top-4 lg:self-start">
          {isLoadingHistory ? (
            <div className="bg-white dark:bg-gray-800 rounded-lg shadow-md p-6">
              <LoadingSpinner size="sm" />
              <p className="text-center text-gray-600 dark:text-gray-400 mt-2">
                {t('voice.loadingHistory', 'Loading history...')}
              </p>
            </div>
          ) : (
            <QueryHistory
              history={queryHistory}
              onSelectQuery={handleSelectQuery}
              onToggleFavorite={handleToggleFavorite}
              onDeleteQuery={handleDeleteQuery}
            />
          )}
        </div>
      </div>
    </div>
  );
};
