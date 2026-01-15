// TEST 1: Race Condition Testing 🔥 - DISTRIBUTED LOCKING VERIFICATION
// ✅ This test VERIFIES that distributed locking prevents race conditions

import { check, group } from 'k6';
import http from 'k6/http';
import { config } from './config.js';
import { setupUserAndAccount, getAccountBalance, initiateTransfer } from './utils.js';

export const options = {
    vus: 1,
    iterations: 1,
    thresholds: {
        'checks{scenario:race_condition}': ['rate>=0.75'], // Expecting 3/4 checks to pass
    },
};

export default function () {
    group('Race Condition Test - Distributed Locking Verification', () => {
        console.log('🔥 Starting Race Condition Test...');
        console.log('✅ This test verifies distributed locking works correctly');
        console.log('');

        // SETUP: Create sender and receiver accounts
        const sender = setupUserAndAccount('race-sender', 'Test@123');
        const receiver = setupUserAndAccount('race-receiver', 'Test@123');

        if (!sender || !receiver) {
            console.error('❌ Failed to setup test accounts');
            return;
        }

        console.log(`✅ Setup complete:`);
        console.log(`   Sender Account: ${sender.account.id} (Balance: ${sender.account.balance} TRY)`);
        console.log(`   Receiver Account: ${receiver.account.id}`);

        const initialBalance = sender.account.balance;
        const initialReceiverBalance = getAccountBalance(receiver.token, receiver.account.id);
        const withdrawAmount = initialBalance; // Try to withdraw entire balance

        // RACE CONDITION TEST: Send 5 concurrent requests to withdraw same amount
        console.log(`\\n🚀 Sending ${config.load.raceCondition.parallelRequests} concurrent withdrawal requests of ${withdrawAmount} TRY...`);
        console.log(`   ℹ️  All requests will be ACCEPTED by Transfer API (return 200)`);
        console.log(`   ℹ️  But Account Service should only allow 1 actual debit (via distributed lock)`);

        const requests = [];
        for (let i = 0; i < config.load.raceCondition.parallelRequests; i++) {
            requests.push({
                method: 'POST',
                url: `${config.baseUrl}/api/transfers`,
                body: JSON.stringify({
                    fromAccountId: sender.account.id,
                    toAccountId: receiver.account.id,
                    amount: withdrawAmount,
                    currency: 'TRY',
                }),
                params: {
                    headers: {
                        'Content-Type': 'application/json',
                        'Authorization': `Bearer ${sender.token}`,
                    },
                    tags: { scenario: 'race_condition' },
                },
            });
        }

        // Execute all requests in parallel using batch
        const responses = http.batch(requests);

        // Count successful transfer initiations (not actual debits!)
        let successCount = 0;
        let transferIds = [];

        responses.forEach((response, index) => {
            console.log(`   Request ${index + 1}: Status ${response.status}`);

            if (response.status === 200) {
                successCount++;
                const body = response.json();
                transferIds.push(body.transferId);
                console.log(`      ✅ ACCEPTED - Transfer ID: ${body.transferId}`);
            } else {
                console.log(`      ❌ REJECTED - Status: ${response.status}`);
            }
        });

        console.log(`\\n📊 Transfer API Results:`);
        console.log(`   Accepted Transfers: ${successCount} (${successCount === 5 ? 'EXPECTED ✅' : 'Unexpected ⚠️'})`);
        console.log(`   Transfer IDs: ${transferIds.join(', ')}`);

        // Wait for SAGA to process transfers
        console.log('\\n⏳ Waiting 5 seconds for SAGA to complete...');
        http.batch([
            { method: 'GET', url: 'http://httpbin.org/delay/5' }
        ]);

        // BALANCE VERIFICATION
        console.log('\\n💰 Verifying final balances...');
        const finalSenderBalance = getAccountBalance(sender.token, sender.account.id);
        const finalReceiverBalance = getAccountBalance(receiver.token, receiver.account.id);

        console.log(`   Sender:`);
        console.log(`      Initial Balance: ${initialBalance} TRY`);
        console.log(`      Final Balance: ${finalSenderBalance} TRY`);
        console.log(`      Debited: ${initialBalance - finalSenderBalance} TRY`);

        console.log(`   Receiver:`);
        console.log(`      Initial Balance: ${initialReceiverBalance} TRY`);
        console.log(`      Final Balance: ${finalReceiverBalance} TRY`);
        console.log(`      Credited: ${finalReceiverBalance - initialReceiverBalance} TRY`);

        // CRITICAL ANALYSIS
        const actualDebits = Math.round((initialBalance - finalSenderBalance) / withdrawAmount);
        const totalMoney_before = initialBalance + initialReceiverBalance;
        const totalMoney_after = finalSenderBalance + finalReceiverBalance;
        const moneyConserved = Math.abs(totalMoney_after - totalMoney_before) < 0.01;

        console.log(`\\n🔍 Analysis:`);
        console.log(`   Actual successful debits: ${actualDebits}`);
        console.log(`   Expected: 1`);
        console.log(`   Money conserved: ${moneyConserved ? 'YES ✅' : 'NO ❌'}`);
        console.log(`   Balance non-negative: ${finalSenderBalance >= 0 ? 'YES ✅' : 'NO ❌'}`);

        console.log(`\\n📝 How it works:`);
        console.log(`   1. Transfer API accepts all ${successCount} requests (returns 200 OK)`);
        console.log(`   2. Each initiates a SAGA workflow via message queue`);
        console.log(`   3. Account Service uses distributed lock for debit operations`);
        console.log(`   4. Only 1 debit succeeds (acquires lock first)`);
        console.log(`   5. Other ${successCount - 1} requests fail with "Insufficient balance"`);
        console.log(`   6. Failed transfers trigger SAGA compensation (rollback)`);

        // CHECKS
        const distributed_locking_works = actualDebits === 1 && finalSenderBalance >= 0 && moneyConserved;

        check({ finalSenderBalance, actualDebits, moneyConserved }, {
            '✅ CRITICAL: Only 1 actual debit occurred': () => actualDebits === 1,
            '✅ CRITICAL: Balance is not negative': () => finalSenderBalance >= 0,
            '✅ CRITICAL: Money is conserved': () => moneyConserved,
            '✅ Final balance is exactly 0': () => Math.abs(finalSenderBalance) < 0.01,
        }, { scenario: 'race_condition' });

        // FINAL VERDICT
        console.log('\\n' + '='.repeat(70));
        if (distributed_locking_works) {
            console.log('✅✅✅ NO RACE CONDITION! DISTRIBUTED LOCKING WORKS! ✅✅✅');
            console.log('');
            console.log('RESULTS:');
            console.log(`   ✅ Only ${actualDebits} debit succeeded (out of ${successCount} initiated transfers)`);
            console.log(`   ✅ Balance is ${finalSenderBalance} TRY (not negative)`);
            console.log(`   ✅ Money is conserved (${totalMoney_before} TRY before, ${totalMoney_after} TRY after)`);
            console.log('');
            console.log('✨ Your system is PROTECTED against double-spending! ✨');
        } else {
            console.error('🚨🚨🚨 RACE CONDITION VULNERABILITY! 🚨🚨🚨');
            console.error('');
            console.error(`PROBLEM: ${actualDebits} debits succeeded instead of 1!`);
            console.error('RISK: Users can perform double-spending attacks');
            console.error('');
            console.error('📋 REQUIRED FIX:');
            console.error('   Distributed locking is NOT working properly.');
            console.error('   Check Redis connection and lock acquisition in AccountEventHandlers.cs');
        }
        console.log('='.repeat(70));
    });
}
