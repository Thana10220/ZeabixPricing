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
Pricing.Api/Data/rules.json
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
PATCH /rules/{id}
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

# 🚦 Rate Limiting & Retry Policy

This service includes built-in **rate limiting** and **retry mechanisms** to ensure stability, prevent abuse, and improve resilience.

---

# 🚦 Rate Limiting

Rate limiting is applied at the API level to prevent excessive requests.

## 🔧 Implementation

Uses .NET built-in middleware:

```csharp
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("fixed", opt =>
    {
        opt.Window = TimeSpan.FromSeconds(10);
        opt.PermitLimit = 20;
        opt.QueueLimit = 5;
    });

    options.AddFixedWindowLimiter("bulk", opt =>
    {
        opt.Window = TimeSpan.FromSeconds(30);
        opt.PermitLimit = 5;
    });
});
```

---

## 📌 Applied Endpoints

```csharp
[EnableRateLimiting("fixed")]
POST /quotes/price

[EnableRateLimiting("bulk")]
POST /quotes/bulk
POST /quotes/bulk/csv
```

---

## 🚫 Behavior

* Exceeding limit returns:

```http
429 Too Many Requests
```

---

# 🔁 Retry Policy

Retry logic is implemented for both:

* Rule loading (infrastructure)
* Job processing (worker)

---

## 🔧 RuleRepository Retry (Polly)

Uses **Polly** for retry + circuit breaker:

```csharp
Policy
    .Handle<IOException>()
    .Or<TimeoutException>()
    .WaitAndRetryAsync(3, retryAttempt =>
        TimeSpan.FromMilliseconds(200 * retryAttempt));
```

---

## 🔌 Circuit Breaker

```csharp
Policy
    .Handle<IOException>()
    .Or<TimeoutException>()
    .CircuitBreakerAsync(3, TimeSpan.FromSeconds(10));
```

---

## 🎯 Behavior

* Retries on transient failures (I/O, timeout)
* Stops retrying on data errors (e.g. invalid JSON)
* Opens circuit after repeated failures

---

# 🔁 Job Retry (Worker)

Background worker retries failed jobs with **exponential backoff**.

## 🔧 Strategy

```text
Retry Delay = 2^RetryCount seconds
```

Example:

| Retry | Delay |
| ----- | ----- |
| 1     | 2 sec |
| 2     | 4 sec |
| 3     | 8 sec |

---

## 🔀 Jitter (Anti-Spike)

Random delay added:

```text
Delay = base + random(0-1s)
```

---

## 🔁 Retry Flow

```text
Job Failed
   ↓
Increase RetryCount
   ↓
Check MaxRetries
   ↓
Re-enqueue job
   ↓
Worker processes again
```

---

## ❌ Max Retry Reached

* Job marked as:

```text
FAILED
```

* Error message stored

---

# 🛡️ Resilience Summary

| Feature             | Purpose                   |
| ------------------- | ------------------------- |
| Rate Limiting       | Prevent overload          |
| Retry Policy        | Handle transient errors   |
| Circuit Breaker     | Prevent cascading failure |
| Exponential Backoff | Avoid retry storms        |
| Jitter              | Reduce traffic spikes     |

---

# 🚀 Future Improvements

* Distributed rate limiting (Redis)
* Dead Letter Queue (DLQ)
* Retry visibility dashboard
* Per-tenant rate limits

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