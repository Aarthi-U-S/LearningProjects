# JWT Authentication API

A production-ready JWT Authentication API built with .NET 10.0, featuring secure user registration, authentication, refresh token rotation, and role-based authorization.

## Features

### Core Features
- ✅ User Registration with BCrypt password hashing
- ✅ User Login with JWT access tokens
- ✅ Refresh Token mechanism with automatic rotation
- ✅ Logout functionality
- ✅ Role-based authorization (Admin, User, etc.)
- ✅ Token revocation (Admin only)
- ✅ Claim-based authentication

### Security Features
- ✅ BCrypt password hashing (industry standard)
- ✅ JWT access tokens with configurable expiration (default: 15 minutes)
- ✅ Refresh tokens with configurable expiration (default: 7 days)
- ✅ Automatic refresh token rotation
- ✅ Refresh token revocation tracking
- ✅ Secure token validation

### Architecture
- ✅ Clean architecture with separation of concerns
- ✅ Repository pattern for data access
- ✅ Service layer for business logic
- ✅ DTOs for request/response objects
- ✅ Dependency injection
- ✅ Async/await pattern throughout
- ✅ Background service for cleaning up expired tokens
- ✅ Structured logging

## API Endpoints

### 1. Register
**Endpoint:** `POST /api/auth/register`

**Request:**
```json
{
  "email": "user@example.com",
  "password": "SecurePassword123!",
  "role": "User"
}
```

**Response:**
```json
{
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "message": "User registered successfully."
}
```

### 2. Login
**Endpoint:** `POST /api/auth/login`

**Request:**
```json
{
  "email": "user@example.com",
  "password": "SecurePassword123!"
}
```

**Response:**
```json
{
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "email": "user@example.com",
  "role": "User",
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "base64-encoded-refresh-token",
  "accessTokenExpiresAt": "2024-01-15T12:15:00Z",
  "refreshTokenExpiresAt": "2024-01-22T12:00:00Z"
}
```

### 3. Refresh Token
**Endpoint:** `POST /api/auth/refresh-token`

**Request:**
```json
{
  "refreshToken": "base64-encoded-refresh-token"
}
```

**Response:**
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "new-base64-encoded-refresh-token",
  "accessTokenExpiresAt": "2024-01-15T12:15:00Z",
  "refreshTokenExpiresAt": "2024-01-22T12:00:00Z"
}
```

### 4. Logout
**Endpoint:** `POST /api/auth/logout`

**Request:**
```json
{
  "refreshToken": "base64-encoded-refresh-token"
}
```

**Response:**
```json
{
  "message": "Logged out successfully."
}
```

### 5. Revoke Token (Admin Only)
**Endpoint:** `POST /api/auth/revoke-token`

**Headers:**
```
Authorization: Bearer {access-token}
```

**Request:**
```json
{
  "refreshToken": "base64-encoded-refresh-token"
}
```

**Response:**
```json
{
  "message": "Token revoked successfully."
}
```

### 6. Protected Endpoint (Example)
**Endpoint:** `GET /api/auth/protected`

**Headers:**
```
Authorization: Bearer {access-token}
```

**Response:**
```json
{
  "message": "This is a protected endpoint",
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "email": "user@example.com",
  "role": "User"
}
```

### 7. Admin Only Endpoint (Example)
**Endpoint:** `GET /api/auth/admin-only`

**Headers:**
```
Authorization: Bearer {access-token}
```

**Response:**
```json
{
  "message": "This endpoint is accessible only to Admins."
}
```

## Configuration

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\Mydb;Database=LearningDB;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "Jwt": {
    "SecretKey": "YourSuperSecretKeyThatIsAtLeast32CharactersLong!ChangeThisInProduction",
    "Issuer": "AuthAPI",
    "Audience": "AuthAPIClient",
    "AccessTokenExpiryMinutes": "15",
    "RefreshTokenExpiryDays": "7"
  }
}
```

### Environment Variables (Production)

For production, use environment variables instead of hardcoding secrets:

```bash
export Jwt__SecretKey="your-production-secret-key-min-32-chars"
export ConnectionStrings__DefaultConnection="your-production-connection-string"
```

## Database Schema

### Users Table
- `Id` (uniqueidentifier, PK)
- `Email` (nvarchar(256))
- `PasswordHash` (nvarchar(500))
- `Role` (nvarchar(50))
- `CreatedAt` (datetime2, default: UTC now)
- `IsActive` (bit, default: true)

### RefreshTokens Table
- `Id` (uniqueidentifier, PK)
- `UserId` (uniqueidentifier, FK to Users)
- `Token` (nvarchar(500), indexed)
- `ExpiresAt` (datetime2)
- `CreatedAt` (datetime2, default: UTC now)
- `IsRevoked` (bit, default: false)
- `RevokedBy` (nvarchar(256), nullable)
- `RevokedAt` (datetime2, nullable)
- `ReplacedByToken` (nvarchar(500), nullable)

## Project Structure

```
Auth/
├── Controllers/
│   └── AuthController.cs          # API endpoints
├── Services/
│   ├── AuthService.cs              # Business logic
│   ├── JwtTokenService.cs          # JWT token generation/validation
│   └── RefreshTokenCleanupService.cs # Background service
├── Repository/
│   └── AuthRepo.cs                 # Data access layer
├── Interfaces/
│   ├── IAuthService.cs
│   ├── IAuthRepo.cs
│   └── IJwtTokenService.cs
├── DTO/
│   ├── RegisterRequest.cs
│   ├── RegisterResponse.cs
│   ├── LoginRequest.cs
│   ├── LoginResponse.cs
│   ├── RefreshTokenRequest.cs
│   ├── RefreshTokenResponse.cs
│   └── RevokeTokenRequest.cs
├── Migrations/
│   └── 20260415022331_AddRefreshTokenSupport.cs
└── Program.cs                      # App configuration

EFCore/
└── Models/
    ├── User.cs                     # User entity
    ├── RefreshToken.cs             # RefreshToken entity
    └── AppDbContext.cs             # EF Core context
```

## Setup Instructions

### 1. Prerequisites
- .NET 10.0 SDK
- SQL Server (LocalDB or full version)
- Visual Studio 2026 or VS Code

### 2. Database Migration

The migration has already been applied. If you need to rerun it:

```bash
dotnet ef database update --context AppDbContext
```

### 3. Install Required Packages

Packages are already configured in `Auth.csproj`:
- BCrypt.Net-Next (4.1.0)
- Microsoft.AspNetCore.Authentication.JwtBearer (10.0.5)
- System.IdentityModel.Tokens.Jwt (8.4.0)
- Microsoft.EntityFrameworkCore.SqlServer (10.0.5)

### 4. Run the Application

```bash
dotnet run
```

The API will be available at `https://localhost:5001` (or the port configured in your launch settings).

## Testing the API

### Using cURL

#### 1. Register a User
```bash
curl -X POST https://localhost:5001/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "admin@example.com",
    "password": "Admin123!",
    "role": "Admin"
  }'
```

#### 2. Login
```bash
curl -X POST https://localhost:5001/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "admin@example.com",
    "password": "Admin123!"
  }'
```

#### 3. Access Protected Endpoint
```bash
curl -X GET https://localhost:5001/api/auth/protected \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN"
```

#### 4. Refresh Token
```bash
curl -X POST https://localhost:5001/api/auth/refresh-token \
  -H "Content-Type: application/json" \
  -d '{
    "refreshToken": "YOUR_REFRESH_TOKEN"
  }'
```

### Using Postman

1. Import the requests above
2. Save the `accessToken` and `refreshToken` from the login response
3. Use the `accessToken` in the Authorization header as `Bearer {token}`
4. Use the `refreshToken` when calling the refresh-token endpoint

## Security Best Practices

### Implemented
1. ✅ **Password Hashing**: BCrypt with default work factor (11)
2. ✅ **JWT Signing**: HMAC SHA-256 algorithm
3. ✅ **Token Expiry**: Short-lived access tokens (15 min), longer refresh tokens (7 days)
4. ✅ **Token Rotation**: Automatic refresh token rotation on use
5. ✅ **Revocation Tracking**: Track when and who revoked tokens
6. ✅ **HTTPS**: Use HTTPS in production
7. ✅ **Input Validation**: Data annotations on DTOs
8. ✅ **Secure Defaults**: IsActive=true, timestamps, etc.

### Production Recommendations
1. 🔒 **Secret Key Management**: Use Azure Key Vault or similar
2. 🔒 **Rate Limiting**: Implement rate limiting on auth endpoints
3. 🔒 **CORS**: Configure CORS policies appropriately
4. 🔒 **Logging**: Don't log sensitive data (passwords, tokens)
5. 🔒 **HTTPS Only**: Enforce HTTPS redirect
6. 🔒 **Account Lockout**: Implement after failed login attempts
7. 🔒 **Email Verification**: Add email verification for new accounts
8. 🔒 **2FA**: Consider multi-factor authentication

## Background Services

### RefreshTokenCleanupService
- Runs every 24 hours
- Removes expired refresh tokens from the database
- Reduces database bloat
- Logs cleanup operations

## Token Claims

Access tokens include the following claims:
- `sub`: User ID (GUID)
- `email`: User email address
- `role`: User role (for authorization)
- `jti`: Unique token identifier
- `IsActive`: User active status

## Error Handling

The API returns appropriate HTTP status codes:
- `200 OK`: Successful request
- `400 Bad Request`: Invalid input or business logic error
- `401 Unauthorized`: Invalid credentials or expired/invalid token
- `500 Internal Server Error`: Server error (logged)

## Logging

Structured logging is implemented using `ILogger<T>`:
- User registration events
- Login events
- Token refresh events
- Token revocation events
- Logout events
- Error events

## Future Enhancements

### Bonus Features (Optional)
- [ ] JWT Key Rotation
- [ ] Sliding refresh token expiration
- [ ] Device fingerprinting
- [ ] Suspicious activity detection
- [ ] Account lockout after failed attempts
- [ ] Email verification
- [ ] Password reset flow
- [ ] Two-factor authentication (2FA)
- [ ] OAuth2 integration (Google, Microsoft, etc.)

## License

This project is for educational purposes.

## Support

For issues or questions, please check the code comments or contact the development team.
