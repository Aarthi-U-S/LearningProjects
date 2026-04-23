# 🚀 Complete Rate Limiting System - End-to-End Flow Explanation

---

## 📋 Table of Contents

1. [System Architecture Overview](#1-system-architecture-overview)
2. [All Components Explained](#2-all-components-explained)
3. [The 6 Core Management APIs](#3-the-6-core-management-apis)
4. [Complete Request Flow](#4-complete-request-flow)
5. [Real-World Scenarios](#5-real-world-scenarios)
6. [Rate Limiting Algorithms](#6-rate-limiting-algorithms)
7. [Configuration Examples](#7-configuration-examples)
8. [Troubleshooting Guide](#8-troubleshooting-guide)

---

<br/>

# 1. System Architecture Overview

## High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                    YOUR RATE LIMITING SYSTEM                    │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  ┌────────────────┐         ┌─────────────────┐                │
│  │   Admin APIs   │────────▶│ Configuration   │                │
│  │  (Management)  │         │  Storage        │                │
│  └────────────────┘         │ (RateLimitOpts) │                │
│         │                   └─────────────────┘                │
│         │ Configure Rules            │                         │
│         │                            │ Read Rules              │
│         ▼                            ▼                         │
│  ┌────────────────┐         ┌─────────────────┐                │
│  │ RateLimitCtrl  │         │  Middleware     │◀───── Every    │
│  │  (6 APIs)      │         │  (Enforcement)  │       Request  │
│  └────────────────┘         └─────────────────┘                │
│         │                            │                         │
│         │                            │                         │
│         ▼                            ▼                         │
│  ┌────────────────┐         ┌─────────────────┐                │
│  │ RateLimitSvc   │────────▶│ RateLimitStore  │                │
│  │  (Business)    │         │  (IMemoryCache) │                │
│  └────────────────┘         └─────────────────┘                │
│         │                            │                         │
│         │                            │ Count/Check             │
│         ▼                            ▼                         │
│  ┌────────────────┐         ┌─────────────────┐                │
│  │   Algorithms   │         │  Request State  │                │
│  │ Fixed/Sliding/ │         │  (Counters)     │                │
│  │  TokenBucket   │         └─────────────────┘                │
│  └────────────────┘                                            │
│         │                                                       │
│         │ Log Events                                           │
│         ▼                                                       │
│  ┌────────────────┐                                            │
│  │ RateLimitRepo  │                                            │
│  │  (SQL Server)  │                                            │
│  │  Analytics     │                                            │
│  └────────────────┘                                            │
└─────────────────────────────────────────────────────────────────┘
```

## B. Storage Layer (In-Memory Cache)

### **MemoryRateLimitStore** - Tracks real-time request counts

```csharp
// Cache structure examples:
IMemoryCache stores:
{
    // Fixed Window: Just a counter
    "192.168.1.1:/api/login:fw" → 3,

    // Sliding Window: List of timestamps
    "user123:/api/data:sw" → [(10:00:00), (10:00:15), (10:00:30)],

    // Token Bucket: Token count + last refill time
    "192.168.1.5:/api/export:tb" → (tokens: 45, lastRefill: 1713608460)
}
```

<br/>

### **Purpose:**

- ✅ Track request counts per user/IP
- ✅ Auto-expire after time window
- ✅ Thread-safe with SemaphoreSlim
- ⚠️ Lost on app restart (not persisted)
- ⚠️ Not shared across multiple servers

---

<br/>

## C. Enforcement Layer (Middleware)

### **RateLimitMiddleware** - Intercepts ALL HTTP requests

```csharp
public class RateLimitMiddleware
{
    public async Task InvokeAsync(HttpContext context)
    {
        // 1. Check if enabled (global kill switch)
        if (!_options.Enabled) { await _next(context); return; }

        // 2. Get endpoint and find rule
        var endpoint = context.Request.Path.Value;
        var rule = GetRuleForEndpoint(endpoint);

        // 3. Extract settings (use rule or defaults)
        var algorithm = rule?.Algorithm ?? _options.DefaultAlgorithm;
        var strategy = rule?.Strategy ?? _options.DefaultStrategy;
        var limit = rule?.RequestLimit ?? _options.DefaultRequestLimit;

        // 4. Identify user/IP
        var identifier = GetIdentifier(context, strategy);
        var key = GenerateKey(identifier, endpoint, strategy);

        // 5. Check rate limit
        var limiter = _algorithmFactory.Create(algorithm, rule);
        var result = await limiter.CheckLimitAsync(key, limit, window);

        // 6. Add headers
        context.Response.Headers["X-RateLimit-Limit"] = limit.ToString();
        context.Response.Headers["X-RateLimit-Remaining"] = result.RequestsRemaining.ToString();

        // 7. Block or allow
        if (!result.IsAllowed)
        {
            context.Response.StatusCode = 429;
            await context.Response.WriteAsJsonAsync(new { error = "Rate limit exceeded" });
            return; // STOP - Don't call next middleware
        }

        await _next(context); // Continue to controller
    }
}
```

<br/>

### **Pipeline Position:**

```
HTTP Request
    ↓
HttpsRedirection
    ↓
RateLimitMiddleware ← Runs BEFORE authentication/authorization
    ↓
Authentication
    ↓
Authorization
    ↓
Controller (Your API)
    ↓
Response
```

---

<br/>

# 3. The 6 Core Management APIs

## 🥇 API #1: Configure Rate Limit

**Purpose:** Create a new rate limit rule for an endpoint

<br/>

**Request:**

```http
POST /api/ratelimit/configure?algorithm=SlidingWindow&strategy=PerUser
Authorization: Bearer <admin-token>
Content-Type: application/json

{
  "endpointPattern": "/api/auth/login",
  "requestLimit": 5,
  "timeWindowMinutes": 1
}
```

<br/>

**Response 200 OK:**

```json
{
  "endpointPattern": "/api/auth/login",
  "algorithm": "SlidingWindow",
  "strategy": "PerUser",
  "requestLimit": 5,
  "timeWindowMinutes": 1,
  "isActive": true
}
```

---

<br/>

## 🥈 API #2: Get All Configurations

**Purpose:** List all configured rate limit rules

<br/>

**Request:**

```http
GET /api/ratelimit/configure
Authorization: Bearer <admin-token>
```

<br/>

**Response 200 OK:**

```json
[
  {
    "endpointPattern": "/api/auth/login",
    "algorithm": "SlidingWindow",
    "strategy": "PerUser",
    "requestLimit": 5,
    "timeWindowMinutes": 1,
    "isActive": true
  },
  {
    "endpointPattern": "/api/data/export",
    "algorithm": "TokenBucket",
    "strategy": "PerUser",
    "requestLimit": 10,
    "timeWindowMinutes": 60,
    "tokenBucketCapacity": 20,
    "tokenBucketRefillRate": 5,
    "isActive": true
  }
]
```

---

<br/>

## 🥉 API #3: Update Configuration

**Purpose:** Modify an existing rate limit rule

<br/>

**Request:**

```http
PUT /api/ratelimit/configure/api/auth/login?algorithm=FixedWindow&strategy=PerIp
Authorization: Bearer <admin-token>
Content-Type: application/json

{
  "endpointPattern": "/api/auth/login",
  "requestLimit": 10,
  "timeWindowMinutes": 5
}
```

<br/>

**Response 200 OK:**

```json
{
  "endpointPattern": "/api/auth/login",
  "algorithm": "FixedWindow",
  "strategy": "PerIp",
  "requestLimit": 10,
  "timeWindowMinutes": 5,
  "isActive": true
}
```

---

<br/>

## 🏅 API #4: Remove Configuration

**Purpose:** Delete a rate limit rule (falls back to defaults)

<br/>

**Request:**

```http
DELETE /api/ratelimit/configure/api/auth/login
Authorization: Bearer <admin-token>
```

<br/>

**Response 200 OK:**

```json
{
  "message": "Rate limit configuration removed for endpoint: /api/auth/login"
}
```

---

<br/>

## 🎖️ API #5: Check Status (Debugging)

**Purpose:** Check current rate limit status for a user/IP

<br/>

**Request:**

```http
POST /api/ratelimit/status?strategy=PerUser
Authorization: Bearer <admin-token>
Content-Type: application/json

{
  "identifier": "user123",
  "endpoint": "/api/auth/login"
}
```

<br/>

**Response 200 OK:**

```json
{
  "isAllowed": false,
  "requestsRemaining": 0,
  "currentCount": 5,
  "limit": 5,
  "retryAfterSeconds": 45
}
```

---

<br/>

## 🏆 API #6: Reset Rate Limit

**Purpose:** Clear rate limit counter for a user/IP (admin override)

<br/>

**Request:**

```http
POST /api/ratelimit/reset/user123:/api/auth/login
Authorization: Bearer <admin-token>
```

<br/>

**Response 200 OK:**

```json
{
  "message": "Rate limit reset successfully for key: user123:/api/auth/login"
}
```

---

<br/>

# 4. Complete Request Flow

## Scenario: User Tries to Login 6 Times in 1 Minute

### Configuration:

```json
{
  "endpointPattern": "/api/auth/login",
  "algorithm": "SlidingWindow",
  "strategy": "PerUser",
  "requestLimit": 5,
  "timeWindowMinutes": 1
}
```

---

<br/>

### **REQUEST #1** (First Login - 10:00:00)

```
┌─────────────────────────────────────────────────────────────┐
│ 1. HTTP Request                                             │
│    POST /api/auth/login                                     │
│    { "username": "john", "password": "pass123" }            │
└─────────────────────────────────────────────────────────────┘
                         ↓
┌─────────────────────────────────────────────────────────────┐
│ 2. Middleware Intercepts                                    │
│    ✅ Check enabled: true                                   │
│    ✅ Get endpoint: "/api/auth/login"                       │
│    ✅ Find rule: { RequestLimit: 5, Algorithm: SW }         │
│    ✅ Get identifier: "john" (from JWT/username)            │
│    ✅ Generate key: "ratelimit:PerUser:john:/api/login"     │
│    ✅ Check limit: 0 < 5 → ALLOWED ✅                        │
│    ✅ Increment counter: 0 → 1                              │
│    ✅ Add headers: X-RateLimit-Remaining: 4                 │
│    ✅ Continue to controller                                │
└─────────────────────────────────────────────────────────────┘
                         ↓
┌─────────────────────────────────────────────────────────────┐
│ 3. Controller Processes                                     │
│    ✅ Validate credentials                                  │
│    ✅ Generate JWT token                                    │
└─────────────────────────────────────────────────────────────┘
                         ↓
┌─────────────────────────────────────────────────────────────┐
│ 4. Response                                                 │
│    HTTP 200 OK                                              │
│    X-RateLimit-Limit: 5                                     │
│    X-RateLimit-Remaining: 4                                 │
│    { "token": "eyJhbGc..." }                                │
└─────────────────────────────────────────────────────────────┘
```

---

<br/>

### **REQUEST #2-5** (10:00:15, 10:00:30, 10:00:45, 10:00:55)

```
Same flow as Request #1:
- Each request increments counter: 1 → 2 → 3 → 4 → 5
- X-RateLimit-Remaining decreases: 4 → 3 → 2 → 1 → 0
- All requests: ✅ 200 OK (Allowed)
```

---

<br/>

### **REQUEST #6** (10:00:58) - **BLOCKED!** ❌

```
┌─────────────────────────────────────────────────────────────┐
│ 1. HTTP Request                                             │
│    POST /api/auth/login                                     │
│    { "username": "john", "password": "wrongpass" }          │
└─────────────────────────────────────────────────────────────┘
                         ↓
┌─────────────────────────────────────────────────────────────┐
│ 2. Middleware Intercepts                                    │
│    ✅ Check enabled: true                                   │
│    ✅ Get endpoint: "/api/auth/login"                       │
│    ✅ Find rule: { RequestLimit: 5, Algorithm: SW }         │
│    ✅ Get identifier: "john"                                │
│    ✅ Generate key: "ratelimit:PerUser:john:/api/login"     │
│    ❌ Check limit: 5 >= 5 → EXCEEDED! ❌                     │
│    ❌ Block request - DO NOT call controller                │
└─────────────────────────────────────────────────────────────┘
                         ↓
┌─────────────────────────────────────────────────────────────┐
│ 3. Response (BLOCKED)                                       │
│    HTTP 429 Too Many Requests                               │
│    X-RateLimit-Limit: 5                                     │
│    X-RateLimit-Remaining: 0                                 │
│    Retry-After: 2                                           │
│                                                             │
│    {                                                        │
│      "error": "Rate limit exceeded. Try again later.",      │
│      "retryAfter": 2                                        │
│    }                                                        │
└─────────────────────────────────────────────────────────────┘
```

**Note:** User must wait 2 seconds (until 10:01:00) for the oldest request to expire from the sliding window.

---

<br/>

# 5. Real-World Scenarios

## Scenario A: Multiple Users at Same Time

```
Time      User      Request              Counter    Result
─────────────────────────────────────────────────────────────
10:00:00  alice     POST /api/login      1          ✅ 200 OK
10:00:05  bob       POST /api/login      1          ✅ 200 OK (independent counter!)
10:00:10  alice     POST /api/login      2          ✅ 200 OK
10:00:15  alice     POST /api/login      3          ✅ 200 OK
10:00:20  alice     POST /api/login      4          ✅ 200 OK
10:00:25  alice     POST /api/login      5          ✅ 200 OK
10:00:30  alice     POST /api/login      6          ❌ 429 BLOCKED!
10:00:30  bob       POST /api/login      2          ✅ 200 OK (not affected!)
10:00:35  bob       POST /api/login      3          ✅ 200 OK
```

**Key Point:** Each user has their own independent rate limit counter! ✅

---

<br/>

## Scenario B: Admin Changes Rules Mid-Flight

```
Time      Event                           Effect
──────────────────────────────────────────────────────────────────
10:00     Configure: Limit = 5            Rule active
10:02     User hits 5 requests            Next request blocked
10:03     Admin updates: Limit = 10       Rule updated immediately
10:04     User tries again                ✅ Allowed! (6th request now OK)
10:05     User makes 4 more requests      ✅ All allowed (total: 10)
10:06     User tries 11th request         ❌ Blocked again
```

**Key Point:** Changes take effect immediately - no restart needed! ⚡

---

<br/>

## Scenario C: Different Endpoints, Same User

```
Time      User      Endpoint                Counter      Result
────────────────────────────────────────────────────────────────
10:00     john      POST /api/login         1 (login)    ✅ 200 OK
10:01     john      POST /api/login         2 (login)    ✅ 200 OK
10:02     john      GET /api/data/export    1 (export)   ✅ 200 OK
10:03     john      POST /api/login         3 (login)    ✅ 200 OK
10:04     john      GET /api/data/export    2 (export)   ✅ 200 OK
```

**Key Point:** Each endpoint has its own rate limit counter! 📊

---

<br/>

## Scenario D: PerIp vs PerUser Strategy

### **PerIp Strategy:**

```
IP: 192.168.1.100
User: alice (1st request)   → Counter: 1
User: bob (2nd request)     → Counter: 2 (same IP!)
User: charlie (3rd request) → Counter: 3 (same IP!)
```

All users from the same IP **share** the rate limit counter.

<br/>

### **PerUser Strategy:**

```
IP: 192.168.1.100
User: alice (1st request)   → Counter: 1 (alice)
User: bob (1st request)     → Counter: 1 (bob)
User: charlie (1st request) → Counter: 1 (charlie)
```

Each user has their own **independent** counter.

---

<br/>

# 6. Rate Limiting Algorithms

## Algorithm #1: Fixed Window

### **How It Works:**

```
Time:     09:00:00 ──────────────────────► 09:01:00 ────────────────────► 09:02:00
Window:   ├────────── Window 1 ──────────┤├────────── Window 2 ──────────┤

Requests: ●●●●● (5 requests)               ●●●● (4 requests)
Counter:  5                                4
```

- Time is divided into fixed windows (e.g., 1 minute)
- Counter resets at window boundary
- Simple and fast

<br/>

### **Problem: Burst at Boundaries** ⚠️

```
Time:     09:00:50 ─────► 09:01:00 ─────► 09:01:10
          ●●●●● (5 req)   │ RESET     ●●●●● (5 req)
                          └─────► 10 requests in 20 seconds! ⚠️
```

---

<br/>

## Algorithm #2: Sliding Window

### **How It Works:**

```
Stores individual request timestamps:
[ 09:00:10, 09:00:25, 09:00:40, 09:00:55 ]

Window slides with each new request:
New request at 09:01:20
Check: Count requests from (09:01:20 - 1min) to 09:01:20
```

- No fixed boundaries
- Smooth rate limiting
- More memory (stores timestamps)

<br/>

### **Advantage: No Burst Problem** ✅

```
Time:     09:00:50 ─────► 09:01:00 ─────► 09:01:10
          ●●●●● (5 req)               ❌ BLOCKED (already 5 in last 60s)
```

---

<br/>

## Algorithm #3: Token Bucket

### **How It Works:**

```
Bucket Capacity: 10 tokens
Refill Rate: 2 tokens/second

Time     Tokens    Action
─────────────────────────────────────────
00:00    10        Full bucket
00:01    10        User makes 3 requests → 7 tokens left
00:02    9         Refilled: 7 + 2 = 9
00:03    10        Refilled: 9 + 2 = 11 (capped at 10)
00:04    5         User makes 5 requests → 5 tokens left
00:05    7         Refilled: 5 + 2 = 7
00:06    1         User makes 6 requests → 1 token left
00:07    3         Refilled: 1 + 2 = 3
00:08    0         User makes 3 requests → 0 tokens left
00:09    0         User tries again → ❌ BLOCKED (no tokens)
00:10    2         Refilled: 0 + 2 = 2
00:11    4         Refilled: 2 + 2 = 4
00:12    6         Refilled: 4 + 2 = 6
```

<br/>

### **Advantages:**

- ✅ Allows controlled bursts (use accumulated tokens)
- ✅ Smooth refill over time
- ✅ Flexible: separate capacity and refill rate

---

<br/>

# 7. Configuration Examples

## Example 1: Secure Login Endpoint

**Use Case:** Prevent brute-force password attacks

```http
POST /api/ratelimit/configure?algorithm=SlidingWindow&strategy=PerUser
Authorization: Bearer <admin-token>

{
  "endpointPattern": "/api/auth/login",
  "requestLimit": 5,
  "timeWindowMinutes": 1
}
```

**Result:** Each user can attempt login max 5 times per minute

---

<br/>

## Example 2: Expensive Data Export

**Use Case:** Limit heavy database queries

```http
POST /api/ratelimit/configure?algorithm=TokenBucket&strategy=PerUser

{
  "endpointPattern": "/api/data/export",
  "requestLimit": 10,
  "timeWindowMinutes": 60,
  "tokenBucketCapacity": 20,
  "tokenBucketRefillRate": 5
}
```

**Result:**
- Users get 20 tokens (capacity)
- Refills 5 tokens/minute
- Can burst up to 20 exports, then limited to 5/minute

---

<br/>

## Example 3: Public API (Per IP)

**Use Case:** Limit anonymous API calls from same IP

```http
POST /api/ratelimit/configure?algorithm=FixedWindow&strategy=PerIp

{
  "endpointPattern": "/api/public/search",
  "requestLimit": 100,
  "timeWindowMinutes": 1
}
```

**Result:** Each IP address limited to 100 requests/minute

---

<br/>

## Example 4: Admin Panel (Per Endpoint)

**Use Case:** Global limit across all users

```http
POST /api/ratelimit/configure?algorithm=SlidingWindow&strategy=PerEndpoint

{
  "endpointPattern": "/api/admin/deleteall",
  "requestLimit": 1,
  "timeWindowMinutes": 60
}
```

**Result:** Endpoint can only be called once per hour (by anyone)

---

<br/>

# 8. Troubleshooting Guide

## Problem 1: User Complains They're Blocked

### **Step 1: Check Their Status**

```http
POST /api/ratelimit/status?strategy=PerUser

{
  "identifier": "user123",
  "endpoint": "/api/auth/login"
}
```

<br/>

### **Step 2: Review Response**

```json
{
  "isAllowed": false,
  "requestsRemaining": 0,
  "currentCount": 5,
  "limit": 5,
  "retryAfterSeconds": 45
}
```

**Diagnosis:** User hit the limit (5/5 requests). Wait 45 seconds.

<br/>

### **Step 3: Reset If Legitimate**

```http
POST /api/ratelimit/reset/user123:/api/auth/login
```

---

<br/>

## Problem 2: Rate Limiting Not Working

### **Check 1: Is It Enabled?**

```http
POST /api/ratelimit/enable?enabled=true
```

<br/>

### **Check 2: Is There a Rule?**

```http
GET /api/ratelimit/configure
```

If empty response:

```http
POST /api/ratelimit/configure?algorithm=SlidingWindow&strategy=PerUser

{
  "endpointPattern": "/api/auth/login",
  "requestLimit": 5,
  "timeWindowMinutes": 1
}
```

<br/>

### **Check 3: Verify Middleware Registration**

Check `Program.cs`:

```csharp
app.UseMiddleware<RateLimitMiddleware>(); // Should be BEFORE UseAuthorization()
```

---

<br/>

## Problem 3: Too Many False Positives

### **Solution: Increase Limits**

```http
PUT /api/ratelimit/configure/api/auth/login?algorithm=SlidingWindow&strategy=PerUser

{
  "endpointPattern": "/api/auth/login",
  "requestLimit": 10,      // Increased from 5
  "timeWindowMinutes": 1
}
```

---

<br/>

## Problem 4: Different Algorithms Behaving Differently

### **Understanding:**

| Algorithm      | Memory Usage | Burst Handling | Accuracy     |
|----------------|--------------|----------------|--------------|
| Fixed Window   | Low (1 int)  | ⚠️ Allows      | Medium       |
| Sliding Window | High (array) | ✅ Prevents    | High         |
| Token Bucket   | Medium (2x)  | ✅ Controlled  | High         |

**Recommendation:**
- **Login endpoints:** Sliding Window (most accurate)
- **Public APIs:** Fixed Window (fastest)
- **Heavy operations:** Token Bucket (allows bursts)

---

<br/>

# Summary

## ✅ System Features

✅ **Configuration:** Dynamic, no restart needed  
✅ **Enforcement:** Automatic via middleware  
✅ **Algorithms:** Fixed Window, Sliding Window, Token Bucket  
✅ **Strategies:** PerUser, PerIp, PerEndpoint, PerIpAndEndpoint, PerUserAndEndpoint, Global  
✅ **Management:** 6 APIs for full control  
✅ **Debugging:** Status check and reset tools  
✅ **Monitoring:** Response headers and database logging  
✅ **Production-Ready:** Kill switch, thread-safe, tested

<br/>

## 🎯 Quick Reference

| Task                     | API Endpoint                                    |
|--------------------------|-------------------------------------------------|
| Create rule              | `POST /api/ratelimit/configure`                 |
| List all rules           | `GET /api/ratelimit/configure`                  |
| Update rule              | `PUT /api/ratelimit/configure/{endpoint}`       |
| Delete rule              | `DELETE /api/ratelimit/configure/{endpoint}`    |
| Check user status        | `POST /api/ratelimit/status`                    |
| Reset user counter       | `POST /api/ratelimit/reset/{key}`               |
| Enable/disable globally  | `POST /api/ratelimit/enable?enabled=true/false` |

<br/>

---

**Your rate limiting system is production-ready!** 🚀

**Questions?** Check the troubleshooting guide above or review the component explanations.
