using System.ComponentModel.DataAnnotations;

namespace Cookbook.Models
{
    public class Recipe
    {
        public int Id { get; set; }
        [Required]
        public string Title { get; set; }

        [Required]
        public string Description { get; set; }

        [Url]
        [Required]
        public string ImageUrl { get; set; }

        
        public string VideoId { get; set; }
        [Required]
        public List<Ingredient> Ingredients { get; set; } = new List<Ingredient>();
        [Required]
        public string CreatedBy { get; set; }
        public List<Comment> Comments { get; set; } = new List<Comment>();
    }
}
