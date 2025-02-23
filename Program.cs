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
    new User() { Id = 1, Name = "Wes" },
    new User() { Id = 2, Name = "John" },
    new User() { Id = 3, Name = "Jane" },
};

//Basic CRUD Endpoints
app.MapGet("/users", () => {
    return Results.Ok(users);
});

app.MapGet("/users/{id}", (int id) => {
    var user = users.FirstOrDefault(u => u.Id == id);
    return Results.Ok(user);
});

app.MapPost("/users", (User user) => {
    users.Add(user);
    return Results.Ok(user);
});

app.MapPut("/users/{id}", (int id, User user) => {
    var userToUpdate = users.FirstOrDefault(u => u.Id == id);
    userToUpdate.Name = user.Name;
    return Results.Ok(user);
});

app.MapDelete("/users/{id}", (int id) => {
    var userToDelete = users.FirstOrDefault(u => u.Id == id);    
    users.Remove(userToDelete);    
    return Results.Ok(userToDelete);
});

app.Run();

public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
}
