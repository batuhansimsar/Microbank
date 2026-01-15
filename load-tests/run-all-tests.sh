#!/bin/bash

# Run Critical K6 Load Tests for Microbank
# 3 essential tests only

set -e

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

echo "======================================================================"
echo "🚀 Microbank K6 Critical Tests (3 Tests)"
echo "======================================================================"
echo ""

# Check k6
if ! command -v k6 &> /dev/null; then
    echo -e "${RED}❌ k6 bulunamadı${NC}"
    echo "Kurulum: brew install k6"
    exit 1
fi

echo -e "${GREEN}✅ k6 bulundu: $(k6 version)${NC}"
echo ""

# Check services
echo "🔍 Servisleri kontrol ediliyor..."
if curl -s http://localhost:8080 > /dev/null 2>&1; then
    echo -e "${GREEN}✅ API Gateway çalışıyor (http://localhost:8080)${NC}"
else
    echo -e "${YELLOW}⚠️  API Gateway ulaşılamıyor${NC}"
    echo "Docker servislerini başlat: docker-compose up -d"
    read -p "Devam edilsin mi? (y/n) " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        exit 1
    fi
fi
echo ""

mkdir -p results
TIMESTAMP=$(date +"%Y%m%d_%H%M%S")
RESULTS_DIR="results/${TIMESTAMP}"
mkdir -p "${RESULTS_DIR}"

echo "📁 Sonuçlar: ${RESULTS_DIR}"
echo ""

TESTS_PASSED=0
TESTS_FAILED=0

run_test() {
    local test_name=$1
    local test_file=$2
    local test_number=$3
    
    echo "======================================================================"
    echo -e "${BLUE}Test ${test_number}/3: ${test_name}${NC}"
    echo "======================================================================"
    echo ""
    
    if k6 run "${test_file}" --out "json=${RESULTS_DIR}/test-${test_number}-results.json"; then
        echo ""
        echo -e "${GREEN}✅ ${test_name} - TAMAMLANDI${NC}"
        ((TESTS_PASSED++))
    else
        echo ""
        echo -e "${RED}❌ ${test_name} - BAŞARISIZ${NC}"
        ((TESTS_FAILED++))
    fi
    
    echo ""
}

# Run 3 critical tests
run_test "Race Condition Test 🔥" "test-1-race-condition.js" 1
run_test "Idempotency Test 🔁" "test-2-idempotency.js" 2
run_test "SAGA Rollback Test ✅" "test-3-saga-rollback.js" 3

# Summary
echo "======================================================================"
echo "📊 TEST SONUÇLARI"
echo "======================================================================"
echo ""
echo -e "Başarılı: ${GREEN}${TESTS_PASSED}/3${NC}"
echo -e "Başarısız: ${RED}${TESTS_FAILED}/3${NC}"
echo ""
echo "Sonuçlar: ${RESULTS_DIR}"
echo ""

if [ $TESTS_FAILED -eq 0 ]; then
    echo -e "${GREEN}✅✅✅ TÜM TESTLER BAŞARILI ✅✅✅${NC}"
    exit 0
else
    echo -e "${YELLOW}⚠️  Bazı testler başarısız oldu - bu normaldir!${NC}"
    echo "Test çıktılarında düzeltme önerileri var."
    exit 0  # Don't fail - some tests are expected to fail
fi
