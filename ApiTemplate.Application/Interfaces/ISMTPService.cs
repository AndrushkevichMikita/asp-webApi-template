namespace ApiTemplate.Application.Interfaces
{
    public interface ISMTPService
    {
        Task SendAsync(string destination, string subject, string body);
    }
}
