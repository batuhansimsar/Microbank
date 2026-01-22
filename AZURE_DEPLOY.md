# 🚀 Azure Deployment - Hızlı Başlangıç

## 📋 Ekranda Göreceğin Yere Eklenecek Ayarlar

Her App Service için **Settings → Environment variables** kısmına ekle:

---

## 🔐 IDENTITY SERVICE

Resimde gördüğün "Add/Edit application setting" penceresine şunları ekle:

### 1. Redis Connection
```
Name:  ConnectionStrings__Redis
Value: YOUR_REDIS_ENDPOINT:PORT,user=default,password=YOUR_REDIS_PASSWORD,ssl=True,abortConnect=false
```

**Get your Redis Cloud credentials from:** https://redis.io/cloud/

### 2. JWT Secret (GİZLİ!)
```
Name:  JwtSettings__SecretKey
Value: YourSuperSecretKeyThatIsAtLeast32CharactersLong!
```

### 3. Database (Azure PostgreSQL kullanıyorsan)
```
Name:  ConnectionStrings__DefaultConnection
Value: Host=SENIN_POSTGRES_SERVER.postgres.database.azure.com;Port=5432;Database=microbank_identity;Username=SENIN_USER;Password=SENIN_PASSWORD;SslMode=Require
```

---

## 🔐 ACCOUNT SERVICE

### 1. Redis Connection (ZORUNLU!)
```
Name:  ConnectionStrings__Redis
Value: YOUR_REDIS_ENDPOINT:PORT,user=default,password=YOUR_REDIS_PASSWORD,ssl=True,abortConnect=false
```

### 2. JWT Secret (Identity ile AYNI olmalı!)
```
Name:  JwtSettings__SecretKey
Value: YourSuperSecretKeyThatIsAtLeast32CharactersLong!
```

### 3. Database
```
Name:  ConnectionStrings__DefaultConnection
Value: Host=SENIN_POSTGRES_SERVER.postgres.database.azure.com;Port=5432;Database=microbank_account;Username=SENIN_USER;Password=SENIN_PASSWORD;SslMode=Require
```

### 4. RabbitMQ Host
```
Name:  RabbitMQ__Host
Value: SENIN_RABBITMQ_HOST
```

---

## 🔐 TRANSFER SERVICE

### 1. Redis Connection
```
Name:  ConnectionStrings__Redis
Value: YOUR_REDIS_ENDPOINT:PORT,user=default,password=YOUR_REDIS_PASSWORD,ssl=True,abortConnect=false
```

**Get your Redis Cloud credentials from:** https://redis.io/cloud/

### 2. JWT Secret (Diğerleriyle AYNI!)
```
Name:  JwtSettings__SecretKey
Value: YourSuperSecretKeyThatIsAtLeast32CharactersLong!
```

### 3. Database
```
Name:  ConnectionStrings__DefaultConnection
Value: Host=SENIN_POSTGRES_SERVER.postgres.database.azure.com;Port=5432;Database=microbank_transfer;Username=SENIN_USER;Password=SENIN_PASSWORD;SslMode=Require
```

### 4. RabbitMQ Host
```
Name:  RabbitMQ__Host
Value: SENIN_RABBITMQ_HOST
```

---

## ⚠️ ÇOK ÖNEMLİ!

1. **JWT SecretKey**: Üç serviste de birebir AYNI olmalı!
2. **Redis Cloud**: Benim verdiğim password kullan (ücretsiz)
3. **PostgreSQL**: Azure'da kendin oluştur veya başka provider kullan
4. **RabbitMQ**: CloudAMQP (managed) önerilir

---

## ✅ GitHub'a Yükleme

```bash
cd /Users/esrefbatuhansimsar/Desktop/Microservices/Microbank

# Git kontrolü
git status

# Tüm değişiklikleri ekle
git add .

# Commit
git commit -m "Secure Redis configuration for Azure deployment"

# GitHub'a push
git push origin main
```

**Artık GitHub'da hiçbir gizli bilgi yok!** ✅

---

## 📝 Senin Yapman Gerekenler

1. ✅ GitHub'a projeyi yükle (gizli bilgiler temizlendi)
2. ✅ Azure'da PostgreSQL oluştur (veya başka provider)
3. ✅ Azure'da RabbitMQ ayarla (CloudAMQP önerilir)
4. ✅ Her App Service'e yukarıdaki environment variable'ları ekle
5. ✅ GitHub'tan Azure'a deploy et
6. ✅ Bitir! 🎉
