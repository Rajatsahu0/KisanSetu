import React, { useRef, useState, useCallback } from 'react';
import { Camera, X, RotateCw, Check } from 'lucide-react';

export interface ImageCaptureProps {
  onCapture: (imageBlob: Blob) => void;
  onError?: (error: string) => void;
  quality?: number;
  facingMode?: 'user' | 'environment';
  maxWidth?: number;
  maxHeight?: number;
}

export const ImageCapture: React.FC<ImageCaptureProps> = ({
  onCapture,
  onError,
  quality = 0.9,
  facingMode = 'environment',
  maxWidth = 1920,
  maxHeight = 1080
}) => {
  const videoRef = useRef<HTMLVideoElement>(null);
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const [stream, setStream] = useState<MediaStream | null>(null);
  const [capturedImage, setCapturedImage] = useState<string | null>(null);
  const [isStreaming, setIsStreaming] = useState(false);
  const [currentFacingMode, setCurrentFacingMode] = useState<'user' | 'environment'>(facingMode);

  const startCamera = useCallback(async () => {
    try {
      const mediaStream = await navigator.mediaDevices.getUserMedia({
        video: {
          facingMode: currentFacingMode,
          width: { ideal: maxWidth },
          height: { ideal: maxHeight }
        }
      });

      if (videoRef.current) {
        videoRef.current.srcObject = mediaStream;
        setStream(mediaStream);
        setIsStreaming(true);
      }
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : 'Failed to access camera';
      console.error('Camera access error:', err);
      onError?.(errorMessage);
    }
  }, [currentFacingMode, maxWidth, maxHeight, onError]);

  const stopCamera = useCallback(() => {
    if (stream) {
      stream.getTracks().forEach(track => track.stop());
      setStream(null);
      setIsStreaming(false);
    }
  }, [stream]);

  const captureImage = useCallback(() => {
    if (!videoRef.current || !canvasRef.current) return;

    const video = videoRef.current;
    const canvas = canvasRef.current;
    const context = canvas.getContext('2d');

    if (!context) return;

    // Set canvas dimensions to match video
    canvas.width = video.videoWidth;
    canvas.height = video.videoHeight;

    // Draw video frame to canvas
    context.drawImage(video, 0, 0, canvas.width, canvas.height);

    // Convert canvas to blob
    canvas.toBlob(
      (blob) => {
        if (blob) {
          // Create preview URL
          const imageUrl = URL.createObjectURL(blob);
          setCapturedImage(imageUrl);
          stopCamera();
        }
      },
      'image/jpeg',
      quality
    );
  }, [quality, stopCamera]);

  const confirmCapture = useCallback(() => {
    if (!canvasRef.current) return;

    canvasRef.current.toBlob(
      (blob) => {
        if (blob) {
          onCapture(blob);
          setCapturedImage(null);
        }
      },
      'image/jpeg',
      quality
    );
  }, [onCapture, quality]);

  const retakePhoto = useCallback(() => {
    if (capturedImage) {
      URL.revokeObjectURL(capturedImage);
      setCapturedImage(null);
    }
    startCamera();
  }, [capturedImage, startCamera]);

  const switchCamera = useCallback(() => {
    stopCamera();
    setCurrentFacingMode(prev => prev === 'user' ? 'environment' : 'user');
  }, [stopCamera]);

  // Start camera when switching facing mode
  React.useEffect(() => {
    if (!isStreaming && !capturedImage) {
      startCamera();
    }
  }, [currentFacingMode, isStreaming, capturedImage, startCamera]);

  // Cleanup on unmount
  React.useEffect(() => {
    return () => {
      stopCamera();
      if (capturedImage) {
        URL.revokeObjectURL(capturedImage);
      }
    };
  }, [stopCamera, capturedImage]);

  return (
    <div className="w-full max-w-2xl mx-auto">
      <div className="relative bg-black rounded-lg overflow-hidden aspect-video">
        {/* Video stream */}
        {!capturedImage && (
          <video
            ref={videoRef}
            autoPlay
            playsInline
            muted
            className="w-full h-full object-cover"
          />
        )}

        {/* Captured image preview */}
        {capturedImage && (
          <img
            src={capturedImage}
            alt="Captured"
            className="w-full h-full object-cover"
          />
        )}

        {/* Hidden canvas for image capture */}
        <canvas ref={canvasRef} className="hidden" />

        {/* Camera controls overlay */}
        <div className="absolute bottom-0 left-0 right-0 p-4 bg-gradient-to-t from-black/70 to-transparent">
          <div className="flex items-center justify-center space-x-4">
            {!capturedImage ? (
              <>
                {/* Switch camera button */}
                <button
                  onClick={switchCamera}
                  className="p-3 bg-white/20 hover:bg-white/30 rounded-full transition-colors"
                  aria-label="Switch camera"
                >
                  <RotateCw className="w-6 h-6 text-white" />
                </button>

                {/* Capture button */}
                <button
                  onClick={captureImage}
                  disabled={!isStreaming}
                  className="p-4 bg-white hover:bg-gray-100 rounded-full transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
                  aria-label="Capture photo"
                >
                  <Camera className="w-8 h-8 text-gray-900" />
                </button>

                {/* Close button */}
                <button
                  onClick={stopCamera}
                  className="p-3 bg-white/20 hover:bg-white/30 rounded-full transition-colors"
                  aria-label="Close camera"
                >
                  <X className="w-6 h-6 text-white" />
                </button>
              </>
            ) : (
              <>
                {/* Retake button */}
                <button
                  onClick={retakePhoto}
                  className="px-6 py-3 bg-white/20 hover:bg-white/30 text-white rounded-lg transition-colors font-medium"
                >
                  <RotateCw className="w-5 h-5 inline mr-2" />
                  Retake
                </button>

                {/* Confirm button */}
                <button
                  onClick={confirmCapture}
                  className="px-6 py-3 bg-green-500 hover:bg-green-600 text-white rounded-lg transition-colors font-medium"
                >
                  <Check className="w-5 h-5 inline mr-2" />
                  Use Photo
                </button>
              </>
            )}
          </div>
        </div>
      </div>

      {/* Instructions */}
      <div className="mt-4 p-3 bg-blue-50 border border-blue-200 rounded-md">
        <p className="text-sm text-blue-800">
          {!capturedImage
            ? 'Position your camera to capture a clear image. Ensure good lighting and focus.'
            : 'Review the captured image. Retake if needed or confirm to use this photo.'}
        </p>
      </div>
    </div>
  );
};

export default ImageCapture;
