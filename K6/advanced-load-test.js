import http from 'k6/http';
import { check, sleep, group } from 'k6';
import { Rate, Trend } from 'k6/metrics';

// Custom metrics
const errorRate = new Rate('errors');
const successRate = new Rate('success');
const responseTime = new Trend('response_time');

const BASE_URL = 'https://localhost:7182';

/**
 * Advanced Load Test Scenarios for Email Analysis Service
 * 
 * Includes:
 * - Stress testing
 * - Endurance testing
 * - Spike testing
 * - Gradual load increase
 */

// Scenario 1: Stress Test (Breaking Point)
export const stressTest = {
  stages: [
    { duration: '2m', target: 100 },  // Rapidly scale to 100 users
    { duration: '5m', target: 100 },  // Stay at 100 users
    { duration: '2m', target: 200 },  // Ramp to 200 users
    { duration: '5m', target: 200 },  // Stay at 200 users
    { duration: '2m', target: 0 },    // Scale down
  ],
  thresholds: {
    'http_req_duration{staticAsset:yes}': ['p(95)<1000', 'p(99)<5000'],
    'http_req_duration{staticAsset:no}': ['p(95)<5000', 'p(99)<10000'],
    errors: ['rate<0.1'],
  },
};

// Scenario 2: Endurance Test (Long Duration)
export const enduranceTest = {
  stages: [
    { duration: '10m', target: 50 },  // Ramp up to 50 users
    { duration: '30m', target: 50 },  // Keep load steady for 30 minutes
    { duration: '10m', target: 0 },   // Ramp down
  ],
  thresholds: {
    http_req_duration: ['p(95)<3000'],
    errors: ['rate<0.05'],
  },
};

// Scenario 3: Spike Test (Sudden Burst)
export const spikeTest = {
  stages: [
    { duration: '2m', target: 10 },   // Normal load
    { duration: '1m', target: 100 },  // Sudden spike
    { duration: '3m', target: 100 },  // Maintain spike
    { duration: '2m', target: 10 },   // Return to normal
    { duration: '1m', target: 0 },    // Cool down
  ],
  thresholds: {
    http_req_duration: ['p(95)<5000'],
    errors: ['rate<0.15'], // Allow higher error rate during spike
  },
};

// Scenario 4: Gradual Ramp (Finding Optimal Load)
export const gradualRampTest = {
  stages: [
    { duration: '5m', target: 5 },    // 5 users
    { duration: '5m', target: 10 },   // 10 users
    { duration: '5m', target: 20 },   // 20 users
    { duration: '5m', target: 40 },   // 40 users
    { duration: '5m', target: 60 },   // 60 users
    { duration: '5m', target: 0 },    // Cool down
  ],
  thresholds: {
    http_req_duration: ['p(95)<5000'],
    errors: ['rate<0.1'],
  },
};

// Default: Smoke Test (Quick Validation)
export const smokeTest = {
  vus: 5,
  duration: '30s',
  thresholds: {
    http_req_duration: ['p(95)<1000'],
    errors: ['rate<0.05'],
  },
};

// ============================================================================
// Test Functions
// ============================================================================

export default function () {
  // Test 1: Health Check
  group('Health Check', () => {
    const res = http.get(`${BASE_URL}/api/EmailAnalysis/health`, {
      headers: { 'Content-Type': 'application/json' },
      tags: { name: 'HealthCheck' },
    });

    const success = check(res, {
      'status is 200': (r) => r.status === 200,
      'response contains Healthy': (r) => r.body.includes('Healthy'),
    });

    responseTime.add(res.timings.duration, { endpoint: 'health' });
    successRate.add(success);
    errorRate.add(!success);
  });

  sleep(0.5);

  // Test 2: Get Categories
  group('Get Categories', () => {
    const res = http.get(`${BASE_URL}/api/EmailAnalysis/categories`, {
      headers: { 'Content-Type': 'application/json' },
      tags: { name: 'GetCategories' },
    });

    const success = check(res, {
      'status is 200': (r) => r.status === 200,
      'response is array': (r) => r.body.includes('['),
    });

    responseTime.add(res.timings.duration, { endpoint: 'categories' });
    successRate.add(success);
    errorRate.add(!success);
  });

  sleep(0.5);

  // Test 3: Batch Email Processing (Main Workload)
  group('Batch Email Processing', () => {
    const payload = JSON.stringify({
      type: 'directory',
      source: 'C:\\EmailFiles',
    });

    const res = http.post(`${BASE_URL}/api/EmailAnalysis/emails/incoming`, payload, {
      headers: { 'Content-Type': 'application/json' },
      tags: { name: 'BatchProcess' },
    });

    const success = check(res, {
      'status is 200': (r) => r.status === 200,
      'response contains batchId': (r) => r.body.includes('batchId'),
      'response contains emails': (r) => r.body.includes('emails'),
      'response contains stats': (r) => r.body.includes('successfullyProcessed'),
    });

    responseTime.add(res.timings.duration, { endpoint: 'batch' });
    successRate.add(success);
    errorRate.add(!success);

    // Parse response to extract processing duration
    try {
      const jsonResponse = JSON.parse(res.body);
      if (jsonResponse.processingDurationSeconds) {
        responseTime.add(jsonResponse.processingDurationSeconds * 1000, { 
          endpoint: 'batch_ai_processing' 
        });
      }
    } catch (e) {
      // Response may not be parseable, continue
    }
  });

  sleep(1);
}

// ============================================================================
// Custom Setup/Teardown Functions
// ============================================================================

export function setup() {
  // Pre-test setup
  console.log('🚀 Starting Email Analysis Service Load Test');
  
  // Verify service is reachable
  const res = http.get(`${BASE_URL}/api/EmailAnalysis/health`);
  if (res.status !== 200) {
    throw new Error(`Service not ready: ${res.status}`);
  }
  
  console.log('✓ Service health check passed');
  return { startTime: new Date() };
}

export function teardown(data) {
  // Post-test cleanup
  console.log('✓ Load test completed');
  console.log(`  Started: ${data.startTime}`);
  console.log(`  Ended: ${new Date()}`);
}

/**
 * Usage Instructions:
 * 
 * 1. SMOKE TEST (Quick validation - 30 seconds):
 *    k6 run K6/advanced-load-test.js
 * 
 * 2. STRESS TEST (Find breaking point):
 *    k6 run -e SCENARIO=stressTest K6/advanced-load-test.js
 *    Then set: export const options = stressTest;
 * 
 * 3. ENDURANCE TEST (Long stability test):
 *    k6 run -e SCENARIO=enduranceTest K6/advanced-load-test.js
 *    Then set: export const options = enduranceTest;
 * 
 * 4. SPIKE TEST (Handle sudden load):
 *    k6 run -e SCENARIO=spikeTest K6/advanced-load-test.js
 *    Then set: export const options = spikeTest;
 * 
 * 5. GRADUAL RAMP TEST (Find optimal capacity):
 *    k6 run -e SCENARIO=gradualRampTest K6/advanced-load-test.js
 *    Then set: export const options = gradualRampTest;
 * 
 * 6. WITH JSON OUTPUT:
 *    k6 run K6/advanced-load-test.js --out json=results.json
 * 
 * 7. WITH CLOUD REPORTING:
 *    k6 run K6/advanced-load-test.js --out cloud
 */
