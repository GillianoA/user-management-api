using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "your-issuer",
            ValidAudience = "your-audience",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("your-super-secret-key-with-at-least-32-characters"))
        };
    });

builder.Services.AddAuthorization();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

//Http logging middleware
app.Use(async (context, next) => {
    var start = DateTime.UtcNow;

    await next(context);

    var elapsed = DateTime.UtcNow - start;
    var statusCode = context.Response.StatusCode;
    var method = context.Request.Method;
    var path = context.Request.Path;

    Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] {method} {path} - Status: {statusCode} - Elapsed: {elapsed.TotalMilliseconds:F2}ms");
});

//Error handling middleware
app.Use(async (context, next) => {
    try {
        await next(context);
    } catch (Exception ex) {
        Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] ERROR: {ex.Message}");

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";

        var error = new {
            error = "Internal server error.",
            message = app.Environment.IsDevelopment() ? ex.Message : "An unexpected error occurred.",
            timestamp = DateTime.UtcNow,
            path = context.Request.Path.ToString()
        };

        await context.Response.WriteAsync("Internal Server Error");
    }
});

//Authentication middleware
app.UseAuthentication();
app.UseAuthorization();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

var users = new List<User>(){
    new User { Id = 1, Name = "Wes", Email = "wes@example.com", Department = "Engineering" },
    new User { Id = 2, Name = "John", Email = "john@example.com", Department = "Marketing" },
    new User { Id = 3, Name = "Jane", Email = "jane@example.com", Department = "HR" },
};

//Login endpoint
app.MapPost("/login", (LoginRequest login) => {
    //DO not do this in production(Ive seen it before, looking at you bob pass)
    if (login.Username == "admin" && login.password == "password") {
        var token = GenerateJwtToken(login.Username);
        return Results.Ok(new { token });
    } else {
        return Results.Unauthorized();
    }
});

//Basic CRUD Endpoints
app.MapGet("/users", [Authorize] () =>
{
    if (!users.Any()) 
        return Results.NotFound(new { error = "No users found" });
    
    return Results.Ok(users);
});

app.MapGet("/users/{id}", [Authorize] (int id) =>
{
    if (id <= 0) 
        return Results.BadRequest(new { error = "Invalid user id" });
    
    var user = users.FirstOrDefault(u => u.Id == id);
    if (user == null) 
        return Results.NotFound(new { error = $"User {id} not found" });
    
    return Results.Ok(user);
});

app.MapPost("/users", [Authorize](User user) =>
{
    if (user == null) 
        return Results.BadRequest(new { error = "User data is required" });

    if (string.IsNullOrEmpty(user.Name)) 
        return Results.BadRequest(new { error = "Name is required" });
    
    if (string.IsNullOrEmpty(user.Email)) 
        return Results.BadRequest(new { error = "Email is required" });
    
    if (string.IsNullOrEmpty(user.Department)) 
        return Results.BadRequest(new { error = "Department is required" });

    if (!user.Email.Contains("@")) 
        return Results.BadRequest(new { error = "Invalid email address" });

    if (user.Name.Length < 2) 
        return Results.BadRequest(new { error = "Name must be at least 2 characters" });

    user.Id = users.Any() ? users.Max(u => u.Id) + 1 : 1;
    users.Add(user);

    return Results.Created($"/users/{user.Id}", user);
});

app.MapPut("/users/{id}", [Authorize] (int id, User user) => {
    try {
        if (id <= 0) return Results.BadRequest("Invalid user id");

        if (user == null) return Results.BadRequest("User data is required");

        var userToUpdate = users.FirstOrDefault(u => u.Id == id);

        if (userToUpdate == null) return Results.NotFound("User not found");

        if(user.Name == null) return Results.BadRequest("Name is required");
        if(user.Name.Length < 2) return Results.BadRequest("Name must be at least 2 characters");
        if(user.Email == null) return Results.BadRequest("Email is required");
        if(!user.Email.Contains("@")) return Results.BadRequest("Invalid email address");
        if(user.Department == null) return Results.BadRequest("Department is required");

        userToUpdate.Name = user.Name;
        userToUpdate.Email = user.Email;
        userToUpdate.Department = user.Department;
        return Results.Ok(userToUpdate);
    } catch (Exception ex) {
        return Results.Problem(
            title: "Internal Server Error",
            detail: "An error occurred while updating a user",
            statusCode: 500
        );
    }
    
});

app.MapDelete("/users/{id}", [Authorize] (int id) => {
    try{
        if (id <= 0) return Results.BadRequest("Invalid user id");

        var userToDelete = users.FirstOrDefault(u => u.Id == id);

        if (userToDelete == null) return Results.NotFound($"User {id} not found"); 

        users.Remove(userToDelete);    
        return Results.Ok(userToDelete);
    } catch (Exception ex) {
        return Results.Problem(
            title: "Internal Server Error",
            detail: "An error occurred while deleting a user",
            statusCode: 500
        );
    }
});

string GenerateJwtToken(string username) {
    var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("your-super-secret-key-with-at-least-32-characters"));
    var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

    var token = new JwtSecurityToken(
        issuer: "your-issuer",
        audience: "your-audience",
        claims: new[] {
            new Claim(ClaimTypes.Name, username)
        },
        expires: DateTime.UtcNow.AddHours(1),
        signingCredentials: credentials
    );

    return new JwtSecurityTokenHandler().WriteToken(token);
}

app.Run();
public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public string Department { get; set; }
}

public class LoginRequest {
    public string Username { get; set; }
    public string password { get; set; }
}
