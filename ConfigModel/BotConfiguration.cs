using System.ComponentModel.DataAnnotations;

namespace TotkBot.ConfigModel;

public class BotConfiguration
{
    public static readonly string SectionName = "BotConfiguration";

    [Required]
    public string Token { get; set; } = null!;

    [Required]
    public string? BotName { get; set; }
    public DateTime TargetDate { get; set; } = new(2023, 05, 12, 16, 0, 0, DateTimeKind.Utc);
    public long AdminId { get; set; }
}
