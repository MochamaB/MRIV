// Custom validator for Inter-factory Borrowing
$(document).ready(function() {
    // Add custom validator for Inter-factory Borrowing
    $.validator.addMethod('interfactoryborrowing', function (value, element, params) {
        // Check if this is an Inter-factory Borrowing requisition
        const ticketDisplay = document.querySelector('select[readonly] option');
        const isInterFactoryBorrowing = ticketDisplay && ticketDisplay.textContent.trim() === "Inter Factory Borrowing";
        
        if (!isInterFactoryBorrowing) {
            return true; // Skip validation if not Inter-factory Borrowing
        }
        
        // Get the station category values
        const issueStationCategory = document.getElementById('issueStationCategory').value;
        const deliveryStationCategory = document.getElementById('deliveryStationCategory').value;
        
        // Both must be 'factory' for Inter-factory Borrowing
        return issueStationCategory === 'factory' && deliveryStationCategory === 'factory';
    }, '');
    
    $.validator.unobtrusive.adapters.add('interfactoryborrowing', {}, function (options) {
        options.rules['interfactoryborrowing'] = true;
        options.messages['interfactoryborrowing'] = options.message;
    });

    // Check if this is an Inter-factory Borrowing requisition
    const ticketDisplay = document.querySelector('select[readonly] option');
    const isInterFactoryBorrowing = ticketDisplay && ticketDisplay.textContent.trim() === "Inter Factory Borrowing";
    
    // Get the station category dropdowns
    const issueStationCategory = document.getElementById('issueStationCategory');
    const deliveryStationCategory = document.getElementById('deliveryStationCategory');
    
    // Function to validate Inter-factory Borrowing requirements
    function validateInterFactoryBorrowing() {
        if (isInterFactoryBorrowing) {
            // For Inter-factory Borrowing, both categories should be 'factory'
            if (issueStationCategory.value !== 'factory' || deliveryStationCategory.value !== 'factory') {
                // Set both dropdowns to 'factory' if they're not already
                if (issueStationCategory.value !== 'factory') {
                    issueStationCategory.value = 'factory';
                    // Trigger change event to load the appropriate stations
                    issueStationCategory.dispatchEvent(new Event('change'));
                }
                
                if (deliveryStationCategory.value !== 'factory') {
                    deliveryStationCategory.value = 'factory';
                    // Trigger change event to load the appropriate stations
                    deliveryStationCategory.dispatchEvent(new Event('change'));
                }
            } else {
                // No need to do anything here since we're not adding/removing any message
            }
        }
    }
    
    // Add event listeners to validate when the dropdowns change
    if (isInterFactoryBorrowing) {
        // Set both dropdowns to 'factory' on page load
        setTimeout(function() {
            if (issueStationCategory.value !== 'factory') {
                issueStationCategory.value = 'factory';
                issueStationCategory.dispatchEvent(new Event('change'));
            }
            
            if (deliveryStationCategory.value !== 'factory') {
                deliveryStationCategory.value = 'factory';
                deliveryStationCategory.dispatchEvent(new Event('change'));
            }
        }, 500); // Small delay to ensure dropdowns are fully loaded
        
        issueStationCategory.addEventListener('change', validateInterFactoryBorrowing);
        deliveryStationCategory.addEventListener('change', validateInterFactoryBorrowing);
        
        // Add form submission validation
        document.getElementById('wizardForm').addEventListener('submit', function(e) {
            if (isInterFactoryBorrowing && (issueStationCategory.value !== 'factory' || deliveryStationCategory.value !== 'factory')) {
                e.preventDefault();
                // No need for alert since we have the message at the top
                validateInterFactoryBorrowing();
                // Scroll to the top to make sure the user sees the validation message
                window.scrollTo(0, 0);
                return false;
            }
            return true;
        });
        
        // Initial validation
        validateInterFactoryBorrowing();
    }
});
