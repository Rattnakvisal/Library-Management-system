// Desktop sidebar collapse
(function () {
    const sidebar = document.getElementById("sidebar");
    const btn = document.getElementById("sidebarToggle");
    if (!sidebar || !btn) return;

    const storageKey = "adminSidebarCollapsed";
    const savedState = localStorage.getItem(storageKey);
    if (savedState === "1") {
        document.body.classList.add("sidebar-collapsed");
    }

    btn.setAttribute(
        "aria-pressed",
        document.body.classList.contains("sidebar-collapsed")
            ? "true"
            : "false",
    );

    btn.addEventListener("click", () => {
        const collapsed = document.body.classList.toggle("sidebar-collapsed");
        localStorage.setItem(storageKey, collapsed ? "1" : "0");
        btn.setAttribute("aria-pressed", collapsed ? "true" : "false");
    });
})();
// Topbar action dropdown behavior
(function () {
    const actionDropdowns = Array.from(
        document.querySelectorAll(".action-dropdown"),
    );
    if (!actionDropdowns.length) return;

    const closeOthers = (currentDropdown) => {
        actionDropdowns.forEach((dropdown) => {
            if (dropdown === currentDropdown) return;
            const toggle = dropdown.querySelector(
                '[data-bs-toggle="dropdown"]',
            );
            if (!toggle || !window.bootstrap) return;
            const instance = bootstrap.Dropdown.getOrCreateInstance(toggle);
            instance.hide();
        });
    };

    const syncMobilePanelState = () => {
        if (window.innerWidth > 991) {
            document.body.classList.remove("topbar-panel-open");
            return;
        }

        const hasOpen = actionDropdowns.some((dropdown) =>
            dropdown.classList.contains("show"),
        );
        document.body.classList.toggle("topbar-panel-open", hasOpen);
    };

    actionDropdowns.forEach((dropdown) => {
        dropdown.addEventListener("show.bs.dropdown", () =>
            closeOthers(dropdown),
        );
        dropdown.addEventListener("shown.bs.dropdown", syncMobilePanelState);
        dropdown.addEventListener("hidden.bs.dropdown", syncMobilePanelState);
    });

    window.addEventListener("resize", syncMobilePanelState);
})();

// Contact message actions + badge synchronization
(function () {
    const contactMenu = document.getElementById("contactMenu");
    if (!contactMenu) return;

    const topbarActions = document.getElementById("topbarActions");
    const notificationBadge = document.getElementById("notificationBadge");
    const contactBadge = document.getElementById("contactBadge");
    const contactUnreadText = document.getElementById("contactMenuUnreadText");
    const markAllButton = document.getElementById("markAllContactsReadBtn");
    let knownUnreadCount = Math.max(0, Number(contactBadge?.textContent || 0));

    const markContactItemAsRead = (item) => {
        if (!item) return;

        item.classList.remove("is-unread");
        item.classList.add("is-read");

        const markButton = item.querySelector(".mark-contact-read-btn");
        if (markButton) {
            markButton.remove();
        }

        const actions = item.querySelector(".contact-item-actions");
        if (actions && !actions.querySelector(".contact-read-label")) {
            const label = document.createElement("span");
            label.className = "contact-read-label";
            label.textContent = "Read";
            actions.appendChild(label);
        }
    };

    const updateBadge = (count) => {
        const unreadCount = Math.max(0, Number(count || 0));
        knownUnreadCount = unreadCount;

        if (contactBadge) {
            contactBadge.textContent = String(unreadCount);
            contactBadge.classList.toggle("d-none", unreadCount <= 0);
        }

        if (contactUnreadText) {
            contactUnreadText.textContent = `${unreadCount} unread`;
        }

        if (markAllButton) {
            markAllButton.classList.toggle("d-none", unreadCount <= 0);
        }

        if (notificationBadge) {
            const baseNotificationCount = Number(
                topbarActions?.dataset.baseNotificationCount || 0,
            );
            const totalNotification = baseNotificationCount + unreadCount;
            notificationBadge.textContent = String(totalNotification);
            notificationBadge.classList.toggle(
                "d-none",
                totalNotification <= 0,
            );
        }
    };

    const showNewFeedbackToast = (newMessageCount) => {
        if (!(window.Swal && typeof window.Swal.fire === "function")) {
            return;
        }

        const label = newMessageCount > 1 ? "messages" : "message";
        Swal.fire({
            toast: true,
            icon: "info",
            title: "New feedback received",
            text: `${newMessageCount} new feedback ${label} from users.`,
            position: "top-end",
            showConfirmButton: false,
            timer: 3200,
            timerProgressBar: true,
        });
    };

    const syncUnreadFeedbackCount = async () => {
        const previousUnreadCount = knownUnreadCount;

        try {
            const response = await fetch(
                "/admin/inbox/contacts/unread-summary",
                {
                    method: "GET",
                    cache: "no-store",
                },
            );
            const payload = await response.json().catch(() => ({}));
            if (!response.ok || !payload.success) {
                return;
            }

            const unreadCount = Math.max(0, Number(payload.unreadCount || 0));
            updateBadge(unreadCount);

            if (unreadCount > previousUnreadCount) {
                showNewFeedbackToast(unreadCount - previousUnreadCount);
            }
        } catch (error) {
            // Keep polling silent to avoid noisy console/network errors in UI.
        }
    };

    contactMenu.addEventListener("click", async (event) => {
        const markButton = event.target.closest(".mark-contact-read-btn");
        if (markButton) {
            const id = markButton.getAttribute("data-contact-id");
            if (!id) return;

            markButton.disabled = true;
            try {
                const response = await fetch(
                    `/admin/inbox/contacts/${id}/mark-read`,
                    { method: "POST" },
                );
                const payload = await response.json().catch(() => ({}));
                if (!response.ok || !payload.success) {
                    return;
                }

                const item = contactMenu.querySelector(
                    `.contact-item[data-contact-id="${id}"]`,
                );
                markContactItemAsRead(item);

                updateBadge(Number(payload.unreadCount || 0));
            } finally {
                markButton.disabled = false;
            }
            return;
        }

        const markAllTarget = event.target.closest("#markAllContactsReadBtn");
        if (markAllTarget) {
            markAllTarget.disabled = true;
            try {
                const response = await fetch(
                    "/admin/inbox/contacts/mark-all-read",
                    { method: "POST" },
                );
                const payload = await response.json().catch(() => ({}));
                if (!response.ok || !payload.success) {
                    return;
                }

                contactMenu
                    .querySelectorAll(".contact-item")
                    .forEach((item) => markContactItemAsRead(item));
                updateBadge(Number(payload.unreadCount || 0));
            } finally {
                markAllTarget.disabled = false;
            }
        }
    });

    window.setInterval(syncUnreadFeedbackCount, 15000);
    document.addEventListener("visibilitychange", () => {
        if (!document.hidden) {
            syncUnreadFeedbackCount();
        }
    });
})();

// Notification mark-as-read behavior for New Users/New Books alerts.
(function () {
    const notificationDropdown = document.getElementById(
        "notificationDropdown",
    );
    const notificationMenu = document.getElementById("notificationMenu");
    const topbarActions = document.getElementById("topbarActions");
    const notificationBadge = document.getElementById("notificationBadge");
    const contactBadge = document.getElementById("contactBadge");
    const notificationUnreadText = document.getElementById(
        "notificationMenuUnreadText",
    );
    const markAllNotificationsReadBtn = document.getElementById(
        "markAllNotificationsReadBtn",
    );
    if (!topbarActions || !notificationBadge) return;

    const serverBaseCount = Number(
        topbarActions.dataset.serverBaseNotificationCount ||
            topbarActions.dataset.baseNotificationCount ||
            0,
    );
    const seenKey = "adminNotificationSeenBaseCount";

    const getEffectiveBaseCount = () => {
        const seenBaseCount = Math.max(
            0,
            Number(localStorage.getItem(seenKey) || 0),
        );
        return Math.max(0, serverBaseCount - seenBaseCount);
    };

    const syncNotificationUi = () => {
        const effectiveBaseCount = getEffectiveBaseCount();
        topbarActions.dataset.baseNotificationCount =
            String(effectiveBaseCount);

        if (notificationUnreadText) {
            notificationUnreadText.textContent = `${effectiveBaseCount} unread`;
        }

        if (markAllNotificationsReadBtn) {
            markAllNotificationsReadBtn.classList.toggle(
                "d-none",
                effectiveBaseCount <= 0,
            );
        }

        const contactUnreadCount = Number(contactBadge?.textContent || 0);
        const totalCount = Math.max(0, effectiveBaseCount + contactUnreadCount);
        notificationBadge.textContent = String(totalCount);
        notificationBadge.classList.toggle("d-none", totalCount <= 0);
    };

    const markNotificationsAsRead = () => {
        localStorage.setItem(seenKey, String(serverBaseCount));
        syncNotificationUi();
    };

    syncNotificationUi();

    if (markAllNotificationsReadBtn) {
        markAllNotificationsReadBtn.addEventListener(
            "click",
            markNotificationsAsRead,
        );
    }

    if (notificationMenu) {
        notificationMenu.addEventListener("click", (event) => {
            const itemLink = event.target.closest("a.action-item");
            if (!itemLink) return;
            markNotificationsAsRead();
        });
    }

    if (
        window.location.pathname.toLowerCase().includes("/admin/managefeedback")
    ) {
        markNotificationsAsRead();
    }

    if (notificationDropdown) {
        notificationDropdown.addEventListener(
            "shown.bs.dropdown",
            syncNotificationUi,
        );
    }
})();

// Profile page specific logout confirmation
const logoutForm = document.getElementById("logoutForm");
if (logoutForm) {
    logoutForm.addEventListener("submit", function (e) {
        e.preventDefault(); // Prevent immediate submission

        Swal.fire({
            title: "Are you sure?",
            text: "You will be logged out of your account.",
            icon: "warning",
            showCancelButton: true,
            confirmButtonColor: "#3085d6",
            cancelButtonColor: "#d33",
            confirmButtonText: "Yes, log out!",
        }).then((result) => {
            if (result.isConfirmed) {
                Swal.fire({
                    title: "Logging out...",
                    text: "You have been logged out successfully.",
                    icon: "success",
                    showConfirmButton: false,
                });
                setTimeout(() => {
                    logoutForm.submit(); // Submit the form after showing the success message
                }, 1700);
            }
        });
    });
}
