$(document).ready(function() {
    initializeSearchIcon();
    styleBorrowingStatuses();
    initializeCustomSelects();
    styleReservationStatuses();
    initializeAddBorrowingButton();
    initializeUpdateBorrowingButton();
    initializeUpdateReservationButton();
    initializeProcessBorrowingButton();
    initializeApproveReservationButton();
    initializeRejectReservationButton();
    initializeEditBorrowingButton();
    initializeEditReservationButton();
});

function initializeSearchIcon() {
    const search_borrowing_input = $('#search-borrowing-input');
    const search_borrowing_icon = $('#search-borrowing-icon');
    const search_reservation_input = $('#search-reservation-input');
    const search_reservation_icon = $('#search-reservation-icon');

    // Borrowing search
    search_borrowing_input.on('focus', function () {
        search_borrowing_icon.hide();
    });

    search_borrowing_input.on('blur', function () {
        if (search_borrowing_input.val().trim() === '') {
            search_borrowing_icon.show();
        }
    });

    // Reservation search
    search_reservation_input.on('focus', function () {
        search_reservation_icon.hide();
    });

    search_reservation_input.on('blur', function () {
        if (search_reservation_input.val().trim() === '') {
            search_reservation_icon.show();
        }
    });
}

function styleBorrowingStatuses() {
    const borrowingStatues = $('.table-status');

    borrowingStatues.each(function() {
        const status = $(this);

        if (status.text() === 'Active') {
            status.css({
                'backgroundColor': '#DBFCE7',
                'border': '1px solid #7BF1A6',
                'display': 'flex',
                'justifyContent': 'center',
                'borderRadius': '10px',
                'padding': '2px 6px'
            });
        }
        else if (status.text() === 'Overdue') {
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

function styleReservationStatuses() {
    const reservationStatuses = $('.reservation-table-status');

    reservationStatuses.each(function() {
        const status = $(this);

        if (status.text() === 'Approved') {
            status.css({
                'backgroundColor': '#DBFCE7',
                'border': '1px solid #7BF1A6',
                'display': 'flex',
                'justifyContent': 'center',
                'borderRadius': '10px',
                'padding': '2px 6px'
            });
        }
        else if (status.text() === 'Rejected') {
            status.css({
                'backgroundColor': '#FEA8A9',
                'border': '1px solid #FF0A0A',
                'display': 'flex',
                'justifyContent': 'center',
                'borderRadius': '10px',
                'padding': '2px 6px'
            });
        }
        else if (status.text() === 'Pending') {
            status.css({
                'backgroundColor': '#FFBE7E',
                'border': '1px solid #D7650F',
                'display': 'flex',
                'justifyContent': 'center',
                'borderRadius': '10px',
                'padding': '2px 6px'
            });
        }
    });
}

function initializeCustomSelects() {
    $('.custom-select-wrapper select').on('mousedown', function() {
        $(this).parent().addClass('active');
    });

    $('.custom-select-wrapper select').on('blur change', function() {
        $(this).parent().removeClass('active');
    });
}

// Bootstrap 5 compatible - uses Bootstrap's Modal instance
function showSuccessAndCloseModal(message, modalId, formId) {
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
        }
    });
}

// ============= BORROWING FUNCTIONS =============

// Extract borrowing data from table row
function extractBorrowingDataFromRow(row) {
    return {
        rowIndex: row.find('th').text(),
        username: row.find('td').eq(0).text().trim(),
        bookTitle: row.find('td').eq(1).text().trim(),
        borrowDate: row.find('td').eq(2).text().trim(),
        dueDate: row.find('td').eq(3).text().trim(),
        status: row.find('td').eq(4).find('.table-status').text().trim()
    };
}

// Populate edit borrowing modal with data
function populateEditBorrowingModal(borrowingData) {
    $('#edit-borrowing-usernameInput').val(borrowingData.username);
    $('#edit-borrowing-bookTitleInput').val(borrowingData.bookTitle);
    $('#edit-borrowing-borrowingDateInput').val(borrowingData.borrowDate);

    // Store row index for later use
    $('#updateBorrowingModal').data('rowIndex', borrowingData.rowIndex);
}

// Initialize edit borrowing button click handler
function initializeEditBorrowingButton() {
    $('.borrowing-book-table').on('click', '.edit-borrowing-btn', function() {
        var row = $(this).closest('tr');
        var borrowingData = extractBorrowingDataFromRow(row);
        populateEditBorrowingModal(borrowingData);
    });
}

// Get borrowing data from add form
function getAddBorrowingFormData() {
    return {
        username: $('#usernameInput').val().trim(),
        bookCode: $('#bookCodeSelect').val(),
        bookTitle: $('#bookTitleInput').val().trim(),
        borrowingDate: $('#borrowingDateInput').val()
    };
}

// Get borrowing data from edit form
function getEditBorrowingFormData() {
    return {
        username: $('#edit-borrowing-usernameInput').val().trim(),
        bookCode: $('#edit-borrowing-bookCodeSelect').val(),
        bookTitle: $('#edit-borrowing-bookTitleInput').val().trim(),
        borrowingDate: $('#edit-borrowing-borrowingDateInput').val()
    };
}

// Validate borrowing data
function validateBorrowingData(borrowingData) {
    if (!borrowingData.username || !borrowingData.bookCode ||
        !borrowingData.bookTitle || !borrowingData.borrowingDate) {
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

function initializeAddBorrowingButton() {
    $('#confirmBorrowing').on('click', function() {
        var borrowingData = getAddBorrowingFormData();

        if (!validateBorrowingData(borrowingData)) {
            return;
        }

        console.log('Borrowing Data:', borrowingData);

        showSuccessAndCloseModal(
            'Borrowing has been added successfully!',
            '#addNewBorrowingModal',
            '#addNewBorrowingForm'
        );
    });
}

function initializeUpdateBorrowingButton() {
    $('#updateBorrowing').on('click', function() {
        var borrowingData = getEditBorrowingFormData();

        if (!validateBorrowingData(borrowingData)) {
            return;
        }

        console.log('Updated Borrowing Data:', borrowingData);

        showSuccessAndCloseModal(
            'Borrowing has been updated successfully!',
            '#updateBorrowingModal',
            '#updateBorrowingForm'
        );
    });
}

function initializeProcessBorrowingButton() {
    $('.borrowing-book-table').on('click', '.process-borrowing-btn', function() {
        const row = $(this).closest('tr');
        const bookTitle = row.find('td').eq(1).text().trim();
        const username = row.find('td').eq(0).text().trim();

        Swal.fire({
            title: 'Process Return',
            text: `Mark "${bookTitle}" borrowed by ${username} as returned?`,
            icon: 'question',
            showCancelButton: true,
            confirmButtonText: 'Yes, return it',
            cancelButtonText: 'Cancel',
            confirmButtonColor: '#28a745',
            cancelButtonColor: '#6c757d'
        }).then(function(result) {
            if (result.isConfirmed) {
                // Here you would make an API call to process the return
                console.log('Processing return for:', {username, bookTitle});

                Swal.fire({
                    title: 'Returned!',
                    text: 'The book has been marked as returned.',
                    icon: 'success',
                    confirmButtonColor: '#28a745'
                });
            }
        });
    });
}

// ============= RESERVATION FUNCTIONS =============

// Extract reservation data from table row
function extractReservationDataFromRow(row) {
    return {
        rowIndex: row.find('th').text(),
        username: row.find('td').eq(0).text().trim(),
        bookTitle: row.find('td').eq(1).text().trim(),
        reservationDate: row.find('td').eq(2).text().trim(),
        status: row.find('td').eq(3).find('.reservation-table-status').text().trim()
    };
}

// Populate edit reservation modal with data
function populateEditReservationModal(reservationData) {
    $('#edit-reservation-usernameInput').val(reservationData.username);
    $('#edit-reservation-bookTitleInput').val(reservationData.bookTitle);
    $('#edit-reservation-reservationDateInput').val(reservationData.reservationDate);

    // Store row index for later use
    $('#updateReservationModal').data('rowIndex', reservationData.rowIndex);
}

// Initialize edit reservation button click handler
function initializeEditReservationButton() {
    $('.reservation-book-table').on('click', '.edit-reservation-btn', function() {
        var row = $(this).closest('tr');
        var reservationData = extractReservationDataFromRow(row);
        populateEditReservationModal(reservationData);
    });
}

// Get reservation data from edit form
function getEditReservationFormData() {
    return {
        username: $('#edit-reservation-usernameInput').val().trim(),
        bookCode: $('#edit-reservation-bookCodeSelect').val(),
        bookTitle: $('#edit-reservation-bookTitleInput').val().trim(),
        reservationDate: $('#edit-reservation-reservationDateInput').val()
    };
}

// Validate reservation data
function validateReservationData(reservationData) {
    if (!reservationData.username || !reservationData.bookCode ||
        !reservationData.bookTitle || !reservationData.reservationDate) {
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

function initializeUpdateReservationButton() {
    $('#updateReservation').on('click', function() {
        var reservationData = getEditReservationFormData();

        if (!validateReservationData(reservationData)) {
            return;
        }

        console.log('Updated Reservation Data:', reservationData);

        showSuccessAndCloseModal(
            'Reservation has been updated successfully!',
            '#updateReservationModal',
            '#updateReservationForm'
        );
    });
}

function initializeApproveReservationButton() {
    $('.reservation-book-table').on('click', '.approve-reservation-btn', function() {
        const row = $(this).closest('tr');
        const bookTitle = row.find('td').eq(1).text().trim();
        const username = row.find('td').eq(0).text().trim();

        Swal.fire({
            title: 'Approve Reservation',
            text: `Approve reservation for "${bookTitle}" by ${username}?`,
            icon: 'question',
            showCancelButton: true,
            confirmButtonText: 'Yes, approve it',
            cancelButtonText: 'Cancel',
            confirmButtonColor: '#28a745',
            cancelButtonColor: '#6c757d'
        }).then(function(result) {
            if (result.isConfirmed) {
                // Here you would make an API call to approve the reservation
                console.log('Approving reservation for:', {username, bookTitle});

                // Update status in the table
                row.find('.reservation-table-status').text('Approved');
                styleReservationStatuses();

                Swal.fire({
                    title: 'Approved!',
                    text: 'The reservation has been approved.',
                    icon: 'success',
                    confirmButtonColor: '#28a745'
                });
            }
        });
    });
}

function initializeRejectReservationButton() {
    $('.reservation-book-table').on('click', '.reject-reservation-btn', function() {
        const row = $(this).closest('tr');
        const bookTitle = row.find('td').eq(1).text().trim();
        const username = row.find('td').eq(0).text().trim();

        Swal.fire({
            title: 'Reject Reservation',
            text: `Reject reservation for "${bookTitle}" by ${username}?`,
            icon: 'warning',
            showCancelButton: true,
            confirmButtonText: 'Yes, reject it',
            cancelButtonText: 'Cancel',
            confirmButtonColor: '#dc3545',
            cancelButtonColor: '#6c757d'
        }).then(function(result) {
            if (result.isConfirmed) {
                // Here you would make an API call to reject the reservation
                console.log('Rejecting reservation for:', {username, bookTitle});

                // Update status in the table
                row.find('.reservation-table-status').text('Rejected');
                styleReservationStatuses();

                Swal.fire({
                    title: 'Rejected!',
                    text: 'The reservation has been rejected.',
                    icon: 'success',
                    confirmButtonColor: '#28a745'
                });
            }
        });
    });
}