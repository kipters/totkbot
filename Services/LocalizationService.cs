using System.Globalization;
using Humanizer;
using Humanizer.Localisation;

namespace TotkBot.Services;

#pragma warning disable CA1508 // Avoid dead conditional code
public class LocalizationService : ILocalizationService
{
    public string GetCountdownMessage(string? language, TimeSpan delta)
    {
        //HACK for now
        language = "it";
        if (delta.TotalSeconds <= 0)
        {
            return language switch
            {
                "it" => "Tears of the Kingdom è uscito\\!",
                _ => "Tears of the Kingdom is now out\\!"
            };
        }
        var culture = new CultureInfo(language ?? "en");
        var template = language switch
        {
            "it" => "**{0}** al rilascio di Tears of the Kingdom",
            _ => "**{0}** until Tears of the Kingdom is released"
        };

        var humanizedDelta = delta.Humanize(precision: 99,
            culture: culture,
            collectionSeparator: ", ",
            maxUnit: TimeUnit.Hour,
            minUnit: TimeUnit.Second,
            toWords: true)
            .Humanize(LetterCasing.Sentence);

        var text = string.Format(culture, template, humanizedDelta);
        return text;
    }

    public string GetWelcomeMessage(string? language)
    {
        //HACK for now
        language = "it";

        return language switch
        {
            "it" => "Ciao! Ti manderò update orari, o puoi aggiungermi in un gruppo.",
            _ => "Hello! I'll send you hourly updates, or you can add me into a group."
        };
    }
}
