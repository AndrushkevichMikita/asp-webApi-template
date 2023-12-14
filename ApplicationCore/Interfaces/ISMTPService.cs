namespace ApplicationCore.Interfaces
{
    public interface ISMTPService
    {
        Task SendAsync(string destination, string subject, string body);
    }
}
