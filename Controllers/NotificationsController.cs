using Microsoft.AspNetCore.Mvc;
using MRIV.Models;
using MRIV.Services;

namespace MRIV.Controllers
{
    // Controllers/NotificationsController.cs
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
                return PartialView("_NotificationDropdown", new List<Notification>());

            var notifications = await _notificationService.GetUserNotificationsAsync(payrollNo, true);
            return PartialView("_NotificationDropdown", notifications);
        }
    }
}
