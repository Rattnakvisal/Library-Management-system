$(document).ready(function() {
    initializeSearchIcon();
});

function initializeSearchIcon() {
    const search_feedback_input = $('#search-feedback-input');
    const search_feedback_icon = $('#search-feedback-icon');
    const book_review_input = $('#book-review-input');
    const book_review_icon = $('#book-review-icon');

    // Borrowing search
    search_feedback_input.on('focus', function () {
        search_feedback_icon.hide();
    });

    search_feedback_input.on('blur', function () {
        if (search_feedback_input.val().trim() === '') {
            search_feedback_icon.show();
        }
    });

    // Reservation search
    book_review_input.on('focus', function () {
        book_review_icon.hide();
    });

    book_review_input.on('blur', function () {
        if (book_review_input.val().trim() === '') {
            book_review_icon.show();
        }
    });
}