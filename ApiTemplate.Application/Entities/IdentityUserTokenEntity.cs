using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApiTemplate.Application.Entities
{
    [Table("AspNetUserTokens")]
    public class IdentityUserTokenEntity : IdentityUserToken<int>
    {
        public ApplicationUserEntity User { get; set; }
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
