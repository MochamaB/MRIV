using Microsoft.AspNetCore.Mvc;
using MRIV.Attributes;
using MRIV.Models;
using MRIV.Services;

namespace MRIV.Controllers
{
    [CustomAuthorize]
    public class NotificationsController : Controller
    {
        private readonly INotificationService _notificationService;

        public NotificationsController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        // Get all notifications for the current user
        public async Task<IActionResult> Index()
        {
            var payrollNo = HttpContext.Session.GetString("EmployeePayrollNo");
            if (string.IsNullOrEmpty(payrollNo))
                return RedirectToAction("Index", "Login");

            var notifications = await _notificationService.GetUserNotificationsAsync(payrollNo);

            // Set ViewBag for notification count
            await SetNotificationCount();

            return View(notifications);
        }

        // AJAX endpoint to get unread notifications count
        [HttpGet]
        public async Task<IActionResult> GetUnreadCount()
        {
            var payrollNo = HttpContext.Session.GetString("EmployeePayrollNo");
            if (string.IsNullOrEmpty(payrollNo))
                return Json(new { count = 0 });

            var count = await _notificationService.GetUnreadCountAsync(payrollNo);
            return Json(new { count });
        }

        // AJAX endpoint to mark a notification as read
        [HttpPost]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            await _notificationService.MarkAsReadAsync(id);
            return Json(new { success = true });
        }

        // AJAX endpoint to get unread notifications for dropdown
        [HttpGet]
        public async Task<IActionResult> GetUnreadNotifications()
        {
            var payrollNo = HttpContext.Session.GetString("EmployeePayrollNo");
            if (string.IsNullOrEmpty(payrollNo))
                return Json(new List<object>());

            var notifications = await _notificationService.GetUserNotificationsAsync(payrollNo, true);
            var notificationData = notifications.Select(n => new
            {
                id = n.Id,
                title = n.Title,
                message = n.Message,
                url = n.URL ?? "#",
                createdAt = n.CreatedAt,
                notificationType = n.NotificationType,
                isRead = n.IsRead
            });

            return Json(notificationData);
        }

        // Helper method to set notification count in ViewBag
        private async Task SetNotificationCount()
        {
            var payrollNo = HttpContext.Session.GetString("EmployeePayrollNo");
            if (!string.IsNullOrEmpty(payrollNo))
            {
                ViewBag.UnreadNotifications = await _notificationService.GetUnreadCountAsync(payrollNo);
            }
            else
            {
                ViewBag.UnreadNotifications = 0;
            }
        }
    }
}