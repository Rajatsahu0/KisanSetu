# KisanMitra AI - React Frontend

A comprehensive React-based web frontend for the KisanMitra AI farming assistant application. This frontend integrates with the existing .NET backend API to provide farmers with an intuitive interface for soil analysis, quality grading, voice queries, planting advisory, and historical data visualization.

## Features

- **Responsive Design**: Mobile-first approach with support for all screen sizes
- **Multi-language Support**: Hindi, English, and regional Indian languages
- **Offline Capability**: Core functionality works without internet connectivity
- **Voice Interface**: Record and process voice queries in local dialects
- **File Upload**: Support for soil health cards and produce images
- **Real-time Data**: Live market prices and weather information
- **Accessibility**: WCAG 2.1 AA compliant with screen reader support

## Technology Stack

- **React 18** with TypeScript for type safety
- **Vite** for fast development and optimized builds
- **Tailwind CSS** for utility-first styling
- **Redux Toolkit** for state management
- **React Query** for server state and caching
- **Headless UI** for accessible components
- **Framer Motion** for animations

## Getting Started

### Prerequisites

- Node.js 18+ and npm
- Access to the KisanMitra AI backend API

### Installation

1. Install dependencies:
```bash
npm install
```

2. Create environment file:
```bash
cp .env.example .env.local
```

3. Configure environment variables:
```env
VITE_API_BASE_URL=http://localhost:5000/api
VITE_AWS_REGION=ap-south-1
VITE_COGNITO_USER_POOL_ID=your-user-pool-id
VITE_COGNITO_CLIENT_ID=your-client-id
```

### Development

Start the development server:
```bash
npm run dev
```

The application will be available at `http://localhost:3000`

### Building

Build for production:
```bash
npm run build
```

### Testing

Run tests:
```bash
npm run test
```

Run tests in watch mode:
```bash
npm run test:watch
```

### Linting

Check code quality:
```bash
npm run lint
```

## Project Structure

```
src/
├── components/          # Reusable UI components
├── pages/              # Page components
├── store/              # Redux store and slices
├── contexts/           # React contexts
├── hooks/              # Custom hooks
├── services/           # API services
├── utils/              # Utility functions
├── types/              # TypeScript type definitions
└── lib/                # Third-party library configurations
```

## Key Features Implementation

### Authentication
- AWS Cognito integration for secure authentication
- JWT token management with automatic refresh
- Phone number-based registration and login

### File Upload
- Direct S3 upload with progress tracking
- File validation and compression
- Support for images, documents, and audio files

### Offline Support
- Service worker for caching
- Request queuing when offline
- Automatic sync when connection returns

### Internationalization
- React Context-based language switching
- Support for RTL languages
- Fallback to English for missing translations

### Responsive Design
- Mobile-first Tailwind CSS approach
- Breakpoint-specific layouts
- Touch-friendly interactions

## API Integration

The frontend integrates with the following backend endpoints:

- `/api/auth/*` - Authentication services
- `/api/soil-analysis/*` - Soil health card processing
- `/api/quality-grading/*` - Produce quality grading
- `/api/voice-query/*` - Voice processing services
- `/api/planting-advisory/*` - Planting recommendations
- `/api/historical-data/*` - Data visualization

## Performance Optimization

- Code splitting with React.lazy()
- Image lazy loading and compression
- Bundle optimization with Vite
- React Query for efficient data fetching
- Service worker for caching strategies

## Accessibility

- Semantic HTML structure
- ARIA labels and roles
- Keyboard navigation support
- Screen reader compatibility
- High contrast color schemes
- Focus management

## Contributing

1. Follow the existing code style and conventions
2. Write tests for new features
3. Ensure accessibility compliance
4. Update documentation as needed
5. Test on multiple devices and browsers

## License

This project is part of the KisanMitra AI system and follows the same licensing terms.