using Microsoft.Extensions.Configuration;
using Mscc.GenerativeAI;
using System.Text.Json; // Cần dùng để xử lý JSON
using CoreAPI.Models;
public class GeminiService
{
    private readonly GenerativeModel _model;
    private readonly string _modelName = Model.Gemini25Flash; // Ví dụ: Sử dụng mô hình Free Tier

    public GeminiService(IConfiguration configuration)
    {
        string apiKey = configuration["Gemini:ApiKey"] ?? throw new ArgumentNullException("Gemini:ApiKey is not configured.");
        var googleAI = new GoogleAI(apiKey: apiKey);
        _model = googleAI.GenerativeModel(model: _modelName);
    }

    /// <summary>
    /// Đề xuất các tags dựa trên nội dung bài viết.
    /// </summary>
    /// <param name="content">Nội dung bài viết.</param>
    /// <param name="maxTags">Số lượng tags tối đa.</param>
    /// <returns>Danh sách các tags.</returns>
    public async Task<TagSuggestionResponse> SuggestTagsAsync(string content, int maxTags)
    {
        // 1. Xây dựng Prompt
        // Yêu cầu Gemini chỉ trả lời bằng một mảng JSON
        var systemInstruction = $@"Bạn là một công cụ đề xuất tag chuyên nghiệp. 
            Phân tích nội dung sau và đề xuất TỐI ĐA {maxTags} tags/từ khóa liên quan nhất. 
            Chỉ trả về kết quả dưới dạng một mảng JSON hợp lệ theo cú pháp sau: 
            [""tag1"", ""tag2"", ""tag3"", ...]";

        var prompt = $"Nội dung bài viết:\n---\n{content}";

        // 2. Cấu hình yêu cầu
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



        // 3. Gọi API
        var response = await _model.GenerateContent(request);

        // 4. Xử lý phản hồi
        if (string.IsNullOrWhiteSpace(response.Text))
        {
            return new TagSuggestionResponse();
        }

        try
        {
            // Deserialize chuỗi JSON thành List<string>
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
            // Trường hợp Gemini trả lời không đúng định dạng JSON
            // Bạn có thể xử lý để trả về một lỗi hoặc log lại.
            // Để đơn giản, ta trả về một mảng rỗng.
            return new TagSuggestionResponse();
        }
    }
}