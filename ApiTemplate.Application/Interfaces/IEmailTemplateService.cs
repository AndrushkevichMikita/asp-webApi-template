using ApiTemplate.Application.Models;

namespace ApiTemplate.Application.Interfaces
{
    public interface IEmailTemplateService
    {
        Task SendDigitCodeAsync(EmailDto model);

        Task SendDigitCodeParallelAsync(List<EmailDto> models);
    }
}
