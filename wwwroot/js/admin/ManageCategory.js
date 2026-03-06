document.addEventListener("DOMContentLoaded", function () {
    const categoryTabBtn = document.getElementById("categoryTab");
    const authorTabBtn = document.getElementById("authorTab");
    const categoryTableSection = document.getElementById(
        "category-table-section",
    );
    const authorTableSection = document.getElementById("author-table-section");
    const addCategoryBtn = document.getElementById("addCategoryBtn");
    const addAuthorBtn = document.getElementById("addAuthorBtn");
    const clearFilterBtn = document.querySelector(".clear-filter-btn");
    const tabInput = document.getElementById("tabInput");
    const categoryModal = new bootstrap.Modal(
        document.getElementById("categoryModal"),
    );
    const authorModal = new bootstrap.Modal(
        document.getElementById("authorModal"),
    );
    const categoryForm = document.getElementById("categoryForm");
    const authorForm = document.getElementById("authorForm");
    const categoryNameInput = document.getElementById("categoryName");
    const categoryImageInput = document.getElementById("categoryImage");
    const categoryDescriptionInput = document.getElementById(
        "categoryDescription",
    );
    const authorIdInput = document.getElementById("authorId");
    const authorNameInput = document.getElementById("authorName");
    const submitBtn = document.getElementById("submitBtn");
    const submitAuthorBtn = document.getElementById("submitAuthorBtn");
    const categoryModalTitle = document.getElementById("categoryModalTitle");
    const authorModalTitle = document.getElementById("authorModalTitle");
    const imagePreview = document.getElementById("imagePreview");
    const previewContainer = document.getElementById("previewContainer");

    let isEditMode = false;
    let editingCategoryName = null;
    let isAuthorEditMode = false;
    let editingAuthorId = null;
    let activeTab = "category";

    function setActiveTab(tabName) {
        const isAuthorTab = tabName === "author";
        activeTab = isAuthorTab ? "author" : "category";

        if (categoryTabBtn) {
            categoryTabBtn.classList.toggle("active", !isAuthorTab);
        }
        if (authorTabBtn) {
            authorTabBtn.classList.toggle("active", isAuthorTab);
        }
        if (categoryTableSection) {
            categoryTableSection.classList.toggle("hidden", isAuthorTab);
        }
        if (authorTableSection) {
            authorTableSection.classList.toggle("hidden", !isAuthorTab);
        }
        if (addCategoryBtn) {
            addCategoryBtn.classList.toggle("hidden", isAuthorTab);
        }
        if (addAuthorBtn) {
            addAuthorBtn.classList.toggle("hidden", !isAuthorTab);
        }
        if (tabInput) {
            tabInput.value = activeTab;
        }
        if (clearFilterBtn) {
            const baseHref =
                clearFilterBtn.dataset.baseHref ||
                clearFilterBtn.getAttribute("href") ||
                "/admin/managecategory";
            clearFilterBtn.dataset.baseHref = baseHref.split("?")[0];
            clearFilterBtn.href =
                activeTab === "author"
                    ? `${clearFilterBtn.dataset.baseHref}?tab=author`
                    : clearFilterBtn.dataset.baseHref;
        }
    }

    function reloadWithActiveTab() {
        const url = new URL(window.location.href);
        if (activeTab === "author") {
            url.searchParams.set("tab", "author");
        } else {
            url.searchParams.delete("tab");
        }
        window.location.href = url.toString();
    }

    function getAlertColor(icon) {
        switch (icon) {
            case "success":
                return "#16a34a";
            case "warning":
                return "#f59e0b";
            case "error":
                return "#dc2626";
            default:
                return "#2563eb";
        }
    }

    async function showAlert(title, text, icon = "info") {
        if (window.Swal && typeof window.Swal.fire === "function") {
            return window.Swal.fire({
                title,
                text,
                icon,
                confirmButtonColor: getAlertColor(icon),
            });
        }

        window.alert(`${title}\n\n${text}`);
        return Promise.resolve();
    }

    async function showConfirm(title, text, confirmButtonText) {
        if (window.Swal && typeof window.Swal.fire === "function") {
            const result = await window.Swal.fire({
                title,
                text,
                icon: "warning",
                showCancelButton: true,
                confirmButtonColor: "#dc2626",
                cancelButtonColor: "#6b7280",
                confirmButtonText,
                cancelButtonText: "Cancel",
            });

            return result.isConfirmed;
        }

        return window.confirm(text);
    }

    if (categoryTabBtn) {
        categoryTabBtn.addEventListener("click", function () {
            setActiveTab("category");
        });
    }

    if (authorTabBtn) {
        authorTabBtn.addEventListener("click", function () {
            setActiveTab("author");
        });
    }

    const initialTab = (
        new URLSearchParams(window.location.search).get("tab") || ""
    ).toLowerCase();
    setActiveTab(initialTab === "author" ? "author" : "category");

    // Add category button
    addCategoryBtn.addEventListener("click", function () {
        isEditMode = false;
        categoryForm.reset();
        previewContainer.style.display = "none";
        categoryNameInput.disabled = false;
        categoryModalTitle.textContent = "Add Category";
        submitBtn.textContent = "Add Category";
        categoryModal.show();
    });

    // Add author button
    addAuthorBtn.addEventListener("click", function () {
        isAuthorEditMode = false;
        editingAuthorId = null;
        authorForm.reset();
        authorIdInput.value = "";
        authorModalTitle.textContent = "Add Author";
        submitAuthorBtn.textContent = "Add Author";
        authorModal.show();
    });

    // Edit author button
    document.querySelectorAll(".edit-author-btn").forEach((btn) => {
        btn.addEventListener("click", function () {
            const authorId = this.getAttribute("data-author-id");
            const authorName = this.getAttribute("data-author-name");

            isAuthorEditMode = true;
            editingAuthorId = authorId;
            authorForm.reset();
            authorIdInput.value = authorId;
            authorNameInput.value = authorName;
            authorModalTitle.textContent = `Edit Author: ${authorName}`;
            submitAuthorBtn.textContent = "Update Author";
            authorModal.show();
        });
    });

    // Edit category button
    document.querySelectorAll(".edit-category-btn").forEach((btn) => {
        btn.addEventListener("click", function () {
            const categoryName = this.getAttribute("data-category-name");
            isEditMode = true;
            editingCategoryName = categoryName;
            categoryNameInput.value = categoryName;
            categoryNameInput.disabled = true;
            categoryDescriptionInput.value = "";
            categoryImageInput.value = "";
            previewContainer.style.display = "none";
            categoryModalTitle.textContent = `Edit Category: ${categoryName}`;
            submitBtn.textContent = "Update Category";
            categoryModal.show();
        });
    });

    // Author form submission
    authorForm.addEventListener("submit", async function (e) {
        e.preventDefault();

        const authorName = authorNameInput.value.trim();
        if (!authorName) {
            await showAlert(
                "Missing Author Name",
                "Author name is required.",
                "warning",
            );
            return;
        }

        const formData = new FormData();
        if (isAuthorEditMode && editingAuthorId) {
            formData.append("authorId", editingAuthorId);
        }
        formData.append("name", authorName);

        submitAuthorBtn.disabled = true;

        try {
            const response = await fetch(
                isAuthorEditMode
                    ? "/admin/manageauthor/update"
                    : "/admin/manageauthor/create",
                {
                    method: "POST",
                    body: formData,
                },
            );

            const result = await response.json();

            if (response.ok && result.success) {
                await showAlert(
                    "Success",
                    result.message || "Author saved successfully.",
                    "success",
                );
                authorModal.hide();
                authorForm.reset();
                reloadWithActiveTab();
            } else {
                await showAlert(
                    "Error",
                    result.message || "Failed to save author.",
                    "error",
                );
            }
        } catch (error) {
            await showAlert(
                "Error",
                "Failed to save author. Please try again.",
                "error",
            );
            console.error("Error:", error);
        } finally {
            submitAuthorBtn.disabled = false;
        }
    });

    // Delete author
    document.querySelectorAll(".delete-author-btn").forEach((btn) => {
        btn.addEventListener("click", async function () {
            const authorId = this.getAttribute("data-author-id");
            const authorName = this.getAttribute("data-author-name");

            const isConfirmed = await showConfirm(
                "Delete Author?",
                `Are you sure you want to delete "${authorName}"? Related books will be moved to "Unknown Author".`,
                "Yes, delete author",
            );

            if (!isConfirmed) {
                return;
            }

            this.disabled = true;

            try {
                const response = await fetch("/admin/manageauthor/delete", {
                    method: "POST",
                    headers: {
                        "Content-Type": "application/json",
                    },
                    body: JSON.stringify({ authorId: Number(authorId) }),
                });

                const result = await response.json();

                if (response.ok && result.success) {
                    await showAlert(
                        "Deleted",
                        result.message || "Author deleted successfully.",
                        "success",
                    );
                    reloadWithActiveTab();
                } else {
                    await showAlert(
                        "Delete Failed",
                        result.message || "Failed to delete author.",
                        "error",
                    );
                }
            } catch (error) {
                await showAlert(
                    "Network Error",
                    "Failed to delete author. Please try again.",
                    "error",
                );
                console.error("Error:", error);
            } finally {
                this.disabled = false;
            }
        });
    });

    // Image preview
    categoryImageInput.addEventListener("change", function (e) {
        const file = e.target.files[0];
        if (file) {
            const reader = new FileReader();
            reader.onload = function (event) {
                imagePreview.src = event.target.result;
                previewContainer.style.display = "block";
            };
            reader.readAsDataURL(file);
        }
    });

    // Form submission
    categoryForm.addEventListener("submit", async function (e) {
        e.preventDefault();

        const categoryName = categoryNameInput.value.trim();
        if (!categoryName) {
            await showAlert(
                "Missing Category Name",
                "Category name is required.",
                "warning",
            );
            return;
        }

        const formData = new FormData();

        if (isEditMode) {
            formData.append("oldName", editingCategoryName);
            formData.append("newName", categoryName);
        } else {
            formData.append("name", categoryName);
        }

        if (categoryImageInput.files.length > 0) {
            formData.append("imageFile", categoryImageInput.files[0]);
        }

        if (categoryDescriptionInput.value.trim()) {
            formData.append(
                "description",
                categoryDescriptionInput.value.trim(),
            );
        }

        submitBtn.disabled = true;

        try {
            const url = isEditMode
                ? "/admin/managecategory/update"
                : "/admin/managecategory/create";

            const response = await fetch(url, {
                method: "POST",
                body: formData,
            });

            const result = await response.json();

            if (response.ok && result.success) {
                await showAlert(
                    "Success",
                    result.message || "Category saved successfully.",
                    "success",
                );
                categoryModal.hide();
                categoryForm.reset();
                reloadWithActiveTab();
            } else {
                await showAlert(
                    "Error",
                    result.message || "An error occurred.",
                    "error",
                );
            }
        } catch (error) {
            await showAlert(
                "Error",
                "Failed to save category. Please try again.",
                "error",
            );
            console.error("Error:", error);
        } finally {
            submitBtn.disabled = false;
        }
    });

    // Delete category
    document.querySelectorAll(".delete-category-btn").forEach((btn) => {
        btn.addEventListener("click", async function () {
            const categoryName = this.getAttribute("data-category-name");

            const isConfirmed = await showConfirm(
                "Delete Category?",
                `Are you sure you want to delete "${categoryName}"? Books in this category will be moved to "Uncategorized".`,
                "Yes, delete category",
            );

            if (!isConfirmed) {
                return;
            }

            this.disabled = true;

            try {
                const response = await fetch("/admin/managecategory/delete", {
                    method: "POST",
                    headers: {
                        "Content-Type": "application/json",
                    },
                    body: JSON.stringify({ name: categoryName }),
                });

                const result = await response.json();

                if (response.ok && result.success) {
                    await showAlert(
                        "Deleted",
                        result.message || "Category deleted successfully.",
                        "success",
                    );
                    reloadWithActiveTab();
                } else {
                    await showAlert(
                        "Delete Failed",
                        result.message || "An error occurred.",
                        "error",
                    );
                }
            } catch (error) {
                await showAlert(
                    "Network Error",
                    "Failed to delete category. Please try again.",
                    "error",
                );
                console.error("Error:", error);
            } finally {
                this.disabled = false;
            }
        });
    });
});
