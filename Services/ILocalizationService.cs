namespace TotkBot.Services;

public interface ILocalizationService
{
    string GetWelcomeMessage(string? language);
    string GetCountdownMessage(string? language, TimeSpan delta);
}
