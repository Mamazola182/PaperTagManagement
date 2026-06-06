namespace AiApi.Models
{
    public class TagSuggestionRequest
    {
        public string Content { get; set; }

        public int MaxTags { get; set; } = 5;
    }
}
