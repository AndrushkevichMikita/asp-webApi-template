using System.ComponentModel.DataAnnotations;

namespace ApplicationCore.Models
{
    public class SMTPSettings
    {
        [Required]
        public string UserName { get; set; }

        [Required]
        public string Password { get; set; }

        [Required]
        public string Host { get; set; }

        [Required]
        public int Port { get; set; }

        [Required]
        public string EmailFrom { get; set; }
    }
}
