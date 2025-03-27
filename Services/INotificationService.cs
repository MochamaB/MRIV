using Microsoft.EntityFrameworkCore;
using MRIV.Models;

namespace MRIV.Services
{
    public interface INotificationService
    {
        Task CreateNotificationAsync(string templateName, Dictionary<string, string> parameters, string recipientId);

        Task CreateNotificationForRoleAsync(string templateName, Dictionary<string, string> parameters, string role);

        Task CreateNotificationForGroupAsync(string templateName, Dictionary<string, string> parameters, List<string> recipientIds);

        Task<List<Notification>> GetUserNotificationsAsync(string userId, bool unreadOnly = false);

        Task MarkAsReadAsync(int notificationId);

        Task<int> GetUnreadCountAsync(string userId);
    }

    // Services/NotificationService.cs
    public class NotificationService : INotificationService
    {
        private readonly RequisitionContext _context;
        private readonly KtdaleaveContext _ktdaContext;

        public NotificationService(RequisitionContext context, KtdaleaveContext ktdaContext)
        {
            _context = context;
            _ktdaContext = ktdaContext;
        }

        public async Task CreateNotificationAsync(string templateName, Dictionary<string, string> parameters, string recipientId)
        {
            var template = await _context.NotificationTemplates
                .FirstOrDefaultAsync(t => t.Name == templateName);

            if (template == null)
                throw new ArgumentException($"Notification template '{templateName}' not found");

            // Process template with parameters
            string title = ProcessTemplate(template.TitleTemplate, parameters);
            string message = ProcessTemplate(template.MessageTemplate, parameters);

            // Create notification
            var notification = new Notification
            {
                Title = title,
                Message = message,
                RecipientId = recipientId,
                CreatedAt = DateTime.Now,
                NotificationType = template.NotificationType
            };

            // Set EntityId and EntityType if available
            if (parameters.TryGetValue("EntityId", out var entityId) &&
                parameters.TryGetValue("EntityType", out var entityType))
            {
                notification.EntityId = int.Parse(entityId);
                notification.EntityType = entityType;
            }

            // Set URL if available
            if (parameters.TryGetValue("URL", out var url))
            {
                notification.URL = url;
            }

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
        }

        public async Task CreateNotificationForRoleAsync(string templateName, Dictionary<string, string> parameters, string role)
        {
            // Find users with the specified role
            var users = await _ktdaContext.EmployeeBkps
                .Where(e => e.Role == role && e.EmpisCurrActive == 0)
                .ToListAsync();

            foreach (var user in users)
            {
                await CreateNotificationAsync(templateName, parameters, user.PayrollNo);
            }
        }

        public async Task CreateNotificationForGroupAsync(string templateName, Dictionary<string, string> parameters, List<string> recipientIds)
        {
            foreach (var recipientId in recipientIds)
            {
                await CreateNotificationAsync(templateName, parameters, recipientId);
            }
        }

        public async Task<List<Notification>> GetUserNotificationsAsync(string userId, bool unreadOnly = false)
        {
            var query = _context.Notifications
                .Where(n => n.RecipientId == userId)
                .OrderByDescending(n => n.CreatedAt);

            if (unreadOnly)
                query = (IOrderedQueryable<Notification>)query.Where(n => n.ReadAt == null);

            return await query.ToListAsync();
        }

        public async Task MarkAsReadAsync(int notificationId)
        {
            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification != null && notification.ReadAt == null)
            {
                notification.ReadAt = DateTime.Now;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<int> GetUnreadCountAsync(string userId)
        {
            return await _context.Notifications
                .CountAsync(n => n.RecipientId == userId && n.ReadAt == null);
        }

        private string ProcessTemplate(string template, Dictionary<string, string> parameters)
        {
            string result = template;
            foreach (var param in parameters)
            {
                result = result.Replace($"{{{param.Key}}}", param.Value);
            }
            return result;
        }
    }
}
