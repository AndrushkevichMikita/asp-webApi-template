using ApplicationCore.Models;

namespace ApplicationCore.Interfaces
{
    public interface IEmailTemplateService
    {
        Task SendDigitCodeAsync(EmailDtoModel model);
        Task SendDigitCodeParallelAsync(List<EmailDtoModel> models);
    }
}
