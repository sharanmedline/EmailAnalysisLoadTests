import http from 'k6/http';
import { check, sleep, group } from 'k6';
import { Rate } from 'k6/metrics';

// Custom metrics
const errorRate = new Rate('errors');

// Test configuration
export const options = {
  // Test 1: Smoke Test
  stages: [
    { duration: '10s', target: 5 },   // Ramp up to 5 users
    { duration: '20s', target: 5 },   // Stay at 5 users
    { duration: '10s', target: 0 },   // Ramp down
  ],
  thresholds: {
    http_req_duration: ['p(95)<1000', 'p(99)<2000'],  // 95% under 1s, 99% under 2s
    errors: ['rate<0.1'],                             // Error rate under 10%
  },
};

// Base URL - change as needed
const BASE_URL = 'https://localhost:7182';

export default function () {
  // Test 1: Health Check
  group('Health Check', () => {
    const res = http.get(`${BASE_URL}/api/EmailAnalysis/health`, {
      headers: { 'Content-Type': 'application/json' },
    });

    const success = check(res, {
      'status is 200': (r) => r.status === 200,
      'response contains Healthy': (r) => r.body.includes('Healthy'),
    });

    errorRate.add(!success);
  });

  sleep(1);

  // Test 2: Get Categories
  group('Get Categories', () => {
    const res = http.get(`${BASE_URL}/api/EmailAnalysis/categories`, {
      headers: { 'Content-Type': 'application/json' },
    });

    const success = check(res, {
      'status is 200': (r) => r.status === 200,
      'response is array': (r) => r.body.includes('['),
    });

    errorRate.add(!success);
  });

  sleep(1);

  // Test 3: Batch Email Processing
  group('Batch Email Processing', () => {
    const payload = JSON.stringify({
      type: 'directory',
      source: 'C:\\EmailFiles',
    });

    const res = http.post(`${BASE_URL}/api/EmailAnalysis/emails/incoming`, payload, {
      headers: { 'Content-Type': 'application/json' },
    });

    const success = check(res, {
      'status is 200': (r) => r.status === 200,
      'response contains batchId': (r) => r.body.includes('batchId'),
      'response contains emails': (r) => r.body.includes('emails'),
    });

    errorRate.add(!success);
  });

  sleep(2);
}
