$(document).ready(function () {
    initializeSearchIcon();
    initializeDeleteFeedbackConfirmation();
});

function initializeSearchIcon() {
    const searchFeedbackInput = $("#search-feedback-input");
    const searchFeedbackIcon = $("#search-feedback-icon");
    const bookReviewInput = $("#book-review-input");
    const bookReviewIcon = $("#book-review-icon");

    searchFeedbackInput.on("focus", function () {
        searchFeedbackIcon.hide();
    });

    searchFeedbackInput.on("blur", function () {
        if (searchFeedbackInput.val().trim() === "") {
            searchFeedbackIcon.show();
        }
    });

    bookReviewInput.on("focus", function () {
        bookReviewIcon.hide();
    });

    bookReviewInput.on("blur", function () {
        if (bookReviewInput.val().trim() === "") {
            bookReviewIcon.show();
        }
    });
}

function initializeDeleteFeedbackConfirmation() {
    $(document).on("submit", ".delete-feedback-form", function (event) {
        const form = this;

        if (form.dataset.swalConfirmed === "1") {
            return;
        }

        event.preventDefault();

        const feedbackEmail = form.dataset.feedbackEmail || "this user";

        Swal.fire({
            icon: "warning",
            title: "Delete Feedback?",
            text: `Delete message from ${feedbackEmail}?`,
            showCancelButton: true,
            confirmButtonText: "Yes, delete",
            cancelButtonText: "Cancel",
            confirmButtonColor: "#dc2626",
            cancelButtonColor: "#64748b",
            reverseButtons: true,
        }).then((result) => {
            if (!result.isConfirmed) {
                return;
            }

            form.dataset.swalConfirmed = "1";
            form.submit();
        });
    });
}
