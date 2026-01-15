// K6 Load Test Configuration for Microbank
export const config = {
    // API Gateway endpoint
    baseUrl: __ENV.API_URL || 'http://localhost:8080',

    // Test user credentials (will be created during setup)
    testUsers: {
        user1: { username: 'testuser1', password: 'Test@123' },
        user2: { username: 'testuser2', password: 'Test@123' },
        user3: { username: 'testuser3', password: 'Test@123' },
    },

    // Load test parameters
    load: {
        raceCondition: {
            vus: 1,
            duration: '30s',
            parallelRequests: 5, // Number of concurrent requests in race condition test
        },
        idempotency: {
            vus: 1,
            duration: '30s',
            duplicateRequests: 10, // Number of duplicate requests with same idempotency key
        },
        sagaRollback: {
            vus: 10,
            duration: '2m',
        },
        deadlockStress: {
            stages: [
                { duration: '1m', target: 50 },  // Ramp up to 50 VUs
                { duration: '3m', target: 200 }, // Ramp up to 200 VUs
                { duration: '2m', target: 500 }, // Peak at 500 VUs
                { duration: '2m', target: 200 }, // Ramp down
                { duration: '1m', target: 0 },   // Cool down
            ],
        },
        circuitBreaker: {
            stages: [
                { duration: '30s', target: 10 },  // Normal load
                { duration: '2m', target: 100 },  // Stress with slow external service
                { duration: '1m', target: 0 },    // Cool down
            ],
        },
    },

    // Thresholds for test success criteria
    thresholds: {
        http_req_duration: ['p(95)<500', 'p(99)<1000'], // 95% of requests under 500ms
        http_req_failed: ['rate<0.05'], // Less than 5% failures (excluding business logic rejections)
    },

    // Timeouts
    timeouts: {
        http: '30s',
        transferCompletion: 30000, // 30 seconds to wait for SAGA completion
        pollInterval: 500, // 500ms between status checks
    },

    // Mock external service configuration
    mockServices: {
        smsServiceUrl: __ENV.SMS_SERVICE_URL || 'http://localhost:3000',
    },
};

// Test data file path
export const TEST_DATA_FILE = './test-data.json';
