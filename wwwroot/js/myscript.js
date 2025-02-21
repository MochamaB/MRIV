document.addEventListener('DOMContentLoaded', function () {
    let itemCounter = document.querySelectorAll('.item-row').length - 1;

       // Initialize existing items on page load or after validation
    initializeExistingItems();

    function initializeExistingItems() {
        // Initialize all existing item rows
        document.querySelectorAll('.item-row').forEach((item, index) => {
            // Add remove button if not first item
            if (index > 0) {
                addRemoveButton(item);
            }

            // Create modal if doesn't exist
            const modalId = `inventoryModal_${index}`;
            if (!document.getElementById(modalId)) {
                createNewModal(index);
            }

            // Update badge container visibility
            const materialCode = item.querySelector('[name$="Material.Code"]')?.value;
            const saveToInventory = item.querySelector('[name$="SaveToInventory"]')?.checked;
            
            if (materialCode || saveToInventory) {
                const badgeContainer = item.querySelector('[id^="badgeContainer"]');
                if (!materialCode) {
                    badgeContainer.classList.add('d-none');
                    
                    // Update badges with existing values
                    updateBadgesFromExistingData(item, index);
                }

                // Enable checkbox if material code exists
                const checkbox = item.querySelector('[name$="SaveToInventory"]');
                if (checkbox && materialCode) {
                    checkbox.disabled = false;
                    badgeContainer.classList.remove('d-none');
                }
            }
        });
    }

    function updateBadgesFromExistingData(item, index) {
        // Get existing values
        const categoryId = item.querySelector(`[name$="[${index}].Material.MaterialCategoryId"]`)?.value;
        const categorySelect = document.querySelector(`select[name$="[${index}].Material.MaterialCategoryId"]`);
        const categoryText = categorySelect?.options[categorySelect.selectedIndex]?.text || 'None';

        const code = item.querySelector(`[name$="[${index}].Material.Code"]`)?.value || 'None';
        
        const vendorId = item.querySelector(`[name$="[${index}].Material.VendorId"]`)?.value;
        const vendorSelect = document.querySelector(`select[name$="[${index}].Material.VendorId"]`);
        const vendorText = vendorSelect?.options[vendorSelect.selectedIndex]?.text || 'None';

        // Update badges
        const badgeContainer = item.querySelector('[id^="badgeContainer"]');
        if (badgeContainer) {
            const categoryBadge = badgeContainer.querySelector('[id^="selectedMaterialCategory"]');
            const codeBadge = badgeContainer.querySelector('[id^="selectedMaterialCode"]');
            const vendorBadge = badgeContainer.querySelector('[id^="selectedMaterialVendor"]');

            if (categoryBadge) categoryBadge.textContent = `Category: ${categoryText}`;
            if (codeBadge) codeBadge.textContent = `Code: ${code}`;
            if (vendorBadge) vendorBadge.textContent = `Vendor: ${vendorText}`;

        }
    }


    // Add new item functionality
    document.getElementById('addNewItemBtn').addEventListener('click', function () {
        const originalItem = document.querySelector('.item-row');
        if (!originalItem) return;

        itemCounter++;
        const clone = originalItem.cloneNode(true);

        // Update all form elements in clone
        updateClonedElements(clone, itemCounter);

        // Reset form values
        resetClonedFormValues(clone);

        // Add remove button
        addRemoveButton(clone);

        // Create and append new modal
        createNewModal(itemCounter);

        // Append cloned item
        document.getElementById('itemsContainer').appendChild(clone);
    });

    function updateClonedElements(clone, index) {
        // Update input elements
        clone.querySelectorAll('[name]').forEach(element => {
            const name = element.getAttribute('name');
            if (name && name.includes('[')) {
                element.setAttribute('name', name.replace(/\[(\d+)\]/, `[${index}]`));
            }
        });

        // Update asp-for attributes
        clone.querySelectorAll('[asp-for]').forEach(element => {
            const aspFor = element.getAttribute('asp-for');
            if (aspFor) {
                element.setAttribute('asp-for', aspFor.replace(/\[(\d+)\]/, `[${index}]`));
            }
        });

        // Update validation elements
        clone.querySelectorAll('[asp-validation-for]').forEach(element => {
            const validationFor = element.getAttribute('asp-validation-for');
            if (validationFor) {
                element.setAttribute('asp-validation-for', validationFor.replace(/\[(\d+)\]/, `[${index}]`));
            }
        });

        // Update IDs and related labels
        clone.querySelectorAll('[id]').forEach(element => {
            const originalId = element.getAttribute('id');
            const newId = `${originalId}_${index}`;
            element.setAttribute('id', newId);

            // Update corresponding labels
            const relatedLabel = clone.querySelector(`label[for="${originalId}"]`);
            if (relatedLabel) {
                relatedLabel.setAttribute('for', newId);
            }
        });

        // Update modal trigger
        const modalTrigger = clone.querySelector('[data-bs-target]');
        if (modalTrigger) {
            modalTrigger.setAttribute('data-bs-target', `#inventoryModal_${index}`);
        }
    }

    function resetClonedFormValues(clone) {
        // Reset input values
        clone.querySelectorAll('input:not([type="button"]):not([type="hidden"])').forEach(input => {
            if (input.type === 'checkbox') {
                input.checked = false;
                input.disabled = true;
            } else if (input.type === 'number' && input.getAttribute('name').includes('.Quantity')) {
                input.value = '1';
            } else {
                input.value = '';
            }
        });

        // Reset select elements
        clone.querySelectorAll('select').forEach(select => {
            const name = select.getAttribute('name');
            if (name?.includes('.Status')) {
                select.value = 'PendingApproval';
            } else if (name?.includes('.Condition')) {
                select.value = 'GoodCondition';
            } else if (name?.includes('.Material.MaterialCategoryId')) {
                select.value = '1'; // Set to default category ID
            } else {
                select.value = '';
            }
        });

        // Reset textarea elements
        clone.querySelectorAll('textarea').forEach(textarea => {
            textarea.value = '';
        });

        // Reset badge container
        const badgeContainer = clone.querySelector('[id^="badgeContainer"]');
        if (badgeContainer) {
            badgeContainer.classList.add('d-none');
            badgeContainer.querySelectorAll('.badge').forEach(badge => {
                const text = badge.textContent.split(':')[0];
                badge.textContent = `${text}: None`;
            });
        }
    }

    function createNewModal(index) {
        const originalModal = document.querySelector('.modal');
        if (!originalModal) return;

        const newModal = originalModal.cloneNode(true);
        newModal.id = `inventoryModal_${index}`;

        // Update modal elements
        updateClonedElements(newModal, index);

        // Reset form values in modal
        resetClonedFormValues(newModal);

        // Append new modal
        document.body.appendChild(newModal);

        // Initialize new Bootstrap modal
        new bootstrap.Modal(newModal);
    }

    function addRemoveButton(clone) {
        const removeBtn = document.createElement('button');
        removeBtn.className = 'btn-close remove-item';
        removeBtn.setAttribute('data-bs-toggle', 'tooltip');
        removeBtn.setAttribute('title', 'Remove item');
        removeBtn.style.cssText = 'position: absolute; right: 10px; top: 10px; z-index: 10; color: red;';

        removeBtn.addEventListener('click', function () {
            clone.remove();
        });

        clone.style.position = 'relative';
        clone.insertBefore(removeBtn, clone.firstChild);
    }

    // Handle generate code button clicks
    document.addEventListener('click', function (e) {
        if (e.target.id.startsWith('generateCodeBtn')) {
            const modal = e.target.closest('.modal');
            const modalId = modal.id;

            // Handle both original and cloned modals
            const index = modalId === 'inventoryModal_0' ? '0' : modalId.split('_')[1];

            const categorySelect = modal.querySelector(`[name$="[${index}].Material.MaterialCategoryId"]`);
            const codeInput = modal.querySelector(`[name$="[${index}].Material.Code"]`);

            const categoryId = categorySelect.value;
            if (!categoryId) {
                alert('Please select a Material Category first.');
                return;
            }

            // Make AJAX call to get next code
            fetch(`/Materials/GetNextCode?categoryId=${categoryId}&rowIndex=${index}`)
                .then(response => response.text())
                .then(code => {
                    codeInput.value = code;
                })
                .catch(error => {
                    console.error('Error:', error);
                    alert('Error generating code. Please try again.');
                });
        }
    });

    // Save inventory details handler
    document.addEventListener('click', function (e) {
        if (e.target.id.startsWith('saveInventoryDetails')) {
            const modal = e.target.closest('.modal');
            const modalId = modal.id;
            const index = modalId === 'inventoryModal_0' ? '0' : modalId.split('_')[1];

            let isValid = true;

            // Validate required fields
            modal.querySelectorAll('[required]').forEach(field => {
                if (!field.value.trim()) {
                    field.classList.add('is-invalid');
                    isValid = false;
                } else {
                    field.classList.remove('is-invalid');
                }
            });

            if (isValid) {
                // Get the related item row
                const itemRow = document.querySelector(`.item-row:nth-child(${parseInt(index) + 1})`);

                // Enable and check the inventory checkbox
                const checkbox = itemRow.querySelector(`[name$="[${index}].SaveToInventory"]`);
                if (checkbox) {
                    checkbox.disabled = false;
                    checkbox.checked = true;
                }

                // Update badges
                const badgeContainer = itemRow.querySelector('[id^="badgeContainer"]');
                if (badgeContainer) {
                    badgeContainer.classList.remove('d-none');

                    // Update category badge
                    const categorySelect = modal.querySelector(`[name$="[${index}].Material.MaterialCategoryId"]`);
                    const categoryBadge = badgeContainer.querySelector('[id^="selectedMaterialCategory"]');
                    if (categorySelect && categoryBadge) {
                        const categoryText = categorySelect.options[categorySelect.selectedIndex].text;
                        categoryBadge.textContent = `Category: ${categoryText}`;
                    }

                    // Update code badge
                    const codeInput = modal.querySelector(`[name$="[${index}].Material.Code"]`);
                    const codeBadge = badgeContainer.querySelector('[id^="selectedMaterialCode"]');
                    if (codeInput && codeBadge) {
                        codeBadge.textContent = `Code: ${codeInput.value}`;
                    }

                    // Update vendor badge
                    const vendorSelect = modal.querySelector(`[name$="[${index}].Material.VendorId"]`);
                    const vendorBadge = badgeContainer.querySelector('[id^="selectedMaterialVendor"]');
                    if (vendorSelect && vendorBadge) {
                        const vendorText = vendorSelect.options[vendorSelect.selectedIndex].text;
                        vendorBadge.textContent = `Vendor: ${vendorText || 'None'}`;
                    }
                }

                // Close the modal
                const modalInstance = bootstrap.Modal.getInstance(modal);
                modalInstance.hide();
            }
        }
    });

    // Clean up modal backdrop
    document.addEventListener('hidden.bs.modal', function (e) {
        const modalBackdrops = document.querySelectorAll('.modal-backdrop');
        modalBackdrops.forEach(backdrop => backdrop.remove());
        document.body.classList.remove('modal-open');
    });

    // Initialize form controls
    document.querySelectorAll('.formcontrol2').forEach(select => {
        select.addEventListener('change', function () {
            this.style.borderColor = this.value ? '#dee2e6' : '#fcb900';
        });
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

document.addEventListener('DOMContentLoaded', function () {
    const selects = document.querySelectorAll('.formcontrol2');

    // Add event listeners to dynamically check if value is empty
    selects.forEach(select => {
        select.addEventListener('change', function () {
            if (!select.value) {
                select.style.borderColor = "#fcb900"; // Yellow
            } else {
                select.style.borderColor = "#dee2e6"; // Default border color
            }
        });
    });
});


    document.addEventListener('DOMContentLoaded', function () {
        const form = document.querySelector('form[asp-action="CreateApprovals"]');
    const collectorNameInput = document.querySelector('input[name="Requisition.CollectorName"]');
    const collectorIdInput = document.querySelector('input[name="Requisition.CollectorId"]');

    form.addEventListener('submit', function (e) {
        let isValid = true;

            // Clear any previous validation messages
            document.querySelectorAll('.validation-error').forEach(el => el.remove());

    // Validate Collector Name
    if (!collectorNameInput.value.trim()) {
        showValidationError(collectorNameInput, "Collector Name is required");
    isValid = false;
            }

    // Validate Collector ID
    if (!collectorIdInput.value.trim()) {
        showValidationError(collectorIdInput, "Collector ID is required");
    isValid = false;
            }

    // Prevent form submission if validation fails
    if (!isValid) {
        e.preventDefault();
            }
        });

    function showValidationError(input, message) {
            const errorSpan = document.createElement('span');
    errorSpan.classList.add('text-danger', 'validation-error');
    errorSpan.textContent = message;

    input.insertAdjacentElement('afterend', errorSpan);
        }
    });




