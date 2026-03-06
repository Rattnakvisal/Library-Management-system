$(document).ready(function () {
    initializeSearchIcon();
    styleBookStatuses();
    initializeCustomSelects();
    initializeCategoryControls();
    initializeAuthorControls();
    initializeEditButton();
    initializeAddBookButton();
    initializeUpdateBookButton();
    initializeDeleteBookButton();
    openDeepLinkAction();
});

// Handle search icon visibility
function initializeSearchIcon() {
    const search_input = $(".search-input");
    const search_icon = $(".search-icon");

    search_input.on("focus", function () {
        search_icon.hide();
    });

    search_input.on("blur", function () {
        if (search_input.val() === "") {
            search_icon.show();
        }
    });
}

// Style status badges based on their text
function styleBookStatuses() {
    const bookStatuses = $(".table-status");
    const stylesByStatus = {
        available: {
            backgroundColor: "#DBFCE7",
            border: "1px solid #7BF1A6",
        },
        unavailable: {
            backgroundColor: "#FEA8A9",
            border: "1px solid #FF0A0A",
        },
        borrowed: {
            backgroundColor: "#FFF4D6",
            border: "1px solid #F5C34B",
        },
        reserved: {
            backgroundColor: "#E0ECFF",
            border: "1px solid #6EA8FE",
        },
        maintenance: {
            backgroundColor: "#E9ECEF",
            border: "1px solid #ADB5BD",
        },
    };

    bookStatuses.each(function () {
        const status = $(this);
        const normalizedStatus = status.text().toString().trim().toLowerCase();
        const style = stylesByStatus[normalizedStatus];

        if (style) {
            status.css({
                backgroundColor: style.backgroundColor,
                border: style.border,
                display: "flex",
                justifyContent: "center",
                borderRadius: "10px",
                padding: "2px 6px",
            });
        }
    });
}

// Initialize custom select dropdown styling
function initializeCustomSelects() {
    $(".custom-select-wrapper select").on("mousedown", function () {
        $(this).parent().addClass("active");
    });

    $(".custom-select-wrapper select").on("blur change", function () {
        $(this).parent().removeClass("active");
    });
}

function getSelectedCategoryName() {
    const value = ($("#categorySelect").val() || "").toString().trim();
    if (!value || value === "__new__") {
        return "";
    }
    return value;
}

function getSelectedAuthorId() {
    const value = ($("#authorSelect").val() || "").toString().trim();
    if (!value || value === "__new__") {
        return 0;
    }

    const parsed = Number(value);
    return Number.isInteger(parsed) && parsed > 0 ? parsed : 0;
}

function getSelectedEditCategoryName() {
    return ($("#edit-categorySelect").val() || "").toString().trim();
}

function upsertCategoryOption(categoryName) {
    const select = $("#categorySelect");
    const normalized = categoryName.trim().toLowerCase();
    const existing = select.find("option").filter(function () {
        return (
            ($(this).val() || "").toString().trim().toLowerCase() === normalized
        );
    });

    if (existing.length > 0) {
        select.val(existing.first().val());
        return;
    }

    $("<option>", { value: categoryName, text: categoryName }).insertBefore(
        select.find('option[value="__new__"]'),
    );
    select.val(categoryName);
}

function upsertEditCategoryOption(categoryName) {
    const select = $("#edit-categorySelect");
    if (!select.length) {
        return;
    }
    if (!categoryName || !categoryName.trim()) {
        select.val("");
        return;
    }

    const normalized = categoryName.trim().toLowerCase();
    const existing = select.find("option").filter(function () {
        return (
            ($(this).val() || "").toString().trim().toLowerCase() === normalized
        );
    });

    if (existing.length > 0) {
        select.val(existing.first().val());
        return;
    }

    $("<option>", { value: categoryName, text: categoryName }).appendTo(select);
    select.val(categoryName);
}

async function requestCreateCategory(name) {
    const formData = new FormData();
    formData.append("name", name);

    return fetch("/admin/managecategory/create", {
        method: "POST",
        body: formData,
    });
}

async function requestCreateAuthor(name) {
    const formData = new FormData();
    formData.append("name", name);

    return fetch("/admin/manageauthor/create", {
        method: "POST",
        body: formData,
    });
}

function initializeCategoryControls() {
    const categorySelect = $("#categorySelect");
    const newCategoryGroup = $("#newCategoryGroup");
    const newCategoryInput = $("#newCategoryInput");

    if (categorySelect.length === 0) {
        return;
    }

    function toggleAddCategoryInput() {
        if (categorySelect.val() === "__new__") {
            newCategoryGroup.removeClass("d-none");
            newCategoryInput.trigger("focus");
        } else {
            newCategoryGroup.addClass("d-none");
        }
    }

    categorySelect.on("change", toggleAddCategoryInput);

    $("#showAddCategoryBtn").on("click", function () {
        categorySelect.val("__new__");
        toggleAddCategoryInput();
    });

    $("#addCategoryOptionBtn").on("click", async function () {
        const newCategoryName = newCategoryInput.val().trim();

        if (!newCategoryName) {
            Swal.fire({
                title: "Missing Category Name",
                text: "Please type a category name.",
                icon: "warning",
                confirmButtonColor: "#ffc107",
            });
            return;
        }

        const saveButton = $("#addCategoryOptionBtn");
        saveButton.prop("disabled", true);

        try {
            const response = await requestCreateCategory(newCategoryName);
            const payload = await response.json().catch(() => ({}));

            if (!response.ok || !payload.success) {
                if (
                    (payload.message || "")
                        .toLowerCase()
                        .includes("already exists")
                ) {
                    upsertCategoryOption(newCategoryName);
                    newCategoryInput.val("");
                    newCategoryGroup.addClass("d-none");
                    return;
                }
                Swal.fire({
                    title: "Add Category Failed",
                    text: payload.message || "Unable to add category.",
                    icon: "error",
                    confirmButtonColor: "#dc3545",
                });
                return;
            }

            upsertCategoryOption(newCategoryName);
            newCategoryInput.val("");
            newCategoryGroup.addClass("d-none");
            Swal.fire({
                title: "Category Added",
                text: `Category \"${newCategoryName}\" has been added.`,
                icon: "success",
                confirmButtonColor: "#28a745",
            });
        } catch (error) {
            Swal.fire({
                title: "Network Error",
                text: "Could not connect to server. Please try again.",
                icon: "error",
                confirmButtonColor: "#dc3545",
            });
        } finally {
            saveButton.prop("disabled", false);
        }
    });
}

function upsertAuthorOption(authorId, authorName) {
    const addSelect = $("#authorSelect");
    const editSelect = $("#edit-authorSelect");
    const normalized = (authorName || "").toString().trim().toLowerCase();
    if (!normalized) {
        return;
    }

    const addExisting = addSelect.find("option").filter(function () {
        return (
            ($(this).text() || "").toString().trim().toLowerCase() ===
            normalized
        );
    });

    if (addExisting.length > 0) {
        addSelect.val(addExisting.first().val());
    } else {
        $("<option>", {
            value: String(authorId),
            text: authorName,
        }).insertBefore(addSelect.find('option[value="__new__"]'));
        addSelect.val(String(authorId));
    }

    const editExisting = editSelect.find("option").filter(function () {
        return (
            ($(this).text() || "").toString().trim().toLowerCase() ===
            normalized
        );
    });

    if (editExisting.length === 0) {
        $("<option>", { value: String(authorId), text: authorName }).appendTo(
            editSelect,
        );
    } else if (authorId) {
        editExisting.first().val(String(authorId));
    }
}

function getExistingAuthorValueByName(authorName) {
    const normalized = (authorName || "").toString().trim().toLowerCase();
    if (!normalized) {
        return "";
    }

    const existing = $("#authorSelect")
        .find("option")
        .filter(function () {
            return (
                ($(this).text() || "").toString().trim().toLowerCase() ===
                normalized
            );
        });

    if (existing.length === 0) {
        return "";
    }

    return (existing.first().val() || "").toString().trim();
}

function initializeAuthorControls() {
    const authorSelect = $("#authorSelect");
    const newAuthorGroup = $("#newAuthorGroup");
    const newAuthorInput = $("#newAuthorInput");

    if (authorSelect.length === 0) {
        return;
    }

    function toggleAddAuthorInput() {
        if (authorSelect.val() === "__new__") {
            newAuthorGroup.removeClass("d-none");
            newAuthorInput.trigger("focus");
        } else {
            newAuthorGroup.addClass("d-none");
        }
    }

    authorSelect.on("change", toggleAddAuthorInput);

    $("#showAddAuthorBtn").on("click", function () {
        authorSelect.val("__new__");
        toggleAddAuthorInput();
    });

    $("#addAuthorOptionBtn").on("click", async function () {
        const newAuthorName = newAuthorInput.val().trim();

        if (!newAuthorName) {
            Swal.fire({
                title: "Missing Author Name",
                text: "Please type an author name.",
                icon: "warning",
                confirmButtonColor: "#ffc107",
            });
            return;
        }

        const saveButton = $("#addAuthorOptionBtn");
        saveButton.prop("disabled", true);

        try {
            const response = await requestCreateAuthor(newAuthorName);
            const payload = await response.json().catch(() => ({}));

            if (!response.ok || !payload.success) {
                if (
                    (payload.message || "")
                        .toLowerCase()
                        .includes("already exists")
                ) {
                    const existingValue =
                        getExistingAuthorValueByName(newAuthorName);
                    if (existingValue && existingValue !== "__new__") {
                        authorSelect.val(existingValue);
                        newAuthorInput.val("");
                        newAuthorGroup.addClass("d-none");
                        return;
                    }

                    Swal.fire({
                        title: "Author Already Exists",
                        text: "Please reload the page to refresh author options.",
                        icon: "info",
                        confirmButtonColor: "#0d6efd",
                    });
                    return;
                }
                Swal.fire({
                    title: "Add Author Failed",
                    text: payload.message || "Unable to add author.",
                    icon: "error",
                    confirmButtonColor: "#dc3545",
                });
                return;
            }

            upsertAuthorOption(payload.authorId || 0, newAuthorName);
            newAuthorInput.val("");
            newAuthorGroup.addClass("d-none");
            Swal.fire({
                title: "Author Added",
                text: `Author \"${newAuthorName}\" has been added.`,
                icon: "success",
                confirmButtonColor: "#28a745",
            });
        } catch (error) {
            Swal.fire({
                title: "Network Error",
                text: "Could not connect to server. Please try again.",
                icon: "error",
                confirmButtonColor: "#dc3545",
            });
        } finally {
            saveButton.prop("disabled", false);
        }
    });
}
// Extract book data from table row
function extractBookDataFromRow(row) {
    const categoryText = (row.data("category-name") || "").toString().trim();
    const rawIsbn = (row.data("isbn") || "").toString().trim();
    const isbn = rawIsbn === "-" ? "" : rawIsbn;
    const rowStatus = (row.data("status") || "").toString().trim();
    return {
        bookId: row.data("book-id"),
        rowIndex: row.index(),
        bookCode: (row.data("book-code") || "").toString().trim(),
        authorId: Number(row.data("author-id") || 0),
        bookCover: row.find("td").eq(0).text().trim(),
        bookTitle: row.find("td").eq(1).find("div").first().text().trim(),
        categoryName: categoryText,
        isbn,
        pages: (row.data("pages") || "").toString().trim(),
        year: (row.data("year") || "").toString().trim(),
        quantity: (row.data("quantity") || row.find("td").eq(4).text().trim())
            .toString()
            .trim(),
        status:
            rowStatus ||
            row.find("td").eq(5).find(".table-status").text().trim(),
        rating: (row.data("rating") || "5").toString().trim(),
        description: (row.data("description") || "").toString().trim(),
    };
}

// Map status text to select dropdown values
function mapStatusToValue(status) {
    const statusMap = {
        available: "available",
        unavailable: "unavailable",
        borrowed: "borrowed",
        reserved: "reserved",
        maintenance: "maintenance",
    };
    if (!status) {
        return "available";
    }
    const normalized = status.toString().trim().toLowerCase();
    return statusMap[normalized] || normalized;
}

// Populate edit modal with book data
function populateEditModal(bookData) {
    $("#editBookIdInput").val(bookData.bookId || "");
    $("#edit-bookCodeInput").val(bookData.bookCode || "");
    $("#edit-bookTitleInput").val(bookData.bookTitle);
    upsertEditCategoryOption(bookData.categoryName || "");
    $("#edit-quantityInput").val(bookData.quantity);
    $("#edit-isbnInput").val(bookData.isbn);
    $("#edit-authorSelect").val(bookData.authorId || "");
    $("#edit-pagesInput").val(bookData.pages);
    $("#edit-yearInput").val(bookData.year);
    $("#edit-statusSelect").val(mapStatusToValue(bookData.status));
    $("#edit-ratingSelect").val(bookData.rating || "5");
    $("#edit-descriptionTextarea").val(bookData.description);

    // Store row index for later use
    $("#editBookModal").data("rowIndex", bookData.rowIndex);
}

// Initialize edit button click handler
function initializeEditButton() {
    $(".book-table").on("click", ".edit-book-btn", function () {
        var row = $(this).closest("tr");
        var bookData = extractBookDataFromRow(row);
        populateEditModal(bookData);
    });
}

// Get book data from add form
function getAddBookFormData() {
    return {
        bookCode: $("#bookCodeInput").val().trim(),
        bookTitle: $("#bookTitleInput").val().trim(),
        categoryName: getSelectedCategoryName(),
        quantity: $("#quantityInput").val().trim(),
        authorId: getSelectedAuthorId(),
        pages: $("#pagesInput").val().trim(),
        year: $("#yearInput").val().trim(),
        status: $("#statusSelect").val(),
        rating: $("#ratingSelect").val(),
        isbn: $("#isbnInput").val().trim(),
        description: $("#descriptionTextarea").val().trim(),
        bookImage: $("#bookImageInput")[0].files[0],
    };
}

function showMissingInformationAlert() {
    Swal.fire({
        title: "Missing Information!",
        text: "Please fill in all required fields marked with *",
        icon: "warning",
        confirmButtonText: "OK",
        confirmButtonColor: "#ffc107",
    });
}

function hasValidBookNumbers(bookData) {
    const quantity = Number(bookData.quantity);
    const pages = Number(bookData.pages);
    const year = Number(bookData.year);

    return (
        Number.isInteger(quantity) &&
        quantity >= 0 &&
        Number.isInteger(pages) &&
        pages > 0 &&
        Number.isInteger(year) &&
        year >= 1000 &&
        year <= 9999
    );
}

// Validate required add-book fields
function validateAddBookData(bookData) {
    const hasValidAuthorId =
        Number.isInteger(bookData.authorId) && bookData.authorId > 0;
    if (
        !bookData.bookCode ||
        !bookData.bookTitle ||
        !bookData.categoryName ||
        !bookData.quantity ||
        !hasValidAuthorId ||
        !bookData.pages ||
        !bookData.year ||
        !bookData.status ||
        !hasValidBookNumbers(bookData)
    ) {
        showMissingInformationAlert();
        return false;
    }
    return true;
}

// Validate required edit-book fields
function validateEditBookData(bookData) {
    const hasValidAuthorId =
        Number.isInteger(bookData.authorId) && bookData.authorId > 0;
    if (
        !bookData.bookId ||
        !bookData.bookCode ||
        !bookData.bookTitle ||
        !bookData.categoryName ||
        !bookData.quantity ||
        !hasValidAuthorId ||
        !bookData.pages ||
        !bookData.year ||
        !bookData.status ||
        !hasValidBookNumbers(bookData)
    ) {
        showMissingInformationAlert();
        return false;
    }
    return true;
}

// Bootstrap 5 compatible - Show success message and close modal
function showSuccessAndCloseModal(message, modalId, formId, onComplete) {
    Swal.fire({
        title: "Success!",
        text: message,
        icon: "success",
        confirmButtonText: "OK",
        confirmButtonColor: "#28a745",
    }).then(function (result) {
        if (result.isConfirmed) {
            // Bootstrap 5 way to hide modal
            const modalElement = document.querySelector(modalId);
            const modal = bootstrap.Modal.getInstance(modalElement);
            if (modal) {
                modal.hide();
            }
            $(formId)[0].reset();
            if ($("#categorySelect").length) {
                $("#categorySelect").val("");
                $("#newCategoryInput").val("");
                $("#newCategoryGroup").addClass("d-none");
            }
            if ($("#authorSelect").length) {
                $("#authorSelect").val("");
                $("#newAuthorInput").val("");
                $("#newAuthorGroup").addClass("d-none");
            }
            if (typeof onComplete === "function") {
                onComplete();
            }
        }
    });
}

async function submitNewBook(bookData) {
    const formData = new FormData();
    formData.append("bookCode", bookData.bookCode);
    formData.append("bookTitle", bookData.bookTitle);
    formData.append("authorId", String(bookData.authorId));
    formData.append("categoryName", bookData.categoryName);
    formData.append("isbn", bookData.isbn || "");
    formData.append("quantity", bookData.quantity || "0");
    formData.append("status", bookData.status);
    formData.append("pages", bookData.pages || "0");
    formData.append("year", bookData.year || "0");
    formData.append("description", bookData.description || "");
    formData.append("rating", bookData.rating || "5");

    if (bookData.bookImage) {
        formData.append("bookImage", bookData.bookImage);
    }

    return fetch("/admin/managebooks/add", {
        method: "POST",
        body: formData,
    });
}

async function submitUpdatedBook(bookData) {
    const formData = new FormData();
    formData.append("bookCode", bookData.bookCode);
    formData.append("bookTitle", bookData.bookTitle);
    formData.append("authorId", String(bookData.authorId));
    formData.append("categoryName", bookData.categoryName);
    formData.append("isbn", bookData.isbn || "");
    formData.append("quantity", bookData.quantity || "0");
    formData.append("status", bookData.status);
    formData.append("pages", bookData.pages || "0");
    formData.append("year", bookData.year || "0");
    formData.append("description", bookData.description || "");
    formData.append("rating", bookData.rating || "5");

    if (bookData.bookImage) {
        formData.append("bookImage", bookData.bookImage);
    }

    return fetch(`/admin/managebooks/update/${bookData.bookId}`, {
        method: "POST",
        body: formData,
    });
}

async function requestDeleteBook(bookId) {
    return fetch(`/admin/managebooks/delete/${bookId}`, {
        method: "POST",
    });
}

// Initialize add book button
function initializeAddBookButton() {
    $("#addBookBtn").on("click", async function () {
        var bookData = getAddBookFormData();

        if (!validateAddBookData(bookData)) {
            return;
        }

        const addButton = $("#addBookBtn");
        addButton.prop("disabled", true);

        try {
            const response = await submitNewBook(bookData);
            const result = await response.json().catch(() => ({}));

            if (!response.ok || !result.success) {
                Swal.fire({
                    title: "Add Book Failed",
                    text:
                        result.message ||
                        "Unable to save the book. Please try again.",
                    icon: "error",
                    confirmButtonColor: "#dc3545",
                });
                return;
            }

            showSuccessAndCloseModal(
                "Book has been added successfully! It will now appear on the user Category page.",
                "#addBookModal",
                "#bookForm",
                function () {
                    window.location.reload();
                },
            );
        } catch (error) {
            Swal.fire({
                title: "Network Error",
                text: "Could not connect to server. Please try again.",
                icon: "error",
                confirmButtonColor: "#dc3545",
            });
        } finally {
            addButton.prop("disabled", false);
        }
    });
}

// Get book data from edit form
function getEditBookFormData() {
    return {
        bookId: $("#editBookIdInput").val(),
        bookCode: $("#edit-bookCodeInput").val().trim(),
        bookTitle: $("#edit-bookTitleInput").val().trim(),
        categoryName: getSelectedEditCategoryName(),
        quantity: $("#edit-quantityInput").val().trim(),
        authorId: Number($("#edit-authorSelect").val() || 0),
        pages: $("#edit-pagesInput").val().trim(),
        year: $("#edit-yearInput").val().trim(),
        status: $("#edit-statusSelect").val(),
        rating: $("#edit-ratingSelect").val(),
        isbn: $("#edit-isbnInput").val().trim(),
        description: $("#edit-descriptionTextarea").val().trim(),
        bookImage: $("#edit-bookImageInput")[0].files[0],
    };
}

// Initialize update book button
function initializeUpdateBookButton() {
    $("#updateBookBtn").on("click", async function () {
        var bookData = getEditBookFormData();

        if (!validateEditBookData(bookData)) {
            return;
        }

        const updateButton = $("#updateBookBtn");
        updateButton.prop("disabled", true);

        try {
            const response = await submitUpdatedBook(bookData);
            const payload = await response.json().catch(() => ({}));

            if (!response.ok || !payload.success) {
                Swal.fire({
                    title: "Update Book Failed",
                    text: payload.message || "Unable to update this book.",
                    icon: "error",
                    confirmButtonColor: "#dc3545",
                });
                return;
            }

            showSuccessAndCloseModal(
                payload.message || "Book has been updated successfully!",
                "#editBookModal",
                "#editBookForm",
                function () {
                    window.location.reload();
                },
            );
        } catch (error) {
            Swal.fire({
                title: "Network Error",
                text: "Could not connect to server. Please try again.",
                icon: "error",
                confirmButtonColor: "#dc3545",
            });
        } finally {
            updateButton.prop("disabled", false);
        }
    });
}

// Initialize delete book button
function initializeDeleteBookButton() {
    $(".book-table").on("click", ".delete-book-btn", function (event) {
        event.preventDefault();
        const row = $(this).closest("tr");
        const bookId = $(this).data("book-id") || row.data("book-id");
        const bookTitle = row.find("td").eq(1).text().trim();

        Swal.fire({
            title: "Are you sure?",
            text: `You are about to delete "${bookTitle}". This action cannot be undone.`,
            icon: "warning",
            showCancelButton: true,
            confirmButtonColor: "#dc3545",
            cancelButtonColor: "#6c757d",
            confirmButtonText: "Yes, delete it",
            cancelButtonText: "Cancel",
        }).then(async (result) => {
            if (result.isConfirmed) {
                if (!bookId) {
                    Swal.fire({
                        title: "Delete Failed",
                        text: "Missing book id.",
                        icon: "error",
                        confirmButtonColor: "#dc3545",
                    });
                    return;
                }

                const deleteButton = $(this);
                deleteButton.prop("disabled", true);

                try {
                    const response = await requestDeleteBook(bookId);
                    const payload = await response.json().catch(() => ({}));

                    if (!response.ok || !payload.success) {
                        Swal.fire({
                            title: "Delete Failed",
                            text:
                                payload.message ||
                                "Unable to delete this book.",
                            icon: "error",
                            confirmButtonColor: "#dc3545",
                        });
                        return;
                    }

                    row.remove();

                    if ($(".book-table tbody tr").length === 0) {
                        $(".book-table tbody").append(
                            '<tr><td colspan="9" class="text-center text-muted py-4">No books found.</td></tr>',
                        );
                    }

                    Swal.fire({
                        title: "Deleted!",
                        text:
                            payload.message ||
                            "The book has been deleted successfully.",
                        icon: "success",
                        confirmButtonColor: "#28a745",
                    });
                } catch (error) {
                    Swal.fire({
                        title: "Network Error",
                        text: "Could not connect to server. Please try again.",
                        icon: "error",
                        confirmButtonColor: "#dc3545",
                    });
                } finally {
                    deleteButton.prop("disabled", false);
                }
            }
        });
    });
}

function openDeepLinkAction() {
    const params = new URLSearchParams(window.location.search);
    const action = (params.get("quickAction") || params.get("action") || "")
        .toString()
        .toLowerCase();
    if (action !== "add-book") {
        return;
    }

    const modalElement = document.getElementById("addBookModal");
    if (!modalElement || typeof bootstrap === "undefined") {
        return;
    }

    const modal = bootstrap.Modal.getOrCreateInstance(modalElement);
    modal.show();
}
