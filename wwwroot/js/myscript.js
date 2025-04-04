$(document).ready(function () {
    // Initial setup
    updateRowIndexes();
    setupModalEvents();
    setupMaterialSearch();

    // Add new item button click handler
    $('#addNewItemBtn').on('click', function () {
        if (!validateCurrentItems()) {
            return; // Stop if validation fails
        }

        // Close all accordions
        closeAllAccordions();

        // Clone the first row (template)
        const $firstRow = $('.item-row').first();
        const $newRow = $firstRow.clone(true);
        const newIndex = $('.item-row').length;

        // Update all indices and IDs in the cloned row
        updateRowContent($newRow, newIndex);

        // Reset form values but keep default dropdowns
        resetFormValues($newRow);

        // Add the remove button (only for non-first items)
        addRemoveButton($newRow, newIndex);

        // Hide the badge container initially
        $newRow.find(`#badgeContainer_${newIndex}`).addClass('d-none');

        // Update accordion state - make sure it's expanded
        $newRow.find('.accordion-button').removeClass('collapsed');
        $newRow.find('.accordion-collapse').addClass('show');

        // Append to container
        $('#itemsAccordion').append($newRow);

        // Setup events for the new row
        setupModalEvents();
        updateRowIndexes();
    });

    // Function to close all accordions
    function closeAllAccordions() {
        $('.accordion-button').addClass('collapsed').attr('aria-expanded', 'false');
        $('.accordion-collapse').removeClass('show');
    }

    // Function to open a specific accordion
    function openAccordion(index) {
        $(`#heading_${index}`).find('.accordion-button').removeClass('collapsed').attr('aria-expanded', 'true');
        $(`#collapse_${index}`).addClass('show');
    }

    // Function to update all row indexes after adding/removing rows
    function updateRowIndexes() {
        $('.item-row').each(function (index) {
            const $row = $(this);
            updateRowContent($row, index);

            // Update accordion header title
            $row.find('.accordion-header .accordion-button').contents().filter(function() {
                return this.nodeType === 3; // Text nodes only
            }).first().replaceWith(`Item ${index + 1}`);

            // Only show remove button for non-first rows
            if (index === 0) {
                $row.find('.remove-item').remove();
            } else {
                addRemoveButton($row, index);
            }
        });
    }

    // Function to update a row's content with new index
    function updateRowContent($row, newIndex) {
        // Update data-index attribute on the row
        $row.attr('data-index', newIndex).data('index', newIndex);

        // Update accordion IDs and attributes
        $row.find('.accordion-header').attr('id', `heading_${newIndex}`);
        
        const $accordionButton = $row.find('.accordion-button');
        $accordionButton.attr('data-bs-target', `#collapse_${newIndex}`);
        $accordionButton.attr('aria-controls', `collapse_${newIndex}`);
        
        const $accordionCollapse = $row.find('.accordion-collapse');
        $accordionCollapse.attr('id', `collapse_${newIndex}`);
        $accordionCollapse.attr('aria-labelledby', `heading_${newIndex}`);

        // Update form element names and IDs
        $row.find('input, select, textarea').each(function () {
            const $input = $(this);

            // Update name attribute (for form submission)
            const name = $input.attr('name');
            if (name && name.includes('RequisitionItems[')) {
                const newName = name.replace(/RequisitionItems\[\d+\]/g, `RequisitionItems[${newIndex}]`);
                $input.attr('name', newName);
            }

            // Update id attribute
            const id = $input.attr('id');
            if (id && id.includes('_')) {
                const baseName = id.split('_')[0];
                $input.attr('id', `${baseName}_${newIndex}`);
            }

            // Update data-index attribute
            $input.attr('data-index', newIndex);
        });

        // Update button data-index attributes
        $row.find('button').each(function () {
            $(this).attr('data-index', newIndex);
        });

        // Update modal ID and related attributes
        const $modal = $row.find('.modal');
        $modal.attr('id', `inventoryModal_${newIndex}`);

        // Update modal trigger button data-bs-target
        $row.find('[data-bs-target]').each(function() {
            const target = $(this).attr('data-bs-target');
            if (target && target.includes('inventoryModal_')) {
                $(this).attr('data-bs-target', `#inventoryModal_${newIndex}`);
            }
        });

        // Update badge container and badges with consistent ID format
        $row.find('[id^=badgeContainer_]').attr('id', `badgeContainer_${newIndex}`);
        $row.find('[id^=selectedMaterialCategory_]').attr('id', `selectedMaterialCategory_${newIndex}`);
        $row.find('[id^=selectedMaterialCode_]').attr('id', `selectedMaterialCode_${newIndex}`);
        $row.find('[id^=selectedMaterialVendor_]').attr('id', `selectedMaterialVendor_${newIndex}`);
    }

    // Reset form values but keep default selections
    function resetFormValues($row) {
        // Clear text inputs
        $row.find('input[type="text"]').val('');

        // Reset number input to default value
        $row.find('input[type="number"]').val(1);

        // Reset textarea
        $row.find('textarea').val('');

        // Don't reset dropdowns (keep default Status and Condition)

        // Disable SaveToInventory checkbox and ensure it's visible
        $row.find('input[type="checkbox"]').prop('disabled', true).prop('checked', false);
        $row.find('.form-check-label').css('display', 'block');

        // Hide badge container
        $row.find('[id^=badgeContainer_]').addClass('d-none');

        // Reset badges
        $row.find('[id^=selectedMaterialCategory_]').text('Category: None');    
        $row.find('[id^=selectedMaterialCode_]').text('SNo.: None');
        $row.find('[id^=selectedMaterialVendor_]').text('Vendor: None');        

        // Reset modal inputs
        $row.find('.materialCode').val('');
        $row.find('.materialCategoryId').val(''); // Empty by default
        $row.find('.materialVendor').val('');
    }

    // Add remove button to row
    function addRemoveButton($row, index) {
        // Remove any existing button first
        $row.find('.remove-item').remove();

        // Only add remove button if index > 0
        if (index > 0) {
            // Create new button
            const $removeBtn = $('<button>', {
                type: 'button',
                class: 'btn-close remove-item',
                'data-index': index,
                title: 'Remove item',
                css: {
                    position: 'absolute',
                    right: '10px',
                    top: '10px',
                    zIndex: 10,
                    filter: 'invert(17%) sepia(100%) saturate(7480%) hue-rotate(357deg) brightness(92%) contrast(118%)'
                }
            });

            // Add click handler
            $removeBtn.on('click', function (e) {
                e.stopPropagation(); // Prevent accordion toggle
                removeRow(index);
            });

            // Append to accordion header button
            $row.find('.accordion-header .accordion-button').append($removeBtn);
        }
    }

    // Remove a row
    function removeRow(index) {
        if (confirm('Are you sure you want to remove this item?')) {
            $(`.item-row[data-index="${index}"]`).remove();
            updateRowIndexes();
            
            // If there's at least one item left, open the last one
            const $items = $('.item-row');
            if ($items.length > 0) {
                const lastIndex = $items.length - 1;
                openAccordion(lastIndex);
            }
        }
    }

    // Setup modal events (save button, generate code)
    function setupModalEvents() {
        // Use event delegation for modal buttons
        $('#itemsAccordion').off('click', '.saveInventoryDetailsBtn').on('click', '.saveInventoryDetailsBtn', function () {
            // Get the button and its container
            const $button = $(this);
            const $modal = $button.closest('.modal');
            const $row = $button.closest('.item-row');

            // Get index from the row data attribute
            const index = $row.data('index');

            // Get form elements within this specific modal
            const $categorySelect = $modal.find('.materialCategoryId');
            const $codeInput = $modal.find('.materialCode');
            const $vendorSelect = $modal.find('.materialVendor');

            // Clear any previous validation messages
            $modal.find('.text-danger').text('');

            // Validate required fields
            let isValid = true;

            if (!$categorySelect.val()) {
                $categorySelect.closest('.form-group').find('.text-danger')     
                    .text('Category is required');
                isValid = false;
            }

            if (!$codeInput.val()) {
                $codeInput.closest('.form-group').find('.text-danger')
                    .text('Code is required');
                isValid = false;
            }

            if (!isValid) {
                return;
            }

            // Update badge container visibility
            const $badgeContainer = $row.find('[id^="badgeContainer_"]');       
            $badgeContainer.removeClass('d-none');

            // Update badge content
            $row.find('[id^="selectedMaterialCategory_"]').text(`Category: ${$categorySelect.find('option:selected').text()}`);
            $row.find('[id^="selectedMaterialCode_"]').text(`SNo.: ${$codeInput.val()}`);
            $row.find('[id^="selectedMaterialVendor_"]').text(`Vendor: ${$vendorSelect.find('option:selected').text() || 'None'}`);

            // Enable SaveToInventory checkbox
            let $checkbox = $row.find('input[type="checkbox"]');

            // Enable the checkbox if found
            if ($checkbox.length > 0) {
                $checkbox.prop('disabled', false).prop('checked', true);        
                $checkbox.closest('.form-check-label').css('display', 'block'); 
            }

            // Close modal using Bootstrap's API
            try {
                const bsModal = bootstrap.Modal.getInstance($modal[0]);
                if (bsModal) {
                    bsModal.hide();
                } else {
                    // Fallback if modal instance isn't found
                    new bootstrap.Modal($modal[0]).hide();
                }
            } catch (error) {
                console.warn('Error closing modal with Bootstrap API:', error); 

                // jQuery fallback
                $modal.modal('hide');

                // Manual DOM cleanup
                $modal.removeClass('show').css('display', 'none');
                $('.modal-backdrop').remove();
                $('body').removeClass('modal-open').css({
                    'overflow': '',
                    'padding-right': ''
                });
            }
            // Clean up modal backdrop and body classes
            setTimeout(() => {
                $('.modal-backdrop').remove();
                $('body')
                    .removeClass('modal-open')
                    .css({
                        'overflow': '',
                        'padding-right': ''
                    });
            }, 100);
        });

        // Use event delegation for generate code button
        $('#itemsAccordion').off('click', '.generateCodeBtn').on('click', '.generateCodeBtn', function () {
            // Get the button and its container
            const $button = $(this);
            const $modal = $button.closest('.modal');
            const $row = $button.closest('.item-row');

            // Get index from the row data attribute
            const index = parseInt($row.data('index'));

            // Get form elements
            const $categorySelect = $modal.find('.materialCategoryId');
            const $codeInput = $modal.find('.materialCode');

            // Clear any previous validation messages
            $modal.find('.text-danger').text('');

            // Validate category selection
            if (!$categorySelect.val()) {
                // Display error in the validation span
                $categorySelect.closest('.form-group').find('.text-danger')     
                    .text('Please select a category first');
                return;
            }

            // Get category ID from the select
            const categoryId = $categorySelect.val();

            // Anti-forgery token
            const token = $('input[name="__RequestVerificationToken"]').val();  

            // Call server with correct data
            $.ajax({
                url: '/MaterialRequisition/GenerateCode',
                type: 'POST',
                contentType: 'application/json',
                data: JSON.stringify({
                    categoryId: parseInt(categoryId),
                    itemIndex: parseInt(index)
                }),
                headers: {
                    'RequestVerificationToken': token
                },
                success: function (response) {
                    if (response && response.code) {
                        $codeInput.val(response.code);
                    }
                },
                error: function (error) {
                    $codeInput.closest('.form-group').find('.text-danger')      
                        .text('Error generating code. Please try again.');      
                }
            });
        });
    }

    // Material search functionality
    function setupMaterialSearch() {
        // Elements
        const $searchInput = $('#materialSearch');
        const $clearButton = $('#clearMaterialSearch');
        const $resultsContainer = $('#searchResultsContainer');
        const $searchForm = $('#materialSearchForm');
        
        if (!$searchInput.length || !$resultsContainer.length) {
            console.log('Material search elements not found - skipping search initialization');
            return;
        }
        
        let timeoutId;
        
        // Debounce function to limit AJAX calls
        function debounce(func, delay) {
            return function(...args) {
                clearTimeout(timeoutId);
                timeoutId = setTimeout(() => func.apply(this, args), delay);
            };
        }
        
        // Handle search input
        const handleSearch = debounce(function(searchTerm) {
            if (searchTerm.length < 2 && searchTerm.length > 0) {
                return; // Don't search for very short terms
            }
            
            if (searchTerm.length === 0) {
                $resultsContainer.hide();
                return;
            }
            
            // Show loading indicator
            $resultsContainer.html('<div class="p-3 text-center"><div class="spinner-border text-success" role="status"><span class="visually-hidden">Loading...</span></div></div>');
            $resultsContainer.show();
            
            // Get anti-forgery token
            const token = $('input[name="__RequestVerificationToken"]').val();
            
            // Call the search API
            $.ajax({
                url: `/MaterialRequisition/SearchMaterials?searchTerm=${encodeURIComponent(searchTerm)}`,
                type: 'GET',
                headers: {
                    'X-Requested-With': 'XMLHttpRequest'
                },
                success: function(html) {
                    $resultsContainer.html(html);
                    $resultsContainer.show();
                    
                    // Attach event handlers to the search results
                    attachMaterialSelectionHandlers();
                },
                error: function() {
                    $resultsContainer.html('<div class="p-3 text-danger">Error searching materials</div>');
                }
            });
        }, 300);
        
        // Show/Hide clear button & handle search
        $searchInput.on('input', function() {
            const searchTerm = $(this).val().trim();
            
            if (searchTerm.length > 0) {
                $clearButton.removeClass('d-none');
            } else {
                $clearButton.addClass('d-none');
                $resultsContainer.hide();
            }
            
            if (searchTerm.length >= 2 || searchTerm.length === 0) {
                handleSearch(searchTerm);
            }
        });
        
        // Clear input
        $clearButton.on('click', function() {
            $searchInput.val('');
            $clearButton.addClass('d-none');
            $resultsContainer.hide();
        });
        
        // Form prevention
        $searchForm.on('submit', function(e) {
            e.preventDefault();
            handleSearch($searchInput.val().trim());
        });
        
        // Close dropdown when clicking outside
        $(document).on('click', function(e) {
            if (!$(e.target).closest('#materialSearchForm').length && 
                !$(e.target).closest('#searchResultsContainer').length) {
                $resultsContainer.hide();
            }
        });
        
        // Function to attach event handlers to material selection buttons
        function attachMaterialSelectionHandlers() {
            $('.material-result, .select-material').off('click').on('click', function(e) {
                e.preventDefault();
                
                const $this = $(this);
                const materialId = $this.data('id');
                const materialName = $this.data('name');
                const materialCode = $this.data('code');
                const categoryId = $this.data('category-id');
                const categoryName = $this.data('category-name');
                const vendorId = $this.data('vendor-id');
                const vendorName = $this.data('vendor-name') || 'None';
                
                // Find the currently open accordion
                const $openAccordion = $('.accordion-collapse.show');
                const index = $openAccordion.closest('.item-row').data('index');
                
                // Fill in the material details
                $openAccordion.find(`[name="RequisitionItems[${index}].Name"]`).val(materialName);
                // Set category in the modal
                $openAccordion.find(`.materialCategoryId[data-index="${index}"]`).val(categoryId);

                // Set code in the modal
                $openAccordion.find(`.materialCode[data-index="${index}"]`).val(materialCode);

                // Set vendor in the modal if available
                if (vendorId) {
                    $openAccordion.find(`.materialVendor[data-index="${index}"]`).val(vendorId);
                }
                
                // Update badges - correct the selectors
                const $badgeContainer = $openAccordion.closest('.item-row').find(`#badgeContainer_${index}`);
                $badgeContainer.removeClass('d-none');

                // Set the text directly with the category name
                $openAccordion.closest('.item-row').find(`#selectedMaterialCategory_${index}`).text(`Category: ${categoryName}`);
                $openAccordion.closest('.item-row').find(`#selectedMaterialCode_${index}`).text(`SNo.: ${materialCode}`);
                $openAccordion.closest('.item-row').find(`#selectedMaterialVendor_${index}`).text(`Vendor: ${vendorName}`);

                // Enable SaveToInventory checkbox
                const $checkbox = $openAccordion.closest('.item-row').find(`#saveToInventory_${index}`);
                $checkbox.prop('disabled', false).prop('checked', true);
                
                // Close the dropdown
                $resultsContainer.hide();
                $clearButton.addClass('d-none');
                $searchInput.val('');
            });
        }
    }

    // Add event handlers for the "Add Inventory Details" link
    $('.item-row').each(function() {
        const index = $(this).data('index');
        const $inventoryLink = $(this).find(`a[data-bs-target="#inventoryModal_${index}"]`);

        $inventoryLink.on('click', function() {
            // Ensure the checkbox is visible when clicking the link
            const $checkboxLabel = $(this).closest('.item-row').find('.form-check-label');
            $checkboxLabel.css('display', 'block');
        });
    });
});

/////  VALIDATION OF REQUISITION ITEMS

$(document).ready(function () {
    const form = document.querySelector('.myForm');
    if (form) {
        const elements = form.querySelectorAll('input, select, textarea');      

        // Function to show validation error
        function showValidationError(input, message) {
            // Clear existing error messages
            const existingError = input.parentElement.querySelector('.validation-error');
            if (existingError) {
                existingError.remove();
            }

            // Create and append error message
            const errorElement = document.createElement('div');
            errorElement.className = 'validation-error text-danger';
            errorElement.textContent = message;
            input.parentElement.appendChild(errorElement);

            // Add error class to input
            input.classList.add('is-invalid');
        }

        // Function to update background based on the element type and value    
        function updateBackground(element) {
            if (element.tagName === 'SELECT') {
                if (element.value) {
                    element.style.backgroundColor = '#f0f7e9'; // Light green for valid select
                } else {
                    element.style.backgroundColor = '#fff'; // White for empty select
                }
            } else if (element.type === 'text' || element.tagName === 'TEXTAREA') {
                if (element.value.trim()) {
                    element.style.backgroundColor = '#f0f7e9'; // Light green for valid text
                } else {
                    element.style.backgroundColor = '#fff'; // White for empty text
                }
            } else if (element.type === 'number') {
                if (element.value && parseInt(element.value) > 0) {
                    element.style.backgroundColor = '#f0f7e9'; // Light green for valid number
                } else {
                    element.style.backgroundColor = '#fff'; // White for invalid number
                }
            }
        }

        // Apply the function to each element and add event listeners for changes
        elements.forEach(element => {
            // Initial background update
            updateBackground(element);

            // Add event listeners for input changes
            element.addEventListener('change', function () {
                updateBackground(this);
            });

            element.addEventListener('input', function () {
                updateBackground(this);
            });
        });

        // Form submission validation
        form.addEventListener('submit', function (event) {
            let hasErrors = false;

            // Check each required element
            elements.forEach(element => {
                if (element.required && !element.value.trim()) {
                    showValidationError(element, 'This field is required');     
                    hasErrors = true;
                }
            });

            // Prevent form submission if there are errors
            if (hasErrors) {
                event.preventDefault();
            }
        });
    } else {
        console.log('No .myForm found - skipping validation');
    }
});

// Validate all current items before adding a new one
function validateCurrentItems() {
    let isValid = true;

    // First, clear any existing validation messages
    $('.item-row').find('.text-danger').text('');

    // Get all item rows
    const $rows = $('.item-row');

    // Loop through each row
    $rows.each(function () {
        const $row = $(this);
        const index = $row.data('index');

        // Get the input elements
        const $nameInput = $row.find(`[name$="].Name"]`);
        const $quantityInput = $row.find(`[name$="].Quantity"]`);
        const $conditionInput = $row.find(`[name$="].Condition"]`);
        const $statusInput = $row.find(`[name$="].Status"]`);

        // Check name
        if (!$nameInput.val()) {
            $nameInput.closest('.form-group').find('.text-danger').text('Name is required');
            isValid = false;
        }

        // Check quantity
        if (!$quantityInput.val() || parseInt($quantityInput.val()) < 1) {  
            $quantityInput.closest('.form-group').find('.text-danger').text('Quantity must be at least 1');
            isValid = false;
        }

        // Check condition
        if (!$conditionInput.val()) {
            $conditionInput.closest('.form-group').find('.text-danger').text('Condition is required');
            isValid = false;
        }

        // Check status
        if (!$statusInput.val()) {
            $statusInput.closest('.form-group').find('.text-danger').text('Status is required');
            isValid = false;
        }
        
        // If this row has validation errors, open its accordion
        if (!isValid && $row.find('.text-danger').text().trim() !== '') {
            openAccordion(index);
            // Only open the first invalid accordion
            return false;
        }
    });

    return isValid;
}

// Handle form submission
$('#wizardRequisitionItems').off('submit').on('submit', function (e) {      
    if ($('input[name="direction"]').val() === 'previous') {
        return true;
    }
    if (!validateCurrentItems()) {
        e.preventDefault(); // Block submission
        return false;
    }
    // If valid, LET DEFAULT SUBMISSION PROCEED
    return true;
});
