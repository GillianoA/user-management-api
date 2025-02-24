using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddUserSecrets<Program>();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.
    GetConnectionString("DefaultConnection"),
    sqlServerOptionsAction: sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null);
    }));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
            ValidAudience = builder.Configuration["JwtSettings:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey is not configured")))
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

//Login endpoint
app.MapPost("/login", (LoginRequest login, IConfiguration configuration) => {
    var adminUsername = configuration ["AdminCredentials:Username"];
    var adminPassword = configuration ["AdminCredentials:Password"];

    if(string.IsNullOrEmpty(adminUsername) || string.IsNullOrEmpty(adminPassword)) {
        return Results.Problem(
            title: "Internal Server Error",
            detail: "Admin credentials not configured",
            statusCode: 500
        );
    }

    if (login.Username == adminUsername && login.password == adminPassword) {
        var token = GenerateJwtToken(login.Username);
        return Results.Ok(new { token });
    } else {
        return Results.Unauthorized();
    }
});

//Basic CRUD Endpoints
app.MapGet("/users", [Authorize] async (ApplicationDbContext db) => {   
    var users = await db.Users.ToListAsync();
    if (!users.Any()) 
        return Results.NotFound(new { error = "No users found" });
    
    return Results.Ok(users);
});

app.MapGet("/users/{id}", [Authorize] async (int id, ApplicationDbContext db) => {
    if (id <= 0) 
        return Results.BadRequest(new { error = "Invalid user id" });
    
    var user = await db.Users.FindAsync(id);
    if (user == null) 
        return Results.NotFound(new { error = $"User {id} not found" });
    
    return Results.Ok(user);
});

app.MapPost("/users", [Authorize] async(User user, ApplicationDbContext db) => {
    if(await db.Users.AnyAsync(u => u.Name == user.Name))
        return Results.BadRequest(new { error = "User already exists" });

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

    user.CreatedAt = DateTime.UtcNow;

    db.Users.Add(user);
    await db.SaveChangesAsync();

    return Results.Created($"/users/{user.Id}", user);
});

app.MapPut("/users/{id}", [Authorize] async (int id, User user, ApplicationDbContext db) => {
    try {
        if (id <= 0) return Results.BadRequest("Invalid user id");

        if (user == null) return Results.BadRequest("User data is required");

        var userToUpdate = await db.Users.FindAsync(id);

        if (userToUpdate == null) return Results.NotFound("User not found");

        if(user.Name == null) return Results.BadRequest("Name is required");
        if(user.Name.Length < 2) return Results.BadRequest("Name must be at least 2 characters");
        if(user.Email == null) return Results.BadRequest("Email is required");
        if(!user.Email.Contains("@")) return Results.BadRequest("Invalid email address");
        if(user.Department == null) return Results.BadRequest("Department is required");

        userToUpdate.Name = user.Name;
        userToUpdate.Email = user.Email;
        userToUpdate.Department = user.Department;

        await db.SaveChangesAsync();
        return Results.Ok(userToUpdate);
    } catch {
        return Results.Problem(
            title: "Internal Server Error",
            detail: "An error occurred while updating a user",
            statusCode: 500
        );
    }
    
});

app.MapDelete("/users/{id}", [Authorize] async (int id, ApplicationDbContext db) => {
    try{
        if (id <= 0) return Results.BadRequest("Invalid user id");

        var userToDelete = await db.Users.FindAsync(id);

        if (userToDelete == null) return Results.NotFound($"User {id} not found"); 

        db.Users.Remove(userToDelete);
        await db.SaveChangesAsync();    
        return Results.Ok(userToDelete);
    } catch {
        return Results.Problem(
            title: "Internal Server Error",
            detail: "An error occurred while deleting a user",
            statusCode: 500
        );
    }
});

string GenerateJwtToken(string username) {
    var JwtSettings = builder.Configuration.GetSection("JwtSettings");
    var secretKey = JwtSettings["SecretKey"];
    var issuer = JwtSettings["Issuer"];
    var audience = JwtSettings["Audience"];

    if(string.IsNullOrEmpty(secretKey) || string.IsNullOrEmpty(issuer) || string.IsNullOrEmpty(audience)) {
        throw new InvalidOperationException("JWT configuration is not complete");
    }

    var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
    var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

    var token = new JwtSecurityToken(
        issuer: issuer,
        audience: audience,
        claims: new[] {
            new Claim(ClaimTypes.Name, username)
        },
        expires: DateTime.UtcNow.AddHours(1),
        signingCredentials: credentials
    );

    return new JwtSecurityTokenHandler().WriteToken(token);
}

app.Run();
public class LoginRequest {
    public required string Username { get; set; }
    public required string password { get; set; }
}
