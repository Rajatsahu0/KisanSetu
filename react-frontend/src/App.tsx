import { lazy, Suspense, useEffect } from 'react'
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import { Provider } from 'react-redux'
import { PersistGate } from 'redux-persist/integration/react'
import { QueryClientProvider } from '@tanstack/react-query'
import { store, persistor } from '@/store'
import { queryClient } from '@/lib/queryClient'
import { ThemeProvider } from '@/contexts/ThemeContext'
import { LanguageProvider } from '@/contexts/LanguageContext'
import { AuthProvider } from '@/contexts/AuthContext'
import { AuthErrorBoundary } from '@/components/auth/AuthErrorBoundary'
import { GlobalErrorBoundary } from '@/components/error/GlobalErrorBoundary'
import { ProtectedRoute } from '@/components/auth/ProtectedRoute'
import { AppLayout } from '@/components/layout'
import { ToastContainer } from '@/components/notifications'
import { useNotifications } from '@/hooks/useNotifications'
import { OfflineIndicator } from '@/components/offline/OfflineIndicator'
import { InstallPrompt } from '@/components/pwa/InstallPrompt'
import { UpdateNotification } from '@/components/pwa/UpdateNotification'
import { SkipLink } from '@/components/common/SkipLink'
import { FloatingLanguageToggle } from '@/components/language/FloatingLanguageToggle'
import { connectivityService } from '@/services/connectivityService'
import { pwaService } from '@/services/pwaService'
import { useOnlineStatus } from '@/hooks/useOnlineStatus'

// Lazy load pages for code splitting
const LoginPage = lazy(() => import('@/pages/LoginPage').then(m => ({ default: m.LoginPage })))
const RegisterPage = lazy(() => import('@/pages/RegisterPage').then(m => ({ default: m.RegisterPage })))
const DashboardPage = lazy(() => import('@/pages/DashboardPage').then(m => ({ default: m.DashboardPage })))
const SoilAnalysisPage = lazy(() => import('@/pages/SoilAnalysisPage').then(m => ({ default: m.SoilAnalysisPage })))
const QualityGradingPage = lazy(() => import('@/pages/QualityGradingPage').then(m => ({ default: m.QualityGradingPage })))
const VoiceQueriesPage = lazy(() => import('@/pages/VoiceQueriesPage').then(m => ({ default: m.VoiceQueriesPage })))
const PlantingAdvisoryPage = lazy(() => import('@/pages/PlantingAdvisoryPage').then(m => ({ default: m.PlantingAdvisoryPage })))
const HistoricalDataPage = lazy(() => import('@/pages/HistoricalDataPage').then(m => ({ default: m.HistoricalDataPage })))
const ProfilePage = lazy(() => import('@/pages/ProfilePage').then(m => ({ default: m.ProfilePage })))
const AboutPage = lazy(() => import('@/pages/AboutPage'))

// Loading fallback component with skeleton
const LoadingFallback = () => (
  <div 
    className="min-h-screen bg-gray-50 dark:bg-gray-900 p-4"
    role="status"
    aria-live="polite"
    aria-label="Loading page content"
  >
    <div className="max-w-7xl mx-auto">
      <div className="animate-pulse space-y-4">
        <div className="h-16 bg-gray-200 dark:bg-gray-700 rounded-lg" />
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {Array.from({ length: 6 }).map((_, i) => (
            <div key={i} className="h-48 bg-gray-200 dark:bg-gray-700 rounded-lg" />
          ))}
        </div>
      </div>
    </div>
    <span className="sr-only">Loading content, please wait...</span>
  </div>
)

// Notifications wrapper component
const NotificationsWrapper = () => {
  const { notifications, dismissNotification } = useNotifications();
  return <ToastContainer notifications={notifications} onDismiss={dismissNotification} />;
};

// Offline and PWA wrapper component
const OfflineAndPWAWrapper = () => {
  useOnlineStatus(); // Initialize online status monitoring

  useEffect(() => {
    // Initialize connectivity monitoring
    connectivityService.init();

    // Initialize PWA service
    pwaService.init();

    return () => {
      connectivityService.cleanup();
    };
  }, []);

  return (
    <>
      <OfflineIndicator />
      <InstallPrompt />
      <UpdateNotification />
    </>
  );
};

function App() {
  return (
    <Provider store={store}>
      <PersistGate loading={null} persistor={persistor}>
        <QueryClientProvider client={queryClient}>
          <ThemeProvider>
            <LanguageProvider>
              <AuthProvider>
                <GlobalErrorBoundary>
                  <BrowserRouter>
                    <SkipLink targetId="main-content">Skip to main content</SkipLink>
                    <AuthErrorBoundary>
                      <Suspense fallback={<LoadingFallback />}>
                        <Routes>
                          <Route path="/login" element={<LoginPage />} />
                          <Route path="/register" element={<RegisterPage />} />
                          
                          {/* Public routes with layout (accessible without login) */}
                          <Route element={<AppLayout />}>
                            <Route path="/dashboard" element={<DashboardPage />} />
                            <Route path="/voice-queries" element={<VoiceQueriesPage />} />
                            <Route path="/planting-advisory" element={<PlantingAdvisoryPage />} />
                            <Route path="/about" element={<AboutPage />} />
                          </Route>

                          {/* Redirects for placeholder pages */}
                          <Route path="/faq" element={<Navigate to="/about" replace />} />
                          <Route path="/help" element={<Navigate to="/about" replace />} />
                          <Route path="/privacy" element={<Navigate to="/dashboard" replace />} />
                          <Route path="/terms" element={<Navigate to="/dashboard" replace />} />
                          
                          {/* Protected routes with layout (login required) */}
                          <Route
                            element={
                              <ProtectedRoute>
                                <AppLayout />
                              </ProtectedRoute>
                            }
                          >
                            <Route path="/soil-analysis" element={<SoilAnalysisPage />} />
                            <Route path="/quality-grading" element={<QualityGradingPage />} />
                            <Route path="/historical-data" element={<HistoricalDataPage />} />
                            <Route path="/profile" element={<ProfilePage />} />
                          </Route>
                          
                          <Route path="/" element={<Navigate to="/dashboard" replace />} />
                        </Routes>
                      </Suspense>
                    </AuthErrorBoundary>
                    <NotificationsWrapper />
                    <OfflineAndPWAWrapper />
                    <FloatingLanguageToggle />
                  </BrowserRouter>
                </GlobalErrorBoundary>
              </AuthProvider>
            </LanguageProvider>
          </ThemeProvider>
        </QueryClientProvider>
      </PersistGate>
    </Provider>
  )
}

export default App