$(document).ready(function() {
    initializeSearchIcon();
    styleBookStatuses();
    initializeCustomSelects();
    initializeEditButton();
    initializeAddBookButton();
    initializeUpdateBookButton();
    initializeDeleteBookButton();
});

// Handle search icon visibility
function initializeSearchIcon() {
    const search_input = $('.search-input');
    const search_icon = $('.search-icon');

    search_input.on('focus', function() {
        search_icon.hide();
    });

    search_input.on('blur', function() {
        if(search_input.val() === '') {
            search_icon.show();
        }
    });
}

// Style status badges based on their text
function styleBookStatuses() {
    const bookStatuses = $('.table-status');

    bookStatuses.each(function() {
        const status = $(this);

        if (status.text() === 'Available') {
            status.css({
                'backgroundColor': '#DBFCE7',
                'border': '1px solid #7BF1A6',
                'display': 'flex',
                'justifyContent': 'center',
                'borderRadius': '10px',
                'padding': '2px 6px'
            });
        }
        else if (status.text() === 'Unavailable') {
            status.css({
                'backgroundColor': '#FEA8A9',
                'border': '1px solid #FF0A0A',
                'display': 'flex',
                'justifyContent': 'center',
                'borderRadius': '10px',
                'padding': '2px 6px'
            });
        }
    });
}

// Initialize custom select dropdown styling
function initializeCustomSelects() {
    $('.custom-select-wrapper select').on('mousedown', function() {
        $(this).parent().addClass('active');
    });

    $('.custom-select-wrapper select').on('blur change', function() {
        $(this).parent().removeClass('active');
    });
}

// Extract book data from table row
function extractBookDataFromRow(row) {
    return {
        rowIndex: row.find('th').text(),
        bookCode: row.find('td').eq(0).text().trim(),
        bookCover: row.find('td').eq(1).text(),
        bookTitle: row.find('td').eq(2).text().trim(),
        author: row.find('td').eq(3).text().trim(),
        isbn: row.find('td').eq(4).text().trim(),
        pages: row.find('td').eq(5).text().trim(),
        year: row.find('td').eq(6).text().trim(),
        quantity: row.find('td').eq(7).text().trim(),
        status: row.find('td').eq(8).find('.table-status').text().trim(),
        description: row.data('description') || ''
    };
}

// Map status text to select dropdown values
function mapStatusToValue(status) {
    const statusMap = {
        'Available': 'available',
        'Unavailable': 'unavailable',
        'Borrowed': 'borrowed',
        'Reserved': 'reserved',
        'Maintenance': 'maintenance'
    };
    return statusMap[status] || status.toLowerCase();
}

// Populate edit modal with book data
function populateEditModal(bookData) {
    $('#edit-bookCodeSelect').val(bookData.bookCode);
    $('#edit-bookTitleInput').val(bookData.bookTitle);
    $('#edit-quantityInput').val(bookData.quantity);
    $('#edit-isbnInput').val(bookData.isbn);
    $('#edit-authorInput').val(bookData.author);
    $('#edit-pagesInput').val(bookData.pages);
    $('#edit-yearInput').val(bookData.year);
    $('#edit-statusSelect').val(mapStatusToValue(bookData.status));
    $('#edit-descriptionTextarea').val(bookData.description);

    // Store row index for later use
    $('#editBookModal').data('rowIndex', bookData.rowIndex);
}

// Initialize edit button click handler
function initializeEditButton() {
    $('.book-table').on('click', '.edit-book-btn', function() {
        var row = $(this).closest('tr');
        var bookData = extractBookDataFromRow(row);
        populateEditModal(bookData);
    });
}

// Get book data from add form
function getAddBookFormData() {
    return {
        bookCode: $('#bookCodeSelect').val(),
        bookTitle: $('#bookTitleInput').val().trim(),
        quantity: $('#quantityInput').val().trim(),
        author: $('#authorInput').val().trim(),
        pages: $('#pagesInput').val().trim(),
        year: $('#yearInput').val().trim(),
        status: $('#statusSelect').val(),
        isbn: $('#isbnInput').val().trim(),
        description: $('#descriptionTextarea').val().trim(),
        bookImage: $('#bookImageInput')[0].files[0]
    };
}

// Validate required book fields
function validateBookData(bookData) {
    if (!bookData.bookCode || !bookData.bookTitle || !bookData.quantity ||
        !bookData.author || !bookData.pages || !bookData.year || !bookData.status) {
        Swal.fire({
            title: 'Missing Information!',
            text: 'Please fill in all required fields marked with *',
            icon: 'warning',
            confirmButtonText: 'OK',
            confirmButtonColor: '#ffc107'
        });
        return false;
    }
    return true;
}

// Show success message and close modal
function showSuccessAndCloseModal(message, modalId, formId) {
    Swal.fire({
        title: 'Success!',
        text: message,
        icon: 'success',
        confirmButtonText: 'OK',
        confirmButtonColor: '#28a745'
    }).then(function(result) {
        if (result.isConfirmed) {
            $(modalId).modal('hide');
            $(formId)[0].reset();
        }
    });
}

// Initialize add book button
function initializeAddBookButton() {
    $('#addBookBtn').on('click', function() {
        var bookData = getAddBookFormData();

        if (!validateBookData(bookData)) {
            return;
        }

        console.log('Book Data:', bookData);

        showSuccessAndCloseModal(
            'Book has been added successfully!',
            '#addBookModal',
            '#bookForm'
        );
    });
}

// Get book data from edit form
function getEditBookFormData() {
    return {
        bookCode: $('#edit-bookCodeSelect').val(),
        bookTitle: $('#edit-bookTitleInput').val().trim(),
        quantity: $('#edit-quantityInput').val().trim(),
        author: $('#edit-authorInput').val().trim(),
        pages: $('#edit-pagesInput').val().trim(),
        year: $('#edit-yearInput').val().trim(),
        status: $('#edit-statusSelect').val(),
        isbn: $('#edit-isbnInput').val().trim(),
        description: $('#edit-descriptionTextarea').val().trim(),
        bookImage: $('#edit-bookImageInput')[0].files[0]
    };
}

// Initialize update book button
function initializeUpdateBookButton() {
    $('#updateBookBtn').on('click', function() {
        var bookData = getEditBookFormData();

        if (!validateBookData(bookData)) {
            return;
        }

        console.log('Updated Book Data:', bookData);

        showSuccessAndCloseModal(
            'Book has been updated successfully!',
            '#editBookModal',
            '#editBookForm'
        );
    });
}
// Initialize delete book button
function initializeDeleteBookButton() {
    $('.book-table').on('click', '.delete-book-btn', function () {
        const row = $(this).closest('tr');
        const bookTitle = row.find('td').eq(2).text().trim();

        Swal.fire({
            title: 'Are you sure?',
            text: `You are about to delete "${bookTitle}". This action cannot be undone.`,
            icon: 'warning',
            showCancelButton: true,
            confirmButtonColor: '#dc3545',
            cancelButtonColor: '#6c757d',
            confirmButtonText: 'Yes, delete it',
            cancelButtonText: 'Cancel'
        }).then((result) => {
            if (result.isConfirmed) {

                // Remove row from table
                row.remove();

                Swal.fire({
                    title: 'Deleted!',
                    text: 'The book has been deleted successfully.',
                    icon: 'success',
                    confirmButtonColor: '#28a745'
                });
            }
        });
    });
}
