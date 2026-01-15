# Microbank K6 Load Testing Suite 🚀

Comprehensive load testing suite for validating **3 critical banking scenarios** in the Microbank microservices architecture.

## 🎯 Test Coverage (3 Essential Tests)

### 1. 🔥 Race Condition Test (WILL FAIL - Vulnerability Detector)
**Status:** ❌ **Expected to FAIL** - No distributed locking in project  
**Why:** Exposes double-spending vulnerabilities  
**What it tests:** 5 concurrent withdrawals from same account with 100 TRY balance  
**Current Behavior:** Multiple requests may succeed, causing negative balance  
**What you'll learn:** You NEED distributed locking (Redis) before production  

### 2. 🔁 Idempotency Test (Partial Support)
**Status:** ⚠️ **Partial** - No Idempotency-Key header support  
**Why:** Shows rapid duplicate request behavior   
**What it tests:** 10 rapid transfer requests without idempotency keys  
**Current Behavior:** Each request creates new transfer (by design)  
**What you'll learn:** Need idempotency key validation for network retry scenarios  

### 3. 🔄 SAGA Rollback Test (SHOULD PASS)
**Status:** ✅ **Should WORK** - CompensateDebitConsumer exists  
**Why:** Validates compensating transactions  
**What it tests:** Transfer to non-existent account triggers automatic refund  
**Current Behavior:** Should refund money to sender  
**What you'll learn:** Your SAGA implementation is working (or not!)  

## 🚨 Important Understanding

**These tests are not just "pass/fail" tests** - they are **diagnostic tools** that:
- ✅ **Expose vulnerabilities** in your current implementation
- ✅ **Provide specific fix recommendations** in the output
- ✅ **Show you what to implement** before going to production
- ✅ **Validate features that DO exist** (like SAGA rollback)

**Don't panic if tests fail** - that's the point! They tell you what's missing.


## 📋 Prerequisites

### 1. Install k6
```bash
# macOS
brew install k6

# Linux
sudo gpg --no-default-keyring --keyring /usr/share/keyrings/k6-archive-keyring.gpg --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys C5AD17C747E3415A3642D57D77C6C491D6AC1D69
echo "deb [signed-by=/usr/share/keyrings/k6-archive-keyring.gpg] https://dl.k6.io/deb stable main" | sudo tee /etc/apt/sources.list.d/k6.list
sudo apt-get update
sudo apt-get install k6

# Windows
choco install k6

# Or download from: https://k6.io/docs/getting-started/installation/
```

### 2. Start Microbank Services
```bash
# From project root
docker-compose up -d

# Verify all services are running
docker-compose ps

# Check API Gateway is accessible
curl http://localhost:8080/api/auth/login
```

### 3. (Optional) Install Mock SMS Service Dependencies
```bash
cd load-tests/mock-external-service
npm install
```

## 🚀 Running Tests

### Quick Start - Run All Tests
```bash
cd load-tests
./run-all-tests.sh
```

This will run all 5 tests sequentially and provide a comprehensive summary.

### Run Individual Tests

#### Test 1: Race Condition 🔥
```bash
cd load-tests
k6 run test-1-race-condition.js
```
**Duration:** ~30 seconds  
**VUs:** 1 (sends parallel batch requests)  
**Success criteria:** Exactly 1 successful withdrawal, final balance = 0

#### Test 2: Idempotency 🔁
```bash
k6 run test-2-idempotency.js
```
**Duration:** ~30 seconds  
**VUs:** 1  
**Success criteria:** Only 1 unique transfer created, balance changed once

#### Test 3: SAGA Rollback 🔄
```bash
k6 run test-3-saga-rollback.js
```
**Duration:** 2 minutes  
**VUs:** 10  
**Success criteria:** Failed transfers trigger refunds, balances restored

#### Test 4: Deadlock Stress 💥
```bash
k6 run test-4-deadlock-stress.js
```
**Duration:** 9 minutes (with ramp up/down)  
**Peak VUs:** 500  
**Success criteria:** < 5% error rate, no connection pool exhaustion, p95 < 1s

**Warning:** This test creates 50 test accounts and generates heavy load. Ensure your system has adequate resources.

#### Test 5: Circuit Breaker 🔌
```bash
# Terminal 1: Start mock SMS service with delay
cd load-tests/mock-external-service
npm install
npm start  # 5s delay by default

# Or with custom delay
DELAY_MS=10000 npm start  # 10s delay

# Terminal 2: Run test
cd load-tests
k6 run test-5-circuit-breaker.js
```
**Duration:** 3.5 minutes  
**Peak VUs:** 100  
**Success criteria:** Transfer API stays fast (p95 < 3s) despite slow external service

## 📊 Understanding Results

### Success Example
```
✓ CRITICAL: Exactly 1 successful transfer (no double-spending)
✓ Sender balance is 0 (not negative - no double spending)
✓ No money created from thin air

✅✅✅ Race Condition Test PASSED - No double-spending detected! ✅✅✅
```

### Failure Example
```
✗ CRITICAL: Exactly 1 successful transfer (no double-spending)
  ↳ 0% — ✓ 0 / ✗ 1

🚨🚨🚨 CRITICAL SECURITY BUG: NEGATIVE BALANCE DETECTED! 🚨🚨🚨
    This indicates a race condition vulnerability!
    Distributed locking mechanism is not working properly!
```

### Key Metrics to Watch

**http_req_duration:** Request latency  
- p(95) < 500ms: Excellent
- p(95) < 1000ms: Good
- p(95) > 2000ms: Needs optimization

**http_req_failed:** Error rate  
- < 1%: Excellent
- < 5%: Acceptable (excluding business logic rejections)
- \> 10%: System issues

**checks:** Test assertions  
- 100%: Perfect
- \> 90%: Good
- < 80%: Critical issues

## 🛠️ Configuration

Edit `config.js` to customize:

```javascript
export const config = {
    baseUrl: 'http://localhost:8080',  // API Gateway URL
    
    load: {
        deadlockStress: {
            stages: [
                { duration: '1m', target: 50 },   // Adjust VUs
                { duration: '3m', target: 200 },
                // ...
            ],
        },
    },
    
    thresholds: {
        http_req_duration: ['p(95)<500'],  // Adjust performance targets
    },
};
```

## 🐛 Troubleshooting

### "Connection refused" errors
**Cause:** Microbank services not running  
**Fix:** 
```bash
docker-compose up -d
docker-compose ps  # Verify all services are "Up"
```

### Race condition test showing multiple successes
**Cause:** Missing distributed locking implementation  
**Fix:** Implement distributed lock in Account service using Redis:
```csharp
// In AccountService, before debit operation
using (var redisLock = await _redisLockFactory.CreateLockAsync($"account:{accountId}", TimeSpan.FromSeconds(30)))
{
    if (redisLock.IsAcquired)
    {
        // Perform debit operation
    }
}
```

### Idempotency test showing multiple debits
**Cause:** Idempotency key not being validated  
**Fix:** Add idempotency key checking in Transfer controller

### SAGA rollback test - balance not restored
**Cause:** Compensating transaction not implemented or failing  
**Fix:** Review SAGA state machine, ensure compensate events are published

### Deadlock test - high error rate
**Possible causes:**
1. Database connection pool too small → Increase pool size in connection string
2. Missing database indexes → Add indexes on frequently queried columns
3. Long-running transactions → Optimize queries, reduce transaction scope

### Circuit breaker test - slow responses
**Cause:** Circuit breaker not implemented or not configured  
**Fix:** Add Polly circuit breaker to external service calls:
```csharp
services.AddHttpClient("SmsService")
    .AddPolicyHandler(Policy
        .Handle<HttpRequestException>()
        .CircuitBreakerAsync(
            handledEventsAllowedBeforeBreaking: 3,
            durationOfBreak: TimeSpan.FromSeconds(30)
        ));
```

## 📈 What to Do After Tests

### All Tests Passing ✅
1. **Document results** - Save test results for compliance/audit
2. **Set up monitoring** - Monitor same metrics in production
3. **Schedule regular tests** - Run weekly/before major deployments
4. **Deploy with confidence** - Your system is production-ready!

### Some Tests Failing ❌
1. **Fix critical failures first** - Race condition and SAGA rollback are highest priority
2. **Review logs** - Check Docker logs: `docker-compose logs transfer-api`
3. **Optimize iteratively** - Fix one issue at a time, re-test
4. **Consider infrastructure** - May need more CPU/memory/database resources

## 🔄 CI/CD Integration

### GitHub Actions Example
```yaml
name: Load Tests

on:
  pull_request:
    branches: [main]

jobs:
  load-test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Start services
        run: docker-compose up -d
      
      - name: Wait for services
        run: sleep 30
      
      - name: Install k6
        run: |
          curl https://github.com/grafana/k6/releases/download/v0.47.0/k6-v0.47.0-linux-amd64.tar.gz -L | tar xvz
          sudo mv k6-v0.47.0-linux-amd64/k6 /usr/local/bin/
      
      - name: Run critical tests
        run: |
          cd load-tests
          k6 run test-1-race-condition.js
          k6 run test-2-idempotency.js
          k6 run test-3-saga-rollback.js
```

## 📚 Additional Resources

- [k6 Documentation](https://k6.io/docs/)
- [k6 Best Practices](https://k6.io/docs/testing-guides/test-types/)
- [Understanding Banking System Testing](https://martinfowler.com/articles/microservice-testing/)
- [SAGA Pattern](https://microservices.io/patterns/data/saga.html)

## 🤝 Contributing

To add new tests:
1. Create `test-N-description.js` in `load-tests/`
2. Follow existing test structure (setup, test, verification)
3. Add to `run-all-tests.sh`
4. Update this README

## 📝 License

Same as parent Microbank project.

---

## 🚨 Important Notes

1. **Never run stress tests against production** - Always use dedicated test environment
2. **Monitor resource usage** - Tests can consume significant CPU/memory
3. **Test data cleanup** - Tests create users/accounts, clean up periodically
4. **Network bandwidth** - High VU tests generate significant network traffic
5. **Database backups** - Always have backups before running destructive tests

---

**Questions or Issues?** Open an issue in the Microbank repository.

**Good luck testing! May your balances always be positive and your deadlocks nonexistent! 🎉**
