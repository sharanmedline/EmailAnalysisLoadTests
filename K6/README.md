# Email Analysis Service - Load Testing Guide

## 📋 Overview

This guide provides comprehensive load testing scripts for the **Email Analysis Service** with GPT-5 Azure OpenAI integration. Multiple testing tools and approaches are provided to suit different testing requirements.

---

## 🛠️ Testing Tools Provided

### 1. **C# Console Load Testing Tool** ⭐ RECOMMENDED
- **Location**: `Tests/LoadTesting/LoadTestRunner.cs` & `Program.cs`
- **Best For**: .NET developers, integrated testing, quick validation
- **Key Features**:
  - Built-in C# (no external dependencies beyond .NET SDK)
  - Supports concurrent request handling
  - Detailed response metrics collection
  - Real-time progress reporting
  - Automatic statistical analysis

**Quick Start:**
```bash
cd Tests/LoadTesting
dotnet build
dotnet run
```

### 2. **Apache JMeter**
- **Location**: `JMeter/EmailAnalysisLoadTest.jmx`
- **Best For**: Enterprise testing, GUI-based test creation, distributed testing
- **Key Features**:
  - Visual test plan configuration
  - Extensive HTML reporting
  - Plugin ecosystem
  - Master-slave distributed testing

**Quick Start:**
```bash
jmeter -t EmailAnalysisLoadTest.jmx -l results.jtl -j jmeter.log
```

### 3. **k6 (Grafana k6)**
- **Location**: `K6/smoke-test.js`
- **Best For**: Modern DevOps, CI/CD pipelines, cloud testing
- **Key Features**:
  - JavaScript-based scripting
  - Cloud execution options
  - Custom metrics
  - Easy version control

**Quick Start:**
```bash
k6 run K6/smoke-test.js
```

---

## 🚀 Detailed Tool Usage

### C# Console Tool

#### Configuration
Edit `Program.cs` to customize:

```csharp
const string baseUrl = "https://localhost:7182"; // Your service URL

// Customize test scenarios:
// Test 1: Light Load
await runner.RunLoadTestAsync(
    concurrentRequests: 5,        // Number of concurrent users
    totalRequests: 10,            // Total requests
    delayBetweenRequests: 100,    // Milliseconds between requests
    verbose: true                 // Show progress
);

// Test 2: Medium Load
await runner.RunLoadTestAsync(
    concurrentRequests: 10,
    totalRequests: 50,
    delayBetweenRequests: 50,
    verbose: false
);

// Test 3: Heavy Load
await runner.RunLoadTestAsync(
    concurrentRequests: 20,
    totalRequests: 100,
    delayBetweenRequests: 0,
    verbose: false
);

// Test 4: Stress Test
await runner.RunLoadTestAsync(
    concurrentRequests: 50,
    totalRequests: 200,
    delayBetweenRequests: 0,
    verbose: false
);
```

#### Output Example

```
════════════════════════════════════════════════════════════════════
EMAIL ANALYSIS SERVICE - LOAD TEST
════════════════════════════════════════════════════════════════════
Base URL: https://localhost:7182
Concurrent Requests: 5
Total Requests: 10
Delay Between Requests: 100ms
Start Time: 2024-12-10 10:30:00.000 UTC
════════════════════════════════════════════════════════════════════

════════════════════════════════════════════════════════════════════
LOAD TEST RESULTS
════════════════════════════════════════════════════════════════════
Total Duration: 45.23 seconds
Total Requests: 10
Successful: 10 (100.00%)
Failed: 0
Requests/sec: 0.22

Response Times (ms):
  Average: 4523.45
  Min: 2100.67
  Max: 8945.23

Data Transfer:
  Total: 2.45 MB
  Average per request: 245.00 KB

End Time: 2024-12-10 10:31:00.000 UTC
════════════════════════════════════════════════════════════════════
```

#### Testing Different Endpoints

Modify `RunSingleRequestAsync()` in `LoadTestRunner.cs`:

```csharp
private async Task RunSingleRequestAsync(int requestNumber)
{
    // Option 1: Test batch directory endpoint (default)
    await TestBatchDirectoryAsync("C:\\EmailFiles", requestNumber);
    
    // Option 2: Test file analysis
    // await TestFileAnalysisAsync("C:\\EmailFiles\\sample.msg", requestNumber);
    
    // Option 3: Test health endpoint
    // var result = await TestHealthCheckAsync();
}
```

---

### Apache JMeter

#### Installation

1. Download from: https://jmeter.apache.org/download_jmeter.cgi
2. Extract to desired location
3. Add to PATH (optional)

#### GUI Mode (Interactive)

```bash
jmeter
# Then open EmailAnalysisLoadTest.jmx file via File menu
```

#### Command Line Mode (Headless)

```bash
# Basic run
jmeter -n -t EmailAnalysisLoadTest.jmx -l results.jtl -j jmeter.log

# With custom parameters
jmeter -n -t EmailAnalysisLoadTest.jmx \
  -l results.jtl \
  -j jmeter.log \
  -Jbase.url=https://localhost:7182 \
  -Jthreads=10 \
  -Jrampup=60 \
  -Jloops=10

# Generate HTML report
jmeter -g results.jtl -o report/ -e
```

#### Configuration Variables

In `EmailAnalysisLoadTest.jmx`:

```
BASE_URL       = https://localhost:7182  (Service URL)
THREADS        = 10                       (Concurrent users)
RAMPUP         = 60                       (Seconds to reach target)
LOOPCOUNTS     = 10                       (Iterations per user)
```

#### Expected Test Plan

The JMeter test includes:

1. **Health Check Endpoint**
   - GET /api/EmailAnalysis/health
   - Assertion: HTTP 200

2. **Batch Email Processing Endpoint** (Main)
   - POST /api/EmailAnalysis/emails/incoming
   - Body: `{"type": "directory", "source": "C:\\EmailFiles"}`
   - Assertion: HTTP 200

3. **Categories Endpoint**
   - GET /api/EmailAnalysis/categories
   - Assertion: HTTP 200

---

### k6 Load Testing

#### Installation

```bash
# Windows (Chocolatey)
choco install k6

# macOS (Homebrew)
brew install k6

# Linux (Snap)
snap install k6
```

#### Running Tests

```bash
# Basic smoke test
k6 run K6/smoke-test.js

# With custom settings
k6 run K6/smoke-test.js \
  --vus 20 \
  --duration 5m \
  --rps 100

# Generate JSON report
k6 run K6/smoke-test.js --out json=results.json

# Send to k6 Cloud
k6 run K6/smoke-test.js --out cloud
```

#### Modifying Test Scenarios

Edit `K6/smoke-test.js`:

```javascript
// Change load profile
export const options = {
  stages: [
    { duration: '5m', target: 10 },   // Ramp to 10 users
    { duration: '10m', target: 10 },  // Stay at 10 users
    { duration: '5m', target: 0 },    // Ramp down
  ],
  thresholds: {
    http_req_duration: ['p(95)<2000'], // 95% under 2s
    errors: ['rate<0.05'],             // Error rate under 5%
  },
};
```

#### Test Groups in k6

The script includes three test groups:

1. **Health Check** - Service availability
2. **Get Categories** - Data retrieval
3. **Batch Email Processing** - Primary functionality

---

## 📊 Load Test Scenarios

### Scenario 1: Light Load (Smoke Test)
```
Purpose: Quick validation, baseline metrics
Concurrency: 5 users
Total Requests: 10
Ramp-up: Immediate
Duration: ~45 seconds
Success Rate Target: >99%
Expected Avg Response: 2-5 seconds
```

### Scenario 2: Medium Load (Normal Operations)
```
Purpose: Peak usage simulation
Concurrency: 10 users
Total Requests: 50
Ramp-up: 60 seconds
Duration: 2-3 minutes
Success Rate Target: >99%
Expected Avg Response: 3-6 seconds
```

### Scenario 3: Heavy Load (Stress Boundaries)
```
Purpose: Find breaking point
Concurrency: 20 users
Total Requests: 100
Ramp-up: 30 seconds
Duration: 5-10 minutes
Success Rate Target: >95%
Expected Avg Response: 4-8 seconds
```

### Scenario 4: Stress Test (Maximum Capacity)
```
Purpose: Identify limits
Concurrency: 50 users
Total Requests: 200
Ramp-up: 0 seconds (immediate)
Duration: 15-20 minutes
Success Rate Target: >90%
Expected Avg Response: 5-15 seconds
```

---

## 📈 Interpreting Results

### Key Metrics

#### 1. Response Time
```
Metric: Average response time
Good:   < 5000ms (5 seconds)
Fair:   5000-10000ms
Poor:   > 10000ms

Note: Your service includes Azure OpenAI calls which can add 3-5 seconds
```

#### 2. Success Rate
```
Light Load:  > 99%    (Expect 99%+)
Medium Load: > 99%    (Expect 99%+)
Heavy Load:  > 95%    (Expect 95%+)
Stress:      > 90%    (Expect 90%+)
```

#### 3. Throughput (RPS)
```
Calculation: Total Successful Requests / Total Duration (seconds)

Example:
  - 100 successful requests in 450 seconds
  - RPS = 100 / 450 = 0.22 requests/second

Note: Lower RPS is normal for services calling Azure OpenAI
```

#### 4. Error Analysis
```
Count errors by type:
- 401: Authentication issues
- 403: Permission issues
- 500: Server errors
- Timeouts: Service overload
- Connection refused: Service down
```

---

## 🎯 Test Endpoints Reference

### 1. Health Check
```
GET /api/EmailAnalysis/health
Purpose: Verify service is running
Response: Service status with configuration details
Expected: HTTP 200, body contains "Healthy"
```

### 2. Get Categories
```
GET /api/EmailAnalysis/categories
Purpose: Get supported email categories
Response: JSON array of category strings
Expected: HTTP 200, array with "Inquiry", "Complaint", etc.
```

### 3. Batch Email Processing (Primary)
```
POST /api/EmailAnalysis/emails/incoming
Purpose: Process all emails in a directory
Request Body: {
  "type": "directory",
  "source": "C:\\EmailFiles"
}
Response: {
  "batchId": "uuid",
  "totalEmailsProcessed": 5,
  "successfullyProcessed": 4,
  "failedToProcess": 1,
  "emails": [{ email analysis results }],
  "processingDurationSeconds": 125.5
}
Expected: HTTP 200
Note: This endpoint calls Azure OpenAI for each email
```

---

## 🔍 Monitoring During Tests

### System Resources to Monitor

```
✓ CPU Usage:     Should stay < 80%
✓ Memory Usage:  Should stay < 80%
✓ Disk I/O:      Watch for bottlenecks
✓ Network:       Monitor latency to Azure
✓ Connections:   Track open connections
```

### Application Logs to Check

```
✓ Error rates increasing over time?
✓ Any authentication failures?
✓ Azure OpenAI quota exceeded?
✓ Database connection pool issues?
✓ Memory leak patterns?
```

---

## 💡 Performance Optimization Tips

### Based on Test Results:

**If response times are high:**
- Check Azure OpenAI API latency
- Verify network connectivity to Azure
- Review service's database queries
- Monitor application memory usage
- Check CPU utilization

**If error rates are high:**
- Verify Azure authentication credentials
- Check Azure OpenAI quota limits
- Review service logs for exceptions
- Verify email directory paths
- Check file permissions

**If throughput is low:**
- Increase concurrency gradually
- Reduce delays between requests
- Review connection pool settings
- Check for resource bottlenecks
- Scale horizontally if needed

---

## 🔐 Performance Baselines (Expected)

| Metric | Target |
|--------|--------|
| Light Load Success Rate | 99%+ |
| Medium Load Success Rate | 99%+ |
| Heavy Load Success Rate | 95%+ |
| Avg Response Time | 3-8 seconds* |
| Max Response Time | < 15 seconds |
| Requests/Second | 0.1-0.5 |

*Higher due to Azure OpenAI processing time

---

## 🚨 Troubleshooting

### "Connection Refused"
```
Check:
  ✓ Service is running
  ✓ Correct URL/port
  ✓ Firewall allows connections
  ✓ SSL certificate is valid
```

### "401 Unauthorized"
```
Check:
  ✓ Azure Entra ID credentials
  ✓ ClientId and TenantId in appsettings.json
  ✓ Service principal has required roles
  ✓ Token service is working
```

### "500 Internal Server Error"
```
Check:
  ✓ Application logs for exceptions
  ✓ Azure OpenAI configuration
  ✓ Email directory exists
  ✓ File permissions
  ✓ Disk space available
```

### "Timeout Errors"
```
Solutions:
  ✓ Reduce concurrent request count
  ✓ Increase HttpClient timeout
  ✓ Check network latency to Azure
  ✓ Scale service vertically
```

---

## 📊 Result Documentation

### Save Results For Comparison

```
After each test run, save:
  - LoadTest_Light_2024-12-10.txt
  - LoadTest_Medium_2024-12-10.txt
  - LoadTest_Heavy_2024-12-10.txt
  - LoadTest_Stress_2024-12-10.txt

Compare over time to track:
  - Performance trends
  - Degradation
  - Improvements after optimization
```

---

## 📝 Best Practices Checklist

**Before Testing:**
- [ ] Service is running and healthy
- [ ] Email directory path is correct
- [ ] Azure OpenAI is accessible
- [ ] System resources are available
- [ ] Network connectivity confirmed

**During Testing:**
- [ ] Monitor server resources
- [ ] Watch for errors in logs
- [ ] Track Azure OpenAI quota usage
- [ ] Note any anomalies
- [ ] Don't interrupt test run

**After Testing:**
- [ ] Review metrics and statistics
- [ ] Check for memory leaks
- [ ] Document baseline metrics
- [ ] Archive results
- [ ] Plan optimization if needed

---

## 🔗 Additional Resources

- **Azure OpenAI**: https://learn.microsoft.com/en-us/azure/ai-services/openai/
- **JMeter**: https://jmeter.apache.org/usermanual/
- **k6**: https://k6.io/docs/
- **Performance Testing**: https://en.wikipedia.org/wiki/Software_testing

---

**Status**: Ready for Use ✅  
**Last Updated**: 2024-12-10  
**Tested With**: .NET 8, Azure OpenAI GPT-5
