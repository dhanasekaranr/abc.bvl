# CSP (Content Security Policy) Implementation Guide for Angular SPA

## Overview

Your API now implements **CSP-compliant security headers** optimized for Angular single-page applications. This guide explains the configuration and how to make your Angular app work with strict CSP.

---

## Current CSP Configuration

### Production (Strict - CSP Compliant) ✅
```
Content-Security-Policy:
  default-src 'self';
  script-src 'self' 'nonce-{random}' 'strict-dynamic';
  style-src 'self' 'nonce-{random}';
  img-src 'self' data: https:;
  font-src 'self' data: https://fonts.gstatic.com;
  connect-src 'self' ws: wss:;
  media-src 'self';
  object-src 'none';
  frame-src 'none';
  worker-src 'self' blob:;
  frame-ancestors 'none';
  form-action 'self';
  base-uri 'self';
  upgrade-insecure-requests;
  block-all-mixed-content;
```

### Development (Relaxed for Swagger/HMR) ⚠️
```
Content-Security-Policy:
  default-src 'self';
  script-src 'self' 'unsafe-eval' 'unsafe-inline';  // Allows Swagger, HMR
  style-src 'self' 'unsafe-inline';                 // Development ease
  img-src 'self' data: https:;
  font-src 'self' data: https://fonts.gstatic.com;
  connect-src 'self' ws: wss:;                      // WebSocket for HMR
  ...
```

---

## Why CSP Matters

### Security Benefits
- ✅ **Prevents XSS attacks** - Blocks inline scripts injected by attackers
- ✅ **Prevents clickjacking** - `frame-ancestors 'none'`
- ✅ **Prevents data injection** - Controls where resources can load from
- ✅ **Prevents MIME confusion** - `X-Content-Type-Options: nosniff`
- ✅ **Compliance** - Required for PCI-DSS, OWASP, and enterprise security

### Without CSP (Your Old Config) ❌
```csharp
"script-src 'self' 'unsafe-inline' 'unsafe-eval'"  // NOT CSP compliant!
```
- **Problem**: `'unsafe-inline'` and `'unsafe-eval'` **defeat CSP protection**
- Allows any inline script to execute (including malicious ones)
- Does NOT meet OWASP, PCI-DSS, or enterprise security standards

### With CSP (New Config) ✅
```csharp
"script-src 'self' 'nonce-{random}' 'strict-dynamic'"  // CSP compliant!
```
- **Benefit**: Only scripts with correct nonce can execute
- Blocks all inline scripts unless they have the server-generated nonce
- Meets enterprise security compliance standards

---

## Angular Application Configuration

### Step 1: Build Configuration (angular.json)

For **production builds**, Angular must be configured to work with CSP nonces:

```json
{
  "projects": {
    "your-app": {
      "architect": {
        "build": {
          "configurations": {
            "production": {
              "optimization": true,
              "outputHashing": "all",
              "sourceMap": false,
              "namedChunks": false,
              "extractLicenses": true,
              "vendorChunk": false,
              "buildOptimizer": true,
              "budgets": [...],
              "fileReplacements": [...],
              "scripts": [],
              "styles": [
                "src/styles.css"
              ],
              // Important: Don't use inline styles in production
              "inlineStyleLanguage": "css",
              "extractCss": true
            }
          }
        }
      }
    }
  }
}
```

### Step 2: index.html Configuration

**❌ BAD (Won't work with CSP):**
```html
<!DOCTYPE html>
<html>
<head>
  <style>
    /* Inline styles - BLOCKED by CSP */
    body { margin: 0; }
  </style>
  <script>
    // Inline scripts - BLOCKED by CSP
    console.log('App starting');
  </script>
</head>
<body>
  <app-root></app-root>
</body>
</html>
```

**✅ GOOD (CSP compliant):**
```html
<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <title>AdminTool</title>
  <base href="/">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <link rel="icon" type="image/x-icon" href="favicon.ico">
  
  <!-- External stylesheets (allowed by 'self') -->
  <link rel="stylesheet" href="styles.css">
  
  <!-- Google Fonts (allowed by font-src) -->
  <link rel="preconnect" href="https://fonts.gstatic.com">
  <link href="https://fonts.googleapis.com/css2?family=Roboto:wght@300;400;500&display=swap" rel="stylesheet">
</head>
<body>
  <app-root></app-root>
  
  <!-- Scripts loaded from external files (allowed by 'self') -->
  <!-- Angular bundles: runtime.js, polyfills.js, main.js -->
</body>
</html>
```

### Step 3: Component Styles

Angular components should use **external stylesheets or component styles**, NOT inline styles:

**❌ BAD (Inline styles in template):**
```typescript
@Component({
  selector: 'app-dashboard',
  template: `
    <div style="color: red;">  <!-- BLOCKED by CSP -->
      Dashboard
    </div>
  `
})
```

**✅ GOOD (Component styles or classes):**
```typescript
@Component({
  selector: 'app-dashboard',
  template: `
    <div class="dashboard-title">
      Dashboard
    </div>
  `,
  styles: [`
    .dashboard-title {
      color: red;
    }
  `]
  // Or use: styleUrls: ['./dashboard.component.css']
})
```

### Step 4: Avoid innerHTML with Scripts

**❌ BAD (Security risk):**
```typescript
@Component({
  template: `
    <div [innerHTML]="userContent"></div>  <!-- XSS vulnerability! -->
  `
})
export class BadComponent {
  userContent = '<script>alert("XSS")</script>';  // BLOCKED by CSP
}
```

**✅ GOOD (Use Angular sanitization):**
```typescript
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';

@Component({
  template: `
    <div [innerHTML]="safeContent"></div>
  `
})
export class GoodComponent {
  safeContent: SafeHtml;

  constructor(private sanitizer: DomSanitizer) {
    // Angular automatically strips scripts
    this.safeContent = this.sanitizer.sanitize(
      SecurityContext.HTML, 
      userProvidedContent
    );
  }
}
```

### Step 5: HTTP API Calls

Your Angular app needs to call the .NET API - already configured:

```typescript
// environment.ts
export const environment = {
  production: false,
  apiUrl: 'https://localhost:7269/api/v1'  // Allowed by 'connect-src self'
};

// service.ts
import { HttpClient } from '@angular/common/http';

@Injectable({ providedIn: 'root' })
export class ScreenService {
  constructor(private http: HttpClient) {}

  getScreens(page: number = 1, pageSize: number = 20) {
    // ✅ Allowed: same-origin or configured CORS origin
    return this.http.get<PagedResult<ScreenDefnDto>>(
      `${environment.apiUrl}/admin/screen-definition/screens`,
      { params: { page, pageSize } }
    );
  }
}
```

### Step 6: Third-Party Scripts (If Needed)

If you need third-party scripts (analytics, maps, etc.), update CSP:

**Option 1: Add specific domains (recommended)**
```csharp
// In SecurityMiddleware.cs BuildContentSecurityPolicy()
"script-src 'self' 'nonce-{nonce}' 'strict-dynamic' https://www.google-analytics.com",
"connect-src 'self' ws: wss: https://www.google-analytics.com",
```

**Option 2: Hash-based (for specific inline scripts)**
```html
<!-- Calculate SHA-256 hash of the script content -->
<script>console.log('init');</script>

<!-- Then add to CSP: -->
script-src 'self' 'sha256-{hash-of-script}'
```

---

## Testing Your CSP Implementation

### Browser Console Check

1. Open your Angular app in Chrome/Edge
2. Open Developer Tools (F12) → Console
3. Look for CSP violations:

**❌ CSP Violation Example:**
```
Refused to execute inline script because it violates the following 
Content Security Policy directive: "script-src 'self'". 
Either the 'unsafe-inline' keyword, a hash, or a nonce is required.
```

**✅ No Violations:**
```
(No CSP errors in console)
```

### Network Tab Check

1. Open Network tab → Headers
2. Look at response headers from your API:

```
Content-Security-Policy: default-src 'self'; script-src 'self' 'nonce-abc123' ...
X-Content-Type-Options: nosniff
X-Frame-Options: DENY
X-XSS-Protection: 1; mode=block
Referrer-Policy: strict-origin-when-cross-origin
Strict-Transport-Security: max-age=31536000; includeSubDomains; preload
```

### CSP Evaluator Tool

Use Google's CSP Evaluator: https://csp-evaluator.withgoogle.com/

**Paste your CSP and check:**
- ✅ **No 'unsafe-inline'** for scripts
- ✅ **No 'unsafe-eval'** for scripts (production)
- ✅ **Nonce-based script loading**
- ✅ **Strict frame-ancestors**

---

## Common Issues and Solutions

### Issue 1: Angular Material or Bootstrap Styles Blocked

**Problem:**
```
Refused to apply inline style because it violates CSP directive: "style-src 'self'"
```

**Solution:**
Use component styles or external CSS files:
```typescript
// angular.json
"styles": [
  "node_modules/@angular/material/prebuilt-themes/indigo-pink.css",
  "node_modules/bootstrap/dist/css/bootstrap.min.css",
  "src/styles.css"
]
```

### Issue 2: Google Maps or External APIs

**Problem:**
```
Refused to load script from 'https://maps.googleapis.com/...' because it violates CSP
```

**Solution:**
Update CSP in `SecurityMiddleware.cs`:
```csharp
"script-src 'self' 'nonce-{nonce}' 'strict-dynamic' https://maps.googleapis.com",
"connect-src 'self' ws: wss: https://maps.googleapis.com",
"img-src 'self' data: https: https://maps.gstatic.com",
```

### Issue 3: WebSocket Connections Blocked

**Problem:**
```
Refused to connect to 'ws://localhost:4200/' because it violates CSP
```

**Solution:**
Already configured! `connect-src` includes `ws: wss:`
```csharp
"connect-src 'self' ws: wss:",  // ✅ WebSocket allowed
```

### Issue 4: Service Worker (PWA) Blocked

**Problem:**
```
Refused to execute service worker because it violates CSP
```

**Solution:**
Already configured! `worker-src` allows service workers:
```csharp
"worker-src 'self' blob:",  // ✅ Service workers allowed
```

---

## Production Deployment Checklist

### Backend (.NET API)
- ✅ `appsettings.Production.json` has `"IsDevelopment": false`
- ✅ `"EnableContentSecurityPolicy": true`
- ✅ `"EnableHsts": true`
- ✅ `"RequireHttps": true`
- ✅ CORS configured with specific origins (not "*")
- ✅ JWT secret is strong (64+ chars) and from environment variable

### Frontend (Angular)
- ✅ Build with `ng build --configuration production`
- ✅ No inline scripts in `index.html`
- ✅ No inline styles in templates (use component styles)
- ✅ No `[innerHTML]` with user content without sanitization
- ✅ All third-party scripts from trusted CDNs added to CSP
- ✅ Test in browser console for CSP violations

### Testing
- ✅ Run app and check browser console for CSP errors
- ✅ Test all features (forms, modals, API calls, file uploads)
- ✅ Validate CSP with https://csp-evaluator.withgoogle.com/
- ✅ Run security scan with OWASP ZAP or similar
- ✅ Check response headers include all security headers

---

## CSP Compliance Levels

### Level 1: Basic (Current Development)
```csharp
"script-src 'self' 'unsafe-eval' 'unsafe-inline'"  // ⚠️ Not compliant
```
- Allows all inline scripts and eval
- Good for development/testing only
- **FAIL** security audits

### Level 2: CSP Compliant (Current Production) ✅
```csharp
"script-src 'self' 'nonce-{random}' 'strict-dynamic'"  // ✅ Compliant
```
- Nonce-based script execution
- Blocks all inline scripts without nonce
- **PASS** security audits (OWASP, PCI-DSS)

### Level 3: Hash-Based (Most Strict)
```csharp
"script-src 'self' 'sha256-{hash1}' 'sha256-{hash2}'"  // ✅ Most secure
```
- Every inline script needs a hash
- Most secure but harder to maintain
- Best for static sites

**Your implementation uses Level 2 (CSP Compliant)** ✅

---

## Security Headers Summary

Your API now returns these headers:

| Header | Value | Purpose |
|--------|-------|---------|
| `Content-Security-Policy` | (See above) | Prevents XSS, code injection |
| `X-Frame-Options` | `DENY` | Prevents clickjacking |
| `X-Content-Type-Options` | `nosniff` | Prevents MIME confusion |
| `X-XSS-Protection` | `1; mode=block` | Legacy XSS protection |
| `Referrer-Policy` | `strict-origin-when-cross-origin` | Controls referrer leakage |
| `Permissions-Policy` | (Restricts features) | Blocks camera, geolocation, etc. |
| `Strict-Transport-Security` | `max-age=31536000...` | Forces HTTPS (production) |
| `Cache-Control` | `no-store...` (sensitive endpoints) | Prevents caching of auth data |

---

## Configuration Files Reference

### appsettings.json (Production)
```json
{
  "Security": {
    "RequireHttps": true,
    "EnableCors": true,
    "AllowedOrigins": ["https://admintool.abc.bvl"],
    "EnableHsts": true,
    "EnableContentSecurityPolicy": true,
    "IsDevelopment": false  // ← Strict CSP
  }
}
```

### appsettings.Development.json
```json
{
  "Security": {
    "RequireHttps": false,
    "EnableCors": true,
    "AllowedOrigins": [
      "https://localhost:3000",
      "http://localhost:3000",
      "http://localhost:4200"  // Angular default port
    ],
    "EnableHsts": false,
    "EnableContentSecurityPolicy": true,
    "IsDevelopment": true  // ← Relaxed CSP for dev/Swagger
  }
}
```

---

## Migration Path

If you have an existing Angular app:

### Phase 1: Audit (Week 1)
1. Enable CSP in development
2. Check console for violations
3. Document all blocked resources

### Phase 2: Fix (Week 2-3)
1. Move inline scripts to .ts files
2. Move inline styles to component styles
3. Update third-party integrations
4. Test all features

### Phase 3: Production (Week 4)
1. Enable strict CSP in production
2. Monitor for issues
3. Adjust CSP as needed
4. Run security audit

---

## Summary

✅ **Your API is now CSP compliant for Angular SPA applications**

**Key Changes:**
- ✅ Production uses nonce-based CSP (no `'unsafe-inline'` or `'unsafe-eval'`)
- ✅ Development uses relaxed CSP for Swagger and HMR
- ✅ Supports Angular Material, Bootstrap, Google Fonts
- ✅ Supports WebSockets for SignalR/HMR
- ✅ Supports Service Workers for PWA
- ✅ All security headers configured (OWASP compliant)

**Angular Requirements:**
- ✅ No inline scripts in `index.html`
- ✅ No inline styles in templates
- ✅ Component styles or external CSS only
- ✅ Proper sanitization for user content
- ✅ External scripts from CDNs must be whitelisted

**Result:** Enterprise-grade security that meets OWASP, PCI-DSS, and CSP compliance standards! 🚀
