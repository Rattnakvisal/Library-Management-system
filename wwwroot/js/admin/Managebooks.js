const search_input = document.querySelector('.search-input');
const search_icon = document.querySelector('.search-icon');

search_input.addEventListener('focus', () => {
    search_icon.style.display = 'none';
});

search_input.addEventListener('blur', () => {
    if(search_input.value === '') {
        search_icon.style.display = 'inline';
    }
});


// Select all status spans
const bookStatuses = document.querySelectorAll('.table-status');

// Loop through each one
bookStatuses.forEach(status => {
    if (status.innerText === 'Available') {
        status.style.backgroundColor = '#DBFCE7';
        status.style.border = '1px solid #7BF1A6';
        status.style.display = 'flex';
        status.style.justifyContent = 'center';
        status.style.borderRadius = '10px';
        status.style.padding = '2px 6px'; // optional: spacing
    }
    else if (status.innerText === 'Unavailable') {
        status.style.backgroundColor = '#FEA8A9';
        status.style.border = '1px solid #FF0A0A';
        status.style.display = 'flex';
        status.style.justifyContent = 'center';
        status.style.borderRadius = '10px';
        status.style.padding = '2px 6px';
    }
});
