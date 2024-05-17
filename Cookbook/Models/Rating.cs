namespace Cookbook.Models
{
    public class Rating
    {
        public int Id { get; set; }
        public int RecipeId { get; set; }
        public Recipe Recipe { get; set; }
        public int Score { get; set; }
        public string UserName { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
