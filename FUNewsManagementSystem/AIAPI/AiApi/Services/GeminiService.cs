using Microsoft.Extensions.Configuration;
using Mscc.GenerativeAI;
using System.Text.Json; 
using AiApi.Models;
public class GeminiService
{
    private readonly GenerativeModel _model;
    private readonly string _modelName = Model.Gemini25Flash; 

    public GeminiService(IConfiguration configuration)
    {
        string apiKey = configuration["Gemini:ApiKey"] ?? throw new ArgumentNullException("Gemini:ApiKey is not configured.");
        var googleAI = new GoogleAI(apiKey: apiKey);
        _model = googleAI.GenerativeModel(model: _modelName);
    }


    public async Task<TagSuggestionResponse> SuggestTagsAsync(string content, int maxTags)
    {
        
        var systemInstruction = $@"Bạn là một công cụ đề xuất tag chuyên nghiệp. 
            Phân tích nội dung sau và đề xuất TỐI ĐA {maxTags} tags/từ khóa liên quan nhất. 
            Chỉ trả về kết quả dưới dạng một mảng JSON hợp lệ theo cú pháp sau: 
            [""tag1"", ""tag2"", ""tag3"", ...]";

        var prompt = $"Nội dung bài viết:\n---\n{content}";

        var request = new GenerateContentRequest
        {
            Contents = new List<Content>
    {
        new Content
        {
            Role = "user",
            Parts = new List<IPart>
            {
                Part.FromText(prompt)
            }
        }
    },
            GenerationConfig = new GenerationConfig
            {
                ResponseMimeType = "application/json"
            },
            SystemInstruction = new Content
            {
                Role = "system",
                Parts = new List<IPart>
        {
            Part.FromText(systemInstruction)
        }
            }
        };

        var response = await _model.GenerateContent(request);

        if (string.IsNullOrWhiteSpace(response.Text))
        {
            return new TagSuggestionResponse();
        }

        try
        {
            var suggestedTags = JsonSerializer.Deserialize<List<string>>(response.Text,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return new TagSuggestionResponse
            {
                SuggestedTags = suggestedTags ?? new List<string>(),
                ModelUsed = _modelName
            };
        }
        catch (JsonException)
        {
            return new TagSuggestionResponse();
        }
    }
}