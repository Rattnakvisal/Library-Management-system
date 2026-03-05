$(document).ready(function () {
    initializeSearchIcon();
    initializeCustomSelects();
    initializeBookCodeBindings();
    initializeEditBorrowingButton();
    initializeEditReservationButton();
    initializeAddBorrowingButton();
    initializeUpdateBorrowingButton();
    initializeUpdateReservationButton();
    initializeProcessBorrowingButton();
    initializeMarkFinePaidButton();
    initializeApproveReservationButton();
    initializeRejectReservationButton();
});

function initializeSearchIcon() {
    const borrowingInput = $('#search-borrowing-input');
    const borrowingIcon = $('#search-borrowing-icon');
    const reservationInput = $('#search-reservation-input');
    const reservationIcon = $('#search-reservation-icon');

    borrowingInput.on('focus', function () {
        borrowingIcon.hide();
    });

    borrowingInput.on('blur', function () {
        if (!borrowingInput.val().trim()) {
            borrowingIcon.show();
        }
    });

    reservationInput.on('focus', function () {
        reservationIcon.hide();
    });

    reservationInput.on('blur', function () {
        if (!reservationInput.val().trim()) {
            reservationIcon.show();
        }
    });
}

function initializeCustomSelects() {
    $('.custom-select-wrapper select').on('mousedown', function () {
        $(this).parent().addClass('active');
    });

    $('.custom-select-wrapper select').on('blur change', function () {
        $(this).parent().removeClass('active');
    });
}

function bindCodeToTitle(selectId, inputId) {
    const select = $(selectId);
    const input = $(inputId);
    if (!select.length || !input.length) {
        return;
    }

    select.on('change', function () {
        const selected = select.find('option:selected');
        const title = (selected.data('title') || '').toString();
        input.val(title);
    });
}

function initializeBookCodeBindings() {
    bindCodeToTitle('#bookCodeSelect', '#bookTitleInput');
    bindCodeToTitle('#edit-borrowing-bookCodeSelect', '#edit-borrowing-bookTitleInput');
    bindCodeToTitle('#edit-reservation-bookCodeSelect', '#edit-reservation-bookTitleInput');
}

function upsertBookCodeOption(select, code, title) {
    if (!code) {
        return;
    }

    const existing = select.find('option').filter(function () {
        return ($(this).val() || '').toString().trim().toLowerCase() === code.trim().toLowerCase();
    });

    if (existing.length > 0) {
        select.val(existing.first().val());
        return;
    }

    $('<option>', {
        value: code,
        text: code,
        'data-title': title || ''
    }).appendTo(select);
    select.val(code);
}

function showAlert(title, text, icon, confirmButtonColor = '#1f4a73') {
    return Swal.fire({
        title,
        text,
        icon,
        confirmButtonColor
    });
}

async function postForm(url, formData) {
    const response = await fetch(url, {
        method: 'POST',
        body: formData
    });

    const payload = await response.json().catch(() => ({}));
    if (!response.ok || !payload.success) {
        throw new Error(payload.message || 'Request failed.');
    }

    return payload;
}

async function postNoBody(url) {
    const response = await fetch(url, {
        method: 'POST'
    });

    const payload = await response.json().catch(() => ({}));
    if (!response.ok || !payload.success) {
        throw new Error(payload.message || 'Request failed.');
    }

    return payload;
}

function getRowBorrowingData(row) {
    return {
        id: row.data('borrowing-id'),
        username: (row.data('username') || '').toString().trim(),
        bookCode: (row.data('book-code') || '').toString().trim(),
        bookTitle: (row.data('book-title') || '').toString().trim(),
        borrowDate: (row.data('borrow-date') || '').toString().trim()
    };
}

function getRowReservationData(row) {
    return {
        id: row.data('reservation-id'),
        username: (row.data('username') || '').toString().trim(),
        bookCode: (row.data('book-code') || '').toString().trim(),
        bookTitle: (row.data('book-title') || '').toString().trim(),
        reservationDate: (row.data('reservation-date') || '').toString().trim()
    };
}

function initializeEditBorrowingButton() {
    $('.borrowing-book-table').on('click', '.edit-borrowing-btn', function () {
        const row = $(this).closest('tr');
        const data = getRowBorrowingData(row);

        $('#editBorrowingIdInput').val(data.id || '');
        $('#edit-borrowing-usernameInput').val(data.username);

        const codeSelect = $('#edit-borrowing-bookCodeSelect');
        upsertBookCodeOption(codeSelect, data.bookCode, data.bookTitle);
        $('#edit-borrowing-bookTitleInput').val(data.bookTitle);
        $('#edit-borrowing-borrowingDateInput').val(data.borrowDate);
    });
}

function initializeEditReservationButton() {
    $('.reservation-book-table').on('click', '.edit-reservation-btn', function () {
        const row = $(this).closest('tr');
        const data = getRowReservationData(row);

        $('#editReservationIdInput').val(data.id || '');
        $('#edit-reservation-usernameInput').val(data.username);

        const codeSelect = $('#edit-reservation-bookCodeSelect');
        upsertBookCodeOption(codeSelect, data.bookCode, data.bookTitle);
        $('#edit-reservation-bookTitleInput').val(data.bookTitle);
        $('#edit-reservation-reservationDateInput').val(data.reservationDate);
    });
}

function initializeAddBorrowingButton() {
    $('#confirmBorrowing').on('click', async function () {
        const username = $('#usernameInput').val().trim();
        const bookCode = ($('#bookCodeSelect').val() || '').toString().trim();
        const borrowDate = $('#borrowingDateInput').val();

        if (!username || !bookCode) {
            await showAlert('Missing Information', 'Username and book code are required.', 'warning', '#f59e0b');
            return;
        }

        const formData = new FormData();
        formData.append('username', username);
        formData.append('bookCode', bookCode);
        if (borrowDate) {
            formData.append('borrowDate', borrowDate);
        }

        try {
            const payload = await postForm('/admin/manageborrowingbook/borrowing/create', formData);
            await showAlert('Success', payload.message || 'Borrowing created successfully.', 'success', '#16a34a');
            window.location.reload();
        } catch (error) {
            await showAlert('Create Failed', error.message, 'error', '#dc2626');
        }
    });
}

function initializeUpdateBorrowingButton() {
    $('#updateBorrowing').on('click', async function () {
        const id = ($('#editBorrowingIdInput').val() || '').toString().trim();
        const username = $('#edit-borrowing-usernameInput').val().trim();
        const bookCode = ($('#edit-borrowing-bookCodeSelect').val() || '').toString().trim();
        const borrowDate = $('#edit-borrowing-borrowingDateInput').val();

        if (!id || !username || !bookCode) {
            await showAlert('Missing Information', 'Username and book code are required.', 'warning', '#f59e0b');
            return;
        }

        const formData = new FormData();
        formData.append('username', username);
        formData.append('bookCode', bookCode);
        if (borrowDate) {
            formData.append('borrowDate', borrowDate);
        }

        try {
            const payload = await postForm(`/admin/manageborrowingbook/borrowing/update/${id}`, formData);
            await showAlert('Success', payload.message || 'Borrowing updated successfully.', 'success', '#16a34a');
            window.location.reload();
        } catch (error) {
            await showAlert('Update Failed', error.message, 'error', '#dc2626');
        }
    });
}

function initializeUpdateReservationButton() {
    $('#updateReservation').on('click', async function () {
        const id = ($('#editReservationIdInput').val() || '').toString().trim();
        const bookCode = ($('#edit-reservation-bookCodeSelect').val() || '').toString().trim();
        const reservationDate = $('#edit-reservation-reservationDateInput').val();

        if (!id || !bookCode) {
            await showAlert('Missing Information', 'Reservation id and book code are required.', 'warning', '#f59e0b');
            return;
        }

        const formData = new FormData();
        formData.append('bookCode', bookCode);
        if (reservationDate) {
            formData.append('reservationDate', reservationDate);
        }

        try {
            const payload = await postForm(`/admin/manageborrowingbook/reservation/update/${id}`, formData);
            await showAlert('Success', payload.message || 'Reservation updated.', 'success', '#16a34a');
            window.location.reload();
        } catch (error) {
            await showAlert('Update Failed', error.message, 'error', '#dc2626');
        }
    });
}

function initializeProcessBorrowingButton() {
    $('.borrowing-book-table').on('click', '.process-borrowing-btn', function () {
        const id = ($(this).data('borrowing-id') || '').toString().trim();
        const row = $(this).closest('tr');
        const title = row.data('book-title') || 'this book';
        const username = row.data('username') || 'this user';

        if (!id) {
            showAlert('Missing Id', 'Could not find borrowing id.', 'error', '#dc2626');
            return;
        }

        Swal.fire({
            title: 'Process Return',
            text: `Mark "${title}" borrowed by ${username} as returned?`,
            icon: 'question',
            showCancelButton: true,
            confirmButtonText: 'Yes, return it',
            cancelButtonText: 'Cancel',
            confirmButtonColor: '#16a34a',
            cancelButtonColor: '#64748b'
        }).then(async function (result) {
            if (!result.isConfirmed) {
                return;
            }

            try {
                const payload = await postNoBody(`/admin/manageborrowingbook/borrowing/return/${id}`);
                await showAlert('Returned', payload.message || 'Book marked as returned.', 'success', '#16a34a');
                window.location.reload();
            } catch (error) {
                await showAlert('Return Failed', error.message, 'error', '#dc2626');
            }
        });
    });
}

function initializeMarkFinePaidButton() {
    $('.borrowing-book-table').on('click', '.mark-fine-paid-btn', function () {
        const id = ($(this).data('borrowing-id') || '').toString().trim();
        const fineAmount = ($(this).data('fine-amount') || '0.00').toString().trim();

        if (!id) {
            showAlert('Missing Id', 'Could not find borrowing id.', 'error', '#dc2626');
            return;
        }

        Swal.fire({
            title: 'Mark Fine as Paid',
            text: `Confirm payment for fine amount $${fineAmount}?`,
            input: 'text',
            inputLabel: 'Remark (optional)',
            inputPlaceholder: 'Payment note',
            icon: 'question',
            showCancelButton: true,
            confirmButtonText: 'Mark Paid',
            cancelButtonText: 'Cancel',
            confirmButtonColor: '#16a34a',
            cancelButtonColor: '#64748b'
        }).then(async function (result) {
            if (!result.isConfirmed) {
                return;
            }

            try {
                const formData = new FormData();
                const remark = (result.value || '').toString().trim();
                if (remark) {
                    formData.append('remark', remark);
                }

                const payload = await postForm(`/admin/manageborrowingbook/fine/mark-paid/${id}`, formData);
                await showAlert('Fine Paid', payload.message || 'Fine marked as paid.', 'success', '#16a34a');
                window.location.reload();
            } catch (error) {
                await showAlert('Update Failed', error.message, 'error', '#dc2626');
            }
        });
    });
}

function initializeApproveReservationButton() {
    $('.reservation-book-table').on('click', '.approve-reservation-btn', function () {
        const id = ($(this).data('reservation-id') || '').toString().trim();
        if (!id) {
            showAlert('Missing Id', 'Could not find reservation id.', 'error', '#dc2626');
            return;
        }

        Swal.fire({
            title: 'Approve Reservation',
            text: 'Approve this reservation request?',
            icon: 'question',
            showCancelButton: true,
            confirmButtonText: 'Approve',
            cancelButtonText: 'Cancel',
            confirmButtonColor: '#16a34a',
            cancelButtonColor: '#64748b'
        }).then(async function (result) {
            if (!result.isConfirmed) {
                return;
            }

            try {
                const payload = await postNoBody(`/admin/manageborrowingbook/reservation/approve/${id}`);
                await showAlert('Approved', payload.message || 'Reservation approved.', 'success', '#16a34a');
                window.location.reload();
            } catch (error) {
                await showAlert('Approve Failed', error.message, 'error', '#dc2626');
            }
        });
    });
}

function initializeRejectReservationButton() {
    $('.reservation-book-table').on('click', '.reject-reservation-btn', function () {
        const id = ($(this).data('reservation-id') || '').toString().trim();
        if (!id) {
            showAlert('Missing Id', 'Could not find reservation id.', 'error', '#dc2626');
            return;
        }

        Swal.fire({
            title: 'Reject Reservation',
            text: 'Reject this reservation request?',
            input: 'text',
            inputLabel: 'Reason (optional)',
            inputPlaceholder: 'Enter rejection reason',
            icon: 'warning',
            showCancelButton: true,
            confirmButtonText: 'Reject',
            cancelButtonText: 'Cancel',
            confirmButtonColor: '#dc2626',
            cancelButtonColor: '#64748b'
        }).then(async function (result) {
            if (!result.isConfirmed) {
                return;
            }

            try {
                const formData = new FormData();
                const reason = (result.value || '').toString().trim();
                if (reason) {
                    formData.append('reason', reason);
                }

                const payload = await postForm(`/admin/manageborrowingbook/reservation/reject/${id}`, formData);
                await showAlert('Rejected', payload.message || 'Reservation rejected.', 'success', '#16a34a');
                window.location.reload();
            } catch (error) {
                await showAlert('Reject Failed', error.message, 'error', '#dc2626');
            }
        });
    });
}
