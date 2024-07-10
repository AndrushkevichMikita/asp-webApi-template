using System.ComponentModel.DataAnnotations;

namespace ApplicationCore.Models
{
    public class RefreshTokenDtoModel
    {
        [Required(AllowEmptyStrings = false)]
        public string Token { get; set; }

        [Required(AllowEmptyStrings = false)]
        public string RefreshToken { get; set; }
    }
}
