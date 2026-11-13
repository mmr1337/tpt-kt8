using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Petstore;

public static class PetstoreCases
{
    private static readonly Uri BaseUrl = new("https://petstore.swagger.io/v2/");
    private static readonly JsonArray Cases = JsonNode.Parse(
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "cases.json")))!.AsArray();

    public static IEnumerable<string> Ids => Cases.Select(item => Key(item!));

    public static async Task Run(string testId)
    {
        var token = Guid.NewGuid().ToString("N")[..12];
        var name = $"tpt_{token}";
        var id = Convert.ToInt64(token, 16) + 1000;
        var scenario = Replace(Cases.Single(item => Key(item!) == testId)!, id, name);
        using var client = new HttpClient { BaseAddress = BaseUrl, Timeout = TimeSpan.FromSeconds(15) };

        try
        {
            if (scenario["setup"] is JsonNode setup)
                RequireStatus(await Send(client, setup), [200]);
            if (scenario["login"]?.GetValue<bool>() == true)
                RequireStatus(await Get(client, $"user/login?username={name}&password=course-example-42"), [200]);

            var result = await Send(client, scenario);
            RequireStatus(result, scenario["status"]!.Deserialize<int[]>()!);
            if (scenario["follow"] is JsonNode follow)
            {
                result = await Get(client, follow.GetValue<string>());
                RequireStatus(result, [200]);
            }
            VerifyBody(result.Body, scenario);
            if (scenario["missing"] is JsonNode missing)
                RequireStatus(await Get(client, missing.GetValue<string>()), [404]);
        }
        finally
        {
            if (scenario["cleanup"] is JsonNode cleanup)
            {
                using var response = await client.DeleteAsync(cleanup.GetValue<string>());
                RequireStatus(new Result((int)response.StatusCode, await response.Content.ReadAsStringAsync()), [200, 404]);
            }
        }
    }

    private static string Key(JsonNode row) => $"{row["operation"]!.GetValue<string>()}-{row["scenario"]!.GetValue<string>()}";

    private static JsonNode Replace(JsonNode row, long id, string name)
    {
        var source = row.ToJsonString().Replace("\"$id\"", id.ToString()).Replace("$id", id.ToString())
            .Replace("$name", name).Replace("$email", $"{name}@example.invalid");
        return JsonNode.Parse(source)!;
    }

    private static Task<Result> Get(HttpClient client, string path) => Send(client,
        new JsonObject { ["method"] = "GET", ["path"] = path });

    private static async Task<Result> Send(HttpClient client, JsonNode row)
    {
        var path = row["path"]!.GetValue<string>();
        if (row["query"] is JsonObject query)
            path += "?" + string.Join("&", query.Select(pair => $"{pair.Key}={Uri.EscapeDataString(pair.Value!.GetValue<string>())}"));

        using var request = new HttpRequestMessage(new HttpMethod(row["method"]!.GetValue<string>()), path);
        request.Headers.Accept.ParseAdd(row["accept"]?.GetValue<string>() ?? "application/json");
        if (row["json"] is JsonNode body)
            request.Content = JsonContent.Create(body);
        if (row["form"] is JsonObject form)
            request.Content = new FormUrlEncodedContent(form.Select(pair => KeyValuePair.Create(pair.Key, pair.Value!.GetValue<string>())));
        if (row["upload"]?.GetValue<bool>() == true)
        {
            var multipart = new MultipartFormDataContent();
            var image = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+/6ZkAAAAASUVORK5CYII=");
            multipart.Add(new ByteArrayContent(image), "file", "pixel.png");
            request.Content = multipart;
        }
        using var response = await client.SendAsync(request);
        return new Result((int)response.StatusCode, await response.Content.ReadAsStringAsync());
    }

    private static void RequireStatus(Result response, int[] expected)
    {
        if (!expected.Contains(response.Status))
            throw new InvalidOperationException($"Expected HTTP {string.Join(" or ", expected)}, got {response.Status}: {response.Body[..Math.Min(300, response.Body.Length)]}");
    }

    private static void VerifyBody(string response, JsonNode scenario)
    {
        if (scenario["checks"] is JsonObject checks)
        {
            var body = JsonNode.Parse(response)!;
            foreach (var (key, expected) in checks)
                if (!JsonNode.DeepEquals(body[key], expected))
                    throw new InvalidOperationException($"Field {key}: expected {expected}, got {body[key]}");
        }

        switch (scenario["kind"]?.GetValue<string>())
        {
            case "inventory":
                var inventory = JsonNode.Parse(response)!.AsObject();
                if (inventory.Any(pair => pair.Value!.GetValue<int>() < 0))
                    throw new InvalidOperationException("Inventory contains a negative quantity");
                break;
            case "message":
                if (string.IsNullOrWhiteSpace(JsonNode.Parse(response)?["message"]?.GetValue<string>()))
                    throw new InvalidOperationException("Response message is missing");
                break;
            case "available":
                var pets = JsonNode.Parse(response)!.AsArray();
                if (pets.Any(pet => pet?["status"]?.GetValue<string>() != "available"))
                    throw new InvalidOperationException("Response contains pets with a different status");
                break;
        }
    }

    private sealed record Result(int Status, string Body);
}
