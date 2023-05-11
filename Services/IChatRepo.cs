namespace TotkBot.Services;

public interface IChatRepo
{
    void AddChat(long chatId, string language);
    void RemoveChat(long chatId);
    IEnumerable<(long chatId, string language)> EnumerateGroups();
}
