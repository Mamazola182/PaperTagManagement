using AiApi.Models;
using Microsoft.AspNetCore.Mvc;

namespace CoreAPI.Controllers
{
    [ApiController]
    [Route("api/ai")]
    public class AiController : Controller
    {
        private readonly GeminiService _geminiService;

        public AiController(GeminiService geminiService)
        {
            _geminiService = geminiService;
        }

        // API: POST /api/ai/suggest-tags
        [HttpPost("suggesttags")]
        public async Task<IActionResult> SuggestTags([FromBody] TagSuggestionRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Content))
            {
                return BadRequest(new { Message = "Nội dung bài viết không được để trống." });
            }

            try
            {
                // Gọi service để đề xuất tags
                var response = await _geminiService.SuggestTagsAsync(request.Content, request.MaxTags);

                return Ok(response);
            }
            catch (Exception ex)
            {
                // Xử lý lỗi (ví dụ: lỗi kết nối API, hết quota Free Tier)
                return StatusCode(500, new { Message = "Lỗi khi gọi API Gemini: " + ex.Message });
            }
        }
    }
}
