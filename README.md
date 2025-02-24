# User Management API

A .NET-based REST API for user management with JWT authentication. This API provides basic CRUD operations for managing users with secure endpoints.

## Features

- JWT Authentication with comprehensive validation
- User CRUD operations with input validation
- Request logging middleware
- Error handling middleware
- Input validation
- Secure endpoints with [Authorize] attribute
- SQL Server with retry policy
- User Secrets management

## Prerequisites

- .NET 9.0 SDK
- SQL Server instance
- An API testing tool (e.g., Postman)

## Installation

1. Clone the repository:
```bash
git clone https://github.com/GillianoA/user-management-api
cd UserManagementAPI
```

2. Install dependencies:
```bash
dotnet restore
```

3. Set up user secrets:
```bash
dotnet user-secrets init
dotnet user-secrets set "AdminCredentials:Username" "your-admin-username"
dotnet user-secrets set "AdminCredentials:Password" "your-admin-password"
dotnet user-secrets set "JwtSettings:SecretKey" "your-secret-key-min-32-chars"
dotnet user-secrets set "JwtSettings:Issuer" "your-issuer"
dotnet user-secrets set "JwtSettings:Audience" "your-audience"
```

4. Configure database connection:
```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "your-connection-string"
```

5. Run the application:
```bash
dotnet run
```

The API will start on `http://localhost:5236` and `https://localhost:7256`

## API Endpoints

### Authentication

#### Login
```
POST /login
```
Request body:
```json
{
    "username": "admin",
    "password": "password"
}
```
Response:
```json
{
    "token": "eyJhbGciOiJIUzI1..."
}
```

### Users

All user endpoints require JWT authentication. Include the token in the Authorization header:
```
Authorization: Bearer eyJhbGciOiJIUzI1...
```

#### Get All Users
```
GET /users
```

#### Get User by ID
```
GET /users/{id}
```

#### Create User
```
POST /users
```
Request body:
```json
{
    "name": "John Doe",
    "email": "john@example.com",
    "department": "Engineering"
}
```

#### Update User
```
PUT /users/{id}
```
Request body:
```json
{
    "name": "John Doe",
    "email": "john@example.com",
    "department": "Engineering"
}
```

#### Delete User
```
DELETE /users/{id}
```

## Error Handling

The API includes comprehensive error handling:
- 400 Bad Request: Invalid input
- 401 Unauthorized: Missing or invalid token
- 404 Not Found: Resource not found
- 500 Internal Server Error: Server-side issues

Error responses include:
- Error title
- Detailed message (in development)
- Timestamp
- Request path

## Security

- JWT authentication with configurable settings
- Password validation
- Input validation for all endpoints
- HTTPS support
- User Secrets for development
- SQL injection prevention through Entity Framework
- Retry policies for database connections

## Configuration

### JWT Settings
- Token expiration: 1 hour
- Required validations:
  - Issuer
  - Audience
  - Lifetime
  - Signing key

### Database
- SQL Server with retry policy:
  - Max retry count: 5
  - Max retry delay: 30 seconds
  - Automatic connection resilience

### Logging
- Request logging:
  - Timestamp
  - HTTP method
  - Path
  - Status code
  - Response time

## Development

### Project Structure
```
UserManagementAPI/
├── Program.cs                 # Main application file
├── UserManagementAPI.csproj   # Project file
├── Properties/
│   └── launchSettings.json    # Launch settings
└── .gitignore                 # Git ignore rules
```

### Adding New Features

1. Add new endpoints in `Program.cs`
2. Implement authentication checks using [Authorize] attribute
3. Include appropriate error handling
4. Add input validation
5. Test endpoints using Postman or similar tools

## Testing

Use Postman or curl to test the endpoints. Example curl commands:

```bash
# Login
curl -X POST http://localhost:5236/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"password"}'

# Get Users (with token)
curl http://localhost:5236/users \
  -H "Authorization: Bearer your-token-here"
```

## Environment Setup

### Development
- Use User Secrets for sensitive data
- Enable detailed error messages
- Configure CORS if needed

### Production
- Use appropriate secrets management
- Disable detailed error messages
- Configure appropriate logging
- Set up proper CORS policies

## Contributing

1. Fork the repository
2. Create a feature branch
3. Commit your changes
4. Push to the branch
5. Create a Pull Request

## Security Considerations

- Keep secrets out of source control
- Regularly rotate JWT secrets
- Use strong passwords
- Monitor logs for suspicious activity
- Keep dependencies updated
- Use HTTPS in production
