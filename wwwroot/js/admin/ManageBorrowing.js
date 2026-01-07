$(document).ready(function() {
    initializeSearchIcon();
    styleBorrowingStatuses();
    initializeCustomSelects();
    styleReservationStatuses()
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