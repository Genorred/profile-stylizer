using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MyApp.Data;
using MyApp.Models;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpClient();

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

builder.Services.AddSingleton<TelegramLoginSessionStore>();
builder.Services.AddSingleton<TelegramBotAuthHandler>();
builder.Services.AddHostedService<TelegramBotHostedService>();
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

static string CreateToken(User user, IConfiguration config)
{
    var claims = new[]
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Email, user.Email),
        new Claim(ClaimTypes.Role, user.Role),
    };

    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

    var token = new JwtSecurityToken(
        issuer: config["Jwt:Issuer"],
        audience: config["Jwt:Audience"],
        claims: claims,
        expires: DateTime.UtcNow.AddHours(2),
        signingCredentials: creds
    );

    return new JwtSecurityTokenHandler().WriteToken(token);
}
app.MapPost(
        "/auth/login",
        async (LoginDto dto, AppDbContext db, IConfiguration config) =>
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);

            if (user is null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            {
                return Results.Unauthorized();
            }

            var token = CreateToken(user, config);

            return Results.Ok(new { access_token = token, token_type = "Bearer" });
        }
    )
    .WithTags("Auth");

app.MapGet(
        "/auth/me",
        async (ClaimsPrincipal principal, AppDbContext db) =>
        {
            var userIdValue = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdValue, out var userId))
                return Results.Unauthorized();

            var user = await db.Users.FindAsync(userId);
            if (user is null)
                return Results.NotFound();

            return Results.Ok(
                new
                {
                    user.Id,
                    user.Name,
                    user.Email,
                    user.Role,
                    user.Bio,
                    user.TelegramId,
                    user.TelegramName,
                    user.TelegramUsername,
                    user.TelegramBio,
                    user.TelegramPictures,
                }
            );
        }
    )
    .RequireAuthorization()
    .WithTags("Auth");

// ...

app.MapPost(
        "/auth/telegram/start",
        (IConfiguration config, TelegramLoginSessionStore sessionStore) =>
        {
            var botUsername = config["Telegram:BotUsername"];
            if (string.IsNullOrWhiteSpace(config["Telegram:BotToken"])
                || string.IsNullOrWhiteSpace(botUsername))
            {
                return Results.Problem("Telegram bot is not configured.");
            }

            var session = sessionStore.Create(TimeSpan.FromMinutes(10));
            var botUrl = $"https://t.me/{botUsername}?start=login_{session.Token}";

            return Results.Ok(new
            {
                token = session.Token,
                bot_url = botUrl,
                expires_at = session.ExpiresAt,
            });
        }
    )
    .WithTags("Auth");

app.MapGet(
        "/auth/telegram/status",
        (string token, TelegramLoginSessionStore sessionStore) =>
        {
            var session = sessionStore.Get(token);
            if (session is null || session.ExpiresAt < DateTime.UtcNow)
            {
                return Results.Ok(new { status = "expired" });
            }

            if (!session.Completed)
            {
                return Results.Ok(new { status = "pending" });
            }

            return Results.Ok(new
            {
                status = "completed",
                access_token = session.AccessToken,
                token_type = "Bearer",
                user_id = session.UserId,
            });
        }
    )
    .WithTags("Auth");

app.UseStaticFiles();

app.Run();

record RegisterDto(string Name, string Email, string Password);

record LoginDto(string Email, string Password);
