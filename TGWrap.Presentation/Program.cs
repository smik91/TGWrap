using System.Text;
using System.Text.Json;
using TGWrap.Presentation.Models;

class Program
{
    static void Main()
    {
        string filePath = "path_to_ur_file_json";
        string jsonContent = File.ReadAllText(filePath);

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        Console.OutputEncoding = Encoding.Unicode;

        var chatData = JsonSerializer.Deserialize<ChatData>(jsonContent, options);
        var userStats = CalculateAllStats(chatData);

        Console.WriteLine("Message statistics by users:");
        long totalMessages = 0;
        foreach (var kvp in userStats)
        {
            Console.WriteLine($"{kvp.Key}: {kvp.Value.MessageCount} messages");
            totalMessages += kvp.Value.MessageCount;
        }
        Console.WriteLine($"Total number of messages: {totalMessages}\n");

        Console.WriteLine("Photo statistics by users:");
        long totalPhotos = 0;
        foreach (var kvp in userStats)
        {
            Console.WriteLine($"{kvp.Key}: {kvp.Value.PhotoCount} photos");
            totalPhotos += kvp.Value.PhotoCount;
        }
        Console.WriteLine($"Total number of photos: {totalPhotos}\n");

        Console.WriteLine("Video statistics by users:");
        long totalVideos = 0;
        foreach (var kvp in userStats)
        {
            Console.WriteLine($"{kvp.Key}: {kvp.Value.VideoCount} videos");
            totalVideos += kvp.Value.VideoCount;
        }
        Console.WriteLine($"Total number of videos: {totalVideos}\n");

        Console.WriteLine("Round video statistics by users:");
        long totalRounds = 0;
        foreach (var kvp in userStats)
        {
            Console.WriteLine($"{kvp.Key}: {kvp.Value.RoundsCount} round videos; {kvp.Value.RoundsDuration} seconds");
            totalRounds += kvp.Value.RoundsCount;
        }
        Console.WriteLine($"Total number of round videos: {totalRounds}\n");

        Console.WriteLine("Voice message statistics by users:");
        long totalVoiceMsg = 0;
        foreach (var kvp in userStats)
        {
            Console.WriteLine($"{kvp.Key}: {kvp.Value.VoiceCount} voice messages; {kvp.Value.VoiceDuration} seconds");
            totalVoiceMsg += kvp.Value.VoiceCount;
        }
        Console.WriteLine($"Total number of voice messages: {totalVoiceMsg}\n");

        var (busiestDay, messagesPerUser) = FindBusiestDay(chatData);

        Console.WriteLine("Day with the highest number of messages:");
        Console.WriteLine($"{busiestDay.ToString("dd-MM-yyyy")} (Total messages: {messagesPerUser.Values.Sum()})\n");
        Console.WriteLine($"Number of messages from users on {busiestDay.ToString("dd-MM-yyyy")}:");
        foreach (var kvp in messagesPerUser)
        {
            Console.WriteLine($"{kvp.Key}: {kvp.Value} messages");
        }

        var (busiestMonth, messagesPerUserMonth) = FindBusiestMonth(chatData);
        Console.WriteLine("\nMonth with the highest number of messages:");
        Console.WriteLine($"{busiestMonth} (Total messages: {messagesPerUserMonth.Values.Sum()})\n");
        Console.WriteLine($"Number of messages from users in {busiestMonth}:");
        foreach (var kvp in messagesPerUserMonth)
        {
            Console.WriteLine($"{kvp.Key}: {kvp.Value} messages");
        }

        var (startIdle, endIdle) = FindLongestIdlePeriod(chatData);
        Console.WriteLine("\nLongest idle period without messages:");
        Console.WriteLine($"{startIdle.ToString("dd-MM-yyyy")} - {endIdle.ToString("dd-MM-yyyy")}");
    }

    private static Dictionary<string, UserStats> CalculateAllStats(ChatData chatData)
    {
        var userStats = new Dictionary<string, UserStats>();

        foreach (var msg in chatData.messages)
        {
            if (msg.from == null)
            {
                continue;
            }

            var username = msg.from;
            

            if (!userStats.ContainsKey(username))
            {
                userStats[username] = new UserStats();
            }

            userStats[username].MessageCount++;

            if (msg.photo != null)
            {
                userStats[username].PhotoCount++;
            }

            if (msg.media_type == "video_file")
            {
                userStats[username].VideoCount++;
            }

            if (msg.media_type == "video_message")
            {
                userStats[username].RoundsCount++;
                userStats[username].RoundsDuration += msg.duration_seconds;
            }

            if (msg.media_type == "voice_message")
            {
                userStats[username].VoiceCount++;
                userStats[username].VoiceDuration += msg.duration_seconds;
            }
        }

        return userStats;
    }

    private static (DateTime BusiestDay, Dictionary<string, long> MessagesPerUser) FindBusiestDay(ChatData chatData)
    {
        var messagesByDate = chatData.messages
            .Where(m => m.date != default(DateTime))
            .GroupBy(m => m.date.Date)
            .Select(g => new { Date = g.Key, MessageCount = g.Count() })
            .ToList();

        if (!messagesByDate.Any())
        {
            throw new InvalidOperationException("No messages found.");
        }

        var busiestDay = messagesByDate
            .OrderByDescending(g => g.MessageCount)
            .First()
            .Date;

        var messagesOnBusiestDay = chatData.messages
            .Where(m => m.date.Date == busiestDay)
            .ToList();

        var messagesPerUser = messagesOnBusiestDay
            .Where(m => m.from != null)
            .GroupBy(m => m.from)
            .ToDictionary(g => g.Key, g => (long)g.Count());

        return (busiestDay, messagesPerUser);
    }

    private static (string BusiestMonth, Dictionary<string, long> MessagesPerUser) FindBusiestMonth(ChatData chatData)
    {
        var messagesByMonth = chatData.messages
            .Where(m => m.date != default(DateTime))
            .GroupBy(m => new { m.date.Year, m.date.Month })
            .Select(g => new
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                MessageCount = g.Count()
            })
            .ToList();

        var busiestMonthData = messagesByMonth
            .OrderByDescending(g => g.MessageCount)
            .First();

        string busiestMonth = $"{busiestMonthData.Month:D2}-{busiestMonthData.Year}";

        var messagesOnBusiestMonth = chatData.messages
            .Where(m => m.date.Year == busiestMonthData.Year && m.date.Month == busiestMonthData.Month)
            .ToList();

        var messagesPerUser = messagesOnBusiestMonth
            .Where(m => m.from != null)
            .GroupBy(m => m.from)
            .ToDictionary(g => g.Key, g => (long)g.Count());

        return (busiestMonth, messagesPerUser);
    }

    private static (DateTime StartIdle, DateTime EndIdle) FindLongestIdlePeriod(ChatData chatData)
    {
        var uniqueDates = chatData.messages
            .Where(m => m.date != default(DateTime))
            .Select(m => m.date.Date)
            .Distinct()
            .OrderBy(d => d)
            .ToList();


        TimeSpan maxGap = TimeSpan.Zero;
        DateTime idleStart = DateTime.MinValue;
        DateTime idleEnd = DateTime.MinValue;

        for (int i = 1; i < uniqueDates.Count; i++)
        {
            var previousDate = uniqueDates[i - 1];
            var currentDate = uniqueDates[i];
            var gap = currentDate - previousDate;

            var gapDays = (gap.Days - 1);

            if (gapDays > maxGap.Days)
            {
                maxGap = gap;
                idleStart = previousDate.AddDays(1);
                idleEnd = currentDate.AddDays(-1);
            }
        }

        return (idleStart, idleEnd);
    }
}