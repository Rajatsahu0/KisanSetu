import React, { useState, useRef, useEffect, useImperativeHandle, forwardRef } from 'react';
import { Mic, Square, Loader2 } from 'lucide-react';
import { useLanguage } from '@/contexts/LanguageContext';

export interface VoiceRecorderHandle {
  stopRecording: () => void;
}

interface VoiceRecorderProps {
  maxDuration: number;
  onRecordingComplete: (audioBlob: Blob) => void;
  onError: (error: string) => void;
  dialect: string;
  onTranscript?: (text: string, confidence: number) => void;
  onInterimTranscript?: (text: string) => void;
  onRecordingStateChange?: (isRecording: boolean) => void;
}

export const VoiceRecorder = forwardRef<VoiceRecorderHandle, VoiceRecorderProps>(({
  maxDuration,
  onRecordingComplete,
  onError,
  dialect,
  onTranscript,
  onInterimTranscript,
  onRecordingStateChange
}, ref) => {
  const { t } = useLanguage();
  const [isRecording, setIsRecording] = useState(false);
  const [recordingTime, setRecordingTime] = useState(0);
  const [audioLevel, setAudioLevel] = useState(0);
  const [isInitializing, setIsInitializing] = useState(false);

  const mediaRecorderRef = useRef<MediaRecorder | null>(null);
  const audioContextRef = useRef<AudioContext | null>(null);
  const analyserRef = useRef<AnalyserNode | null>(null);
  const animationFrameRef = useRef<number | null>(null);
  const chunksRef = useRef<Blob[]>([]);
  const timerRef = useRef<NodeJS.Timeout | null>(null);
  const isRecordingRef = useRef(false);

  useEffect(() => {
    return () => {
      stopRecording();
      if (audioContextRef.current) audioContextRef.current.close();
      if (animationFrameRef.current) cancelAnimationFrame(animationFrameRef.current);
      if (timerRef.current) clearInterval(timerRef.current);
    };
  }, []);

  useImperativeHandle(ref, () => ({
    stopRecording
  }));

  const setRecordingState = (value: boolean) => {
    isRecordingRef.current = value;
    setIsRecording(value);
    onRecordingStateChange?.(value);
  };

  const startRecording = async () => {
    try {
      setIsInitializing(true);
      chunksRef.current = [];
      setRecordingTime(0);

      const stream = await navigator.mediaDevices.getUserMedia({ audio: true });

      const audioContext = new AudioContext();
      const source = audioContext.createMediaStreamSource(stream);
      const analyser = audioContext.createAnalyser();
      analyser.fftSize = 256;
      source.connect(analyser);

      audioContextRef.current = audioContext;
      analyserRef.current = analyser;

      const mimeType = getSupportedMimeType();
      const mediaRecorder = new MediaRecorder(stream, { mimeType });

      mediaRecorder.ondataavailable = (event) => {
        if (event.data.size > 0) chunksRef.current.push(event.data);
      };

      mediaRecorder.onstop = () => {
        const audioBlob = new Blob(chunksRef.current, { type: mimeType });
        setTimeout(() => onRecordingComplete(audioBlob), 300);
        stream.getTracks().forEach(track => track.stop());
      };

      mediaRecorderRef.current = mediaRecorder;
      mediaRecorder.start();
      setRecordingState(true);
      setIsInitializing(false);

      // Web Speech API
      try {
        const SpeechRecognition = (window as any).SpeechRecognition || (window as any).webkitSpeechRecognition;
        if (SpeechRecognition && onTranscript) {
          const recognition = new SpeechRecognition();
          recognition.lang = dialect || 'hi-IN';
          recognition.continuous = true;
          recognition.interimResults = true;
          recognition.maxAlternatives = 1;

          recognition.onresult = (event: any) => {
            let finalTranscript = '';
            let interimTranscript = '';
            let maxConfidence = 0;
            for (let i = 0; i < event.results.length; i++) {
              const result = event.results[i];
              if (result.isFinal) {
                finalTranscript += result[0].transcript;
                maxConfidence = Math.max(maxConfidence, result[0].confidence);
              } else {
                interimTranscript += result[0].transcript;
              }
            }
            if (onInterimTranscript && (interimTranscript || finalTranscript)) {
              onInterimTranscript(finalTranscript || interimTranscript);
            }
            if (finalTranscript && onTranscript) {
              onTranscript(finalTranscript, maxConfidence);
            }
          };

          recognition.onerror = () => {};
          recognition.onend = () => {};

          recognition.start();
          (mediaRecorderRef.current as any)._speechRecognition = recognition;
        }
      } catch (e) {
        console.error('[SpeechAPI] Failed to initialize:', e);
      }

      timerRef.current = setInterval(() => {
        setRecordingTime(prev => {
          const newTime = prev + 1;
          if (newTime >= maxDuration) stopRecording();
          return newTime;
        });
      }, 1000);

      visualizeAudio();
    } catch (error) {
      setIsInitializing(false);
      onError(error instanceof Error ? error.message : 'Failed to access microphone');
    }
  };

  const stopRecording = () => {
    if (mediaRecorderRef.current && isRecordingRef.current) {
      try {
        const recognition = (mediaRecorderRef.current as any)._speechRecognition;
        if (recognition) recognition.stop();
      } catch (e) { /* ignore */ }
      setTimeout(() => {
        if (mediaRecorderRef.current && mediaRecorderRef.current.state !== 'inactive') {
          mediaRecorderRef.current.stop();
        }
      }, 150);
      setRecordingState(false);
    }

    if (timerRef.current) { clearInterval(timerRef.current); timerRef.current = null; }
    if (animationFrameRef.current) { cancelAnimationFrame(animationFrameRef.current); animationFrameRef.current = null; }
    if (audioContextRef.current) { audioContextRef.current.close(); audioContextRef.current = null; }
  };

  const visualizeAudio = () => {
    if (!analyserRef.current) return;
    const analyser = analyserRef.current;
    const dataArray = new Uint8Array(analyser.frequencyBinCount);

    const updateLevel = () => {
      analyser.getByteFrequencyData(dataArray);
      const average = dataArray.reduce((sum, value) => sum + value, 0) / dataArray.length;
      setAudioLevel(Math.min(100, (average / 255) * 100));
      if (isRecordingRef.current) {
        animationFrameRef.current = requestAnimationFrame(updateLevel);
      }
    };
    updateLevel();
  };

  const getSupportedMimeType = (): string => {
    const types = ['audio/webm;codecs=opus', 'audio/webm', 'audio/ogg;codecs=opus', 'audio/mp4', 'audio/wav'];
    for (const type of types) {
      if (MediaRecorder.isTypeSupported(type)) return type;
    }
    return 'audio/webm';
  };

  const formatTime = (seconds: number): string => {
    const mins = Math.floor(seconds / 60);
    const secs = seconds % 60;
    return `${mins}:${secs.toString().padStart(2, '0')}`;
  };

  const handleToggleRecording = () => {
    if (isRecording) {
      stopRecording();
    } else {
      startRecording();
    }
  };

  return (
    <div className="bg-white dark:bg-gray-800 rounded-lg shadow-md p-6">
      <div className="flex flex-col items-center space-y-6">
        {/* Recording Button */}
        <button
          onClick={handleToggleRecording}
          disabled={isInitializing}
          className={`relative w-24 h-24 rounded-full flex items-center justify-center transition-all duration-300 ${
            isRecording
              ? 'bg-red-500 hover:bg-red-600 animate-pulse'
              : 'bg-green-500 hover:bg-green-600'
          } ${isInitializing ? 'opacity-50 cursor-not-allowed' : ''}`}
          aria-label={isRecording ? t('voice.stopRecording', 'Stop Recording') : t('voice.startRecording', 'Start Recording')}
        >
          {isInitializing ? (
            <Loader2 className="w-10 h-10 text-white animate-spin" />
          ) : isRecording ? (
            <Square className="w-10 h-10 text-white" />
          ) : (
            <Mic className="w-10 h-10 text-white" />
          )}
        </button>

        {/* Recording Time */}
        {isRecording && (
          <div className="text-2xl font-mono font-bold text-gray-900 dark:text-gray-100">
            {formatTime(recordingTime)} / {formatTime(maxDuration)}
          </div>
        )}

        {/* Waveform Visualization */}
        {isRecording && (
          <div className="w-full max-w-md">
            <div className="flex items-center justify-center space-x-1 h-20">
              {Array.from({ length: 20 }).map((_, index) => {
                const height = Math.max(10, (audioLevel / 100) * 80 * (0.5 + Math.random() * 0.5));
                return (
                  <div
                    key={index}
                    className="bg-green-500 rounded-full transition-all duration-100"
                    style={{ width: '4px', height: `${height}px` }}
                  />
                );
              })}
            </div>
          </div>
        )}

        {/* Instructions */}
        <div className="text-center text-sm text-gray-600 dark:text-gray-400">
          {isRecording ? (
            <p>{t('voice.recordingInProgress', 'Recording in progress... Click to stop')}</p>
          ) : (
            <p>{t('voice.clickToRecord', 'Click the microphone to start recording')}</p>
          )}
          <p className="mt-1">
            {t('voice.maxDuration', 'Maximum duration: {{seconds}} seconds', { seconds: maxDuration })}
          </p>
        </div>

        {/* Dialect Info */}
        <div className="text-xs text-gray-500 dark:text-gray-500">
          {t('voice.selectedDialect', 'Selected dialect')}: {dialect}
        </div>
      </div>
    </div>
  );
});
