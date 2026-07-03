# Knowledge Base and Advisory System

## Overview

The Knowledge Base and Advisory system provides farmers with expert agricultural advice using Amazon Bedrock Knowledge Bases with Retrieval Augmented Generation (RAG).

## Components

### 1. Knowledge Base Service
- Queries Amazon Bedrock Knowledge Base
- Uses hybrid search (semantic + keyword)
- Returns answers with citations
- Calculates confidence scores

### 2. Advisory Service
- Handles farmer questions (voice or text)
- Maintains conversation context (up to 10 exchanges)
- Escalates low-confidence questions to experts
- Provides citations for transparency

### 3. Expert Escalation Handler
- Queues questions with confidence < 70%
- Notifies agricultural experts
- Tracks escalation status
- Target response time: 24 hours

## Amazon Bedrock Knowledge Base Setup

### Prerequisites
1. AWS Account with Bedrock access
2. Amazon OpenSearch Serverless collection
3. S3 bucket with agricultural documents (10,000+ PDFs)
4. IAM roles with appropriate permissions

### Step 1: Create OpenSearch Serverless Collection

```bash
aws opensearchserverless create-collection \
  --name kisan-mitra-knowledge-base \
  --type VECTORSEARCH \
  --description "Vector store for Kisan Mitra AI agricultural knowledge"
```

### Step 2: Upload Documents to S3

```bash
aws s3 sync ./agricultural-documents s3://kisan-mitra-kb-documents/ \
  --recursive \
  --exclude "*.DS_Store"
```

Document categories to include:
- Crop management guides
- Pest and disease control
- Soil health management
- Regenerative farming practices
- Government agricultural schemes
- Regional farming calendars
- Seed variety information
- Irrigation best practices
- Post-harvest handling
- Market linkage information

### Step 3: Create Bedrock Knowledge Base

```bash
aws bedrock-agent create-knowledge-base \
  --name kisan-mitra-agricultural-kb \
  --description "Agricultural knowledge base for Kisan Mitra AI" \
  --role-arn arn:aws:iam::ACCOUNT_ID:role/BedrockKnowledgeBaseRole \
  --knowledge-base-configuration '{
    "type": "VECTOR",
    "vectorKnowledgeBaseConfiguration": {
      "embeddingModelArn": "arn:aws:bedrock:us-east-1::foundation-model/amazon.titan-embed-text-v1"
    }
  }' \
  --storage-configuration '{
    "type": "OPENSEARCH_SERVERLESS",
    "opensearchServerlessConfiguration": {
      "collectionArn": "arn:aws:aoss:us-east-1:ACCOUNT_ID:collection/COLLECTION_ID",
      "vectorIndexName": "kisan-mitra-index",
      "fieldMapping": {
        "vectorField": "embedding",
        "textField": "text",
        "metadataField": "metadata"
      }
    }
  }'
```

### Step 4: Create Data Source

```bash
aws bedrock-agent create-data-source \
  --knowledge-base-id KNOWLEDGE_BASE_ID \
  --name agricultural-documents \
  --data-source-configuration '{
    "type": "S3",
    "s3Configuration": {
      "bucketArn": "arn:aws:s3:::kisan-mitra-kb-documents"
    }
  }'
```

### Step 5: Start Ingestion Job

```bash
aws bedrock-agent start-ingestion-job \
  --knowledge-base-id KNOWLEDGE_BASE_ID \
  --data-source-id DATA_SOURCE_ID
```

Monitor ingestion progress:
```bash
aws bedrock-agent get-ingestion-job \
  --knowledge-base-id KNOWLEDGE_BASE_ID \
  --data-source-id DATA_SOURCE_ID \
  --ingestion-job-id INGESTION_JOB_ID
```

### Step 6: Test Knowledge Base

```bash
aws bedrock-agent-runtime retrieve-and-generate \
  --input '{"text": "What are the best practices for tomato cultivation?"}' \
  --retrieve-and-generate-configuration '{
    "type": "KNOWLEDGE_BASE",
    "knowledgeBaseConfiguration": {
      "knowledgeBaseId": "KNOWLEDGE_BASE_ID",
      "modelArn": "arn:aws:bedrock:us-east-1::foundation-model/anthropic.claude-3-5-sonnet-20241022-v2:0"
    }
  }'
```

## Configuration

Add to `appsettings.json`:

```json
{
  "AWS": {
    "BedrockKnowledgeBase": {
      "KnowledgeBaseId": "YOUR_KNOWLEDGE_BASE_ID",
      "ModelArn": "arn:aws:bedrock:us-east-1::foundation-model/anthropic.claude-3-5-sonnet-20241022-v2:0",
      "ConfidenceThreshold": 0.7,
      "MaxResults": 5
    }
  }
}
```

## Usage Example

```csharp
var question = new AdvisoryQuestion(
    QuestionText: "What is the best time to plant wheat in Madhya Pradesh?",
    FarmerId: "farmer123",
    Context: "Location: Indore, Madhya Pradesh",
    ConversationHistory: new List<string>()
);

var response = await advisoryService.AskQuestionAsync(question, cancellationToken);

Console.WriteLine($"Answer: {response.AnswerText}");
Console.WriteLine($"Sources: {string.Join(", ", response.Sources.Select(s => s.DocumentTitle))}");
Console.WriteLine($"Requires Expert: {response.RequiresExpertEscalation}");
```

## Supported Languages

The Knowledge Base supports queries in all 15 Indian languages:
- Hindi, Tamil, Telugu, Bengali, Marathi
- Gujarati, Kannada, Malayalam, Odia, Punjabi
- Assamese, Urdu, Kashmiri, Konkani, Sindhi

## Performance Considerations

- Knowledge Base queries typically complete in 2-5 seconds
- Caching is implemented for frequently asked questions
- Conversation context is maintained in DynamoDB
- Maximum 10 exchanges per conversation session

## Monitoring

CloudWatch metrics tracked:
- Query count and latency
- Confidence score distribution
- Escalation rate
- Citation count per response
- Cache hit rate

## Troubleshooting

### Low Confidence Scores
- Check if documents cover the topic
- Verify document quality and formatting
- Consider adding more relevant documents
- Review embedding model performance

### Slow Queries
- Check OpenSearch Serverless performance
- Verify network connectivity
- Review vector index configuration
- Consider increasing maxResults parameter

### Missing Citations
- Verify data source ingestion completed
- Check S3 bucket permissions
- Review document metadata
- Ensure documents are in supported formats (PDF, TXT, MD)
