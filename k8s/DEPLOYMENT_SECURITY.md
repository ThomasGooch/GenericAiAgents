# 🔐 Kubernetes Deployment Security Guide

## ⚠️ CRITICAL: Configure Secrets Before Deployment

**DO NOT deploy without configuring secrets first!**

## 1. Create Secure Passwords

Generate strong, unique passwords for each service:

```bash
# Generate secure passwords (32 characters each)
POSTGRES_PASSWORD=$(openssl rand -base64 32)
REDIS_PASSWORD=$(openssl rand -base64 32)
JWT_SECRET=$(openssl rand -base64 32)
API_KEY=$(openssl rand -base64 32)

echo "Generated passwords (SAVE THESE SECURELY):"
echo "POSTGRES_PASSWORD: $POSTGRES_PASSWORD"
echo "REDIS_PASSWORD: $REDIS_PASSWORD"
echo "JWT_SECRET: $JWT_SECRET"
echo "API_KEY: $API_KEY"
```

## 2. Update Kubernetes Secrets

Edit `secrets.yaml` and replace all `CHANGEME_SET_SECURE_PASSWORD` placeholders:

```bash
# Edit the secrets file
nano k8s/secrets.yaml

# Replace each occurrence of CHANGEME_SET_SECURE_PASSWORD with generated passwords
```

## 3. Deploy in Correct Order

```bash
# 1. Create namespace first
kubectl create namespace agent-system

# 2. Apply secrets (MUST be first!)
kubectl apply -f k8s/secrets.yaml

# 3. Apply configuration
kubectl apply -f k8s/configmap.yaml

# 4. Deploy the application
kubectl apply -f k8s/deployment.yaml
```

## 4. Verify Security

```bash
# Check that secrets are properly created
kubectl get secrets -n agent-system

# Verify pods are using secrets (should show environment variables from secrets)
kubectl describe pod -l app=agent-system -n agent-system

# Test that passwords are NOT visible in config
kubectl get configmap agent-system-config -n agent-system -o yaml
```

## 5. Security Best Practices

### ✅ Do:
- Generate unique passwords for each environment (dev/staging/prod)
- Use strong passwords (32+ characters)
- Store secrets in a password manager
- Rotate passwords regularly
- Use RBAC to limit secret access
- Enable secret encryption at rest

### ❌ Don't:
- Commit real passwords to Git
- Reuse passwords across environments
- Share passwords in plain text
- Use weak or predictable passwords
- Store secrets in ConfigMaps

## 6. Monitoring Secret Access

```bash
# Monitor secret access in audit logs
kubectl get events -n agent-system --field-selector involvedObject.kind=Secret

# Check pod security context
kubectl get pod -l app=agent-system -n agent-system -o jsonpath='{.items[*].spec.securityContext}'
```

## 7. Secret Rotation

To rotate secrets:

1. Generate new passwords
2. Update the Secret object
3. Restart pods to pick up new secrets

```bash
# Update secret
kubectl patch secret agent-system-secrets -n agent-system --type='json' -p='[{"op": "replace", "path": "/data/postgres-password", "value": "BASE64_ENCODED_NEW_PASSWORD"}]'

# Restart deployment to pick up new secret
kubectl rollout restart deployment/agent-system -n agent-system
```

## 8. Emergency Response

If secrets are compromised:

1. **Immediately rotate all passwords**
2. **Check audit logs for unauthorized access**
3. **Review all pods with access to secrets**
4. **Update all dependent services**
5. **Document the incident**

Remember: **Security is not optional** - take time to do this correctly!