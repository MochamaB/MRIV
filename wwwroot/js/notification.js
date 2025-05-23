﻿
  
        $(document).ready(function() {
            // Function to load notifications
            function loadNotifications() {
                $.ajax({
                    url: '/Notifications/GetUnreadNotifications',
                    type: 'GET',
                    success: function (data) {
                        $('#notificationDropdownMenu').html(data);
                    }
                });
            }
        
        // Load notifications when dropdown is opened
        $('#notificationDropdown').on('show.bs.dropdown', function() {
            loadNotifications();
        });

        // Update notification count periodically (every 60 seconds)
        function updateNotificationCount() {
            $.ajax({
                url: '@Url.Action("GetUnreadCount", "Notifications")',
                type: 'GET',
                success: function (response) {
                    const $count = $('.notification-count');
                    $count.text(response.count);

                    if (response.count > 0) {
                        $count.removeClass('d-none');
                    } else {
                        $count.addClass('d-none');
                    }
                }
            });
        }

        // Initial load
        updateNotificationCount();

        // Set up interval for periodic updates
        setInterval(updateNotificationCount, 60000); // Every minute

        // Mark as read when clicking a notification
        $(document).on('click', '.notification-item', function() {
            const id = $(this).data('id');

        // No need to stop event propagation, we want the link to work

        // Mark as read in background
        $.ajax({
            url: '@Url.Action("MarkAsRead", "Notifications")',
        type: 'POST',
        data: {id: id }
            });
        });
    });
   