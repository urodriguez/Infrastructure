﻿using System;
using System.Reflection;
using Core.WebApi;
using Infrastructure.CrossCutting.Authentication;
using Logging.Application;
using Logging.Application.Dtos;
using Mailing.Domain;
using Mailing.Dtos;
using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MimeKit;

namespace Mailing.Controllers
{
    [Route("api/[controller]")]
    public class EmailsController : InfrastructureController
    {
        public EmailsController(IClientService clientService, ILogService logService, ICorrelationService correlationService, IConfiguration config) : base(clientService, logService)
        {
            _logService.Configure(new LogSettings
            {
                Application = "Infrastructure",
                Project = "Mailing",
                Environment = config.GetValue<string>("Environment"),
                CorrelationId = correlationService.Create(null, false).Id
            });
        }

        [HttpPost]
        public IActionResult Post([FromBody] EmailDto emailDto)
        {
            return Execute(emailDto.Account, () =>
            {
                Email email;

                if (emailDto.UseCustomSmtpServer)
                {
                    _logService.LogInfoMessage($"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}  | Using Custom SmtpServer | emailDto.To={emailDto.To}");

                    if (emailDto.SmtpServerConfiguration == null || emailDto.SmtpServerConfiguration.Sender == null || emailDto.SmtpServerConfiguration.Host == null)
                    {
                        const string msg = "Missing required data on SmtpServerConfiguration when UseCustomSmtpServer is enable";
                        _logService.LogErrorMessage($"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}  | Invalid request data| msg={msg} - emailDto.To={emailDto.To}");
                        throw new ArgumentNullException(msg);
                    }

                    email = new Email(
                        emailDto.SmtpServerConfiguration.Sender.Name,
                        emailDto.SmtpServerConfiguration.Sender.Email,
                        emailDto.SmtpServerConfiguration.Sender.Password,
                        emailDto.SmtpServerConfiguration.Host.Name,
                        emailDto.SmtpServerConfiguration.Host.Port,
                        emailDto.SmtpServerConfiguration.Host.UseSsl,
                        emailDto.To,
                        emailDto.Subject,
                        emailDto.Body
                    );
                }
                else
                {
                    _logService.LogInfoMessage($"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}  | Using UciRod SmtpServer | emailDto.To={emailDto.To}");
                    email = new Email(emailDto.To, emailDto.Subject, emailDto.Body);
                }

                var mimeMessage = new MimeMessage();

                mimeMessage.From.Add(new MailboxAddress(email.SmtpConfiguration.Sender.Name, email.SmtpConfiguration.Sender.Email));

                mimeMessage.To.Add(new MailboxAddress(email.To));

                mimeMessage.Subject = email.Subject;

                mimeMessage.Body = new TextPart("html")
                {
                    Text = email.Body
                };

                using (var client = new SmtpClient())
                {
                    // For demo-purposes, accept all SSL certificates (in case the server supports STARTTLS)
                    client.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;
                    client.Timeout = 10000;

                    _logService.LogInfoMessage(
                        $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}  | Connecting with host | email.To={email.To} - host.name={email.SmtpConfiguration.Host.Name}"
                    );
                    // The third parameter is useSSL ('true' if the client should make an SSL-wrapped connection to the server)
                    client.Connect(email.SmtpConfiguration.Host.Name, email.SmtpConfiguration.Host.Port, email.SmtpConfiguration.Host.UseSsl);

                    _logService.LogInfoMessage(
                        $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}  | " +
                        $"Authenticating into host | " +
                        $"email.To={email.To} - host.name={email.SmtpConfiguration.Host.Name} - sender.email={email.SmtpConfiguration.Sender.Email}"
                    );
                    // Note: only needed if the SMTP server requires authentication
                    client.Authenticate(email.SmtpConfiguration.Sender.Email, email.SmtpConfiguration.Sender.Password);

                    _logService.LogInfoMessage(
                        $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}  | " +
                        $"Sending email | " +
                        $"email.To={email.To} - STATUS=PENDING"
                    );

                    client.Send(mimeMessage);

                    _logService.LogInfoMessage(
                        $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}  | " +
                        $"Email sent | " +
                        $"email.To={email.To} - STATUS=OK"
                    );

                    client.Disconnect(true);

                    return Ok();
                }
            });
        }
    }
}
