# K6 Load Tests - Hızlı Başlangıç 🚀

## 3 Kritik Test ⚠️

Bu testler projenin **kritik açıklarını bul ve göster** için tasarlandı.
Bazı testler **BAŞARISIZ OLACAK** - bu normaldir!

## Hızlı Kurulum

```bash
# 1. k6 Kur
brew install k6

# 2. Docker Servislerini Başlat
cd /Users/esrefbatuhansimsar/Desktop/Microservices/Microbank
docker-compose up -d

# 3. Test Dizinine Git
cd load-tests

# 4. Tüm Testleri Çalıştır
./run-all-tests.sh
```

## Tek Tek Test Çalıştırma

```bash
# Test 1: Race Condition (double-spending açığı)
k6 run test-1-race-condition.js

# Test 2: Idempotency (duplike işlem kontrolü)
k6 run test-2-idempotency.js

# Test 3: SAGA Rollback (otomatik geri alma)
k6 run test-3-saga-rollback.js
```

## Test Sonuçları Beklentileri

| Test | Durum | Ne Demek? |
|------|-------|-----------|
| 🔥 **Test 1: Race Condition** | ❌ BAŞARISIZ | Distributed lock GEREK |
| 🔁 **Test 2: Idempotency** | ⚠️ Kısmi | Idempotency key GEREK |
| ✅ **Test 3: SAGA Rollback** | ✅ BAŞARILI | Zaten çalışıyor! |

## Sonraki Adımlar

Her test çıktısında **nasıl düzelteceğin** yazıyor!

### Öncelik Sırası:
1. 🔥 **Distributed Locking ekle** (Test 1)
2. 🔁 **Idempotency Key ekle** (Test 2)
3. ✅ **SAGA çalışıyor** - kontrol et (Test 3)

## Detay için

- [README.md](README.md) - Tam dokümantasyon
- Test çıktıları - Her başarısız testte fix önerileri var

**Başarısızlık = Öğrenme fırsatı! 🎯**
