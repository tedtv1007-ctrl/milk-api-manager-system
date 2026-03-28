---
name: insecure-defaults
description: |
  Detect insecure default configurations, hardcoded credentials, and fail-open security patterns.
  Based on Trail of Bits 'insecure-defaults' skill.
---

# Insecure Defaults Auditor

## Purpose
This skill provides a systematic framework for identifying "footguns"—configurations or code patterns that are insecure by default. Use during security reviews, PR audits, or initial codebase triage.

## Instructions

### 1. Configuration Audit
Scan configuration files (`.yaml`, `.json`, `.toml`, `.xml`, `Dockerfile`, `docker-compose.yml`) for:
*   **Permissive Network Settings:** `0.0.0.0` binding, disabled TLS/SSL, or default ports with no authentication.
*   **Debug Modes:** `DEBUG=True` or `development` environments enabled in production-like configs.
*   **Resource Limits:** Missing CPU/Memory limits in container configs.
*   **Privileged Execution:** Containers running as `root` or with `privileged: true`.

### 2. Authentication & Authorization
Identify "fail-open" or weak security patterns:
*   **Hardcoded Secrets:** Search for placeholders like `TODO: remove`, `test-password`, or actual API keys.
*   **Default Credentials:** Check for common default admin passwords or "guest" accounts.
*   **Missing Authorization:** Identify API endpoints that lack appropriate auth attributes.
*   **Weak Cryptography:** Use of MD5, SHA1, or ECB mode for encryption.

### 3. Web & API Security
*   **CORS Policies:** Look for `Access-Control-Allow-Origin: *`.
*   **Security Headers:** Check for missing `Content-Security-Policy`, `Strict-Transport-Security`, or `X-Content-Type-Options`.
*   **Cookie Security:** Ensure cookies lack `HttpOnly`, `Secure`, or `SameSite` attributes.

### 4. Error Handling
*   **Information Leakage:** Identify stack trace exposure to end-users.
*   **Fail-Open Logic:** Look for logic where a security check failure results in the code continuing.

## Best Practices for Milk API Manager
*   Check APISIX plugin configurations for permissive rules.
*   Verify `.env.example` doesn't contain real secrets.
*   Ensure the .NET 8 backend has all recommended security headers.
