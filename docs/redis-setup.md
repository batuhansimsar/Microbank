# Redis Setup Guide

## Overview

This guide explains how to configure Redis for the Microbank microservices architecture. Redis is used for:
- **Distributed locking** (Account service): Prevents race conditions during concurrent account operations
- **Future use cases**: Caching, session management, rate limiting

## Configuration Options

### Option 1: Local Docker Redis (Development)

This is the default configuration for local development and testing.

#### Connection String Format
```
localhost:6379,abortConnect=false
```

Or from within Docker containers:
```
redis:6379,abortConnect=false
```

#### Setup Steps

1. **Already configured!** The `docker-compose.yml` file includes a Redis service.

2. **Start Redis with other services:**
   ```bash
   docker-compose up -d redis
   ```

3. **Verify Redis is running:**
   ```bash
   docker-compose ps redis
   docker-compose logs redis
   ```

4. **Test Redis connection:**
   ```bash
   docker-compose exec redis redis-cli ping
   # Should return: PONG
   ```

#### Features
- ✅ Free tier with 30MB storage
- ✅ Automatic persistence to Docker volume (`redis-data`)
- ✅ No authentication required (safe for local development)
- ✅ Fast startup and teardown

---

### Option 2: Redis Cloud (Production)

For production deployments, use Redis Cloud for managed, scalable Redis instances.

#### Connection String Format
```
<endpoint>:<port>,password=<password>,ssl=True,abortConnect=false
```

**Example:**
```
YOUR_REDIS_ENDPOINT:PORT,password=YOUR_PASSWORD_HERE,ssl=True,abortConnect=false
```

#### Setup Steps

1. **Create a Redis Cloud database:**
   - Go to [Redis Cloud Console](https://redis.io/cloud/)
   - Create a new database (Free tier available)
   - Select your cloud provider (e.g., AWS, Azure)
   - Choose a region
   - **Note down the endpoint and password**
   
   ⚠️ **Get your password**: Click the "Connect" button in Redis Cloud dashboard or check database configuration

2. **Update configuration for production:**

   **For Docker deployments**, update `docker-compose.yml`:
   ```yaml
   account-api:
     environment:
       - ConnectionStrings__Redis=YOUR_REDIS_ENDPOINT:PORT,password=YOUR_REDIS_PASSWORD,ssl=True,abortConnect=false
   ```

   **For local testing with cloud Redis**, update `appsettings.json`:
   ```json
   {
     "ConnectionStrings": {
       "Redis": "YOUR_REDIS_ENDPOINT:PORT,password=YOUR_REDIS_PASSWORD,ssl=True,abortConnect=false"
     }
   }
   ```

3. **Security best practices:**
   - ✅ Use environment variables for passwords (never commit to git)
   - ✅ Enable SSL/TLS (included in connection string above)
   - ✅ Restrict IP access in Redis Cloud dashboard
   - ✅ Rotate passwords regularly

#### Redis Cloud Features
- **Free tier available**: 30MB RAM, 30 connections
- **Managed service**: Automatic backups and updates
- **Cloud providers**: AWS, Azure, GCP
- **SSL/TLS**: Encrypted connections
- **High availability**: Optional for paid tiers

> **Note**: Get your credentials from the Redis Cloud dashboard to complete the connection string.

---

## Current Service Configuration

### Account Service
- **Uses Redis**: ✅ Yes
- **Purpose**: Distributed locking for account operations
- **Implementation**: `RedisDistributedLockService` using StackExchange.Redis
- **Configuration**: `ConnectionStrings:Redis` in appsettings.json

### Transfer Service
- **Uses Redis**: ⚠️ Available for future use
- **Purpose**: Can be used for caching or distributed locking
- **Configuration**: Connection string configured but not yet utilized

### Identity Service
- **Uses Redis**: ⚠️ Available for future use
- **Purpose**: Can be used for session management or token blacklisting
- **Configuration**: Connection string configured but not yet utilized

---

## Troubleshooting

### Connection Errors

**Error: "It was not possible to connect to the redis server(s)"**
- Check if Redis is running: `docker-compose ps redis`
- Verify connection string format
- For cloud: check firewall/IP whitelist settings

**Error: "NOAUTH Authentication required"**
- Add password to connection string: `redis:6379,password=yourpass`

### Performance Issues

**Slow operations:**
- Check Redis memory usage: `docker-compose exec redis redis-cli INFO memory`
- Monitor connections: `docker-compose exec redis redis-cli CLIENT LIST`

### Data Persistence

**Local Docker:**
- Data persists in Docker volume: `redis-data`
- To clear all Redis data: `docker-compose down -v redis-data`

**Redis Cloud:**
- Automatic persistence and backups (depending on tier)
- Check Redis Cloud dashboard for backup settings

---

## Testing Redis Integration

### 1. Test Distributed Locking (Account Service)

```bash
# Start services
docker-compose up -d

# Create test accounts and perform concurrent transfers
# The distributed lock should prevent race conditions

# Monitor Redis keys
docker-compose exec redis redis-cli KEYS '*'
docker-compose exec redis redis-cli GET "lock:account:123"
```

### 2. Monitor Redis Activity

```bash
# Watch Redis commands in real-time
docker-compose exec redis redis-cli MONITOR

# Check Redis stats
docker-compose exec redis redis-cli INFO stats
```

---

## Additional Resources

- [StackExchange.Redis Documentation](https://stackexchange.github.io/StackExchange.Redis/)
- [Redis Cloud Free Tier](https://redis.io/try-free/)
- [Redis Best Practices](https://redis.io/docs/manual/patterns/)
