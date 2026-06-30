using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using MyApp.Data;
using MyApp.Models;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc(
        "v1",
        new OpenApiInfo
        {
            Title = "MVP Back-End API",

            Version = "v1",

            Description = "Документація REST API навчального MVP-проєкту.",
        }
    );
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "MVP Back-End APIv1");

    options.DocumentTitle = "MVP Back-End API";
});

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

app.MapGet("/users", async (AppDbContext db) => await db.Users.ToListAsync()).WithTags("Users");

app.MapGet(
    "/users/{id}",
    async (int id, AppDbContext db) =>
        await db.Users.FindAsync(id) is User user ? Results.Ok(user) : Results.NotFound()
).WithTags("Users");

app.MapPost(
    "/users",
    async (User user, AppDbContext db) =>
    {
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return Results.Created($"/users/{user.Id}", user);
    }
).WithTags("Users");


app.MapPut(
    "/users/{id}",
    async (int id, User input, AppDbContext db) =>
    {
        var user = await db.Users.FindAsync(id);
        if (user is null)
            return Results.NotFound();

        user.Name = input.Name;
        user.Email = input.Email;
        await db.SaveChangesAsync();
        return Results.NoContent();
    }
).WithTags("Users");

app.MapDelete(
    "/users/{id}",
    async (int id, AppDbContext db) =>
    {
        var user = await db.Users.FindAsync(id);
        if (user is null)
            return Results.NotFound();

        db.Users.Remove(user);
        await db.SaveChangesAsync();
        return Results.NoContent();
    }
).WithTags("Users");

app.Run();
