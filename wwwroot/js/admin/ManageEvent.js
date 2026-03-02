$(document).ready(function () {
    initializeAddEventButton();
    initializeUpdateEventButton();
    initializeEditEventButton();
    initializeDeleteEventButton();
});

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
    const response = await fetch(url, { method: 'POST' });
    const payload = await response.json().catch(() => ({}));
    if (!response.ok || !payload.success) {
        throw new Error(payload.message || 'Request failed.');
    }

    return payload;
}

function extractEventDataFromRow(row) {
    return {
        id: (row.data('event-id') || '').toString().trim(),
        eventName: (row.data('event-name') || '').toString().trim(),
        description: (row.data('description') || '').toString().trim(),
        location: (row.data('location') || '').toString().trim(),
        startDate: (row.data('start-date') || '').toString().trim(),
        endDate: (row.data('end-date') || '').toString().trim()
    };
}

function populateEditEventModal(eventData) {
    $('#editEventIdInput').val(eventData.id);
    $('#edit-eventNameInput').val(eventData.eventName);
    $('#edit-locationInput').val(eventData.location);
    $('#edit-startDateInput').val(eventData.startDate);
    $('#edit-endDateInput').val(eventData.endDate);
    $('#edit-descriptionTextarea').val(eventData.description);
}

function initializeEditEventButton() {
    $('.event-table').on('click', '.edit-event-btn', function () {
        const row = $(this).closest('tr');
        const eventData = extractEventDataFromRow(row);
        populateEditEventModal(eventData);
    });
}

function getAddEventFormData() {
    return {
        eventName: $('#eventNameInput').val().trim(),
        location: $('#locationInput').val().trim(),
        startDate: $('#startDateInput').val(),
        endDate: $('#endDateInput').val(),
        description: $('#descriptionTextarea').val().trim(),
        eventImage: $('#eventImageInput')[0].files[0]
    };
}

function getEditEventFormData() {
    return {
        id: ($('#editEventIdInput').val() || '').toString().trim(),
        eventName: $('#edit-eventNameInput').val().trim(),
        location: $('#edit-locationInput').val().trim(),
        startDate: $('#edit-startDateInput').val(),
        endDate: $('#edit-endDateInput').val(),
        description: $('#edit-descriptionTextarea').val().trim(),
        eventImage: $('#edit-eventImageInput')[0].files[0]
    };
}

function validateEventData(eventData) {
    if (!eventData.eventName || !eventData.location ||
        !eventData.startDate || !eventData.endDate || !eventData.description) {
        showAlert('Missing Information', 'Please fill in all required fields.', 'warning', '#f59e0b');
        return false;
    }

    if (new Date(eventData.startDate) > new Date(eventData.endDate)) {
        showAlert('Invalid Date Range', 'End date must be after or equal to start date.', 'error', '#dc2626');
        return false;
    }

    return true;
}

function hideModal(modalId) {
    const element = document.querySelector(modalId);
    const modal = bootstrap.Modal.getInstance(element);
    if (modal) {
        modal.hide();
    }
}

function buildEventFormData(eventData) {
    const formData = new FormData();
    formData.append('eventName', eventData.eventName);
    formData.append('location', eventData.location);
    formData.append('startDate', eventData.startDate);
    formData.append('endDate', eventData.endDate);
    formData.append('description', eventData.description);

    if (eventData.eventImage) {
        formData.append('eventImage', eventData.eventImage);
    }

    return formData;
}

function initializeAddEventButton() {
    $('#addEventBtn').on('click', async function () {
        const eventData = getAddEventFormData();
        if (!validateEventData(eventData)) {
            return;
        }

        try {
            const payload = await postForm('/admin/manageevent/add', buildEventFormData(eventData));
            await showAlert('Success', payload.message || 'Event has been added successfully.', 'success', '#16a34a');
            hideModal('#addEventModal');
            $('#addEventForm')[0].reset();
            window.location.reload();
        } catch (error) {
            await showAlert('Add Failed', error.message, 'error', '#dc2626');
        }
    });
}

function initializeUpdateEventButton() {
    $('#updateEventBtn').on('click', async function () {
        const eventData = getEditEventFormData();
        if (!eventData.id) {
            await showAlert('Missing Information', 'Could not find event id.', 'warning', '#f59e0b');
            return;
        }

        if (!validateEventData(eventData)) {
            return;
        }

        try {
            const payload = await postForm(`/admin/manageevent/update/${eventData.id}`, buildEventFormData(eventData));
            await showAlert('Success', payload.message || 'Event has been updated successfully.', 'success', '#16a34a');
            hideModal('#editEventModal');
            $('#editEventForm')[0].reset();
            window.location.reload();
        } catch (error) {
            await showAlert('Update Failed', error.message, 'error', '#dc2626');
        }
    });
}

function initializeDeleteEventButton() {
    $('.event-table').on('click', '.delete-event-btn', function () {
        const row = $(this).closest('tr');
        const id = ($(this).data('event-id') || '').toString().trim();
        const eventName = (row.data('event-name') || '').toString().trim();
        const startDate = (row.data('start-date') || '').toString().trim();

        if (!id) {
            showAlert('Missing Id', 'Could not find event id.', 'error', '#dc2626');
            return;
        }

        Swal.fire({
            title: 'Delete Event?',
            text: `Delete "${eventName}" scheduled on ${startDate}?`,
            icon: 'warning',
            showCancelButton: true,
            confirmButtonColor: '#dc2626',
            cancelButtonColor: '#64748b',
            confirmButtonText: 'Yes, delete it',
            cancelButtonText: 'Cancel'
        }).then(async function (result) {
            if (!result.isConfirmed) {
                return;
            }

            try {
                const payload = await postNoBody(`/admin/manageevent/delete/${id}`);
                await showAlert('Deleted', payload.message || 'The event has been deleted.', 'success', '#16a34a');
                window.location.reload();
            } catch (error) {
                await showAlert('Delete Failed', error.message, 'error', '#dc2626');
            }
        });
    });
}
