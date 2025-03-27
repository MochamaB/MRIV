/**
 * NPPK Admin Panel Plugins Initialization
 * This file initializes various plugins used in the admin panel
 */

document.addEventListener('DOMContentLoaded', function() {
    // Initialize Flatpickr date pickers
    const datePickers = document.querySelectorAll('.flatpickr-input');
    if (datePickers.length > 0) {
        datePickers.forEach(function(element) {
            const options = element.dataset.flatpickrOptions ? JSON.parse(element.dataset.flatpickrOptions) : {};
            flatpickr(element, options);
        });
    }

    // Initialize Choices.js selects
    const choicesSelects = document.querySelectorAll('.form-select');
    if (choicesSelects.length > 0) {
        choicesSelects.forEach(function(element) {
            const options = element.dataset.choicesOptions ? JSON.parse(element.dataset.choicesOptions) : {};
            new Choices(element, {
                placeholder: true,
                placeholderValue: 'Select an option',
                searchPlaceholderValue: 'Search...',
                removeItemButton: true,
                ...options
            });
        });
    }

    // Initialize Toastify for notifications
    window.showToast = function(message, type = 'success') {
        const bgColor = type === 'success' ? '#0ab39c' : 
                        type === 'error' ? '#f06548' : 
                        type === 'warning' ? '#f7b84b' : '#405189';
        
        Toastify({
            text: message,
            duration: 3000,
            close: true,
            gravity: "top",
            position: "right",
            backgroundColor: bgColor,
            stopOnFocus: true
        }).showToast();
    };

    // Show toast messages from session if they exist
    if (typeof flashMessages !== 'undefined' && flashMessages) {
        for (const type in flashMessages) {
            if (flashMessages[type]) {
                window.showToast(flashMessages[type], type);
            }
        }
    }
});