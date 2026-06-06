namespace CoreAPI.Models
{
    public class TagSuggestionResponse
    {
        public List<string> SuggestedTags { get; set; } = new List<string>();

        public string ModelUsed { get; set; }
    }
}
