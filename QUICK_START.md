# 🚀 LOAD TESTING - QUICK START GUIDE

## 📦 What You Have

**Three complete load testing tools for your Email Analysis Service:**

1. ✅ **C# Console Tool** (Recommended for .NET)
2. ✅ **Apache JMeter** (Enterprise-grade)
3. ✅ **k6 JavaScript** (Modern/DevOps)

---

## ⚡ 60-Second Quick Start

### Option 1: C# Console (Easiest)

```bash
cd Tests/LoadTesting
dotnet build
dotnet run
```

**Output**: Automatic 4 test scenarios with detailed metrics.

### Option 2: k6 (Fastest Setup)

```bash
# Install: https://k6.io/docs/getting-started/installation/
k6 run K6/smoke-test.js
```

### Option 3: JMeter (Most Features)

```bash
jmeter -n -t JMeter/EmailAnalysisLoadTest.jmx -l results.jtl
```

---

## 📊 Test Scenarios Included

| Test | Users | Requests | Duration | Purpose |
|------|-------|----------|----------|---------|
| **Light** | 5 | 10 | 45s | Baseline |
| **Medium** | 10 | 50 | 2-3m | Normal ops |
| **Heavy** | 20 | 100 | 5-10m | Stress |
| **Stress** | 50 | 200 | 15-20m | Limits |

---

## 🎯 What Gets Tested

```
✓ GET  /api/EmailAnalysis/health
✓ GET  /api/EmailAnalysis/categories
✓ POST /api/EmailAnalysis/emails/incoming  (Main endpoint)
```

---

## 📈 Key Metrics

```
Success Rate:      Should be > 95%
Response Time:     Should be < 8 seconds (includes Azure OpenAI)
Throughput:        0.1-0.5 requests/second
Error Rate:        Should be < 5%
```

---

## ⚙️ Configuration

### C# Tool
Edit `Program.cs`:
```csharp
const string baseUrl = "https://localhost:7182";
```

### JMeter
Edit test variables:
```
BASE_URL = https://localhost:7182
THREADS = 10
RAMPUP = 60
LOOPCOUNTS = 10
```

### k6
Edit `K6/smoke-test.js`:
```javascript
const BASE_URL = 'https://localhost:7182';
```

---

## 🔧 File Locations

```
EmailAnalysisLoadTests/
├── Tests/LoadTesting/
│   ├── LoadTestRunner.cs      ← Core logic
│   └── Program.cs             ← Entry point
├── JMeter/
│   └── EmailAnalysisLoadTest.jmx
├── K6/
│   ├── smoke-test.js
│   └── README.md
└── README.md
```

---

## ✅ Pre-Test Checklist

- [ ] Service is running (`https://localhost:7182`)
- [ ] Email directory exists (`C:\EmailFiles`)
- [ ] Azure OpenAI is configured
- [ ] .NET SDK installed (for C# tool)
- [ ] System has adequate resources

---

## 🚨 Common Issues

| Issue | Fix |
|-------|-----|
| Connection refused | Start service, check URL |
| 401 errors | Verify Azure auth credentials |
| Slow responses | Normal (Azure OpenAI latency 2-5s) |
| High errors | Check logs, verify Azure quota |

---

## 📊 Expected Results

```
Light Load:   100% success, avg 4-5s response
Medium Load:  99% success, avg 5-6s response
Heavy Load:   95%+ success, avg 6-8s response
Stress:       90%+ success, avg 8-15s response
```

*Times include Azure OpenAI processing*

---

## 🎓 Next Steps

1. Run light load test first
2. Review results/metrics
3. Increase load gradually
4. Document baseline metrics
5. Run stress test to find limits

---

## 📚 Full Guides

- **C# Tool**: See `Tests/LoadTesting/` comments
- **JMeter**: See embedded test plan
- **k6**: See `K6/README.md`

---

**Repository**: https://github.com/sharanmedline/EmailAnalysisLoadTests  
**Ready**: ✅ All tools tested and working
