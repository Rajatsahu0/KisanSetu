import React, { useState, useRef, useEffect } from 'react';
import { Play, Pause, Volume2, VolumeX, RotateCcw } from 'lucide-react';
import { useLanguage } from '@/contexts/LanguageContext';

interface AudioPlayerProps {
  audioUrl: string;
  autoPlay?: boolean;
  onPlaybackComplete?: () => void;
  hideOnError?: boolean;
}

export const AudioPlayer: React.FC<AudioPlayerProps> = ({
  audioUrl,
  autoPlay = false,
  onPlaybackComplete,
  hideOnError = false
}) => {
  const { t } = useLanguage();
  const [isPlaying, setIsPlaying] = useState(false);
  const [currentTime, setCurrentTime] = useState(0);
  const [duration, setDuration] = useState(0);
  const [volume, setVolume] = useState(1);
  const [isMuted, setIsMuted] = useState(false);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const audioRef = useRef<HTMLAudioElement | null>(null);

  useEffect(() => {
    // Check if audioUrl is valid
    if (!audioUrl || audioUrl.trim() === '' || !audioUrl.startsWith('http')) {
      setError('Invalid or missing audio URL');
      setIsLoading(false);
      return;
    }

    const audio = new Audio();
    audioRef.current = audio;

    audio.addEventListener('loadedmetadata', () => {
      setDuration(audio.duration);
      setIsLoading(false);
    });

    audio.addEventListener('timeupdate', () => {
      setCurrentTime(audio.currentTime);
    });

    audio.addEventListener('ended', () => {
      setIsPlaying(false);
      setCurrentTime(0);
      if (onPlaybackComplete) {
        onPlaybackComplete();
      }
    });

    audio.addEventListener('error', () => {
      const audioError = audio.error;
      let errorMessage = 'Failed to load audio';
      
      if (audioError) {
        switch (audioError.code) {
          case MediaError.MEDIA_ERR_ABORTED:
            errorMessage = 'Audio loading was aborted';
            break;
          case MediaError.MEDIA_ERR_NETWORK:
            errorMessage = 'Network error while loading audio. This may be a CORS issue.';
            break;
          case MediaError.MEDIA_ERR_DECODE:
            errorMessage = 'Audio decoding failed';
            break;
          case MediaError.MEDIA_ERR_SRC_NOT_SUPPORTED:
            errorMessage = 'Audio format not supported or URL inaccessible. Check CORS settings.';
            break;
        }
      }
      
      console.error('Audio error:', errorMessage, 'URL:', audioUrl, 'Error details:', audioError);
      setError(errorMessage);
      setIsLoading(false);
    });

    // Set the source after all event listeners are attached
    audio.preload = 'auto';
    audio.src = audioUrl;

    if (autoPlay) {
      audio.play().then(() => {
        setIsPlaying(true);
      }).catch((err) => {
        console.error('Autoplay failed:', err);
      });
    }

    return () => {
      audio.pause();
      audio.src = '';
    };
  }, [audioUrl, autoPlay, onPlaybackComplete]);

  const togglePlayPause = () => {
    if (!audioRef.current) return;

    if (isPlaying) {
      audioRef.current.pause();
      setIsPlaying(false);
    } else {
      audioRef.current.play().then(() => {
        setIsPlaying(true);
      }).catch((err) => {
        setError('Failed to play audio');
        console.error('Play failed:', err);
      });
    }
  };

  const handleSeek = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (!audioRef.current) return;

    const newTime = parseFloat(e.target.value);
    audioRef.current.currentTime = newTime;
    setCurrentTime(newTime);
  };

  const handleVolumeChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (!audioRef.current) return;

    const newVolume = parseFloat(e.target.value);
    audioRef.current.volume = newVolume;
    setVolume(newVolume);
    setIsMuted(newVolume === 0);
  };

  const toggleMute = () => {
    if (!audioRef.current) return;

    if (isMuted) {
      audioRef.current.volume = volume || 0.5;
      setIsMuted(false);
    } else {
      audioRef.current.volume = 0;
      setIsMuted(true);
    }
  };

  const handleRestart = () => {
    if (!audioRef.current) return;

    audioRef.current.currentTime = 0;
    setCurrentTime(0);
    audioRef.current.play().then(() => {
      setIsPlaying(true);
    }).catch((err) => {
      console.error('Restart failed:', err);
    });
  };

  const formatTime = (seconds: number): string => {
    if (isNaN(seconds)) return '0:00';
    const mins = Math.floor(seconds / 60);
    const secs = Math.floor(seconds % 60);
    return `${mins}:${secs.toString().padStart(2, '0')}`;
  };

  if (error) {
    // If hideOnError is true, don't render anything
    if (hideOnError) {
      return null;
    }
    
    return (
      <div className="bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-lg p-4">
        <p className="text-red-600 dark:text-red-400 text-sm">{error}</p>
      </div>
    );
  }

  if (isLoading) {
    // If hideOnError is true and still loading, show minimal loading state
    if (hideOnError) {
      return (
        <div className="bg-gray-50 dark:bg-gray-800 rounded-lg p-2">
          <p className="text-gray-600 dark:text-gray-400 text-xs">
            {t('voice.loadingAudio', 'Loading audio...')}
          </p>
        </div>
      );
    }
    
    return (
      <div className="bg-gray-50 dark:bg-gray-800 rounded-lg p-4">
        <p className="text-gray-600 dark:text-gray-400 text-sm">
          {t('voice.loadingAudio', 'Loading audio...')}
        </p>
      </div>
    );
  }

  return (
    <div className="bg-white dark:bg-gray-800 rounded-lg shadow-md p-4">
      <div className="flex flex-col space-y-4">
        {/* Controls Row */}
        <div className="flex items-center space-x-4">
          {/* Play/Pause Button */}
          <button
            onClick={togglePlayPause}
            className="w-12 h-12 rounded-full bg-green-500 hover:bg-green-600 flex items-center justify-center transition-colors"
            aria-label={isPlaying ? t('voice.pause', 'Pause') : t('voice.play', 'Play')}
          >
            {isPlaying ? (
              <Pause className="w-6 h-6 text-white" />
            ) : (
              <Play className="w-6 h-6 text-white ml-1" />
            )}
          </button>

          {/* Restart Button */}
          <button
            onClick={handleRestart}
            className="w-10 h-10 rounded-full bg-gray-200 dark:bg-gray-700 hover:bg-gray-300 dark:hover:bg-gray-600 flex items-center justify-center transition-colors"
            aria-label={t('voice.restart', 'Restart')}
          >
            <RotateCcw className="w-5 h-5 text-gray-700 dark:text-gray-300" />
          </button>

          {/* Time Display */}
          <div className="flex-1 text-sm font-mono text-gray-700 dark:text-gray-300">
            {formatTime(currentTime)} / {formatTime(duration)}
          </div>

          {/* Volume Controls */}
          <div className="flex items-center space-x-2">
            <button
              onClick={toggleMute}
              className="w-8 h-8 rounded-full hover:bg-gray-200 dark:hover:bg-gray-700 flex items-center justify-center transition-colors"
              aria-label={isMuted ? t('voice.unmute', 'Unmute') : t('voice.mute', 'Mute')}
            >
              {isMuted ? (
                <VolumeX className="w-5 h-5 text-gray-700 dark:text-gray-300" />
              ) : (
                <Volume2 className="w-5 h-5 text-gray-700 dark:text-gray-300" />
              )}
            </button>

            <input
              type="range"
              min="0"
              max="1"
              step="0.01"
              value={isMuted ? 0 : volume}
              onChange={handleVolumeChange}
              className="w-20 h-2 bg-gray-200 dark:bg-gray-700 rounded-lg appearance-none cursor-pointer"
              aria-label={t('voice.volume', 'Volume')}
            />
          </div>
        </div>

        {/* Progress Bar */}
        <div className="w-full">
          <input
            type="range"
            min="0"
            max={duration || 0}
            step="0.1"
            value={currentTime}
            onChange={handleSeek}
            className="w-full h-2 bg-gray-200 dark:bg-gray-700 rounded-lg appearance-none cursor-pointer"
            aria-label={t('voice.seekPosition', 'Seek position')}
          />
        </div>
      </div>
    </div>
  );
};
