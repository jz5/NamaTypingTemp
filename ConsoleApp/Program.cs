using Dwango.Nicolive.Chat.Service.Edge;
using Google.Protobuf;
using NamaTyping.NicoLive;

namespace ConsoleApp;

class Program
{
    static async Task Main(string[] args)
    {
        // メッセージリトリーバの初期化
        var retriever = new Retriever();

        // Protobufのメッセージパーサーを定義
        var entryParser = new MessageParser<ChunkedEntry>(() => new ChunkedEntry());
        var messageParser = new MessageParser<ChunkedMessage>(() => new ChunkedMessage());

        // メッセージURIを指定
        var at = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var uri =
            $"https://mpn.live.nicovideo.jp/api/view/v4/BBxpyaqzTjyoP0nvLMKOvkjmKTDleM-f9UBU85TpEHp8sDIbU65MxXFMBzQxBUQ?at={at}";

        // メッセージの取得と表示
        await FetchAndDisplayMessagesAsync(retriever, entryParser, messageParser, uri);

        
        Console.ReadKey(true);
        
    }

    static async Task FetchAndDisplayMessagesAsync(
        Retriever retriever,
        MessageParser<ChunkedEntry> entryParser,
        MessageParser<ChunkedMessage> messageParser,
        string uri
    )
    {
        var entries = await retriever.RetrieveAsync(uri, entryParser);
        foreach (var message in entries)
        {
            Console.WriteLine($"ChunkedEntry: {message}"); // メッセージの内容を表示

            if (message.EntryCase == ChunkedEntry.EntryOneofCase.Previous)
            {
                await FetchAndDisplayMessagesAsync(retriever, messageParser, message.Previous.Uri);
            }
            else if (message.EntryCase == ChunkedEntry.EntryOneofCase.Segment)
            {
                await FetchAndDisplayMessagesAsync(retriever, messageParser, message.Segment.Uri);
            }
        }
    }

    static async Task FetchAndDisplayMessagesAsync(
        Retriever retriever,
        MessageParser<ChunkedMessage> parser,
        string uri)
    {
        var messages = await retriever.RetrieveAsync(uri, parser);
        foreach (var message in messages)
        {
            Console.WriteLine($"ChunkedMessage: {message}"); // メッセージの内容を表示
        }
    }
}