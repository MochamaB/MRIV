document.addEventListener('DOMContentLoaded', function () {
    // Clone item functionality
    let itemCounter = 0;
    const newIndex = itemCounter;


    // Add new item
    $('#addNewItemBtn').on('click', function () {
        const originalItem = $('.item-row').first();
        if (originalItem.length === 0) return;

        itemCounter++;
        const newIndex = itemCounter;
        const clone = originalItem.clone(true);

        // Update IDs in cloned item
        // Update asp-for attributes
        clone.find('[asp-for]').each(function () {
            const originalAspFor = $(this).attr('asp-for');
            const newAspFor = originalAspFor.replace(/\[(\d+)\]/, `[${newIndex}]`);
            $(this).attr('asp-for', newAspFor);
        });
        // Update name attributes
        clone.find('[name]').each(function () {
            const originalName = $(this).attr('name');
            // Check if name attribute contains array index
            if (originalName.includes('[')) {
                const newName = originalName.replace(/\[(\d+)\]/, `[${newIndex}]`);
                $(this).attr('name', newName);
            }
        });
        // Update validation span elements
        clone.find('[asp-validation-for]').each(function () {
            const originalValidationFor = $(this).attr('asp-validation-for');
            const newValidationFor = originalValidationFor.replace(/\[(\d+)\]/, `[${newIndex}]`);
            $(this).attr('asp-validation-for', newValidationFor);
        });

        clone.find('[id]').each(function () {
            const originalId = $(this).attr('id');
            const newId = originalId + '_' + itemCounter;
            $(this).attr('id', newId);

            // Update labels
            const label = clone.find(`label[for="${originalId}"]`);
            if (label.length) label.attr('for', newId);
        });

        // 1. Reset the checkbox state for the CLONED item
        const clonedCheckboxId = `#saveToInventory_${itemCounter}`;
        clone.find(clonedCheckboxId)
            .prop('checked', false)
            .prop('disabled', true);

        // 2. Hide the badge container for the CLONED item
        const clonedBadgeContainerId = `#badgeContainer_${itemCounter}`;
        clone.find(clonedBadgeContainerId).addClass('d-none');

        // 3. Reset badge text for the CLONED item
        clone.find(`#selectedMaterialCategory_${itemCounter}`).text('Category: None');
        clone.find(`#selectedMaterialCode_${itemCounter}`).text('Code: None');
        clone.find(`#selectedMaterialVendor_${itemCounter}`).text('Vendor: None');

        // Create new modal for cloned item
        const newModalId = 'inventoryModal_' + itemCounter;
        const modalClone = $('#inventoryModal').clone().attr('id', newModalId);

        // Update modal IDs
        modalClone.find('[id]').each(function () {
            const originalModalId = $(this).attr('id');
            if (originalModalId !== 'inventoryModal') {
                $(this).attr('id', originalModalId + '_' + itemCounter);
            }
        });

        // Update modal trigger in clone
        clone.find('[data-bs-target="#inventoryModal"]')
            .attr('data-bs-target', '#' + newModalId);

        // Append cloned modal to body
        $('body').append(modalClone);

        // Initialize Bootstrap modal
        new bootstrap.Modal(modalClone[0]);

        // Reset form elements in clone
        clone.find('input:not([type="button"]), select, textarea').val('');

        clone.find('#saveToInventory').prop('checked', false).prop('disabled', true);
        clone.find('.badge').text(function (i, text) {
            return text.replace(/:.*/, ': None');
        });


        // Add remove button
        const removeBtn = $(`
    <button class="btn-close remove-item" 
        data-bs-toggle="tooltip" title="Remove item" 
        style="position: absolute; right: 10px; top: 10px; z-index: 10; color: red;">
        <i class="mdi mdi-close"></i>
    </button>`).click(function () {
            $(this).closest('.item-row').remove();
        });

        // Ensure the item row has `position: relative;` for proper button placement
        clone.css("position", "relative");

        // Prepend remove button directly inside `.item-row` before `.col-md-6`
        clone.prepend(removeBtn);

        // Reset Status and Condition to defaults
        clone.find('select[name$=".Status"]').val('PendingApproval'); // Enum name
        clone.find('select[name$=".Condition"]').val('GoodCondition'); // Enum name
        clone.find('input[name$=".Quantity"]').val(1);
        // Set MaterialCategoryId to 1 (Uncategorized) in the cloned modal
        modalClone.find('select[name$=".Material.MaterialCategoryId"]').val('1');

        // Append cloned item
        $('#itemsContainer').append(clone);
    });

    // Generate code handler (fixed for first modal)
    $(document).on('click', '[id^="generateCodeBtn"]', function () {
        const modal = $(this).closest('.modal');
        const modalId = modal.attr('id');
        const isOriginalModal = modalId === 'inventoryModal';

        // Get correct suffix
        const suffix = isOriginalModal ? '' : modalId.split('_')[1];

        // Get correct selectors
        const categorySelector = isOriginalModal ?
            '#materialCategoryId' :
            `#materialCategoryId_${suffix}`;

        const codeSelector = isOriginalModal ?
            '#materialCode' :
            `#materialCode_${suffix}`;

      //  codeSelector.val('1');
        const categoryId = $(categorySelector).val();

        if (!categoryId) {
            alert('Please select a Material Category first.');
            return;
        }

        $.ajax({
            url: '/Materials/GetNextCode',
            type: 'GET',
            data: {
                categoryId: categoryId,
                rowIndex: suffix || '0' // Send '0' for original modal
            },
            success: function (code) {
                $(codeSelector).val(code);
            },
            error: function (xhr) {
                console.error('Error:', xhr.responseText);
                alert('Error generating code. Please try again.');
            }
        });
    });

    // Save modal data handler (unchanged)
    // Save modal data handler (fixed for first modal)
    $(document).on('click', '[id^="saveInventoryDetails"]', function () {
        const modal = $(this).closest('.modal');
        const modalId = modal.attr('id');
        const isOriginalModal = modalId === 'inventoryModal';
        const suffix = isOriginalModal ? '' : modalId.split('_')[1];

        let isValid = true;

        // Validate required fields
        modal.find('[required]').each(function () {
            if (!$(this).val().trim()) {
                $(this).addClass('is-invalid');
                isValid = false;
            } else {
                $(this).removeClass('is-invalid');
            }
        });

        if (isValid) {
            // Update checkbox (handle original and cloned)
            const checkboxId = isOriginalModal ?
                '#saveToInventory' :
                `#saveToInventory_${suffix}`;

            $(checkboxId)
                .prop('disabled', false)
                .prop('checked', true);

            // Get selectors based on modal type
            const categorySelector = isOriginalModal ?
                '#materialCategoryId' :
                `#materialCategoryId_${suffix}`;

            const codeSelector = isOriginalModal ?
                '#materialCode' :
                `#materialCode_${suffix}`;

            const vendorSelector = isOriginalModal ?
                '#materialVendor' :
                `#materialVendor_${suffix}`;

            // Update badges (handle original and cloned)
            const categoryText = $(`${categorySelector} option:selected`).text();
            const codeValue = $(codeSelector).val();
            const vendorText = $(`${vendorSelector} option:selected`).text() || 'None';

            // Badge selectors
            const categoryBadge = isOriginalModal ?
                '#selectedMaterialCategory' :
                `#selectedMaterialCategory_${suffix}`;

            const codeBadge = isOriginalModal ?
                '#selectedMaterialCode' :
                `#selectedMaterialCode_${suffix}`;

            const vendorBadge = isOriginalModal ?
                '#selectedMaterialVendor' :
                `#selectedMaterialVendor_${suffix}`;

            $(categoryBadge).text(`Category: ${categoryText}`);
            $(codeBadge).text(`Code: ${codeValue}`);
            $(vendorBadge).text(`Vendor: ${vendorText}`);

            // Show badge container
            const badgeContainer = isOriginalModal ?
                '#badgeContainer' :
                `#badgeContainer_${suffix}`;

            $(badgeContainer).removeClass('d-none');

            // Close modal properly
            bootstrap.Modal.getInstance(modal[0]).hide();
        }
    });

    // Remove modal backdrop after close
    $(document).on('hidden.bs.modal', '.modal', function () {
        $('.modal-backdrop').remove();
        $('body').removeClass('modal-open');
    });
});

/////  VALIDATION OF REQUISITION ITEMS



///SEARCH INPUT
document.addEventListener('DOMContentLoaded', function () {
    const searchInput = document.getElementById('searchbar');
    const searchResults = document.getElementById('searchResults');

    // Initially hide the results
    searchResults.classList.remove('show');

    // Show results when input is focused or clicked
    searchInput.addEventListener('focus', function () {
        searchResults.classList.add('show');
    });

    searchInput.addEventListener('click', function () {
        searchResults.classList.add('show');
    });

    // Hide results when clicking outside
    document.addEventListener('click', function (event) {
        const isClickInsideSearch = searchInput.contains(event.target);
        const isClickInsideResults = searchResults.contains(event.target);

        // If click is outside both the search input and results
        if (!isClickInsideSearch && !isClickInsideResults) {
            searchResults.classList.remove('show');
        }
    });

    // Prevent hiding results when clicking inside the results div
    searchResults.addEventListener('click', function (event) {
        // If clicking a select button, hide the results
        if (event.target.tagName === 'BUTTON') {
            searchResults.classList.remove('show');
        }
        // For other clicks inside results (like links), prevent propagation
        event.stopPropagation();
    });
});

