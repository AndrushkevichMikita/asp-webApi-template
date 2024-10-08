﻿using ApiTemplate.Application.Interfaces;
using ApiTemplate.Domain.Configuration;
using ApiTemplate.SharedKernel.ExceptionHandler;
using Microsoft.Extensions.Options;
using System.Net.Mail;

namespace ApiTemplate.Application.Services
{
    public class SMTPService : ISMTPService
    {
        private readonly SMTPConfiguration _settings;

        public SMTPService(IOptions<SMTPConfiguration> smtp)
        {
            _settings = smtp.Value;
        }

        public async Task SendAsync(string destination, string subject, string body)
        {
            try
            {
                using var client = new SmtpClient(_settings.Host, _settings.Port);
                client.UseDefaultCredentials = false;
                client.Credentials = new System.Net.NetworkCredential(_settings.UserName, _settings.Password);
                client.EnableSsl = true;

                await client.SendMailAsync(new MailMessage(_settings.EmailFrom, destination)
                {
                    Subject = subject,
                    IsBodyHtml = true,
                    Body = body
                });
            }
            catch (SmtpException ex)
            {
                if (ex.StatusCode == SmtpStatusCode.MailboxUnavailable)
                {
                    throw new MyApplicationException(ErrorStatus.NotFound, $"Mailbox [{destination}] unavailable");
                }
                throw new MyApplicationException(ErrorStatus.InvalidData, "EmailService. Send: ", ex);
            }
            catch (Exception ex)
            {
                throw new MyApplicationException(ErrorStatus.InvalidData, "EmailService. Send: ", ex);
            };
        }
    }
}
