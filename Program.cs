var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

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

//Basic CRUD Endpoints
app.MapGet("/users", () => {
    try{
        if(users == null) return Results.NotFound("No users found");
        return Results.Ok(users);
    } catch (Exception ex) {
        return Results.Problem(
            title: "Internal Server Error",
            detail: "An error occurred while retrieving users",
            statusCode: 500
        );
    }
});

app.MapGet("/users/{id}", (int id) => {
    try {
        if(id <= 0) return Results.BadRequest("Invalid user id");
        var user = users.FirstOrDefault(u => u.Id == id);
        if (user == null) return Results.NotFound("User not found");
        return Results.Ok(user);
    } catch (Exception ex) {
        return Results.Problem(
            title: "Internal Server Error",
            detail: $"An error occurred while retrieving user {id}",
            statusCode: 500
        );
    }
    
});

app.MapPost("/users", (User user) => {
    try {
        if (user == null) return Results.BadRequest("User data is required");

        if (string.IsNullOrEmpty(user.Name)) return Results.BadRequest("Name is required");
        if (string.IsNullOrEmpty(user.Email)) return Results.BadRequest("Email is required");
        if (string.IsNullOrEmpty(user.Department)) return Results.BadRequest("Department is required");

        //Validate Email
        if (!user.Email.Contains("@")) return Results.BadRequest("Invalid email address");

        //valid name length
        if (user.Name.Length < 2) return Results.BadRequest("Name must be at least 2 characters");

        user.Id = users.Any() ?users.Max(u => u.Id) + 1 : 1;
        users.Add(user);

        return Results.Created($"/users/{user.Id}", user);
    } catch (Exception ex) {
        return Results.Problem(
            title: "Internal Server Error",
            detail: "An error occurred while creating a user",
            statusCode: 500
        );
    }
});

app.MapPut("/users/{id}", (int id, User user) => {
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

app.MapDelete("/users/{id}", (int id) => {
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

app.Run();

public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public string Department { get; set; }
}
