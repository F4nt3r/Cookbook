using System.ComponentModel.DataAnnotations;

namespace Cookbook.Models
{
    public class Ingredient
    {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }

        [Required]
        public string Quantity { get; set; }
    }
}
