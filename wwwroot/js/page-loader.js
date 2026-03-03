(() => {
    function initializePageLoader() {
        const loader = document.getElementById("pageLoader");
        if (!loader) {
            return;
        }

        const minVisibleMs = 500;
        const shownAt = performance.now();

        const showLoader = () => {
            loader.classList.remove("is-hidden");
            loader.setAttribute("aria-hidden", "false");
        };

        const hideLoader = () => {
            const elapsed = performance.now() - shownAt;
            const remaining = Math.max(0, minVisibleMs - elapsed);
            window.setTimeout(() => {
                loader.classList.add("is-hidden");
                loader.setAttribute("aria-hidden", "true");
            }, remaining);
        };

        const shouldIgnoreLink = (link) => {
            if (!link) {
                return true;
            }

            const href = link.getAttribute("href") || "";
            if (!href || href.startsWith("#")) {
                return true;
            }

            if (link.target && link.target.toLowerCase() === "_blank") {
                return true;
            }

            if (link.hasAttribute("download")) {
                return true;
            }

            const url = new URL(link.href, window.location.href);
            if (url.origin !== window.location.origin) {
                return true;
            }

            if (
                url.pathname === window.location.pathname &&
                url.search === window.location.search &&
                url.hash
            ) {
                return true;
            }

            return false;
        };

        showLoader();

        if (document.readyState === "complete") {
            hideLoader();
        } else {
            window.addEventListener("load", hideLoader, { once: true });
        }

        document.addEventListener("click", (event) => {
            const link = event.target.closest("a[href]");
            if (event.defaultPrevented || shouldIgnoreLink(link)) {
                return;
            }

            showLoader();
        });

        document.addEventListener("submit", (event) => {
            if (event.defaultPrevented) {
                return;
            }

            const form = event.target;
            if (
                !(form instanceof HTMLFormElement) ||
                form.hasAttribute("data-no-loader")
            ) {
                return;
            }

            showLoader();
        });

        window.addEventListener("pageshow", (event) => {
            if (event.persisted) {
                loader.classList.add("is-hidden");
                loader.setAttribute("aria-hidden", "true");
            }
        });
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", initializePageLoader, {
            once: true,
        });
        return;
    }

    initializePageLoader();
})();
