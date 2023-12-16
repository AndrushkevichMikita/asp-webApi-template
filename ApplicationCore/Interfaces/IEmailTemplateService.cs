using ApplicationCore.Models;

namespace ApplicationCore.Interfaces
{
    public interface IEmailTemplateService
    {
        Task SendDigitCodeAsync(EmailModel model);
        Task SendDigitCodeParallelAsync(List<EmailModel> models);
    }
}
