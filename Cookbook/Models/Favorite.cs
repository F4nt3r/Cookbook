using Microsoft.AspNetCore.Identity;

namespace Cookbook.Models
{
    public class Favorite
    {
        public int Id { get; set; }
        public int RecipeId { get; set; }
        public Recipe Recipe { get; set; }
        public string UserName { get; set; }
        public IdentityUser User { get; set; }
    }
}
