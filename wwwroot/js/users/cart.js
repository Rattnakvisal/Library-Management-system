
document.addEventListener('DOMContentLoaded', function () {
    showReservationDecisionAlert();

    const groups = document.querySelectorAll('.inventory-group');

    groups.forEach(function (group) {
        const masterTick = group.querySelector('.master-tick');
        const itemTicks = group.querySelectorAll('.cart-item-checkbox:not(:disabled)');

        if (!masterTick || itemTicks.length === 0) {
            return;
        }

        masterTick.addEventListener('change', function () {
            itemTicks.forEach(function (checkbox) {
                checkbox.checked = masterTick.checked;
            });
        });

        itemTicks.forEach(function (checkbox) {
            checkbox.addEventListener('change', function () {
                const allChecked = Array.from(itemTicks).every(function (item) {
                    return item.checked;
                });
                masterTick.checked = allChecked;
            });
        });
    });
});

function showReservationDecisionAlert() {
    const meta = document.getElementById('reservation-alert-meta');
    if (!meta || typeof Swal === 'undefined') {
        return;
    }

    const approved = Number(meta.dataset.approved || 0);
    const rejected = Number(meta.dataset.rejected || 0);
    const version = String(meta.dataset.version || '');
    const hasDecision = approved + rejected > 0;

    if (!hasDecision || !version) {
        return;
    }

    const storageKey = 'library_reservation_alert_seen_version';
    const seenVersion = localStorage.getItem(storageKey) || '';
    if (seenVersion === version) {
        return;
    }

    const parts = [];
    if (approved > 0) {
        parts.push(`${approved} reservation(s) approved`);
    }
    if (rejected > 0) {
        parts.push(`${rejected} reservation(s) rejected`);
    }

    Swal.fire({
        title: 'Reservation Update',
        text: `${parts.join(' and ')}. Please check your cart status.`,
        icon: rejected > 0 ? 'warning' : 'success',
        confirmButtonColor: '#12345a'
    });

    localStorage.setItem(storageKey, version);
}
