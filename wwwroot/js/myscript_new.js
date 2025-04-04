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
            $row.find('.accordion-header .accordion-button').text(`Item ${index + 1}`);

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
                    'margin-right': '15px',
                    'z-index': 10,
                    'filter': 'invert(17%) sepia(100%) saturate(7480%) hue-rotate(357deg) brightness(92%) contrast(118%)'
                }
            });

            // Add click handler
            $removeBtn.on('click', function (e) {
                e.stopPropagation(); // Prevent accordion toggle
                removeRow(index);
            });

            // Append to the flex container after the accordion button
            $row.find('.accordion-header .d-flex').append($removeBtn);
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

            // Check if code exists
            const code = $codeInput.val().trim();
            const token = $('input[name="__RequestVerificationToken"]').val();
            
            // Disable the save button while checking
            $button.prop('disabled', true).html('<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Saving...');
            
            $.ajax({
                url: '/MaterialRequisition/CheckCodeExists',
                type: 'POST',
                data: JSON.stringify({ code: code }),
                contentType: 'application/json',
                headers: {
                    'RequestVerificationToken': token
                },
                success: function(response) {
                    if (response.exists) {
                        // Code exists, show error
                        $codeInput.closest('.form-group').find('.text-danger')
                            .text('This code already exists. Please use a different code or generate a new one.');
                        $button.prop('disabled', false).text('Save Material');
                    } else {
                        // Code doesn't exist, proceed with saving
                        saveMaterialDetails($button, $modal, $row, index, $categorySelect, $codeInput, $vendorSelect);
                    }
                },
                error: function() {
                    // Error checking code, proceed anyway
                    saveMaterialDetails($button, $modal, $row, index, $categorySelect, $codeInput, $vendorSelect);
                }
            });
        });
        
        // Function to save material details after validation
        function saveMaterialDetails($button, $modal, $row, index, $categorySelect, $codeInput, $vendorSelect) {
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

            // Reset button state
            $button.prop('disabled', false).text('Save Material');

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
        }

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
            console.log('Selected categoryId:', categoryId);
            console.log('Parsed categoryId:', parseInt(categoryId));
            console.log('Index:', index);

            // Anti-forgery token
            const token = $('input[name="__RequestVerificationToken"]').val();  

            // Show loading indicator
            $button.prop('disabled', true).html('<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Generating...');

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
                    $button.prop('disabled', false).text('Generate Code');
                    
                    if (response && response.code) {
                        $codeInput.val(response.code);
                    } else {
                        $codeInput.closest('.form-group').find('.text-danger')
                            .text('Error generating code. Please try again.');
                    }
                },
                error: function (error) {
                    $button.prop('disabled', false).text('Generate Code');
                    $codeInput.closest('.form-group').find('.text-danger')      
                        .text('Error generating code. Please try again.');      
                }
            });
        });

        // Add validation for material code input
        $('#itemsAccordion').off('blur', '.materialCode').on('blur', '.materialCode', function() {
            const $input = $(this);
            const code = $input.val().trim();
            
            // Skip validation if empty (will be caught by required validation)
            if (!code) return;
            
            // Get the error message element
            const $errorMsg = $input.closest('.form-group').find('.text-danger');
            
            // Clear any previous error message
            $errorMsg.text('');
            
            // Anti-forgery token
            const token = $('input[name="__RequestVerificationToken"]').val();
            
            // Check if code exists
            $.ajax({
                url: '/MaterialRequisition/CheckCodeExists',
                type: 'POST',
                data: JSON.stringify({ code: code }),
                contentType: 'application/json',
                headers: {
                    'RequestVerificationToken': token
                },
                success: function(response) {
                    if (response.exists) {
                        $errorMsg.text('This code already exists. Please use a different code or generate a new one.');
                    }
                },
                error: function() {
                    // Silent fail - don't show error to user for validation
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
                e.stopPropagation(); // Prevent event bubbling
                
                const $this = $(this);
                const materialId = $this.data('id');
                const materialName = $this.data('name');
                const materialCode = $this.data('code');
                const materialDescription = $this.data('description');
                const categoryId = $this.data('category-id');
                const categoryName = $this.data('category-name');
                const vendorId = $this.data('vendor-id');
                const vendorName = $this.data('vendor-name');
                
                // Find the currently open accordion
                const $openAccordion = $('.accordion-collapse.show');
                const $currentRow = $openAccordion.closest('.item-row');
                const index = $currentRow.data('index');
                
                // Check if the current row is already populated
                const currentName = $openAccordion.find(`[name="RequisitionItems[${index}].Name"]`).val();
                
                // If the current row is already populated, create a new row
                if (currentName && currentName.trim() !== '') {
                    // Trigger the add new item button click
                    $('#addNewItemBtn').click();
                    
                    // Now get the newly created row (which should be open)
                    const $newOpenAccordion = $('.accordion-collapse.show');
                    const $newRow = $newOpenAccordion.closest('.item-row');
                    const newIndex = $newRow.data('index');
                    
                    // Fill in the material details for the new row
                    populateMaterialDetails($newOpenAccordion, newIndex, materialName, materialId, materialCode, 
                        materialDescription, categoryId, categoryName, vendorId, vendorName);
                } else {
                    // Use the current row
                    populateMaterialDetails($openAccordion, index, materialName, materialId, materialCode, 
                        materialDescription, categoryId, categoryName, vendorId, vendorName);
                }
                
                // Close the dropdown - use direct DOM manipulation to ensure it's hidden
                $resultsContainer.css('display', 'none');
                $clearButton.addClass('d-none');
                $searchInput.val('');
            });
        }
        
        // Helper function to populate material details in a row
        function populateMaterialDetails($accordion, index, name, id, code, description, categoryId, categoryName, vendorId, vendorName) {
            // Fill in the material details
            $accordion.find(`[name="RequisitionItems[${index}].Name"]`).val(name);
            $accordion.find(`[name="RequisitionItems[${index}].MaterialId"]`).val(id);
            $accordion.find(`[name="RequisitionItems[${index}].MaterialCode"]`).val(code);
            
            // Set the description if available
            if (description) {
                $accordion.find(`[name="RequisitionItems[${index}].Description"]`).val(description);
            }
            
            // Update hidden fields for material data
            $accordion.find(`[name="RequisitionItems[${index}].Material.Id"]`).val(id);
            $accordion.find(`[name="RequisitionItems[${index}].Material.Code"]`).val(code);
            $accordion.find(`[name="RequisitionItems[${index}].Material.MaterialCategoryId"]`).val(categoryId);
            $accordion.find(`[name="RequisitionItems[${index}].Material.VendorId"]`).val(vendorId);
            
            // Get the row element
            const $row = $accordion.closest('.item-row');
            
            // Update badges
            const $badgeContainer = $row.find(`#badgeContainer_${index}`);
            $badgeContainer.removeClass('d-none');
            
            // Format badges with proper content
            $accordion.find(`#selectedMaterialCategory_${index}`).text(`Category: ${categoryName || 'Unknown'}`);
            $accordion.find(`#selectedMaterialCode_${index}`).text(`SNo.: ${code || 'No Code'}`);
            
            // Set vendor badge
            if (vendorId) {
                // Use the vendor name from data attribute if available, otherwise get from dropdown
                let displayVendorName = vendorName || 'Unknown';
                if (!displayVendorName || displayVendorName === 'Unknown') {
                    const $vendorSelect = $row.find(`[name="RequisitionItems[${index}].Material.VendorId"]`);
                    displayVendorName = $vendorSelect.find(`option[value="${vendorId}"]`).text() || vendorId;
                }
                $accordion.find(`#selectedMaterialVendor_${index}`).text(`Vendor: ${displayVendorName}`).removeClass('d-none');
            } else {
                $accordion.find(`#selectedMaterialVendor_${index}`).addClass('d-none');
            }
            
            // Populate and make modal fields readonly
            const $modal = $row.find(`#inventoryModal_${index}`);
            
            // Set category dropdown
            const $categorySelect = $modal.find(`.materialCategoryId`);
            $categorySelect.val(categoryId);
            $categorySelect.prop('disabled', true);
            
            // Set code field
            const $codeInput = $modal.find(`.materialCode`);
            $codeInput.val(code);
            $codeInput.prop('readonly', true);
            
            // Disable generate code button
            const $generateCodeBtn = $modal.find(`.generateCodeBtn`);
            $generateCodeBtn.prop('disabled', true);
            
            // Set vendor dropdown
            const $vendorSelect = $modal.find(`.materialVendor`);
            $vendorSelect.val(vendorId);
            $vendorSelect.prop('disabled', true);
            
            // Enable SaveToInventory checkbox
            const $checkbox = $row.find('input[type="checkbox"]');
            if ($checkbox.length > 0) {
                $checkbox.prop('disabled', false).prop('checked', true);
                $checkbox.closest('.form-check-label').css('display', 'block');
            }
            
            // Update background colors for readonly fields
            updateBackground($categorySelect[0]);
            updateBackground($codeInput[0]);
            updateBackground($vendorSelect[0]);
        }
    }

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
