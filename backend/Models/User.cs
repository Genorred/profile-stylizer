namespace MyApp.Models;

public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = "user";
    public long? TelegramId { get; set; }
    public string TelegramName { get; set; } = string.Empty;
    public string TelegramUsername { get; set; } = string.Empty;
    public string TelegramBio { get; set; } = string.Empty;
    public List<string> TelegramPictures { get; set; } = new();
}
