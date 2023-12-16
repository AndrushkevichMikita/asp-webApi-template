using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApplicationCore.Entities
{
    [Table("AspNetUserTokens")]
    public class IdentityUserTokenEntity : IdentityUserToken<int>
    {
        public UserEntity User { get; set; }
    }

    public enum TokenEnum
    {
        JwtToken = 1,
        PasswordToken,
        EmailToken,
        SignUpToken,
        UnsubscribeSMS
    }
}
