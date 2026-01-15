// TEST 2: Idempotency Testing 🔁 - WITH IDEMPOTENCY-KEY HEADER

import { check, group } from 'k6';
import http from 'k6/http';
import { config } from './config.js';
import { setupUserAndAccount, getAccountBalance } from './utils.js';

export const options = {
    vus: 1,
    iterations: 1,
    thresholds: {
        'checks{scenario:idempotency}': ['rate>0.7'],
    },
};

export default function () {
    group('Idempotency Test - WITH Idempotency-Key Header', () => {
        console.log('🔁 Starting Idempotency Test...');
        console.log('✅ Testing WITH Idempotency-Key header support');
        console.log('');

        // SETUP
        const sender = setupUserAndAccount('idempotency-sender', 'Test@123');
        const receiver = setupUserAndAccount('idempotency-receiver', 'Test@123');

        if (!sender || !receiver) {
            console.error('❌ Failed to setup test accounts');
            return;
        }

        console.log(`✅ Setup complete:`);
        console.log(`   Sender Account: ${sender.account.id} (Balance: ${sender.account.balance} TRY)`);
        console.log(`   Receiver Account: ${receiver.account.id}`);

        const initialBalance = sender.account.balance;
        const initialReceiverBalance = getAccountBalance(receiver.token, receiver.account.id);
        const transferAmount = 50;

        // IDEMPOTENCY TEST: Send 10 requests with SAME idempotency key
        const idempotencyKey = `test-key-${Date.now()}`;

        console.log(`\\n🚀 Sending 10 rapid-fire transfer requests...`);
        console.log(`   Amount: ${transferAmount} TRY each`);
        console.log(`   Idempotency-Key: ${idempotencyKey}`);
        console.log(`   Expected: Only 1 transfer should be created (others return cached response)`);

        let successCount = 0;
        let cachedCount = 0;
        let transferIds = new Set();

        for (let i = 0; i < 10; i++) {
            const response = http.post(
                `${config.baseUrl}/api/transfers`,
                JSON.stringify({
                    fromAccountId: sender.account.id,
                    toAccountId: receiver.account.id,
                    amount: transferAmount,
                    currency: 'TRY',
                }),
                {
                    headers: {
                        'Content-Type': 'application/json',
                        'Authorization': `Bearer ${sender.token}`,
                        'Idempotency-Key': idempotencyKey,  // SAME KEY FOR ALL!
                    },
                    tags: { scenario: 'idempotency' },
                }
            );

            if (response.status === 200) {
                successCount++;
                const body = response.json();
                const transferId = body.transferId;

                if (transferIds.has(transferId)) {
                    cachedCount++;
                    console.log(`   Request ${i + 1}: ✅ CACHED - Same Transfer ID: ${transferId}`);
                } else {
                    transferIds.add(transferId);
                    console.log(`   Request ${i + 1}: ✅ NEW - Transfer ID: ${transferId}`);
                }
            } else {
                console.log(`   Request ${i + 1}: ❌ FAILED - Status: ${response.status}`);
            }
        }

        console.log(`\\n📊 Results:`);
        console.log(`   Requests Sent: 10`);
        console.log(`   Successful Responses: ${successCount}`);
        console.log(`   Unique Transfer IDs: ${transferIds.size}`);
        console.log(`   Cached Responses: ${cachedCount}`);

        // Wait for SAGA
        console.log('\\n⏳ Waiting 8 seconds for SAGA to complete...');
        http.batch([{ method: 'GET', url: 'http://httpbin.org/delay/8' }]);

        // BALANCE VERIFICATION
        console.log('\\n💰 Verifying final balances...');
        const finalSenderBalance = getAccountBalance(sender.token, sender.account.id);
        const finalReceiverBalance = getAccountBalance(receiver.token, receiver.account.id);

        console.log(`   Sender Balance:`);
        console.log(`      Initial: ${initialBalance} TRY`);
        console.log(`      Final: ${finalSenderBalance} TRY`);
        console.log(`      Debited: ${initialBalance - finalSenderBalance} TRY`);

        console.log(`   Receiver Balance:`);
        console.log(`      Initial: ${initialReceiverBalance} TRY`);
        console.log(`      Final: ${finalReceiverBalance} TRY`);
        console.log(`      Credited: ${finalReceiverBalance - initialReceiverBalance} TRY`);

        // ANALYSIS
        const actualTransfers = Math.round((initialBalance - finalSenderBalance) / transferAmount);
        const totalMoney_before = initialBalance + initialReceiverBalance;
        const totalMoney_after = finalSenderBalance + finalReceiverBalance;
        const moneyConserved = Math.abs(totalMoney_after - totalMoney_before) < 0.01;

        console.log(`\\n🔍 Analysis:`);
        console.log(`   Expected with idempotency: Only 1 transfer`);
        console.log(`   Actual transfers completed: ${actualTransfers}`);
        console.log(`   Unique Transfer IDs: ${transferIds.size}`);
        console.log(`   Money conserved: ${moneyConserved ? 'YES ✅' : 'NO ❌'}`);

        // CHECKS
        const idempotency_works = transferIds.size === 1 && actualTransfers === 1 && cachedCount === 9;

        check({ transferIds: transferIds.size, actualTransfers, cachedCount, moneyConserved }, {
            '✅ CRITICAL: Only 1 unique transfer ID': () => transferIds.size === 1,
            '✅ CRITICAL: Only 1 actual transfer completed': () => actualTransfers === 1,
            '✅ CRITICAL: 9 requests returned cached response': () => cachedCount === 9,
            '✅ CRITICAL: Money is conserved': () => moneyConserved,
            '✅ Final sender balance correct': () => Math.abs(finalSenderBalance - (initialBalance - transferAmount)) < 0.01,
        }, { scenario: 'idempotency' });

        // VERDICT
        console.log('\\n' + '='.repeat(70));
        if (idempotency_works) {
            console.log('✅✅✅ IDEMPOTENCY WORKING PERFECTLY! ✅✅✅');
            console.log('');
            console.log('RESULTS:');
            console.log(`   ✅ 10 requests sent with same Idempotency-Key`);
            console.log(`   ✅ Only 1 transfer created (${Array.from(transferIds)[0]})`);
            console.log(`   ✅ 9 requests returned cached response`);
            console.log(`   ✅ Only ${transferAmount} TRY debited (not ${transferAmount * 10})`);
            console.log(`   ✅ Money conserved`);
            console.log('');
            console.log('HOW IT WORKS:');
            console.log('   1. First request creates transfer and caches response');
            console.log('   2. Requests 2-10 find cached response by idempotency key');
            console.log('   3. Cached response returned immediately (no new transfer)');
            console.log('   4. User protected from accidental duplicate charges!');
            console.log('');
            console.log('✨ Your system is PROTECTED against duplicate transactions! ✨');
        } else {
            console.error('🚨 IDEMPOTENCY NOT WORKING! 🚨');
            console.error('');
            console.error(`PROBLEM: ${transferIds.size} transfers created instead of 1!`);
            console.error(`PROBLEM: ${cachedCount} cached responses instead of 9!`);
            console.error('');
            console.error('INVESTIGATION NEEDED:');
            console.error('   1. Check if IdempotencyService is registered in DI');
            console.error('   2. Check if Idempotency-Key header is being read correctly');
            console.error('   3. Check database for IdempotentRequests table');
            console.error('   4. Review TransfersController.InitiateTransfer() logic');
        }
        console.log('='.repeat(70));
    });
}
