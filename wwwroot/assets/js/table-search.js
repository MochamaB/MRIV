/**
 * Custom table search and sorting functionality for MRIV
 * Replaces bootstrap-table functionality
 */
document.addEventListener('DOMContentLoaded', function() {
    // Initialize search on all search boxes with the table-search class
    initializeTableSearch();
    
    // Initialize sorting on all tables with sortable columns
    initializeTableSorting();
});

/**
 * Initialize table search functionality
 */
function initializeTableSearch() {
    const searchBoxes = document.querySelectorAll('.search-box .search');
    
    searchBoxes.forEach(searchBox => {
        // Find the closest table to this search box
        const card = searchBox.closest('.card');
        if (!card) return;
        
        const table = card.querySelector('table');
        if (!table) return;
        
        // Add event listener for input changes
        searchBox.addEventListener('input', function() {
            const searchTerm = this.value.toLowerCase().trim();
            performTableSearch(table, searchTerm);
        });
        
        // Add event listener for search icon click
        const searchIcon = searchBox.parentElement.querySelector('.search-icon');
        if (searchIcon) {
            searchIcon.addEventListener('click', function() {
                const searchTerm = searchBox.value.toLowerCase().trim();
                performTableSearch(table, searchTerm);
            });
        }
        
        // Add clear button functionality
        addClearButton(searchBox);
    });
}

/**
 * Add a clear button to search inputs
 */
function addClearButton(searchInput) {
    // Create clear button if it doesn't exist
    let clearButton = searchInput.parentElement.querySelector('.search-clear-icon');
    
    if (!clearButton) {
        clearButton = document.createElement('i');
        clearButton.className = 'ri-close-line search-clear-icon';
        clearButton.style.display = 'none';
        clearButton.style.position = 'absolute';
        clearButton.style.right = '2.5rem';
        clearButton.style.top = '50%';
        clearButton.style.transform = 'translateY(-50%)';
        clearButton.style.cursor = 'pointer';
        searchInput.parentElement.appendChild(clearButton);
        
        // Show/hide clear button based on input content
        searchInput.addEventListener('input', function() {
            clearButton.style.display = this.value ? 'block' : 'none';
        });
        
        // Clear the search when clicked
        clearButton.addEventListener('click', function() {
            searchInput.value = '';
            searchInput.dispatchEvent(new Event('input'));
            this.style.display = 'none';
            searchInput.focus();
        });
    }
}

/**
 * Perform search on table rows
 */
function performTableSearch(table, searchTerm) {
    const tbody = table.querySelector('tbody');
    if (!tbody) return;
    
    const rows = tbody.querySelectorAll('tr');
    
    // If search term is empty, show all rows
    if (!searchTerm) {
        rows.forEach(row => {
            row.style.display = '';
        });
        return;
    }
    
    // Check each row
    rows.forEach(row => {
        const text = row.textContent.toLowerCase();
        
        // Show/hide row based on search term
        if (text.includes(searchTerm)) {
            row.style.display = '';
        } else {
            row.style.display = 'none';
        }
    });
    
    // Check if we have any visible rows
    const visibleRows = Array.from(rows).filter(row => row.style.display !== 'none');
    
    // Show/hide no results message
    let noResultsRow = tbody.querySelector('.no-results-row');
    
    if (visibleRows.length === 0) {
        // Create no results row if it doesn't exist
        if (!noResultsRow) {
            noResultsRow = document.createElement('tr');
            noResultsRow.className = 'no-results-row';
            const td = document.createElement('td');
            td.colSpan = table.querySelectorAll('thead th').length || 5;
            td.className = 'text-center py-3';
            td.textContent = 'No matching records found';
            noResultsRow.appendChild(td);
            tbody.appendChild(noResultsRow);
        } else {
            noResultsRow.style.display = '';
        }
    } else if (noResultsRow) {
        noResultsRow.style.display = 'none';
    }
}

/**
 * Initialize table sorting functionality
 */
function initializeTableSorting() {
    // Find all tables in the document
    const tables = document.querySelectorAll('table');
    
    tables.forEach(table => {
        const headers = table.querySelectorAll('th[data-sortable="true"]');
        
        headers.forEach(header => {
            // Add sort icons
            const sortIcon = document.createElement('span');
            sortIcon.className = 'sort-icon ms-1';
            sortIcon.innerHTML = '<i class="ri-arrow-up-down-line text-muted"></i>';
            header.appendChild(sortIcon);
            
            // Add cursor pointer style
            header.style.cursor = 'pointer';
            
            // Add click event for sorting
            header.addEventListener('click', function() {
                const columnIndex = Array.from(header.parentNode.children).indexOf(header);
                const currentDirection = header.getAttribute('data-sort-direction') || 'none';
                
                // Reset all headers
                headers.forEach(h => {
                    if (h !== header) {
                        h.setAttribute('data-sort-direction', 'none');
                        const icon = h.querySelector('.sort-icon');
                        if (icon) {
                            icon.innerHTML = '<i class="ri-arrow-up-down-line text-muted"></i>';
                        }
                    }
                });
                
                // Set new sort direction
                let newDirection = 'asc';
                if (currentDirection === 'asc') {
                    newDirection = 'desc';
                } else if (currentDirection === 'desc') {
                    newDirection = 'none';
                }
                
                header.setAttribute('data-sort-direction', newDirection);
                
                // Update icon
                if (newDirection === 'asc') {
                    sortIcon.innerHTML = '<i class="ri-arrow-up-line"></i>';
                } else if (newDirection === 'desc') {
                    sortIcon.innerHTML = '<i class="ri-arrow-down-line"></i>';
                } else {
                    sortIcon.innerHTML = '<i class="ri-arrow-up-down-line text-muted"></i>';
                }
                
                // Sort the table
                sortTable(table, columnIndex, newDirection);
            });
        });
    });
}

/**
 * Sort table by column
 */
function sortTable(table, columnIndex, direction) {
    const tbody = table.querySelector('tbody');
    if (!tbody) return;
    
    const rows = Array.from(tbody.querySelectorAll('tr:not(.no-results-row)'));
    
    // If direction is none, restore original order
    if (direction === 'none') {
        // Try to restore original order if we have it stored
        if (table.originalRows) {
            table.originalRows.forEach(row => tbody.appendChild(row));
        }
        return;
    }
    
    // Store original order if not already stored
    if (!table.originalRows) {
        table.originalRows = rows.map(row => row.cloneNode(true));
    }
    
    // Sort the rows
    rows.sort((rowA, rowB) => {
        const cellA = rowA.querySelectorAll('td')[columnIndex];
        const cellB = rowB.querySelectorAll('td')[columnIndex];
        
        if (!cellA || !cellB) return 0;
        
        const valueA = cellA.textContent.trim();
        const valueB = cellB.textContent.trim();
        
        // Check if values are dates
        const dateA = parseDate(valueA);
        const dateB = parseDate(valueB);
        
        if (dateA && dateB) {
            return direction === 'asc' ? dateA - dateB : dateB - dateA;
        }
        
        // Check if values are numbers
        const numA = parseFloat(valueA.replace(/[^0-9.-]+/g, ''));
        const numB = parseFloat(valueB.replace(/[^0-9.-]+/g, ''));
        
        if (!isNaN(numA) && !isNaN(numB)) {
            return direction === 'asc' ? numA - numB : numB - numA;
        }
        
        // Default to string comparison
        return direction === 'asc' 
            ? valueA.localeCompare(valueB) 
            : valueB.localeCompare(valueA);
    });
    
    // Reorder the rows in the DOM
    rows.forEach(row => tbody.appendChild(row));
}

/**
 * Try to parse a date from various formats
 */
function parseDate(dateStr) {
    if (!dateStr) return null;
    
    // Try to parse common date formats
    const formats = [
        // DD MMM YYYY (e.g., "15 Jan 2023")
        str => {
            const match = str.match(/(\d{1,2})\s+(Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec)\s+(\d{4})/i);
            if (match) {
                const months = {jan:0,feb:1,mar:2,apr:3,may:4,jun:5,jul:6,aug:7,sep:8,oct:9,nov:10,dec:11};
                const day = parseInt(match[1], 10);
                const month = months[match[2].toLowerCase()];
                const year = parseInt(match[3], 10);
                return new Date(year, month, day);
            }
            return null;
        },
        // MM/DD/YYYY or DD/MM/YYYY
        str => {
            const parts = str.split(/[\/.-]/);
            if (parts.length === 3) {
                // Try both MM/DD/YYYY and DD/MM/YYYY
                const date1 = new Date(parts[2], parts[0] - 1, parts[1]);
                const date2 = new Date(parts[2], parts[1] - 1, parts[0]);
                
                // Return the one that seems more valid
                if (!isNaN(date1.getTime())) return date1;
                if (!isNaN(date2.getTime())) return date2;
            }
            return null;
        },
        // ISO format (YYYY-MM-DD)
        str => {
            const date = new Date(str);
            return !isNaN(date.getTime()) ? date : null;
        }
    ];
    
    // Try each format
    for (const format of formats) {
        const date = format(dateStr);
        if (date) return date.getTime();
    }
    
    return null;
}
