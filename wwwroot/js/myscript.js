document.addEventListener('DOMContentLoaded', function () {
    const itemsContainer = document.getElementById('itemsContainer');
    const addNewItemBtn = document.getElementById('addNewItemBtn');
    const materialSearch = document.getElementById('materialSearch');
    const clearMaterialSearch = document.getElementById('clearMaterialSearch');
    const searchResultsContainer = document.getElementById('searchResultsContainer');

    // 1. Add new item button click
    addNewItemBtn.addEventListener('click', function () {
        // Submit the form to add a new item
        const form = document.createElement('form');
        form.method = 'post';
        form.action = '/MaterialRequisition/AddRequisitionItem';
        document.body.appendChild(form);
        form.submit();
    });

    // 2. Remove item button click
    document.addEventListener('click', function (e) {
        if (e.target.classList.contains('remove-item') || e.target.closest('.remove-item')) {
            const btn = e.target.classList.contains('remove-item') ? e.target : e.target.closest('.remove-item');
            const index = btn.getAttribute('data-index');

            if (confirm('Are you sure you want to remove this item?')) {
                document.getElementById(`removeForm_${index}`).submit();
            }
        }
    });

    // 3. Generate code button click
    document.addEventListener('click', function (e) {
        if (e.target.classList.contains('generateCodeBtn')) {
            const index = e.target.getAttribute('data-index');
            const categorySelect = document.querySelector(`.materialCategoryId[data-index="${index}"]`);
            const codeInput = document.querySelector(`.materialCode[data-index="${index}"]`);

            if (categorySelect.value) {
                fetch('/MaterialRequisition/GenerateCode', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
                    },
                    body: JSON.stringify({
                        categoryId: parseInt(categorySelect.value),
                        itemIndex: parseInt(index)
                    })
                })
                    .then(response => response.json())
                    .then(data => {
                        codeInput.value = data.code;
                    })
                    .catch(error => console.error('Error generating code:', error));
            } else {
                alert('Please select a material category first');
            }
        }
    });

    // 4. Save inventory details button click
    document.addEventListener('click', function (e) {
        if (e.target.classList.contains('saveInventoryDetailsBtn')) {
            const index = e.target.getAttribute('data-index');
            const categorySelect = document.querySelector(`.materialCategoryId[data-index="${index}"]`);
            const codeInput = document.querySelector(`.materialCode[data-index="${index}"]`);
            const vendorSelect = document.querySelector(`.materialVendor[data-index="${index}"]`);

            // Basic validation
            if (!categorySelect.value || !codeInput.value) {
                alert('Please fill in all required fields');
                return;
            }

            // Update badge container visibility
            const badgeContainer = document.getElementById(`badgeContainer_${index}`);
            badgeContainer.classList.remove('d-none');

            // Update badge content
            const categoryBadge = document.getElementById(`selectedMaterialCategory_${index}`);
            const codeBadge = document.getElementById(`selectedMaterialCode_${index}`);
            const vendorBadge = document.getElementById(`selectedMaterialVendor_${index}`);

            categoryBadge.textContent = `Category: ${categorySelect.options[categorySelect.selectedIndex].text}`;
            codeBadge.textContent = `SNo.: ${codeInput.value}`;
            vendorBadge.textContent = `Vendor: ${vendorSelect.options[vendorSelect.selectedIndex].text || 'None'}`;

            // Enable SaveToInventory checkbox
            // First try with ID
            let saveToInventory = document.getElementById(`saveToInventory_${index}`);

            // If not found, try with name attribute (which contains the index)
            if (!saveToInventory) {
                saveToInventory = document.querySelector(`input[name$="[${index}].SaveToInventory"]`);
            }
            // If we found the checkbox, enable and check it
            if (saveToInventory) {
                saveToInventory.disabled = false;
                saveToInventory.checked = true;
                console.log(`Checkbox found and enabled: ${saveToInventory.id}`);
            } else {
                console.error(`SaveToInventory checkbox not found for index ${index}`);
            }

            // Close modal
            const modalElement = document.getElementById(`inventoryModal_${index}`);
            const modal = bootstrap.Modal.getInstance(modalElement);
            if (modal) {
                modal.hide();
            } else {
                // If modal instance is not found, create one and hide it
                new bootstrap.Modal(modalElement).hide();
            }
            setTimeout(() => {
                const modalBackdrops = document.querySelectorAll('.modal-backdrop');
                modalBackdrops.forEach(backdrop => {
                    backdrop.remove();
                });
                // Also restore the body classes
                document.body.classList.remove('modal-open');
                document.body.style.overflow = '';
                document.body.style.paddingRight = '';
            }, 150);
        }
    });

    // 5. Material search functionality
    let searchTimeout;
    materialSearch.addEventListener('input', function () {
        clearTimeout(searchTimeout);
        const searchTerm = this.value.trim();

        if (searchTerm.length > 0) {
            clearMaterialSearch.classList.remove('d-none');
        } else {
            clearMaterialSearch.classList.add('d-none');
            searchResultsContainer.style.display = 'none';
            return;
        }

        // Only search when user has stopped typing for 300ms
        searchTimeout = setTimeout(() => {
            if (searchTerm.length >= 2) {
                fetch('/MaterialRequisition/SearchMaterials', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
                    },
                    body: JSON.stringify({ searchTerm })
                })
                    .then(response => response.json())
                    .then(data => {
                        searchResultsContainer.innerHTML = '';

                        if (data.length === 0) {
                            searchResultsContainer.innerHTML = '<div class="dropdown-item">No materials found</div>';
                        } else {
                            data.forEach(material => {
                                const item = document.createElement('div');
                                item.className = 'dropdown-item';
                                item.style.cursor = 'pointer';
                                item.innerHTML = `<strong>${material.code}</strong> - ${material.name}`;
                                item.addEventListener('click', () => selectMaterial(material));
                                searchResultsContainer.appendChild(item);
                            });
                        }

                        searchResultsContainer.style.display = 'block';
                    })
                    .catch(error => console.error('Error searching materials:', error));
            } else {
                searchResultsContainer.style.display = 'none';
            }
        }, 300);
    });

    // Clear search
    clearMaterialSearch.addEventListener('click', function () {
        materialSearch.value = '';
        this.classList.add('d-none');
        searchResultsContainer.style.display = 'none';
    });

    // Select material from search results
    function selectMaterial(material) {
        // Find first empty item or add new one
        let emptyItemFound = false;
        const items = document.querySelectorAll('.item-row');

        for (let i = 0; i < items.length; i++) {
            const nameInput = items[i].querySelector('input[name^="RequisitionItems"][name$="Name"]');

            if (!nameInput.value) {
                fillItemWithMaterial(i, material);
                emptyItemFound = true;
                break;
            }
        }

        if (!emptyItemFound) {
            // Submit form to add new item, then fill it with the material details
            sessionStorage.setItem('selectedMaterial', JSON.stringify(material));
            addNewItemBtn.click();
        }

        // Close search results
        searchResultsContainer.style.display = 'none';
        materialSearch.value = '';
        clearMaterialSearch.classList.add('d-none');
    }

    function fillItemWithMaterial(index, material) {
        // Fill in form fields
        const nameInput = document.querySelector(`input[name="RequisitionItems[${index}].Name"]`);
        const descInput = document.querySelector(`textarea[name="RequisitionItems[${index}].Description"]`);
        const categorySelect = document.querySelector(`.materialCategoryId[data-index="${index}"]`);
        const codeInput = document.querySelector(`.materialCode[data-index="${index}"]`);
        const vendorSelect = document.querySelector(`.materialVendor[data-index="${index}"]`);

        nameInput.value = material.name;
        descInput.value = material.description || '';
        categorySelect.value = material.categoryId;
        codeInput.value = material.code;
        vendorSelect.value = material.vendorId || '';

        // Update badges
        const badgeContainer = document.getElementById(`badgeContainer_${index}`);
        badgeContainer.classList.remove('d-none');

        const categoryBadge = document.getElementById(`selectedMaterialCategory_${index}`);
        const codeBadge = document.getElementById(`selectedMaterialCode_${index}`);
        const vendorBadge = document.getElementById(`selectedMaterialVendor_${index}`);

        categoryBadge.textContent = `Category: ${material.categoryName}`;
        codeBadge.textContent = `SNo.: ${material.code}`;

        if (material.vendorId) {
            // Find vendor name from selected option
            const vendorName = vendorSelect.options[vendorSelect.selectedIndex]?.text || 'None';
            vendorBadge.textContent = `Vendor: ${vendorName}`;
        } else {
            vendorBadge.textContent = 'Vendor: None';
        }

        // Enable SaveToInventory checkbox
        const saveToInventory = document.getElementById(`saveToInventory_${index}`);
        saveToInventory.disabled = false;
        saveToInventory.checked = true;
    }

    // Check for stored material after page load (for new items)
    const storedMaterial = sessionStorage.getItem('selectedMaterial');
    if (storedMaterial) {
        const material = JSON.parse(storedMaterial);
        const items = document.querySelectorAll('.item-row');
        const lastIndex = items.length - 1;

        fillItemWithMaterial(lastIndex, material);
        sessionStorage.removeItem('selectedMaterial');
    }
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
    const form = document.querySelector('.myForm');

    if (!form) {
        console.error('Form not found');
        return;
    }

    form.addEventListener('submit', function (e) {
        let isValid = true;
        const requiredInputs = form.querySelectorAll('input[required], select[required], textarea[required]');

        // Clear previous errors
        document.querySelectorAll('.validation-error').forEach(el => el.remove());
        requiredInputs.forEach(input => input.classList.remove('is-invalid'));

        requiredInputs.forEach(input => {
            const value = input.value.trim();
            const isCheckbox = input.type === 'checkbox';
            const isRadio = input.type === 'radio';
            const isSelect = input.tagName === 'SELECT';

            if ((isCheckbox && !input.checked) ||
                (isRadio && !form.querySelector(`input[name="${input.name}"]:checked`)) ||
                (!isCheckbox && !isRadio && !value)) {

                isValid = false;
                input.classList.add('is-invalid');
                const label = form.querySelector(`label[for="${input.id}"]`);
                const fieldName = label ? label.textContent.replace('*', '').trim() : 'This field';
                showValidationError(input, `${fieldName} is required`);
            }
        });

        if (!isValid) {
            e.preventDefault();
            window.scrollTo({ top: 0, behavior: 'smooth' });
        }
    });

    function showValidationError(input, message) {
        const errorSpan = document.createElement('span');
        errorSpan.classList.add('text-danger', 'validation-error', 'd-block', 'mt-1');
        errorSpan.textContent = message;

        // Insert after the input element
        input.parentNode.insertBefore(errorSpan, input.nextElementSibling);
    }
});


document.addEventListener('DOMContentLoaded', function () {
    // Target select, input (text and number), and textarea elements
    const elements = document.querySelectorAll('select, input[type="text"], input[type="number"], textarea');

    // Function to update background based on the element type and value
    function updateBackground(element) {
        const tag = element.tagName.toLowerCase();
        if (tag === 'select') {
            // For select elements, check if a non-default option is selected
            if (element.selectedIndex > 0) {
                element.style.backgroundColor = '#f8f9fa'; // Light blue background
            } else {
                element.style.backgroundColor = ''; // Reset to default
            }
        } else if (tag === 'input' || tag === 'textarea') {
            // For input and textarea, check if the value is not empty
            if (element.value.trim() !== '') {
                element.style.backgroundColor = '#f8f9fa';
            } else {
                element.style.backgroundColor = '';
            }
        }
    }

    // Apply the function to each element and add event listeners for changes
    elements.forEach(element => {
        // Initial background update
        updateBackground(element);

        // Listen for change events
        element.addEventListener('change', function () {
            updateBackground(this);
        });

        // For inputs and textareas, also update on input event for immediate feedback
        element.addEventListener('input', function () {
            updateBackground(this);
        });
    });
});




