# User Management API

A .NET-based REST API for user management with JWT authentication. This API provides basic CRUD operations for managing users with secure endpoints.

## Features

- JWT Authentication
- User CRUD operations
- Request logging
- Error handling middleware
- Input validation
- Secure endpoints with [Authorize] attribute

## Prerequisites

- .NET 9.0 SDK
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

3. Run the application:
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

## Security

- JWT authentication with configurable settings
- Password validation
- Input validation for all endpoints
- HTTPS support

## Development

### Project Structure
```
UserManagementAPI/
├── Program.cs           # Main application file
├── UserManagementAPI.csproj
└── Properties/
    └── launchSettings.json
```

### Adding New Features

1. Add new endpoints in `Program.cs`
2. Implement authentication checks using [Authorize] attribute
3. Include appropriate error handling
4. Test endpoints using Postman or similar tools

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

## Configuration

JWT settings in `Program.cs`:
- Token expiration: 1 hour
- Issuer: "your-issuer"
- Audience: "your-audience"
- Secret key: Must be at least 32 characters