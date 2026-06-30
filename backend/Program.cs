using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using MyApp.Data;
using MyApp.Models;
using System.Text;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));

builder
    .Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)
            ),
        };
    });

builder.Services.AddAuthorization();

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
    options.AddSecurityDefinition(
        "Bearer",
        new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Введіть JWT-токен, отриманий з ендпоінта /auth/login.",
        }
    );

    options.AddSecurityRequirement(
        new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer",
                    },
                },
                Array.Empty<string>()
            },
        }
    );
});

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

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
    )
    .WithTags("Users");

app.MapPost(
        "/users",
        async (User user, AppDbContext db) =>
        {
            db.Users.Add(user);
            await db.SaveChangesAsync();
            return Results.Created($"/users/{user.Id}", user);
        }
    )
    .WithTags("Users");

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
    )
    .WithTags("Users");

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
    )
    .WithTags("Users");

app.MapPost(
        "/auth/register",
        async (RegisterDto dto, AppDbContext db) =>
        {
            if (await db.Users.AnyAsync(u => u.Email == dto.Email))
            {
                return Results.Conflict("Користувач з таким email вже існує.");
            }

            var user = new User
            {
                Name = dto.Name,
                Email = dto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = "user",
            };

            db.Users.Add(user);
            await db.SaveChangesAsync();

            return Results.Created(
                $"/users/{user.Id}",
                new
                {
                    user.Id,
                    user.Name,
                    user.Email,
                    user.Role,
                }
            );
        }
    )
    .WithTags("Auth");

app.Run();

record RegisterDto(string Name, string Email, string Password);
