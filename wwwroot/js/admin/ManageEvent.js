$(document).ready(function() {
    initializeAddEventButton();
    initializeUpdateEventButton();
    initializeEditEventButton();
    initializeDeleteEventButton();
});

// Extract event data from table row
function extractEventDataFromRow(row) {
    return {
        rowIndex: row.find('th').text(),
        eventImage: row.find('td').eq(0).find('img').attr('src'),
        eventName: row.find('td').eq(1).text().trim(),
        description: row.find('td').eq(2).text().trim(),
        startDate: row.find('td').eq(3).text().trim(),
        endDate: row.find('td').eq(4).text().trim(),
        location: row.find('td').eq(5).text().trim(),
        fullDescription: row.data('description') || row.find('td').eq(2).text().trim()
    };
}

// Populate edit modal with event data
function populateEditEventModal(eventData) {
    $('#edit-eventNameInput').val(eventData.eventName);
    $('#edit-locationInput').val(eventData.location);
    $('#edit-startDateInput').val(eventData.startDate);
    $('#edit-endDateInput').val(eventData.endDate);
    $('#edit-descriptionTextarea').val(eventData.fullDescription);

    // Store row index for later use
    $('#editEventModal').data('rowIndex', eventData.rowIndex);
}

// Initialize edit event button click handler
function initializeEditEventButton() {
    $('.event-table').on('click', '.edit-event-btn', function() {
        var row = $(this).closest('tr');
        var eventData = extractEventDataFromRow(row);
        populateEditEventModal(eventData);
    });
}

// Get event data from add form
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

// Get event data from edit form
function getEditEventFormData() {
    return {
        eventName: $('#edit-eventNameInput').val().trim(),
        location: $('#edit-locationInput').val().trim(),
        startDate: $('#edit-startDateInput').val(),
        endDate: $('#edit-endDateInput').val(),
        description: $('#edit-descriptionTextarea').val().trim(),
        eventImage: $('#edit-eventImageInput')[0].files[0]
    };
}

// Validate event data
function validateEventData(eventData) {
    if (!eventData.eventName || !eventData.location ||
        !eventData.startDate || !eventData.endDate || !eventData.description) {
        Swal.fire({
            title: 'Missing Information!',
            text: 'Please fill in all required fields marked with *',
            icon: 'warning',
            confirmButtonText: 'OK',
            confirmButtonColor: '#ffc107'
        });
        return false;
    }

    // Validate date range
    if (new Date(eventData.startDate) > new Date(eventData.endDate)) {
        Swal.fire({
            title: 'Invalid Date Range!',
            text: 'End date must be after or equal to start date',
            icon: 'error',
            confirmButtonText: 'OK',
            confirmButtonColor: '#dc3545'
        });
        return false;
    }

    return true;
}

// Bootstrap 5 compatible - Show success message and close modal
function showSuccessAndCloseModal(message, modalId, formId) {
    Swal.fire({
        title: 'Success!',
        text: message,
        icon: 'success',
        confirmButtonText: 'OK',
        confirmButtonColor: '#28a745'
    }).then(function(result) {
        if (result.isConfirmed) {
            // Bootstrap 5 way to hide modal
            const modalElement = document.querySelector(modalId);
            const modal = bootstrap.Modal.getInstance(modalElement);
            if (modal) {
                modal.hide();
            }
            $(formId)[0].reset();
        }
    });
}

// Initialize add event button
function initializeAddEventButton() {
    $('#addEventBtn').on('click', function() {
        var eventData = getAddEventFormData();

        if (!validateEventData(eventData)) {
            return;
        }

        console.log('Event Data:', eventData);

        showSuccessAndCloseModal(
            'Event has been added successfully!',
            '#addEventModal',
            '#addEventForm'
        );
    });
}

// Initialize update event button
function initializeUpdateEventButton() {
    $('#updateEventBtn').on('click', function() {
        var eventData = getEditEventFormData();

        if (!validateEventData(eventData)) {
            return;
        }

        console.log('Updated Event Data:', eventData);

        showSuccessAndCloseModal(
            'Event has been updated successfully!',
            '#editEventModal',
            '#editEventForm'
        );
    });
}

// Initialize delete event button
function initializeDeleteEventButton() {
    $('.event-table').on('click', '.delete-event-btn', function () {
        const row = $(this).closest('tr');
        const eventName = row.find('td').eq(1).text().trim();
        const startDate = row.find('td').eq(3).text().trim();

        Swal.fire({
            title: 'Are you sure?',
            text: `You are about to delete "${eventName}" scheduled for ${startDate}. This action cannot be undone.`,
            icon: 'warning',
            showCancelButton: true,
            confirmButtonColor: '#dc3545',
            cancelButtonColor: '#6c757d',
            confirmButtonText: 'Yes, delete it',
            cancelButtonText: 'Cancel'
        }).then((result) => {
            if (result.isConfirmed) {
                // Here you would make an API call to delete the event
                console.log('Deleting event:', eventName);

                // Remove row from table
                row.remove();

                Swal.fire({
                    title: 'Deleted!',
                    text: 'The event has been deleted successfully.',
                    icon: 'success',
                    confirmButtonColor: '#28a745'
                });
            }
        });
    });
}