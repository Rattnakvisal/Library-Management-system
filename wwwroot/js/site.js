(() => {
    const consentCookieName = "library_cookie_consent";
    const dismissedCookieName = "library_cookie_banner_closed";
    const acceptedValue = "accepted";
    const essentialOnlyValue = "essential_only";

    function getCookie(name) {
        const prefix = `${name}=`;
        const parts = document.cookie ? document.cookie.split(";") : [];
        for (const part of parts) {
            const cookie = part.trim();
            if (cookie.startsWith(prefix)) {
                return decodeURIComponent(cookie.slice(prefix.length));
            }
        }
        return "";
    }

    function setCookie(name, value, days) {
        const expires = new Date(
            Date.now() + days * 24 * 60 * 60 * 1000,
        ).toUTCString();
        const secure = window.location.protocol === "https:" ? "; Secure" : "";
        document.cookie = `${name}=${encodeURIComponent(value)}; expires=${expires}; path=/; SameSite=Lax${secure}`;
    }

    function deleteCookie(name) {
        document.cookie = `${name}=; expires=Thu, 01 Jan 1970 00:00:00 GMT; path=/; SameSite=Lax`;
    }

    document.addEventListener("DOMContentLoaded", () => {
        const banner = document.getElementById("cookieConsentAlert");
        const acceptBtn = document.getElementById("cookieAcceptBtn");
        const rejectBtn = document.getElementById("cookieRejectBtn");
        const closeBtn = document.getElementById("cookieCloseBtn");
        const exitAnimationMs = 240;

        if (!banner || !acceptBtn || !rejectBtn || !closeBtn) {
            return;
        }

        const consent = getCookie(consentCookieName);
        const dismissed = getCookie(dismissedCookieName);

        const hasStoredConsent = consent === acceptedValue || consent === essentialOnlyValue;

        if (!hasStoredConsent && dismissed !== "1") {
            banner.classList.remove("is-hiding");
            banner.hidden = false;
            requestAnimationFrame(() => {
                banner.classList.add("show");
            });
        }

        function hideBanner() {
            if (banner.hidden || banner.classList.contains("is-hiding")) {
                return;
            }

            banner.classList.add("is-hiding");
            banner.classList.remove("show");
            setTimeout(() => {
                banner.hidden = true;
                banner.classList.remove("is-hiding");
            }, exitAnimationMs);
        }

        acceptBtn.addEventListener("click", () => {
            setCookie(consentCookieName, acceptedValue, 180);
            deleteCookie(dismissedCookieName);
            hideBanner();
        });

        rejectBtn.addEventListener("click", () => {
            setCookie(consentCookieName, essentialOnlyValue, 180);
            deleteCookie(dismissedCookieName);
            hideBanner();
        });

        closeBtn.addEventListener("click", () => {
            setCookie(dismissedCookieName, "1", 3);
            hideBanner();
        });

        document.addEventListener("keydown", (event) => {
            if (event.key === "Escape" && banner.classList.contains("show")) {
                setCookie(dismissedCookieName, "1", 3);
                hideBanner();
            }
        });
    });
})();
