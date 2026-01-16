$(document).ready(function() {
    // Tab Toggle Logic
    $('#staffTab').on('click', function() {
        $('.table-container, .add-btn').addClass('hidden');
        $('#staff-table-section, #addStaffBtn').removeClass('hidden');
        $('.tab').removeClass('active'); $(this).addClass('active');
    });
    $('#studentTab').on('click', function() {
        $('.table-container, .add-btn').addClass('hidden');
        $('#student-table-section, #addStudentBtn').removeClass('hidden');
        $('.tab').removeClass('active'); $(this).addClass('active');
    });

    // Search Icon focus toggle
    $('#searchInput').on('focus', () => $('#searchIcon').hide()).on('blur', function() {
        if (!$(this).val()) $('#searchIcon').show();
    });
});

// SWEETALERT FUNCTIONS
function handleSave(formId, modalId, isEdit = false) {
    const form = document.getElementById(formId);
    const formData = new FormData(form);
    let hasEmpty = false;

    // Check if required fields (any input/select) are empty
    for (let [key, value] of formData.entries()) {
        if (!value.trim()) { hasEmpty = true; break; }
    }

    if (hasEmpty) {
        Swal.fire({
            icon: 'warning',
            title: 'Incomplete Input',
            text: 'Please fill in all required fields!',
            confirmButtonColor: '#264A73'
        });
        return;
    }

    // Show Success Alert
    Swal.fire({
        icon: 'success',
        title: isEdit ? 'Updated!' : 'Saved!',
        text: isEdit ? 'User details updated successfully.' : 'New user added successfully.',
        showConfirmButton: false,
        timer: 1500
    });

    // Close Modal
    const modal = bootstrap.Modal.getInstance(document.getElementById(modalId));
    modal.hide();
    if (!isEdit) form.reset();
}

function handleDelete(name) {
    Swal.fire({
        title: 'Are you sure?',
        text: `You are about to delete ${name}.`,
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#dc3545',
        cancelButtonColor: '#6c757d',
        confirmButtonText: 'Yes, delete it!'
    }).then((result) => {
        if (result.isConfirmed) {
            Swal.fire('Deleted!', 'The user record has been removed.', 'success');
        }
    });
}