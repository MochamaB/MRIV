/**
 * Menu Manager JavaScript
 */
$(document).ready(function() {
    // Initialize Nestable
    if ($('#menu-items-list').length) {
        // Initialize nestable menu
        const nestableOptions = {
            maxDepth: 3,
            group: 1,
            expandBtnHTML: '<button data-action="expand" type="button"><i class="fas fa-plus"></i></button>',
            collapseBtnHTML: '<button data-action="collapse" type="button"><i class="fas fa-minus"></i></button>'
        };
        
        $('.dd').nestable(nestableOptions);
        
        // Save menu order button
        $('#save-menu-order').on('click', function(e) {
            e.preventDefault();
            
            const menuId = $(this).data('menu-id');
            const orderData = $('.dd').nestable('serialize');
            const csrfToken = $('meta[name="csrf-token"]').attr('content');
            
            // Show loading state
            $(this).addClass('disabled');
            $(this).html('<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Saving...');
            
            // Send AJAX request to update menu order
            $.ajax({
                url: `/admin/menus/${menuId}/items/order`,
                type: 'POST',
                data: JSON.stringify({
                    items: orderData
                }),
                contentType: 'application/json',
                headers: {
                    'X-CSRF-TOKEN': csrfToken
                },
                success: function(data) {
                    // Reset button state
                    $('#save-menu-order').removeClass('disabled');
                    $('#save-menu-order').html('<i class="fas fa-save align-bottom me-1"></i> Save Order');
                    
                    // Show success message
                    if (data.success) {
                        Toastify({
                            text: "Menu order updated successfully!",
                            duration: 3000,
                            close: true,
                            gravity: "top",
                            position: "right",
                            className: "bg-success",
                        }).showToast();
                    } else {
                        throw new Error(data.message || 'Failed to update menu order');
                    }
                },
                error: function(error) {
                    // Reset button state
                    $('#save-menu-order').removeClass('disabled');
                    $('#save-menu-order').html('<i class="fas fa-save align-bottom me-1"></i> Save Order');
                    
                    // Show error message
                    Toastify({
                        text: error.message || "An error occurred while updating menu order",
                        duration: 3000,
                        close: true,
                        gravity: "top",
                        position: "right",
                        className: "bg-danger",
                    }).showToast();
                    
                    console.error('Error:', error);
                }
            });
        });
        
        // Expand/Collapse All buttons
        $('#expand-all').on('click', function(e) {
            e.preventDefault();
            $('.dd').nestable('expandAll');
        });
        
        $('#collapse-all').on('click', function(e) {
            e.preventDefault();
            $('.dd').nestable('collapseAll');
        });
    }
    
    // Menu Item Form
    if ($('#menu-item-form').length) {
        const pageSelect = $('#page_id');
        const urlField = $('#url');
        
        // Toggle fields based on page selection
        if (pageSelect.length && urlField.length) {
            pageSelect.on('change', function() {
                if ($(this).val()) {
                    urlField.prop('disabled', true);
                    urlField.parent().addClass('opacity-50');
                } else {
                    urlField.prop('disabled', false);
                    urlField.parent().removeClass('opacity-50');
                }
            });
            
            // Initial state
            if (pageSelect.val()) {
                urlField.prop('disabled', true);
                urlField.parent().addClass('opacity-50');
            }
        }
    }
    
    // Delete menu item confirmation
    $('.delete-menu-item').on('click', function(e) {
        e.preventDefault();
        
        const menuItemId = $(this).data('id');
        const menuItemTitle = $(this).data('title');
        
        // Show confirmation dialog
        if (confirm(`Are you sure you want to delete "${menuItemTitle}"? This will also delete all child items.`)) {
            // Submit the delete form
            $(`#delete-menu-item-${menuItemId}`).submit();
        }
    });
});
