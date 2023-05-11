using SQLitePCL;
using static SQLitePCL.raw;
using static SQLitePCL.Ugly.ugly;

namespace TotkBot.Services;

public class SqliteChatRepo : IChatRepo
{
    private const string DbName = "chats.db";
    public SqliteChatRepo()
    {
        Batteries_V2.Init();

        using var db = open_v2(DbName, SQLITE_OPEN_READWRITE | SQLITE_OPEN_CREATE, null);
        db.exec("PRAGMA journal_mode=WAL");
        db.exec("""
            CREATE TABLE IF NOT EXISTS Groups (
                chat_id INTEGER NOT NULL, 
                lang TEXT NOT NULL, 
                enabled INTEGER NOT NULL DEFAULT 1
            )
        """);

        db.exec("""
            CREATE UNIQUE INDEX IF NOT EXISTS chat_id_idx
            ON Groups (chat_id)
        """);
    }

    public void AddChat(long chatId, string language)
    {
        using var db = open_v2(DbName, SQLITE_OPEN_READWRITE, null);
        using var stmt = db.prepare("""
            INSERT OR REPLACE INTO Groups
            (chat_id, lang)
            VALUES (?, ?)
        """);
        stmt.bind(1, chatId);
        stmt.bind(2, language);

        stmt.step_done();
    }

    public IEnumerable<(long chatId, string language)> EnumerateGroups()
    {
        using var db = open_v2(DbName, SQLITE_OPEN_READONLY, null);
        using var stmt = db.prepare("""
            SELECT chat_id, lang FROM Groups
        """);

        while (stmt.step() == SQLITE_ROW)
        {
            var chatId = stmt.column_int64(0);
            var lang = stmt.column_text(1);

            yield return (chatId, lang);
        }
    }

    public void RemoveChat(long chatId)
    {
        using var db = open_v2(DbName, SQLITE_OPEN_READWRITE, null);
        using var stmt = db.prepare("DELETE FROM Groups WHERE chat_id = ?");
        stmt.bind(1, chatId);

        stmt.step_done();
    }
}