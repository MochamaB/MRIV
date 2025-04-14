using System.Net;
using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using MRIV.Models;

namespace MRIV.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string to, string subject, string htmlMessage);
        Task SendTemplatedEmailAsync(string to, string templateName, Dictionary<string, string> parameters);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly RequisitionContext _context;
        
        public EmailService(IConfiguration configuration, RequisitionContext context)
        {
            _configuration = configuration;
            _context = context;
        }
        
        public async Task SendEmailAsync(string to, string subject, string htmlMessage)
        {
            try
            {
                // Configure SMTP client
                using var client = new SmtpClient(_configuration["Email:SmtpServer"])
                {
                    Port = int.Parse(_configuration["Email:Port"]),
                    Credentials = new NetworkCredential(_configuration["Email:Username"], _configuration["Email:Password"]),
                    EnableSsl = bool.Parse(_configuration["Email:EnableSsl"])
                };
                
                // Create the message
                var message = new MailMessage
                {
                    From = new MailAddress(_configuration["Email:From"], "KTDA Material Requisition System"),
                    Subject = subject,
                    Body = htmlMessage,
                    IsBodyHtml = true
                };
                
                message.To.Add(to);
                
                // Send the email
                await client.SendMailAsync(message);
                
                // Log success
                Console.WriteLine($"Email sent successfully to {to}");
            }
            catch (Exception ex)
            {
                // Log error but don't throw to prevent disrupting the application flow
                Console.WriteLine($"Failed to send email: {ex.Message}");
            }
        }
        
        public async Task SendTemplatedEmailAsync(string to, string templateName, Dictionary<string, string> parameters)
        {
            try
            {
                // Get the template
                var template = await _context.NotificationTemplates
                    .FirstOrDefaultAsync(t => t.Name == templateName);
                    
                if (template == null)
                {
                    // If template doesn't exist, create a generic email with branding
                    string subject = "KTDA Material Requisition System Notification";
                    
                    if (parameters.TryGetValue("Title", out var title))
                    {
                        subject = title;
                    }
                    
                    string message = "You have a new notification from the KTDA Material Requisition System.";
                    
                    if (parameters.TryGetValue("Message", out var messageContent))
                    {
                        message = messageContent;
                    }

                    var fallbackHtmlMessage = CreateBrandedEmailHtml(subject, message, parameters);
                    await SendEmailAsync(to, subject, fallbackHtmlMessage);
                    return;
                }
                    
                // Process template
                string processedSubject = ProcessTemplate(template.TitleTemplate, parameters);
                string processedMessage = ProcessTemplate(template.MessageTemplate, parameters);
                
                // Create HTML email with template content
                string htmlMessage = CreateBrandedEmailHtml(processedSubject, processedMessage, parameters);
                
                // Send the email
                await SendEmailAsync(to, processedSubject, htmlMessage);
            }
            catch (Exception ex)
            {
                // Log error but don't throw
                Console.WriteLine($"Failed to send templated email: {ex.Message}");
            }
        }
        
        private string CreateBrandedEmailHtml(string subject, string message, Dictionary<string, string> parameters)
        {
            // Get URL if available
            string url = "#";
            if (parameters.TryGetValue("URL", out var urlValue))
            {
                url = urlValue;
            }
            
            return $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                <div style='background-color: #324721; padding: 20px; text-align: center;'>
                    <img src='{_configuration["AppUrl"]}/assets/images/ktda-logo2.png' alt='KTDA Logo' height='50'>
                    <h2 style='color: white; margin-top: 10px;'>KTDA Material Requisition System</h2>
                </div>
                <div style='padding: 20px; border: 1px solid #ddd; border-top: none;'>
                    <h3>{subject}</h3>
                    <p style='font-size: 16px; line-height: 1.6;'>{message.Replace("\n", "<br>")}</p>
                    <div style='margin-top: 30px; text-align: center;'>
                        <a href='{url}' style='background-color: #324721; color: white; padding: 10px 20px; text-decoration: none; border-radius: 4px;'>View Details</a>
                    </div>
                </div>
                <div style='background-color: #f5f5f5; padding: 15px; text-align: center; font-size: 12px; color: #666;'>
                    <p>This is an automated message from the KTDA Material Requisition System.</p>
                    <p>&copy; {DateTime.Now.Year} KTDA. All rights reserved.</p>
                </div>
            </div>";
        }
        
        private string ProcessTemplate(string template, Dictionary<string, string> parameters)
        {
            // Replace placeholders with actual values
            foreach (var param in parameters)
            {
                template = template.Replace($"{{{param.Key}}}", param.Value);
            }
            return template;
        }
    }
}
