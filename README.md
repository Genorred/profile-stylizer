# Profile Stylizer

MVP Full-Stack застосунок для створення стилізованої картки профілю на основі даних Telegram та авторизації через email/password або Telegram. Проєкт демонструє повний цикл роботи сучасного веб-додатку: серверна частина, база даних, API, авторизація, Swagger UI та інтерфейс користувача.
Стилiзовану картку можна выкористовувати для завантаження на своєму персональному сайтi.

## Мета проєкту

Створити мінімально життєздатний застосунок, який дозволяє:

- реєструвати та входити в систему;
- підключати Telegram-авторизацію;
- переглядати профіль користувача;
- генерувати стилізовану PNG-картку на основі даних профілю.

## Основні функції

- Реєстрація нового користувача
- Вхід за email та паролем з JWT
- Авторизація через Telegram
- Перегляд профілю та Telegram-даних
- Генерація стилізованої картки за допомогою SkiaSharp
- REST API з документацією через Swagger

## Технологічний стек

### Back-End

- ASP.NET Core 8
- Entity Framework Core
- SQLite
- JWT Bearer Authentication
- Swagger / OpenAPI
- Telegram.Bot
- SkiaSharp

### Front-End

- Vue 3
- Bootstrap 5
- Axios
- HTML/CSS/JavaScript

## Архітектура проєкту

Проєкт побудовано як Full-Stack MVP:

- Back-End обробляє авторизацію, зберігає дані користувачів у базі SQLite та надає REST API.
- Front-End взаємодіє з API через HTTP-запити і відображає дані користувача.
- Swagger UI використовується для демонстрації та тестування API.

## Структура проєкту

```text
backend/
  Data/                # EF Core контекст бази даних
  Migrations/          # міграції SQLite
  Models/              # моделі сутностей
  Services/            # сервіс генерації картки та Telegram-авторизації
  wwwroot/             # статичний Front-End (index.html, app.js, styles.css)
  Program.cs           # налаштування API, авторизації, endpoint-ів
frontend/              # окремий шаблон Vite/TypeScript, наразі не використовується в MVP
```

## REST API

Основні ендпоінти:

- GET /users — перегляд усіх користувачів
- GET /users/{id} — перегляд конкретного користувача
- POST /users — створення користувача
- PUT /users/{id} — оновлення даних користувача
- DELETE /users/{id} — видалення користувача

- POST /auth/register — реєстрація користувача
- POST /auth/login — вхід користувача, повертає JWT
- GET /auth/me — отримання профілю поточного користувача

- POST /auth/telegram/start — ініціалізація Telegram-входу
- GET /auth/telegram/status — перевірка статусу Telegram-входу

- GET /stylized-card — генерація стилізованої картки профілю

Swagger UI доступний за адресою:

- http://localhost:5149/swagger

## Запуск проєкту

### Вимоги

- .NET SDK 8+
- Linux/macOS/Windows з підтримкою .NET

### Кроки

```bash
cd backend
dotnet restore
dotnet run --launch-profile http
```

Після запуску відкрийте у браузері:

- http://localhost:5149/index.html
- http://localhost:5149/swagger

### Налаштування Telegram

Для роботи Telegram-авторизації необхідно вказати токен бота у файлі:

- backend/appsettings.json

або через User Secrets.

## Демонстраційний сценарій

1. Зареєструвати користувача або увійти через email/password.
2. Увійти через Telegram для автоматичного заповнення профілю.
3. Переглянути дані профілю у інтерфейсі.
4. Сгенерувати стилізовану картку.

## Чек-лист готовності MVP

- [x] Back-End запускається без помилок
- [x] База даних підключена та створюється автоматично
- [x] CRUD-ендпоінти доступні для основної сутності
- [x] Реєстрація та авторизація працюють
- [x] Swagger UI доступний
- [x] Front-End взаємодіє з API
- [x] Є технічна документація у вигляді README
