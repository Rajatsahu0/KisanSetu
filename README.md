# 🌾 KisanSetu (Known as KisanMitra AI)

**AI-Powered Decision Intelligence Platform for Indian Agriculture**

> *"Every farmer deserves AI-powered intelligence, in their own voice."*

KisanSetu is a unified, multi-modal AI Co-pilot designed to solve the three biggest challenges facing India's 350M+ smallholder farmers: **language barriers**, **market transparency**, and **technical data complexity**. Built entirely on Google Cloud, it supports the farmer from sowing to selling through voice-first, multilingual interaction.

---

## 🎯 Problem Statement

- **350M+ Indian farmers** lack access to timely agricultural intelligence
- **86% are small/marginal** farmers (< 2 hectares) with limited tech access
- **40%+ rural illiteracy** makes text-based tools unusable
- **15-30% income loss** due to market price opacity and middleman exploitation
- **Language exclusion** — most AgriTech tools are English-only; farmers speak Hindi, Punjabi, Bengali, Tamil + regional dialects

---

## 💡 Solution — Four AI-Powered Modules

### 🎤 Krishi-Vani (Voice Intelligence)
Voice queries in **10+ Indian languages** including regional dialects (Bundelkhandi, Bhojpuri, Marwari). Farmers ask questions like *"Nashik mein tamatar ka bhav kya hai?"* and receive spoken AI-powered responses with real-time government mandi prices.

**Pipeline:** Voice → Google Speech-to-Text → Gemini Flash (intent parsing) → data.gov.in API → Gemini Flash (response generation) → Client TTS

### 📷 Quality Grader (Vision AI)
Farmers photograph their produce and receive an **AI-certified quality grade (A/B/C)** with estimated market pricing at the nearest mandi. Eliminates middleman exploitation by giving farmers data-backed price references.

**Pipeline:** Photo → Cloud Storage → Vision AI (labels + properties) → Gemini Flash (multimodal grading) → Quality Classifier → Price Calculator

### 📄 Dhara-Analyzer (Soil Intelligence)
Digitizes government **Soil Health Cards** using OCR, extracts all nutrient data (N, P, K, pH, micronutrients), and generates a personalized **12-month Regenerative Farming Plan** with carbon sequestration estimation.

**Pipeline:** Soil Card photo → Document AI (Form Parser) → Soil Data Parser → Gemini Pro (regenerative plan) → Carbon Estimator

### 🌤️ Sowing Oracle (Planting Advisory)
Predicts the precise **"golden window"** for planting by correlating hyper-local weather forecasts with soil data. Recommends optimal seed varieties ranked by yield potential with confidence scoring.

**Pipeline:** Location + Crop → Open-Meteo (16-day forecast) + NASA POWER (5-year climate) → Gemini Pro (planting analysis) → Seed Variety Recommender

---

## 🏗️ Architecture

```
┌─────────────────────────────────────────────────────────────┐
│              Frontend (React PWA + Firebase Hosting)          │
│         Voice-first • Multilingual • Offline-capable         │
└──────────────────────────┬──────────────────────────────────┘
                           │ HTTPS
┌──────────────────────────▼──────────────────────────────────┐
│         Google Cloud Run (ASP.NET Core 8 Container)          │
│    Auto-scaling • Min-instances=1 • asia-south1 (Mumbai)     │
└──────────────────────────┬──────────────────────────────────┘
                           │
         ┌─────────────────┼─────────────────┐
         ▼                 ▼                 ▼
   ┌───────────┐    ┌───────────┐    ┌───────────────┐
   │ Firestore  │    │  Cloud    │    │  Vertex AI    │
   │ (Database) │    │  Storage  │    │  (Gemini)     │
   └───────────┘    └───────────┘    └───────────────┘
```

**Design Pattern:** Clean Architecture (Core → Infrastructure → API)  
**Key Principle:** Domain logic is cloud-agnostic; only Infrastructure layer touches GCP SDKs

---

## ☁️ Google Cloud Services Used (11 Services)

| Service | Purpose |
|---------|---------|
| **Cloud Run** | Containerized .NET 8 API, auto-scales 1→10, asia-south1 |
| **Vertex AI (Gemini 2.5 Flash)** | Fast AI: query parsing, grading, response generation |
| **Vertex AI (Gemini 2.5 Pro)** | Complex reasoning: planting analysis, soil plans |
| **Cloud Firestore** | Serverless NoSQL — profiles, prices, history, soil data |
| **Cloud Storage** | Images, audio, documents, generated plans |
| **Speech-to-Text** | Voice transcription in 10+ Indian languages |
| **Vision AI** | Produce image analysis (labels, properties) |
| **Document AI** | Soil Health Card OCR (Form Parser) |
| **Firebase Auth** | Phone SMS OTP (no password needed) |
| **Secret Manager** | API keys stored securely |
| **Cloud Pub/Sub** | Event-driven notifications |

---

## 🌍 External APIs Integrated (4 APIs)

| API | Purpose | Cost |
|-----|---------|------|
| **data.gov.in** | Live government mandi prices (official source) | Free (API key required) |
| **Open-Meteo** | 16-day weather forecasts | Free (no key) |
| **NASA POWER** | 5-year historical climate averages | Free (no key) |
| **Open-Meteo Geocoding** | Location → lat/lon resolution | Free (no key) |

---

## 🛠️ Tech Stack

### Backend
- **.NET 8 / C#** — ASP.NET Core Web API
- **Clean Architecture** — Core (domain) → Infrastructure (GCP) → API (controllers)
- **Cloud Run** — Dockerized, auto-scaling, zero cold starts
- **Entity Framework Core** — InMemory for dev
- **CoreWCF** — SOAP for government system integration (eNAM)

### Frontend
- **React 18 + TypeScript** — Vite bundler
- **Tailwind CSS + Headless UI** — Accessible, responsive
- **Redux Toolkit + React Query** — State management
- **i18next** — Internationalization (Hindi, English, regional)
- **PWA (Workbox)** — Offline-capable, installable
- **Firebase JS SDK** — Client-side phone auth

### AI Strategy
- **Gemini 2.5 Flash** (~80% of requests) — speed-critical: parsing, grading, responses
- **Gemini 2.5 Pro** (~20% of requests) — quality-critical: planting, soil plans, advisory
- **Grounded AI** — Real data injected into LLM context to prevent hallucination

---

## 📁 Project Structure

```
├── src/                              # Backend (.NET 8)
│   ├── KisanMitraAI.API/            # Controllers, Middleware, Program.cs
│   ├── KisanMitraAI.Core/           # Interfaces, Models, Domain Logic
│   └── KisanMitraAI.Infrastructure/ # GCP integrations, Repositories
│
├── react-frontend/                   # Frontend (React + TypeScript)
│   └── src/
│       ├── components/              # UI components by feature
│       ├── pages/                   # Route-level pages
│       ├── services/                # API client modules
│       ├── store/                   # Redux slices
│       ├── contexts/                # Auth, Language, Theme
│       ├── i18n/                    # Translations
│       └── config/                  # Firebase config
│
├── .gitignore
├── .env.example                      # Frontend environment template
└── README.md                         # This file
```

---

## 🚀 Getting Started

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 18+](https://nodejs.org/)
- [Google Cloud CLI (gcloud)](https://cloud.google.com/sdk/docs/install)
- GCP Project with APIs enabled (see below)

### Backend Setup

```bash
# 1. Authenticate with GCP
gcloud auth application-default login
gcloud config set project YOUR_PROJECT_ID

# 2. Update appsettings.json with your API keys
#    - Weather.ApiKey (OpenWeatherMap)
#    - MandiPriceApi.ApiKey (data.gov.in)
#    - GCP.ProjectId

# 3. Restore and run
dotnet restore
dotnet run --project src/KisanMitraAI.API
```

API will be available at `http://localhost:5001`

### Frontend Setup

```bash
cd react-frontend

# 1. Copy environment file
cp .env.example .env
# Edit .env with your API URL and GCP project ID

# 2. Install and run
npm install
npm run dev
```

Frontend will be available at `http://localhost:3000`

### GCP APIs to Enable

```bash
gcloud services enable \
  run.googleapis.com \
  aiplatform.googleapis.com \
  firestore.googleapis.com \
  storage.googleapis.com \
  speech.googleapis.com \
  vision.googleapis.com \
  documentai.googleapis.com \
  secretmanager.googleapis.com
```

---

## 🐳 Docker Deployment

```bash
# Build
docker build -t kisansetu-api -f src/KisanMitraAI.API/Dockerfile .

# Run locally
docker run -p 8080:8080 kisansetu-api

# Deploy to Cloud Run
gcloud builds submit --tag asia-south1-docker.pkg.dev/PROJECT_ID/repo/kisansetu-api:latest

gcloud run deploy kisansetu-api \
  --image asia-south1-docker.pkg.dev/PROJECT_ID/repo/kisansetu-api:latest \
  --region asia-south1 \
  --min-instances 1 --max-instances 10 \
  --memory 1Gi --cpu 2
```

---

## 💰 Cost Analysis (10,000 Active Farmers)

| Category | Monthly Cost |
|----------|---:|
| AI Services (Gemini Flash + Pro) | $145–210 |
| Compute (Cloud Run) | $45–65 |
| Speech + Vision + Document AI | $65–90 |
| Data Storage (Firestore + GCS) | $20–35 |
| Monitoring + Networking | $5–13 |
| **Total** | **$280–413** |
| **Per Farmer/Month** | **$0.028–0.041 (~₹2.8)** |

---

## 🌐 Supported Languages

| Language | Code | Dialects |
|----------|------|----------|
| Hindi | hi-IN | Bundelkhandi, Bhojpuri, Marwari |
| English | en-IN | — |
| Punjabi | pa-IN | — |
| Bengali | bn-IN | — |
| Tamil | ta-IN | — |
| Telugu | te-IN | — |
| Marathi | mr-IN | — |
| Gujarati | gu-IN | — |
| Kannada | kn-IN | — |
| Malayalam | ml-IN | — |

---

## 🔒 Security

- ✅ Firebase JWT verification on every API request
- ✅ Phone SMS OTP authentication (no passwords)
- ✅ Secret Manager for all external API keys
- ✅ Cloud Run IAM — principle of least privilege
- ✅ Rate limiting: 100 requests/minute per farmer
- ✅ Input validation on all endpoints
- ✅ CORS configured for frontend origin

---

## 📊 Key Metrics

| Metric | Value |
|--------|-------|
| Response time (text query) | 1–3 seconds |
| Response time (voice) | 3–5 seconds |
| Languages supported | 10+ |
| API endpoints | 20+ |
| GCP services used | 11 |
| External APIs integrated | 4 |
| Cold start time | 0ms (min-instances=1) |
| Auto-scale range | 1–10 instances |
| Deployment region | asia-south1 (Mumbai) |

---

## 🏆 Hackathon Track

**Track 1 — AI-Powered Decision Intelligence Platform**

KisanSetu fulfills all Track 1 requirements:
- ✅ Ingests data from multiple sources (voice, images, documents, APIs)
- ✅ Enables natural language interaction with data
- ✅ Generates insights, recommendations, and forecasts
- ✅ Identifies patterns and trends
- ✅ Supports decision-making through AI-powered assistance
- ✅ Deployed as a scalable, real-world application on Google Cloud

---

## 🤝 Impact

| Area | Outcome |
|------|---------|
| **Income** | 15-30% higher selling price through real-time market intelligence |
| **Sustainability** | Reduced chemical fertilizer use via regenerative plans |
| **Inclusivity** | Voice-first access regardless of literacy level |
| **Efficiency** | Optimal planting timing → higher yield, less water waste |
| **Fair Pricing** | Certified quality grades eliminate middleman exploitation |

---

## 📄 License

This project was built for the Google Cloud Hackathon 2026.

---

## 👥 Team

**KisanSetu** — *क‍िसान सेतु* — The Farmer's Bridge 🌾

*Built with ❤️ for India's farming community*
