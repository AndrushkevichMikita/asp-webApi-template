using ApiTemplate.Application.Models;

namespace ApiTemplate.Application.Interfaces
{
    public interface IEmailTemplateService
    {
        Task SendDigitCodeAsync(EmailDtoModel model);
        Task SendDigitCodeParallelAsync(List<EmailDtoModel> models);
    }
}
