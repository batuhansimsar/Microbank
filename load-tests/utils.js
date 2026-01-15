// K6 Utility Functions for Microbank Load Tests
import http from 'k6/http';
import { check, sleep } from 'k6';
import { config } from './config.js';

/**
 * Register a new user
 * @param {string} username 
 * @param {string} password 
 * @returns {boolean} Success status
 */
export function registerUser(username, password) {
    const payload = JSON.stringify({
        email: `${username}@test.com`,
        password: password,
        fullName: username,
    });

    const params = {
        headers: {
            'Content-Type': 'application/json',
        },
        timeout: config.timeouts.http,
    };

    const response = http.post(`${config.baseUrl}/api/auth/register`, payload, params);

    const success = check(response, {
        'user registration successful': (r) => r.status === 200 || r.status === 400, // 400 if already exists
    });

    return success;
}

/**
 * Authenticate and get JWT token
 * @param {string} username 
 * @param {string} password 
 * @returns {string|null} JWT token or null on failure
 */
export function getAuthToken(username, password) {
    const payload = JSON.stringify({
        email: `${username}@test.com`,
        password: password,
    });

    const params = {
        headers: {
            'Content-Type': 'application/json',
        },
        timeout: config.timeouts.http,
    };

    const response = http.post(`${config.baseUrl}/api/auth/login`, payload, params);

    const success = check(response, {
        'authentication successful': (r) => r.status === 200,
        'token received': (r) => r.json('token') !== undefined,
    });

    if (success && response.status === 200) {
        const body = response.json();
        return body.token;
    }

    return null;
}

/**
 * Create a new bank account for authenticated user
 * @param {string} token JWT authentication token
 * @returns {object|null} Account object {id, accountNumber, balance, currency} or null
 */
export function createTestAccount(token) {
    const params = {
        headers: {
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${token}`,
        },
        timeout: config.timeouts.http,
    };

    const response = http.post(`${config.baseUrl}/api/accounts`, null, params);

    const success = check(response, {
        'account creation successful': (r) => r.status === 200,
        'account data received': (r) => r.json('id') !== undefined,
    });

    if (success && response.status === 200) {
        return response.json();
    }

    return null;
}

/**
 * Get account balance
 * @param {string} token JWT authentication token
 * @param {string} accountId Account ID (GUID)
 * @returns {number|null} Current balance or null on failure
 */
export function getAccountBalance(token, accountId) {
    const params = {
        headers: {
            'Authorization': `Bearer ${token}`,
        },
        timeout: config.timeouts.http,
    };

    const response = http.get(`${config.baseUrl}/api/accounts/${accountId}/balance`, params);

    if (response.status === 200) {
        const body = response.json();
        return body.balance;
    }

    return null;
}

/**
 * Get full account details
 * @param {string} token JWT authentication token
 * @param {string} accountId Account ID (GUID)
 * @returns {object|null} Account object or null
 */
export function getAccountDetails(token, accountId) {
    const params = {
        headers: {
            'Authorization': `Bearer ${token}`,
        },
        timeout: config.timeouts.http,
    };

    const response = http.get(`${config.baseUrl}/api/accounts/${accountId}`, params);

    if (response.status === 200) {
        return response.json();
    }

    return null;
}

/**
 * Initiate a money transfer
 * @param {string} token JWT authentication token
 * @param {string} fromAccountId Source account ID
 * @param {string} toAccountId Destination account ID
 * @param {number} amount Transfer amount
 * @param {string} currency Currency code (default: TRY)
 * @param {string} idempotencyKey Optional idempotency key
 * @returns {object} Response object {response, transferId, status}
 */
export function initiateTransfer(token, fromAccountId, toAccountId, amount, currency = 'TRY', idempotencyKey = null) {
    const payload = JSON.stringify({
        fromAccountId: fromAccountId,
        toAccountId: toAccountId,
        amount: amount,
        currency: currency,
    });

    const headers = {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${token}`,
    };

    // Add idempotency key if provided
    if (idempotencyKey) {
        headers['Idempotency-Key'] = idempotencyKey;
    }

    const params = {
        headers: headers,
        timeout: config.timeouts.http,
    };

    const response = http.post(`${config.baseUrl}/api/transfers`, payload, params);

    let transferId = null;
    let status = null;

    if (response.status === 200) {
        const body = response.json();
        transferId = body.transferId;
        status = body.status;
    }

    return {
        response: response,
        transferId: transferId,
        status: status,
    };
}

/**
 * Get transfer status
 * @param {string} token JWT authentication token
 * @param {string} transferId Transfer ID
 * @returns {object|null} Transfer object or null
 */
export function getTransferStatus(token, transferId) {
    const params = {
        headers: {
            'Authorization': `Bearer ${token}`,
        },
        timeout: config.timeouts.http,
    };

    const response = http.get(`${config.baseUrl}/api/transfers/${transferId}`, params);

    if (response.status === 200) {
        return response.json();
    }

    return null;
}

/**
 * Wait for transfer to complete (poll status)
 * @param {string} token JWT authentication token
 * @param {string} transferId Transfer ID
 * @param {number} maxWaitMs Maximum wait time in milliseconds
 * @returns {object|null} Final transfer status or null on timeout
 */
export function waitForTransferCompletion(token, transferId, maxWaitMs = 30000) {
    const startTime = Date.now();
    const pollInterval = config.timeouts.pollInterval;

    while (Date.now() - startTime < maxWaitMs) {
        const transfer = getTransferStatus(token, transferId);

        if (transfer && (transfer.Status === 'Completed' || transfer.Status === 'Failed')) {
            return transfer;
        }

        sleep(pollInterval / 1000); // k6 sleep takes seconds
    }

    return null; // Timeout
}

/**
 * Generate a unique idempotency key
 * @returns {string} UUID v4 string
 */
export function generateIdempotencyKey() {
    // Simple UUID v4 generator for k6
    return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function (c) {
        const r = Math.random() * 16 | 0;
        const v = c === 'x' ? r : (r & 0x3 | 0x8);
        return v.toString(16);
    });
}

/**
 * Generate random amount between min and max
 * @param {number} min Minimum amount
 * @param {number} max Maximum amount
 * @returns {number} Random amount with 2 decimal places
 */
export function randomAmount(min = 10, max = 500) {
    return Math.round((Math.random() * (max - min) + min) * 100) / 100;
}

/**
 * Setup helper: Create user and account
 * @param {string} username 
 * @param {string} password 
 * @returns {object|null} {token, userId, account} or null on failure
 */
export function setupUserAndAccount(username, password) {
    // Register user (ignore if already exists)
    registerUser(username, password);

    // Login to get token
    const token = getAuthToken(username, password);
    if (!token) {
        console.error(`Failed to authenticate user: ${username}`);
        return null;
    }

    // Create account
    const account = createTestAccount(token);
    if (!account) {
        console.error(`Failed to create account for user: ${username}`);
        return null;
    }

    return {
        token: token,
        account: account,
    };
}
