import React, { useCallback, useState } from 'react';
import { useDropzone } from 'react-dropzone';
import { Upload, X, FileText, Image as ImageIcon } from 'lucide-react';
import { validateFile, FileValidationOptions, formatFileSize } from '../../utils/fileValidation';
import { useLanguage } from '@/contexts/LanguageContext';

const MAX_IMAGE_DIMENSION = 2000;
const JPEG_QUALITY = 0.85;

function compressImage(file: File): Promise<File> {
  return new Promise((resolve) => {
    if (!file.type.startsWith('image/')) {
      resolve(file);
      return;
    }

    const img = new Image();
    img.onload = () => {
      let { width, height } = img;

      if (width <= MAX_IMAGE_DIMENSION && height <= MAX_IMAGE_DIMENSION && file.size <= 1_500_000) {
        URL.revokeObjectURL(img.src);
        resolve(file);
        return;
      }

      if (width > MAX_IMAGE_DIMENSION || height > MAX_IMAGE_DIMENSION) {
        const scale = Math.min(MAX_IMAGE_DIMENSION / width, MAX_IMAGE_DIMENSION / height);
        width = Math.round(width * scale);
        height = Math.round(height * scale);
      }

      const canvas = document.createElement('canvas');
      canvas.width = width;
      canvas.height = height;
      const ctx = canvas.getContext('2d')!;
      ctx.drawImage(img, 0, 0, width, height);
      URL.revokeObjectURL(img.src);

      canvas.toBlob(
        (blob) => {
          if (!blob) { resolve(file); return; }
          const compressed = new File([blob], file.name.replace(/\.[^.]+$/, '.jpg'), {
            type: 'image/jpeg',
            lastModified: file.lastModified,
          });
          console.log(`[Compress] ${file.name}: ${formatFileSize(file.size)} -> ${formatFileSize(compressed.size)}`);
          resolve(compressed);
        },
        'image/jpeg',
        JPEG_QUALITY
      );
    };

    img.onerror = () => resolve(file);
    img.src = URL.createObjectURL(file);
  });
}

export interface FileUploadZoneProps {
  accept?: string[];
  maxSize?: number;
  maxFiles?: number;
  onUpload: (files: File[]) => Promise<void>;
  onProgress?: (progress: number) => void;
  onError?: (error: string) => void;
  disabled?: boolean;
  multiple?: boolean;
  label?: string;
  description?: string;
  validationOptions?: FileValidationOptions;
}

export const FileUploadZone: React.FC<FileUploadZoneProps> = ({
  accept = ['image/jpeg', 'image/png', 'application/pdf'],
  maxSize = 10 * 1024 * 1024,
  maxFiles = 1,
  onUpload,
  onProgress,
  onError,
  disabled = false,
  multiple = false,
  label,
  description,
  validationOptions
}) => {
  const { t } = useLanguage();
  const [selectedFiles, setSelectedFiles] = useState<File[]>([]);
  const [uploading, setUploading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const displayLabel = label || t('upload.uploadFiles', 'Upload Files');
  const displayDesc = description || t('upload.dragDrop', 'Drag and drop files here, or click to select');

  const onDrop = useCallback(
    async (acceptedFiles: File[], rejectedFiles: any[]) => {
      setError(null);

      if (rejectedFiles.length > 0) {
        const rejection = rejectedFiles[0];
        let errorMessage = t('upload.fileRejected', 'File rejected');
        if (rejection.errors) {
          const err = rejection.errors[0];
          if (err.code === 'file-too-large') {
            errorMessage = t('upload.fileTooLarge', 'File is too large. Maximum size is {{size}}', { size: formatFileSize(maxSize) });
          } else if (err.code === 'file-invalid-type') {
            errorMessage = t('upload.invalidFileType', 'Invalid file type');
          } else {
            errorMessage = err.message;
          }
        }
        setError(errorMessage);
        onError?.(errorMessage);
        return;
      }

      const validationOpts = validationOptions || { maxSize, allowedTypes: accept };
      const validatedFiles: File[] = [];
      for (const file of acceptedFiles) {
        const result = await validateFile(file, validationOpts);
        if (!result.isValid) {
          setError(result.error || t('upload.uploadFailed', 'Upload failed'));
          onError?.(result.error || t('upload.uploadFailed', 'Upload failed'));
          return;
        }
        validatedFiles.push(file);
      }

      if (maxFiles && validatedFiles.length > maxFiles) {
        const errorMessage = t('upload.maxFilesAllowed', 'Maximum {{count}} file(s) allowed', { count: maxFiles });
        setError(errorMessage);
        onError?.(errorMessage);
        return;
      }

      const processedFiles: File[] = [];
      for (const file of validatedFiles) {
        processedFiles.push(await compressImage(file));
      }

      setSelectedFiles(processedFiles);

      if (onUpload) {
        setUploading(true);
        try {
          await onUpload(processedFiles);
          onProgress?.(100);
        } catch (err) {
          const errorMessage = err instanceof Error ? err.message : t('upload.uploadFailed', 'Upload failed');
          setError(errorMessage);
          onError?.(errorMessage);
        } finally {
          setUploading(false);
        }
      }
    },
    [accept, maxSize, maxFiles, onUpload, onProgress, onError, validationOptions, t]
  );

  const { getRootProps, getInputProps, isDragActive } = useDropzone({
    onDrop,
    accept: accept.reduce((acc, type) => ({ ...acc, [type]: [] }), {}),
    maxSize, maxFiles, multiple,
    disabled: disabled || uploading
  });

  const removeFile = (index: number) => { setSelectedFiles(prev => prev.filter((_, i) => i !== index)); setError(null); };

  const getFileIcon = (file: File) => {
    if (file.type.startsWith('image/')) return <ImageIcon className="w-8 h-8 text-blue-500" />;
    return <FileText className="w-8 h-8 text-gray-500" />;
  };

  return (
    <div className="w-full">
      <div {...getRootProps()}
        className={`border-2 border-dashed rounded-lg p-8 text-center cursor-pointer transition-colors duration-200
          ${isDragActive ? 'border-blue-500 bg-blue-50' : 'border-gray-300 hover:border-gray-400'}
          ${disabled || uploading ? 'opacity-50 cursor-not-allowed' : ''}
          ${error ? 'border-red-500 bg-red-50' : ''}`}>
        <input {...getInputProps()} />
        <div className="flex flex-col items-center justify-center space-y-4">
          <Upload className={`w-12 h-12 ${isDragActive ? 'text-blue-500' : 'text-gray-400'}`} />
          <div>
            <p className="text-lg font-medium text-gray-700">{displayLabel}</p>
            <p className="text-sm text-gray-500 mt-1">{displayDesc}</p>
          </div>
          <div className="text-xs text-gray-500">
            <p>{t('upload.acceptedFormats', 'Accepted formats')}: {accept.map(type => type.split('/')[1]).join(', ').toUpperCase()}</p>
            <p>{t('upload.maximumSize', 'Maximum size')}: {formatFileSize(maxSize)}</p>
            {maxFiles > 1 && <p>{t('upload.maximumFiles', 'Maximum files')}: {maxFiles}</p>}
          </div>
        </div>
      </div>

      {error && (
        <div className="mt-4 p-3 bg-red-50 border border-red-200 rounded-md">
          <p className="text-sm text-red-600">{error}</p>
        </div>
      )}

      {selectedFiles.length > 0 && (
        <div className="mt-4 space-y-2">
          <p className="text-sm font-medium text-gray-700">{t('upload.selectedFiles', 'Selected Files:')}</p>
          {selectedFiles.map((file, index) => (
            <div key={`${file.name}-${index}`} className="flex items-center justify-between p-3 bg-gray-50 rounded-md border border-gray-200">
              <div className="flex items-center space-x-3">
                {getFileIcon(file)}
                <div className="flex-1 min-w-0">
                  <p className="text-sm font-medium text-gray-900 truncate">{file.name}</p>
                  <p className="text-xs text-gray-500">{formatFileSize(file.size)}</p>
                </div>
              </div>
              {!uploading && (
                <button onClick={() => removeFile(index)} className="p-1 hover:bg-gray-200 rounded-full transition-colors" aria-label="Remove file">
                  <X className="w-4 h-4 text-gray-500" />
                </button>
              )}
            </div>
          ))}
        </div>
      )}

      {uploading && (
        <div className="mt-4 p-3 bg-blue-50 border border-blue-200 rounded-md">
          <p className="text-sm text-blue-600">{t('upload.uploading', 'Uploading files...')}</p>
        </div>
      )}
    </div>
  );
};
