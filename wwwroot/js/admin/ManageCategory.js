document.addEventListener('DOMContentLoaded', function () {
    const categoryModal = new bootstrap.Modal(document.getElementById('categoryModal'));
    const authorModal = new bootstrap.Modal(document.getElementById('authorModal'));
    const categoryForm = document.getElementById('categoryForm');
    const authorForm = document.getElementById('authorForm');
    const categoryNameInput = document.getElementById('categoryName');
    const categoryImageInput = document.getElementById('categoryImage');
    const categoryDescriptionInput = document.getElementById('categoryDescription');
    const authorIdInput = document.getElementById('authorId');
    const authorNameInput = document.getElementById('authorName');
    const submitBtn = document.getElementById('submitBtn');
    const submitAuthorBtn = document.getElementById('submitAuthorBtn');
    const categoryModalTitle = document.getElementById('categoryModalTitle');
    const authorModalTitle = document.getElementById('authorModalTitle');
    const imagePreview = document.getElementById('imagePreview');
    const previewContainer = document.getElementById('previewContainer');

    let isEditMode = false;
    let editingCategoryName = null;
    let isAuthorEditMode = false;
    let editingAuthorId = null;

    // Add category button
    document.getElementById('addCategoryBtn').addEventListener('click', function () {
        isEditMode = false;
        categoryForm.reset();
        previewContainer.style.display = 'none';
        categoryNameInput.disabled = false;
        categoryModalTitle.textContent = 'Add Category';
        submitBtn.textContent = 'Add Category';
        categoryModal.show();
    });

    // Add author button
    document.getElementById('addAuthorBtn').addEventListener('click', function () {
        isAuthorEditMode = false;
        editingAuthorId = null;
        authorForm.reset();
        authorIdInput.value = '';
        authorModalTitle.textContent = 'Add Author';
        submitAuthorBtn.textContent = 'Add Author';
        authorModal.show();
    });

    // Edit author button
    document.querySelectorAll('.edit-author-btn').forEach(btn => {
        btn.addEventListener('click', function () {
            const authorId = this.getAttribute('data-author-id');
            const authorName = this.getAttribute('data-author-name');

            isAuthorEditMode = true;
            editingAuthorId = authorId;
            authorForm.reset();
            authorIdInput.value = authorId;
            authorNameInput.value = authorName;
            authorModalTitle.textContent = `Edit Author: ${authorName}`;
            submitAuthorBtn.textContent = 'Update Author';
            authorModal.show();
        });
    });

    // Edit category button
    document.querySelectorAll('.edit-category-btn').forEach(btn => {
        btn.addEventListener('click', function () {
            const categoryName = this.getAttribute('data-category-name');
            isEditMode = true;
            editingCategoryName = categoryName;
            categoryNameInput.value = categoryName;
            categoryNameInput.disabled = true;
            categoryDescriptionInput.value = '';
            categoryImageInput.value = '';
            previewContainer.style.display = 'none';
            categoryModalTitle.textContent = `Edit Category: ${categoryName}`;
            submitBtn.textContent = 'Update Category';
            categoryModal.show();
        });
    });

    // Author form submission
    authorForm.addEventListener('submit', async function (e) {
        e.preventDefault();

        const authorName = authorNameInput.value.trim();
        if (!authorName) {
            showAlert('Error', 'Author name is required.', 'danger');
            return;
        }

        const formData = new FormData();
        if (isAuthorEditMode && editingAuthorId) {
            formData.append('authorId', editingAuthorId);
        }
        formData.append('name', authorName);

        try {
            const response = await fetch(isAuthorEditMode ? '/admin/manageauthor/update' : '/admin/manageauthor/create', {
                method: 'POST',
                body: formData
            });

            const result = await response.json();

            if (response.ok && result.success) {
                showAlert('Success', result.message, 'success');
                authorModal.hide();
                authorForm.reset();
                setTimeout(() => {
                    location.reload();
                }, 1200);
            } else {
                showAlert('Error', result.message || 'Failed to save author.', 'danger');
            }
        } catch (error) {
            showAlert('Error', 'Failed to save author. Please try again.', 'danger');
            console.error('Error:', error);
        }
    });

    // Delete author
    document.querySelectorAll('.delete-author-btn').forEach(btn => {
        btn.addEventListener('click', async function () {
            const authorId = this.getAttribute('data-author-id');
            const authorName = this.getAttribute('data-author-name');

            if (confirm(`Are you sure you want to delete the author "${authorName}"? Related books will be moved to "Unknown Author".`)) {
                try {
                    const response = await fetch('/admin/manageauthor/delete', {
                        method: 'POST',
                        headers: {
                            'Content-Type': 'application/json'
                        },
                        body: JSON.stringify({ authorId: Number(authorId) })
                    });

                    const result = await response.json();

                    if (response.ok && result.success) {
                        showAlert('Success', result.message, 'success');
                        setTimeout(() => {
                            location.reload();
                        }, 1200);
                    } else {
                        showAlert('Error', result.message || 'Failed to delete author.', 'danger');
                    }
                } catch (error) {
                    showAlert('Error', 'Failed to delete author. Please try again.', 'danger');
                    console.error('Error:', error);
                }
            }
        });
    });

    // Image preview
    categoryImageInput.addEventListener('change', function (e) {
        const file = e.target.files[0];
        if (file) {
            const reader = new FileReader();
            reader.onload = function (event) {
                imagePreview.src = event.target.result;
                previewContainer.style.display = 'block';
            };
            reader.readAsDataURL(file);
        }
    });

    // Form submission
    categoryForm.addEventListener('submit', async function (e) {
        e.preventDefault();

        const formData = new FormData();
        
        if (isEditMode) {
            formData.append('oldName', editingCategoryName);
            formData.append('newName', categoryNameInput.value.trim());
        } else {
            formData.append('name', categoryNameInput.value.trim());
        }

        if (categoryImageInput.files.length > 0) {
            formData.append('imageFile', categoryImageInput.files[0]);
        }

        if (categoryDescriptionInput.value.trim()) {
            formData.append('description', categoryDescriptionInput.value.trim());
        }

        try {
            const url = isEditMode 
                ? '/admin/managecategory/update' 
                : '/admin/managecategory/create';
            
            const response = await fetch(url, {
                method: 'POST',
                body: formData
            });

            const result = await response.json();

            if (response.ok && result.success) {
                showAlert('Success', result.message, 'success');
                categoryModal.hide();
                categoryForm.reset();
                setTimeout(() => {
                    location.reload();
                }, 1500);
            } else {
                showAlert('Error', result.message || 'An error occurred.', 'danger');
            }
        } catch (error) {
            showAlert('Error', 'Failed to save category. Please try again.', 'danger');
            console.error('Error:', error);
        }
    });

    // Delete category
    document.querySelectorAll('.delete-category-btn').forEach(btn => {
        btn.addEventListener('click', async function () {
            const categoryName = this.getAttribute('data-category-name');
            
            if (confirm(`Are you sure you want to delete the category "${categoryName}"? Books in this category will be moved to "Uncategorized".`)) {
                try {
                    const response = await fetch('/admin/managecategory/delete', {
                        method: 'POST',
                        headers: {
                            'Content-Type': 'application/json'
                        },
                        body: JSON.stringify({ name: categoryName })
                    });

                    const result = await response.json();

                    if (response.ok && result.success) {
                        showAlert('Success', result.message, 'success');
                        setTimeout(() => {
                            location.reload();
                        }, 1500);
                    } else {
                        showAlert('Error', result.message || 'An error occurred.', 'danger');
                    }
                } catch (error) {
                    showAlert('Error', 'Failed to delete category. Please try again.', 'danger');
                    console.error('Error:', error);
                }
            }
        });
    });

    function showAlert(title, message, type) {
        const alertDiv = document.createElement('div');
        alertDiv.className = `alert alert-${type} alert-dismissible fade show`;
        alertDiv.setAttribute('role', 'alert');
        alertDiv.innerHTML = `
            <strong>${title}:</strong> ${message}
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        `;
        
        const container = document.querySelector('.dashboard-page');
        container.insertBefore(alertDiv, container.firstChild);
        
        setTimeout(() => {
            alertDiv.remove();
        }, 5000);
    }
});
