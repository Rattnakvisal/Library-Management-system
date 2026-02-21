$(document).ready(function () {
    const page = $('#manageUsersPage');
    if (!page.length) {
        return;
    }

    const successMessage = page.data('alert-success');
    const errorMessage = page.data('alert-error');

    const normalizeTab = (tab) => (String(tab || '').toLowerCase() === 'staffs' || String(tab || '').toLowerCase() === 'staff') ? 'staffs' : 'students';
    const buildClearFilterUrl = (tab) => {
        const params = new URLSearchParams();
        const activeTab = normalizeTab(tab || $('#searchTabInput').val());
        const searchText = ($('#searchInput').val() || '').trim();

        params.set('tab', activeTab);
        if (searchText) {
            params.set('search', searchText);
        }

        return `?${params.toString()}`;
    };

    const updateClearFilterLinks = (tab) => {
        const href = buildClearFilterUrl(tab);
        $('#filterResetLink').attr('href', href);
        $('#clearFilterBtn').attr('href', href);
    };

    const setTab = (tab) => {
        const activeTab = normalizeTab(tab);

        $('.tab').removeClass('active');
        $('.table-container').addClass('hidden');
        $('.add-btn').addClass('hidden');

        if (activeTab === 'staffs') {
            $('#staffTab').addClass('active');
            $('#staff-table-section').removeClass('hidden');
            $('#addStaffBtn').removeClass('hidden');
        } else {
            $('#studentTab').addClass('active');
            $('#student-table-section').removeClass('hidden');
            $('#addStudentBtn').removeClass('hidden');
        }

        $('#searchTabInput').val(activeTab);
        $('#filterTabInput').val(activeTab);
        $('.return-tab-input').val(activeTab);
        updateClearFilterLinks(activeTab);
    };

    setTab(page.data('active-tab'));

    $('#staffTab').on('click', function () {
        setTab('staffs');
    });

    $('#studentTab').on('click', function () {
        setTab('students');
    });

    $('#searchInput').on('focus', function () {
        $('#searchIcon').hide();
        updateClearFilterLinks();
    }).on('input', function () {
        updateClearFilterLinks();
    }).on('blur', function () {
        if (!$(this).val()) {
            $('#searchIcon').show();
        }
        updateClearFilterLinks();
    });

    $('#editStudentModal').on('show.bs.modal', function (event) {
        const button = $(event.relatedTarget);
        const form = $('#formEditStudent');

        form.find('input[name="UserId"]').val(button.attr('data-user-id') || '');
        form.find('input[name="UserCode"]').val(button.attr('data-user-code') || '');
        form.find('input[name="FullName"]').val(button.attr('data-full-name') || '');
        form.find('input[name="Email"]').val(button.attr('data-email') || '');
        form.find('input[name="PhoneNumber"]').val(button.attr('data-phone') || '');

        const genderText = String(button.attr('data-gender') || '').toLowerCase();
        const gender = genderText === 'female' ? 'F' : 'M';
        form.find('select[name="Gender"]').val(gender);
    });

    $('#editStaffModal').on('show.bs.modal', function (event) {
        const button = $(event.relatedTarget);
        const form = $('#formEditStaff');

        form.find('input[name="UserId"]').val(button.attr('data-user-id') || '');
        form.find('input[name="UserCode"]').val(button.attr('data-user-code') || '');
        form.find('input[name="FullName"]').val(button.attr('data-full-name') || '');
        form.find('input[name="Email"]').val(button.attr('data-email') || '');
        form.find('input[name="PhoneNumber"]').val(button.attr('data-phone') || '');

        const genderText = String(button.attr('data-gender') || '').toLowerCase();
        const roleText = String(button.attr('data-role') || '').toLowerCase();
        const gender = genderText === 'female' ? 'F' : 'M';
        const role = roleText === 'librarian' ? 'Librarian' : 'Admin';

        form.find('select[name="Gender"]').val(gender);
        form.find('select[name="Role"]').val(role);
    });

    $('.delete-user-form').on('submit', function (event) {
        event.preventDefault();

        const form = this;
        const userName = $(form).data('user-name') || 'this user';

        Swal.fire({
            title: 'Are you sure?',
            text: `You are about to delete ${userName}.`,
            icon: 'warning',
            showCancelButton: true,
            confirmButtonColor: '#dc3545',
            cancelButtonColor: '#6c757d',
            confirmButtonText: 'Yes, delete it!'
        }).then((result) => {
            if (result.isConfirmed) {
                form.submit();
            }
        });
    });

    if (successMessage) {
        Swal.fire({
            icon: 'success',
            title: 'Success',
            text: successMessage,
            timer: 1800,
            showConfirmButton: false
        });
    }

    if (errorMessage) {
        Swal.fire({
            icon: 'error',
            title: 'Action Failed',
            text: errorMessage,
            confirmButtonColor: '#264A73'
        });
    }
});
