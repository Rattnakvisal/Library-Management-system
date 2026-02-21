 $(document).ready(function() {
    initializeSearchIcon();
    styleBookStatuses();
    initializeCustomSelects();
    initializeCategoryControls();
    initializeEditButton();
    initializeAddBookButton();
    initializeUpdateBookButton();
    initializeDeleteBookButton();
    openDeepLinkAction();
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

function getSelectedCategoryName() {
    const value = ($('#categorySelect').val() || '').toString().trim();
    if (!value || value === '__new__') {
        return '';
    }
    return value;
}

function getSelectedEditCategoryName() {
    return ($('#edit-categorySelect').val() || '').toString().trim();
}

function upsertCategoryOption(categoryName) {
    const select = $('#categorySelect');
    const normalized = categoryName.trim().toLowerCase();
    const existing = select.find('option').filter(function () {
        return ($(this).val() || '').toString().trim().toLowerCase() === normalized;
    });

    if (existing.length > 0) {
        select.val(existing.first().val());
        return;
    }

    $('<option>', { value: categoryName, text: categoryName })
        .insertBefore(select.find('option[value="__new__"]'));
    select.val(categoryName);
}

function upsertEditCategoryOption(categoryName) {
    const select = $('#edit-categorySelect');
    if (!select.length) {
        return;
    }
    if (!categoryName || !categoryName.trim()) {
        select.val('');
        return;
    }

    const normalized = categoryName.trim().toLowerCase();
    const existing = select.find('option').filter(function () {
        return ($(this).val() || '').toString().trim().toLowerCase() === normalized;
    });

    if (existing.length > 0) {
        select.val(existing.first().val());
        return;
    }

    $('<option>', { value: categoryName, text: categoryName }).appendTo(select);
    select.val(categoryName);
}

async function requestCreateCategory(name) {
    return fetch('/admin/managecategory/create', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ name })
    });
}

function initializeCategoryControls() {
    const categorySelect = $('#categorySelect');
    const newCategoryGroup = $('#newCategoryGroup');
    const newCategoryInput = $('#newCategoryInput');

    if (categorySelect.length === 0) {
        return;
    }

    function toggleAddCategoryInput() {
        if (categorySelect.val() === '__new__') {
            newCategoryGroup.removeClass('d-none');
            newCategoryInput.trigger('focus');
        } else {
            newCategoryGroup.addClass('d-none');
        }
    }

    categorySelect.on('change', toggleAddCategoryInput);

    $('#showAddCategoryBtn').on('click', function () {
        categorySelect.val('__new__');
        toggleAddCategoryInput();
    });

    $('#addCategoryOptionBtn').on('click', async function () {
        const newCategoryName = newCategoryInput.val().trim();

        if (!newCategoryName) {
            Swal.fire({
                title: 'Missing Category Name',
                text: 'Please type a category name.',
                icon: 'warning',
                confirmButtonColor: '#ffc107'
            });
            return;
        }

        const saveButton = $('#addCategoryOptionBtn');
        saveButton.prop('disabled', true);

        try {
            const response = await requestCreateCategory(newCategoryName);
            const payload = await response.json().catch(() => ({}));

            if (!response.ok || !payload.success) {
                if ((payload.message || '').toLowerCase().includes('already exists')) {
                    upsertCategoryOption(newCategoryName);
                    newCategoryInput.val('');
                    newCategoryGroup.addClass('d-none');
                    return;
                }
                Swal.fire({
                    title: 'Add Category Failed',
                    text: payload.message || 'Unable to add category.',
                    icon: 'error',
                    confirmButtonColor: '#dc3545'
                });
                return;
            }

            upsertCategoryOption(newCategoryName);
            newCategoryInput.val('');
            newCategoryGroup.addClass('d-none');
        } catch (error) {
            Swal.fire({
                title: 'Network Error',
                text: 'Could not connect to server. Please try again.',
                icon: 'error',
                confirmButtonColor: '#dc3545'
            });
        } finally {
            saveButton.prop('disabled', false);
        }
    });
}
// Extract book data from table row
function extractBookDataFromRow(row) {
    const categoryText = (row.data('category-name') || '').toString().trim();
    return {
        bookId: row.data('book-id'),
        rowIndex: row.index(),
        bookCode: '',
        bookCover: row.find('td').eq(0).text().trim(),
        bookTitle: row.find('td').eq(1).find('div').first().text().trim(),
        categoryName: categoryText,
        author: row.find('td').eq(2).text().trim(),
        isbn: row.find('td').eq(3).text().trim(),
        pages: '',
        year: '',
        quantity: row.find('td').eq(4).text().trim(),
        status: row.find('td').eq(5).find('.table-status').text().trim(),
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
    if (!status) {
        return 'available';
    }
    return statusMap[status] || status.toLowerCase();
}

// Populate edit modal with book data
function populateEditModal(bookData) {
    $('#editBookIdInput').val(bookData.bookId || '');
    $('#edit-bookCodeSelect').val(bookData.bookCode);
    $('#edit-bookTitleInput').val(bookData.bookTitle);
    upsertEditCategoryOption(bookData.categoryName || '');
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
        categoryName: getSelectedCategoryName(),
        quantity: $('#quantityInput').val().trim(),
        author: $('#authorInput').val().trim(),
        pages: $('#pagesInput').val().trim(),
        year: $('#yearInput').val().trim(),
        status: $('#statusSelect').val(),
        rating: $('#ratingSelect').val(),
        isbn: $('#isbnInput').val().trim(),
        description: $('#descriptionTextarea').val().trim(),
        bookImage: $('#bookImageInput')[0].files[0]
    };
}

function showMissingInformationAlert() {
    Swal.fire({
        title: 'Missing Information!',
        text: 'Please fill in all required fields marked with *',
        icon: 'warning',
        confirmButtonText: 'OK',
        confirmButtonColor: '#ffc107'
    });
}

// Validate required add-book fields
function validateAddBookData(bookData) {
    if (!bookData.bookCode || !bookData.bookTitle || !bookData.categoryName || !bookData.quantity ||
        !bookData.author || !bookData.pages || !bookData.year || !bookData.status) {
        showMissingInformationAlert();
        return false;
    }
    return true;
}

// Validate required edit-book fields
function validateEditBookData(bookData) {
    if (!bookData.bookId || !bookData.bookTitle || !bookData.categoryName || !bookData.author || !bookData.status) {
        showMissingInformationAlert();
        return false;
    }
    return true;
}

// Bootstrap 5 compatible - Show success message and close modal
function showSuccessAndCloseModal(message, modalId, formId, onComplete) {
    Swal.fire({
        title: 'Success!',
        text: message,
        icon: 'success',
        confirmButtonText: 'OK',
        confirmButtonColor: '#28a745'
    }).then(function(result) {
        if (result.isConfirmed) {
            // Bootstrap 5 way to hide modal
            const modalElement = document.querySelector(modalId);
            const modal = bootstrap.Modal.getInstance(modalElement);
            if (modal) {
                modal.hide();
            }
            $(formId)[0].reset();
            if ($('#categorySelect').length) {
                $('#categorySelect').val('');
                $('#newCategoryInput').val('');
                $('#newCategoryGroup').addClass('d-none');
            }
            if (typeof onComplete === 'function') {
                onComplete();
            }
        }
    });
}

async function submitNewBook(bookData) {
    const formData = new FormData();
    formData.append('bookCode', bookData.bookCode);
    formData.append('bookTitle', bookData.bookTitle);
    formData.append('author', bookData.author);
    formData.append('categoryName', bookData.categoryName);
    formData.append('isbn', bookData.isbn || '');
    formData.append('quantity', bookData.quantity || '0');
    formData.append('status', bookData.status);
    formData.append('pages', bookData.pages || '0');
    formData.append('year', bookData.year || '0');
    formData.append('description', bookData.description || '');
    formData.append('rating', bookData.rating || '5');

    if (bookData.bookImage) {
        formData.append('bookImage', bookData.bookImage);
    }

    return fetch('/admin/managebooks/add', {
        method: 'POST',
        body: formData
    });
}

async function submitUpdatedBook(bookData) {
    const formData = new FormData();
    formData.append('bookTitle', bookData.bookTitle);
    formData.append('author', bookData.author);
    formData.append('categoryName', bookData.categoryName);

    if (bookData.bookImage) {
        formData.append('bookImage', bookData.bookImage);
    }

    return fetch(`/admin/managebooks/update/${bookData.bookId}`, {
        method: 'POST',
        body: formData
    });
}

async function requestDeleteBook(bookId) {
    return fetch(`/admin/managebooks/delete/${bookId}`, {
        method: 'POST'
    });
}

// Initialize add book button
function initializeAddBookButton() {
    $('#addBookBtn').on('click', async function() {
        var bookData = getAddBookFormData();

        if (!validateAddBookData(bookData)) {
            return;
        }

        const addButton = $('#addBookBtn');
        addButton.prop('disabled', true);

        try {
            const response = await submitNewBook(bookData);
            const result = await response.json().catch(() => ({}));

            if (!response.ok || !result.success) {
                Swal.fire({
                    title: 'Add Book Failed',
                    text: result.message || 'Unable to save the book. Please try again.',
                    icon: 'error',
                    confirmButtonColor: '#dc3545'
                });
                return;
            }

            showSuccessAndCloseModal(
                'Book has been added successfully! It will now appear on the user Category page.',
                '#addBookModal',
                '#bookForm',
                function () { window.location.reload(); }
            );
        } catch (error) {
            Swal.fire({
                title: 'Network Error',
                text: 'Could not connect to server. Please try again.',
                icon: 'error',
                confirmButtonColor: '#dc3545'
            });
        } finally {
            addButton.prop('disabled', false);
        }
    });
}

// Get book data from edit form
function getEditBookFormData() {
    return {
        bookId: $('#editBookIdInput').val(),
        bookCode: $('#edit-bookCodeSelect').val(),
        bookTitle: $('#edit-bookTitleInput').val().trim(),
        categoryName: getSelectedEditCategoryName(),
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
    $('#updateBookBtn').on('click', async function() {
        var bookData = getEditBookFormData();

        if (!validateEditBookData(bookData)) {
            return;
        }

        const updateButton = $('#updateBookBtn');
        updateButton.prop('disabled', true);

        try {
            const response = await submitUpdatedBook(bookData);
            const payload = await response.json().catch(() => ({}));

            if (!response.ok || !payload.success) {
                Swal.fire({
                    title: 'Update Book Failed',
                    text: payload.message || 'Unable to update this book.',
                    icon: 'error',
                    confirmButtonColor: '#dc3545'
                });
                return;
            }

            showSuccessAndCloseModal(
                payload.message || 'Book has been updated successfully!',
                '#editBookModal',
                '#editBookForm',
                function () { window.location.reload(); }
            );
        } catch (error) {
            Swal.fire({
                title: 'Network Error',
                text: 'Could not connect to server. Please try again.',
                icon: 'error',
                confirmButtonColor: '#dc3545'
            });
        } finally {
            updateButton.prop('disabled', false);
        }
    });
}

// Initialize delete book button
function initializeDeleteBookButton() {
    $('.book-table').on('click', '.delete-book-btn', function (event) {
        event.preventDefault();
        const row = $(this).closest('tr');
        const bookId = $(this).data('book-id') || row.data('book-id');
        const bookTitle = row.find('td').eq(1).text().trim();

        Swal.fire({
            title: 'Are you sure?',
            text: `You are about to delete "${bookTitle}". This action cannot be undone.`,
            icon: 'warning',
            showCancelButton: true,
            confirmButtonColor: '#dc3545',
            cancelButtonColor: '#6c757d',
            confirmButtonText: 'Yes, delete it',
            cancelButtonText: 'Cancel'
        }).then(async (result) => {
            if (result.isConfirmed) {
                if (!bookId) {
                    Swal.fire({
                        title: 'Delete Failed',
                        text: 'Missing book id.',
                        icon: 'error',
                        confirmButtonColor: '#dc3545'
                    });
                    return;
                }

                const deleteButton = $(this);
                deleteButton.prop('disabled', true);

                try {
                    const response = await requestDeleteBook(bookId);
                    const payload = await response.json().catch(() => ({}));

                    if (!response.ok || !payload.success) {
                        Swal.fire({
                            title: 'Delete Failed',
                            text: payload.message || 'Unable to delete this book.',
                            icon: 'error',
                            confirmButtonColor: '#dc3545'
                        });
                        return;
                    }

                    row.remove();

                    if ($('.book-table tbody tr').length === 0) {
                        $('.book-table tbody').append(
                            '<tr><td colspan="9" class="text-center text-muted py-4">No books found.</td></tr>'
                        );
                    }

                    Swal.fire({
                        title: 'Deleted!',
                        text: payload.message || 'The book has been deleted successfully.',
                        icon: 'success',
                        confirmButtonColor: '#28a745'
                    });
                } catch (error) {
                    Swal.fire({
                        title: 'Network Error',
                        text: 'Could not connect to server. Please try again.',
                        icon: 'error',
                        confirmButtonColor: '#dc3545'
                    });
                } finally {
                    deleteButton.prop('disabled', false);
                }
            }
        });
    });
}

function openDeepLinkAction() {
    const params = new URLSearchParams(window.location.search);
    const action = (params.get('quickAction') || params.get('action') || '').toString().toLowerCase();
    if (action !== 'add-book') {
        return;
    }

    const modalElement = document.getElementById('addBookModal');
    if (!modalElement || typeof bootstrap === 'undefined') {
        return;
    }

    const modal = bootstrap.Modal.getOrCreateInstance(modalElement);
    modal.show();
}

