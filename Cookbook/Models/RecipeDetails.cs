namespace Cookbook.Models
{
    public class RecipeDetails
    {
        public Recipe Recipe { get; set; }
        public Comment NewComment { get; set; }
        public bool IsFavorite { get; set; }
    }
}
