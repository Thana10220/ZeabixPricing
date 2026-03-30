# 🚀 Pricing Engine Service

---

# 🧱 Architecture Overview

```text
┌──────────────┐
│   Client     │
└──────┬───────┘
       │ HTTP
┌──────▼───────┐
│ Pricing.Api  │  ← Controllers
└──────┬───────┘
       │
┌──────▼──────────────┐
│ Pricing.Application │  ← Services
│  - CalculateQuote   │
│  - BulkQuote        │
│  - RuleService      │
└──────┬──────────────┘
       │
┌──────▼──────────────┐
│ Pricing.Domain      │  ← Entities / Enums
└──────┬──────────────┘
       │
┌──────▼──────────────┐
│ Pricing.Infrastructure │
│  - RuleRepository (JSON)
│  - InMemoryJobQueue
│  - Background Worker
└─────────────────────┘
```

---

# ⚙️ Features

### ✅ Pricing Rules

* Weight tier pricing
* Remote area surcharge
* Time window promotion
* Priority-based rule execution (per type)

### ✅ Bulk Processing

* Submit batch requests (JSON / CSV)
* Async processing via background worker
* Retry mechanism on failure

### ✅ Observability

* Structured logging
* Correlation ID support

---

# 🛠️ Setup Guide

## 1. Clone Project

```bash
git clone <repo-url>
cd Pricing
```

---

## 2. Run Locally (without Docker)

```bash
dotnet run --project Pricing.Api
```

API available at:

```
http://localhost:5076
```

---

## 3. Run with Docker 🐳

### Build & Run

```bash
docker compose up --build
```

### Stop

```bash
docker compose down
```
---
# swagger (API Document)

http://localhost:5076/swagger/index.html

---

# 📂 Configuration

## rules.json

Located at:

```
Pricing.Api/rules.json
```

Example:

```json
{
  "id": "uuid",
  "name": "WeightTier - 0-10",
  "type": "WeightTier",
  "priority": 1,
  "isActive": true,
  "configJson": {
    "min": 0,
    "max": 10,
    "price": 0
  }
}
```

---

# 📡 API Endpoints

---

## 🧮 Calculate Price

```http
POST /quotes/price
```

### Request

```json
{
  "weight": 10,
  "area": "เชียงใหม่",
  "requestTime": "2026-01-01T10:00:00Z"
}
```

### Response

```json
{
  "finalPrice": 160,
  "appliedRules": [
    "WeightTier(10-30)",
    "RemoteAreaSurcharge",
    "TimeWindowPromotion"
  ]
}
```

---

## 📦 Bulk Pricing (JSON)

```http
POST /quotes/bulk
```

*** Attach Sample File in directory sample_data => bulk_quotes.csv

### Request

```json
[
  {
    "weight": 10,
    "area": "เชียงใหม่",
    "requestTime": "2026-01-01T10:00:00Z"
  }
]
```

### Response

```json
{
  "job_id": "abc-123"
}
```

---

## 📄 Bulk Pricing (CSV)

```http
POST /quotes/bulk/csv
Content-Type: multipart/form-data
```

CSV format:

```
weight,area,requestTime
10,เชียงใหม่,2026-01-01T10:00:00Z
```

---

## 🔍 Get Job Status

```http
GET /quotes/jobs/{jobId}
```

### Response

```json
{
  "jobId": "abc-123",
  "status": "Completed",
  "retryCount": 0,
  "results": [
    {
      "weight": 10,
      "area": "เชียงใหม่",
      "finalPrice": 160
    }
  ]
}
```

---

## ⚙️ Rule Management

### Get All Rules

```http
GET /rules
```

---

### Create Rule

```http
POST /rules
```

```json
{
  "name": "WeightTier - 50-100",
  "type": "WeightTier",
  "priority": 5,
  "isActive": true,
  "effectiveFrom": "2026-01-01T00:00:00Z",
  "effectiveTo": "2027-01-01T00:00:00Z",
  "configJson": {
    "min": 50,
    "max": 100,
    "price": 30
  }
}
```

---

### Update Rule

```http
PUT /rules/{id}
```

---

### Delete Rule

```http
DELETE /rules/{id}
```

---

# 🔁 Job Processing Flow

```text
POST /quotes/bulk
      ↓
Create Job
      ↓
Enqueue Job
      ↓
Background Worker
      ↓
Process Items
      ↓
Update Status (Completed / Failed)
```

---

# 🔄 Retry Mechanism

* Configurable retry count
* Delay between retries
* Failed jobs marked after max retries

---

# 🧪 Testing

Run tests:

```bash
dotnet test
```

---

# 🐳 Docker Notes

* rules.json is mounted as volume
* Changes persist across container restarts

```yaml
volumes:
  - ./Pricing.Api/rules.json:/app/rules.json
```

---