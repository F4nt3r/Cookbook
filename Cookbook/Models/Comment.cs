using System.ComponentModel.DataAnnotations;

namespace Cookbook.Models
{
    public class Comment
    {
        public int Id { get; set; }

        [Required]
        public string Content { get; set; } // Treść komentarza

        [Required]
        public string UserId { get; set; } // Id użytkownika, który dodał komentarz

        [Required]
        public string UserName { get; set; } // Nazwa użytkownika, który dodał komentarz
        [Required]
        [DataType(DataType.DateTime)]
        public DateTime CreatedAt { get; set; } // Data i czas utworzenia komentarza

    }
}
