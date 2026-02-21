$(document).ready(function () {
    initializeAddCategory();
    initializeRenameCategory();
    initializeDeleteCategory();
});

async function requestCreateCategory(name) {
    return fetch('/admin/managecategory/create', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ name })
    });
}

async function requestRenameCategory(oldName, newName) {
    return fetch('/admin/managecategory/rename', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ oldName, newName })
    });
}

async function requestDeleteCategory(name) {
    return fetch('/admin/managecategory/delete', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ name })
    });
}

function initializeAddCategory() {
    $('#addCategoryBtn').on('click', async function () {
        const result = await Swal.fire({
            title: 'Add Category',
            input: 'text',
            inputLabel: 'Category name',
            inputPlaceholder: 'eg. Accounting',
            showCancelButton: true,
            confirmButtonText: 'Add',
            cancelButtonText: 'Cancel',
            inputValidator: (value) => {
                if (!value || !value.trim()) {
                    return 'Category name is required.';
                }
                return null;
            }
        });

        if (!result.isConfirmed) {
            return;
        }

        const response = await requestCreateCategory(result.value.trim());
        const payload = await response.json().catch(() => ({}));

        if (!response.ok || !payload.success) {
            Swal.fire('Add Failed', payload.message || 'Unable to add category.', 'error');
            return;
        }

        Swal.fire('Success', payload.message || 'Category added.', 'success')
            .then(() => window.location.reload());
    });
}

function initializeRenameCategory() {
    $('.category-table').on('click', '.rename-category-btn', async function () {
        const oldName = $(this).data('category-name');
        const result = await Swal.fire({
            title: 'Rename Category',
            input: 'text',
            inputValue: oldName,
            inputLabel: 'New category name',
            showCancelButton: true,
            confirmButtonText: 'Save',
            cancelButtonText: 'Cancel',
            inputValidator: (value) => {
                if (!value || !value.trim()) {
                    return 'Category name is required.';
                }
                return null;
            }
        });

        if (!result.isConfirmed) {
            return;
        }

        const newName = result.value.trim();
        const response = await requestRenameCategory(oldName, newName);
        const payload = await response.json().catch(() => ({}));

        if (!response.ok || !payload.success) {
            Swal.fire('Rename Failed', payload.message || 'Unable to rename category.', 'error');
            return;
        }

        Swal.fire('Success', payload.message || 'Category renamed.', 'success')
            .then(() => window.location.reload());
    });
}

function initializeDeleteCategory() {
    $('.category-table').on('click', '.delete-category-btn', async function () {
        const categoryName = $(this).data('category-name');

        const confirm = await Swal.fire({
            title: 'Delete Category?',
            text: `Books in "${categoryName}" will move to "Uncategorized".`,
            icon: 'warning',
            showCancelButton: true,
            confirmButtonColor: '#dc3545',
            confirmButtonText: 'Delete',
            cancelButtonText: 'Cancel'
        });

        if (!confirm.isConfirmed) {
            return;
        }

        const response = await requestDeleteCategory(categoryName);
        const payload = await response.json().catch(() => ({}));

        if (!response.ok || !payload.success) {
            Swal.fire('Delete Failed', payload.message || 'Unable to delete category.', 'error');
            return;
        }

        Swal.fire('Deleted', payload.message || 'Category deleted.', 'success')
            .then(() => window.location.reload());
    });
}
