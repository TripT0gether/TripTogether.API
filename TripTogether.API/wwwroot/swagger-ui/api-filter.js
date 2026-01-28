// API Filter for Swagger UI
(function() {
    'use strict';
    
    let filterContainer = null;
    let filterInput = null;
    
    function createFilterBox() {
        // Create filter container
        filterContainer = document.createElement('div');
        filterContainer.className = 'api-filter-container';
        filterContainer.innerHTML = 
            '<input ' +
            'type="text" ' +
            'class="api-filter-input" ' +
            'placeholder="Filter APIs... (e.g., auth, user, trip)" ' +
            'id="api-filter-input" />';
        
        // Get filter input
        filterInput = filterContainer.querySelector('#api-filter-input');
        
        // Add event listeners
        filterInput.addEventListener('input', debounce(filterApis, 200));
        filterInput.addEventListener('keydown', function(e) {
            if (e.key === 'Escape') {
                clearFilter();
            }
        });
        
        return filterContainer;
    }
    
    function insertFilterBox() {
        const infoContainer = document.querySelector('.swagger-ui .info');
        if (infoContainer && !document.querySelector('.api-filter-container')) {
            const filterBox = createFilterBox();
            infoContainer.parentNode.insertBefore(filterBox, infoContainer.nextSibling);
            console.log('API Filter box added successfully');
        }
    }
    
    function filterApis() {
        const filterValue = filterInput.value.toLowerCase().trim();
        const operations = document.querySelectorAll('.swagger-ui .opblock');
        const tags = document.querySelectorAll('.swagger-ui .opblock-tag-section');
        
        let visibleOperationsCount = 0;
        
        // Filter operations
        operations.forEach(function(operation) {
            const summary = operation.querySelector('.opblock-summary');
            const path = operation.querySelector('.opblock-summary-path');
            const method = operation.querySelector('.opblock-summary-method');
            
            let shouldShow = true;
            
            if (filterValue) {
                const searchableText = [
                    summary ? summary.textContent : '',
                    path ? path.textContent : '',
                    method ? method.textContent : ''
                ].join(' ').toLowerCase();
                
                shouldShow = searchableText.includes(filterValue);
            }
            
            if (shouldShow) {
                operation.classList.remove('filtered-hidden');
                visibleOperationsCount++;
            } else {
                operation.classList.add('filtered-hidden');
            }
        });
        
        // Hide/show tag sections based on whether they have visible operations
        tags.forEach(function(tag) {
            const tagOperations = tag.querySelectorAll('.opblock:not(.filtered-hidden)');
            if (filterValue && tagOperations.length === 0) {
                tag.classList.add('filtered-hidden');
            } else {
                tag.classList.remove('filtered-hidden');
            }
        });
        
        // Update placeholder text to show results count
        if (filterValue) {
            filterInput.placeholder = 'Showing ' + visibleOperationsCount + ' APIs matching "' + filterValue + '" (Press Esc to clear)';
        } else {
            filterInput.placeholder = 'Filter APIs... (e.g., auth, user, trip)';
        }
    }
    
    function clearFilter() {
        if (filterInput) {
            filterInput.value = '';
            filterApis();
            filterInput.focus();
        }
    }
    
    function debounce(func, wait) {
        let timeout;
        return function executedFunction() {
            const args = arguments;
            const later = function() {
                clearTimeout(timeout);
                func.apply(null, args);
            };
            clearTimeout(timeout);
            timeout = setTimeout(later, wait);
        };
    }
    
    // Initialize when DOM is ready
    function initialize() {
        // Wait for Swagger UI to load
        const checkForSwaggerUI = setInterval(function() {
            if (document.querySelector('.swagger-ui .info')) {
                insertFilterBox();
                clearInterval(checkForSwaggerUI);
            }
        }, 100);
        
        // Fallback timeout
        setTimeout(function() {
            clearInterval(checkForSwaggerUI);
            insertFilterBox();
        }, 5000);
    }
    
    // Start initialization
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initialize);
    } else {
        initialize();
    }
    
})();