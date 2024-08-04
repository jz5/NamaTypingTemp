using Google.Protobuf;

namespace NamaTyping.NicoLive;

// プロトコルバッファーメッセージを取得するジェネリッククラス
public class Retriever
{
    private readonly HttpClient _httpClient = new();

    public async Task<IEnumerable<T>> RetrieveAsync<T>(string uri, MessageParser<T> parser) where T : IMessage<T>
    {
        var messages = new List<T>();

        Console.WriteLine($"Fetching messages... {uri}");
        HttpResponseMessage response;
        try
        {
            response = (await _httpClient.GetAsync(uri)).EnsureSuccessStatusCode();
        }
        catch (HttpRequestException e)
        {
            Console.WriteLine($"Network error while fetching messages: {e.Message}");
            // 通信エラーの再試行などの処理をここに追加
            return messages;
        }

        var stream = await response.Content.ReadAsStreamAsync();
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);
        memoryStream.Position = 0;

        List<byte> unread = new List<byte>();

        while (memoryStream.Position < memoryStream.Length)
        {
            try
            {
                var message = parser.ParseDelimitedFrom(memoryStream);
                messages.Add(message);
            }
            catch (InvalidProtocolBufferException e)
            {
                // 途中でちぎれていた場合、次回に読むための処理
                Console.WriteLine($"Incomplete message at position {memoryStream.Position}: {e.Message}");
                unread.AddRange(memoryStream.ToArray()[(int)memoryStream.Position..(int)memoryStream.Length]);
                break;
            }
        }

        // 通信が途中で切れた場合に備えて未読データを記録しておく
        if (unread.Count > 0)
        {
            Console.WriteLine($"Unprocessed data length: {unread.Count}");
            // 必要に応じて、`unread`リストのデータを保存や再試行処理などを行う
        }

        return messages;
    }
}