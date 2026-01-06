# JWT Secret Management Documentation

## Overview
JWT secrets have been removed from `appsettings.json` files to follow security best practices. Secrets should be managed through environment variables or user secrets.

## Development Setup (User Secrets)

### For Each Service:

#### Identity.API
```bash
cd src/Services/Identity/Identity.API
dotnet user-secrets init
dotnet user-secrets set "JwtSettings:SecretKey" "DEV-SECRET-KEY-32-CHARS-MINIMUM-LENGTH"
```

#### Account.API
```bash
cd src/Services/Account/Account.API
dotnet user-secrets init
dotnet user-secrets set "JwtSettings:SecretKey" "DEV-SECRET-KEY-32-CHARS-MINIMUM-LENGTH"
```

#### Transfer.API
```bash
cd src/Services/Transfer/Transfer.API
dotnet user-secrets init
dotnet user-secrets set "JwtSettings:SecretKey" "DEV-SECRET-KEY-32-CHARS-MINIMUM-LENGTH"
```

#### Gateway.API (if applicable)
```bash
cd src/ApiGateways/Gateway.API
dotnet user-secrets init
dotnet user-secrets set "JwtSettings:SecretKey" "DEV-SECRET-KEY-32-CHARS-MINIMUM-LENGTH"
```

## Production Setup (Docker + Environment Variables)

### Step 1: Create `.env` file
```bash
cp .env.example .env
```

### Step 2: Generate Secure Secret Key
```bash
# Using openssl
openssl rand -base64 32

# Or use a password generator with minimum 32 characters
```

### Step 3: Update `.env` file
```env
JWT_SECRET_KEY=<your-generated-secret-here>
```

### Step 4: Ensure `.env` is in `.gitignore`
```bash
echo ".env" >> .gitignore
```

### Step 5: Docker Compose will automatically load from `.env`
The `docker-compose.yml` uses environment variable substitution: `${JWT_SECRET_KEY}`

## Production Deployment (Azure/AWS)

### Azure
Use **Azure Key Vault**:
```bash
az keyvault secret set --vault-name <your-keyvault> --name JwtSecretKey --value <your-secret>
```

In application configuration:
```json
{
  "JwtSettings": {
    "SecretKey": "@Microsoft.KeyVault(SecretUri=https://<vault>.vault.azure.net/secrets/JwtSecretKey/)"
  }
}
```

### AWS
Use **AWS Secrets Manager**:
```bash
aws secretsmanager create-secret --name JwtSecretKey --secret-string <your-secret>
```

## Kubernetes
Use Kubernetes Secrets:
```yaml
apiVersion: v1
kind: Secret
metadata:
  name: jwt-secret
type: Opaque
data:
  secret-key: <base64-encoded-secret>
```

## Security Best Practices
1. ✅ Never commit secrets to git
2. ✅ Use different secrets for development, staging, and production
3. ✅ Rotate secrets periodically (every 90 days recommended)
4. ✅ Use secret management services in production
5. ✅ Minimum 32 characters for JWT secrets
6. ✅ Use cryptographically secure random generation
