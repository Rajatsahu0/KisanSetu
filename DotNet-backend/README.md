# KisanMitra AI - .NET Backend API

A robust .NET 8 Web API backend for the KisanMitra AI farming assistant. This API powers AI-driven soil analysis, quality grading, voice intelligence, planting advisory, and historical analytics — built entirely on Google Cloud Platform services.

## Features

- **Soil Analysis**: Process soil health cards using Google Document AI and Vision API
- **Quality Grading**: AI-powered produce quality assessment via Vertex AI
- **Voice Intelligence**: Multi-dialect voice queries using Google Cloud Speech-to-Text
- **Planting Advisory**: Context-aware crop recommendations powered by Gemini
- **Historical Analytics**: Trend analysis and data visualization
- **Multi-language Support**: Hindi, English, and regional Indian languages
- **Offline Support**: Request queuing and sync for low-connectivity areas
- **Firebase Authentication**: Secure phone-number-based auth

## Technology Stack

- **.NET 8** Web API
- **Google Cloud Platform**
  - Vertex AI (Gemini) for AI/ML
  - Cloud Firestore for database
  - Cloud Storage for file uploads
  - Cloud Speech V2 for voice processing
  - Cloud Vision for image analysis
  - Document AI for document processing
  - Secret Manager for secure configuration
  - Pub/Sub for async messaging
- **Firebase Admin SDK** for authentication
- **Entity Framework Core** (InMemory for development)
- **Swashbuckle** for Swagger/OpenAPI documentation
- **JWT Bearer** authentication

## Project Structure

```
DotNet-backend/
├── KisanMitraAI.API/            # Web API layer
│   ├── Controllers/             # API endpoints
│   ├── Middleware/              # Custom middleware
│   ├── Swagger/                 # Swagger configuration
│   ├── Program.cs              # Application entry point
│   └── Dockerfile              # Container configuration
├── KisanMitraAI.Core/          # Domain layer
│   ├── AI/                     # AI service interfaces
│   ├── Authentication/         # Auth models & interfaces
│   ├── Authorization/          # Role-based access
│   ├── Models/                 # Domain models
│   ├── SoilAnalysis/           # Soil analysis logic
│   ├── QualityGrading/         # Grading logic
│   ├── VoiceIntelligence/      # Voice processing
│   ├── PlantingAdvisory/       # Advisory logic
│   ├── HistoricalAnalytics/    # Analytics logic
│   ├── MultiLanguage/          # Translation services
│   ├── Offline/                # Offline sync
│   ├── GovernmentIntegration/  # eNAM & govt APIs
│   └── Workflows/              # Business workflows
├── KisanMitraAI.Infrastructure/ # Infrastructure layer
│   ├── AI/                     # Vertex AI implementations
│   ├── Data/                   # EF Core DbContext
│   ├── Repositories/           # Data access
│   ├── Storage/                # Cloud Storage
│   ├── Vision/                 # Vision API
│   ├── SoilAnalysis/           # Soil processing
│   ├── Migrations/             # DB migrations
│   └── ...                     # Other implementations
└── enam_quality.html           # eNAM quality reference
```

## API Endpoints

| Endpoint | Description |
|----------|-------------|
| `/api/auth/*` | Authentication (register, login, refresh) |
| `/api/profile/*` | User profile management |
| `/api/soil-analysis/*` | Soil health card processing |
| `/api/quality-grading/*` | Produce quality grading |
| `/api/voice-query/*` | Voice query processing |
| `/api/planting-advisory/*` | Planting recommendations |
| `/api/historical-data/*` | Historical data & trends |
| `/api/advisory/*` | General advisory services |

## Getting Started

### Prerequisites

- .NET 8 SDK
- Google Cloud project with enabled APIs (Vertex AI, Firestore, Storage, Speech, Vision, Document AI)
- Firebase project for authentication
- GCP service account credentials

### Configuration

1. Copy the example settings:
```bash
cp appsettings.example.json appsettings.Development.json
```

2. Configure your GCP credentials and project settings in `appsettings.Development.json`.

### Running Locally

```bash
cd KisanMitraAI.API
dotnet restore
dotnet run
```

The API will be available at `https://localhost:5001` with Swagger UI at `/swagger`.

### Docker

```bash
docker build -t kisanmitra-api .
docker run -p 5000:8080 kisanmitra-api
```

## Architecture

The project follows **Clean Architecture** principles:

- **API Layer**: Controllers, middleware, request/response handling
- **Core Layer**: Domain models, interfaces, business logic (no external dependencies)
- **Infrastructure Layer**: External service implementations (GCP, database, storage)

## Security

- Firebase JWT token validation
- Role-based authorization
- Input validation and sanitization
- Rate limiting middleware
- Secure credential management via GCP Secret Manager

## License

This project is part of the KisanMitra AI system and follows the same licensing terms.
