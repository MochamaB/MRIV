// Notification handling
$(document).ready(function () {
    // Load notifications when dropdown is opened
    $('#page-header-notifications-dropdown').on('show.bs.dropdown', function () {
        loadNotifications();
    });

    // Function to load notifications via AJAX
    function loadNotifications() {
        $.get('/Notifications/GetUnreadNotifications', function (data) {
            var notificationsHtml = '';
            
            if (data && data.length > 0) {
                data.forEach(function (notification) {
                    var timeAgo = moment(notification.createdAt).fromNow();
                    var icon = getNotificationIcon(notification.notificationType);
                    var bgColor = getNotificationBgColor(notification.notificationType);
                    
                    notificationsHtml += `
                        <div class="text-reset notification-item d-block dropdown-item position-relative">
                            <div class="d-flex">
                                <div class="avatar-xs me-3 flex-shrink-0">
                                    <span class="avatar-title ${bgColor} rounded-circle fs-16">
                                        <i class="${icon}"></i>
                                    </span>
                                </div>
                                <div class="flex-grow-1">
                                    <a href="${notification.url}" class="stretched-link" onclick="markAsRead(${notification.id}, event)">
                                        <h6 class="mt-0 mb-2 lh-base">
                                            ${notification.title}
                                        </h6>
                                    </a>
                                    <p class="mb-0 fs-11 fw-medium text-uppercase text-muted">
                                        <span><i class="mdi mdi-clock-outline"></i> ${timeAgo}</span>
                                    </p>
                                </div>
                            </div>
                        </div>
                    `;
                });
            } else {
                notificationsHtml = `
                    <div class="w-25 w-sm-50 pt-3 mx-auto">
                        <img src="/assets/images/svg/bell.svg" class="img-fluid" alt="no-notification">
                    </div>
                    <div class="text-center pb-5 mt-2">
                        <h6 class="fs-18 fw-semibold lh-base">No notifications yet</h6>
                    </div>
                `;
            }

            $('#all-noti-tab .pe-2').html(notificationsHtml);
        });
    }

    // Function to mark notification as read
    window.markAsRead = function(notificationId, event) {
        event.preventDefault();
        $.post('/Notifications/MarkAsRead/' + notificationId, function() {
            // Update unread count
            updateUnreadCount();
            // Follow the notification URL
            window.location.href = event.target.closest('a').href;
        });
    };

    // Function to update unread count
    function updateUnreadCount() {
        $.get('/Notifications/GetUnreadCount', function(data) {
            var badge = $('.topbar-badge');
            if (data.count > 0) {
                badge.text(data.count).removeClass('d-none');
            } else {
                badge.addClass('d-none');
            }
        });
    }

    // Helper function to get notification icon based on type
    function getNotificationIcon(type) {
        switch (type.toLowerCase()) {
            case 'approval':
                return 'bx bx-check-circle';
            case 'dispatch':
                return 'bx bx-package';
            case 'requisition':
                return 'bx bx-file';
            default:
                return 'bx bx-bell';
        }
    }

    // Helper function to get notification background color based on type
    function getNotificationBgColor(type) {
        switch (type.toLowerCase()) {
            case 'approval':
                return 'bg-success-subtle text-success';
            case 'dispatch':
                return 'bg-info-subtle text-info';
            case 'requisition':
                return 'bg-primary-subtle text-primary';
            default:
                return 'bg-secondary-subtle text-secondary';
        }
    }

    // Update unread count periodically (every 30 seconds)
    setInterval(updateUnreadCount, 30000);
});
