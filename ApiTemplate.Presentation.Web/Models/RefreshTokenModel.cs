using System.ComponentModel.DataAnnotations;

namespace ApiTemplate.Presentation.Web.Models
{
    public class RefreshTokenModel
    {
        [Required(AllowEmptyStrings = false)]
        public string Token { get; set; }

        [Required(AllowEmptyStrings = false)]
        public string RefreshToken { get; set; }
    }
}
