using Dwango.Nicolive.Chat.Service.Edge;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
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
            $"https://mpn.live.nicovideo.jp/api/view/v4/BBxpyaqzTjyoP0nvLMKOvkjmKTDleM-f9UBU85TpEHp8sDIbU65MxXFMBzQxBUQ";

        // メッセージの取得と表示
        await FetchForwardPlaylistMessagesAsync(retriever, entryParser, messageParser, uri, 0);


        Console.ReadKey(true);
    }

    static async Task FetchForwardPlaylistMessagesAsync(
        Retriever retriever,
        MessageParser<ChunkedEntry> entryParser,
        MessageParser<ChunkedMessage> messageParser,
        string uri,
        long from
    )
    {
        
        var initialPhase = true;
        var next = from;
        while (true)
        {
            var at = next <= 0 ? "now" : next.ToString();
            var entries = await retriever.RetrieveAsync($"{uri}?at={at}", entryParser);
            foreach (var entry in entries)
            {
                Console.WriteLine($"ChunkedEntry: {entry}"); // メッセージの内容を表示

                if (entry.EntryCase == ChunkedEntry.EntryOneofCase.Backward && initialPhase)
                {
                    
                }
                else if (entry.EntryCase == ChunkedEntry.EntryOneofCase.Previous && initialPhase)
                {
                    await PullMessage(retriever, messageParser, entry.Previous.Uri);
                }
                else if (entry.EntryCase == ChunkedEntry.EntryOneofCase.Segment)
                {
                    await SleepUntil(entry.Segment.From, 1000);
                    _ = PullMessage(retriever, messageParser, entry.Segment.Uri);
                }
                else if (entry.EntryCase == ChunkedEntry.EntryOneofCase.Next)
                {
                    Console.WriteLine($"Next: {entry.Next.At}");
                    next = entry.Next.At;
                }

            }
            initialPhase = false;
        }
    }
    
    private static async Task SleepUntil(Timestamp timestamp, int prefetch = 0)
    {
        var until = timestamp.Seconds * 1000 + timestamp.Nanos / 1000_000;
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        Console.WriteLine($"until: {until}, now: {now}");
        
        if (until <= now)
            return;
        
        Console.WriteLine($"waiting {(int)((until - now - prefetch) / 1000)}sec");
        await Task.Delay((int)(until - now - prefetch));
    }
  
    
    static async Task PullMessage(
        Retriever retriever,
        MessageParser<ChunkedMessage> parser,
        string uri)
    {
        var messages = await retriever.RetrieveAsync(uri, parser);
        foreach (var message in messages)
        {
            
            if (message.PayloadCase == ChunkedMessage.PayloadOneofCase.Message)
            {
                var at = message.Meta.At.ToDateTimeOffset();
                Console.WriteLine($"{at}: {message.Message.Chat.Vpos}: {message.Message.Chat.Content}");    
            }
            else
            {
                Console.WriteLine($"{message.PayloadCase}");
            }
            
            // Console.WriteLine($"ChunkedMessage: {message}"); // メッセージの内容を表示
        }
    }
}