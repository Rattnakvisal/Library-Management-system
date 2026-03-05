(() => {
    "use strict";

    const prefersReducedMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;

    function getVisibleUniqueElements(selectors) {
        const elements = selectors.flatMap((selector) => Array.from(document.querySelectorAll(selector)));

        return Array.from(new Set(elements)).filter((element) => {
            if (!element || element.classList.contains("hidden")) {
                return false;
            }

            return element.getClientRects().length > 0;
        });
    }

    function animateElements(elements, options = {}) {
        if (!elements.length) {
            return;
        }

        const {
            className = "admin-motion-reveal",
            baseDelay = 0,
            step = 70,
            maxDelay = 640
        } = options;

        elements.forEach((element, index) => {
            const delay = Math.min(baseDelay + index * step, maxDelay);
            element.style.setProperty("--admin-anim-delay", `${delay}ms`);
            element.classList.add(className);

            if (prefersReducedMotion) {
                element.classList.add("is-visible");
            }
        });

        if (prefersReducedMotion) {
            return;
        }

        requestAnimationFrame(() => {
            elements.forEach((element) => element.classList.add("is-visible"));
        });
    }

    function setupAdminAnimations() {
        if (!document.body.classList.contains("admin-shell")) {
            return;
        }

        const sectionElements = getVisibleUniqueElements([
            ".content > .dashboard-page",
            ".content > .setting-shell",
            ".content > .dashboard-page > header",
            ".content > .dashboard-page > section",
            ".content > .dashboard-page > .reports-page",
            ".content > .dashboard-page > .reports-page > *",
            ".content > .setting-shell > *",
            ".content > .setting-shell .setting-card > *"
        ]);

        const popElements = getVisibleUniqueElements([
            ".content .stat-card",
            ".content .sum-card",
            ".content .quick-panel .qtab-content.show",
            ".content .report-table-wrapper",
            ".content .security-side-panel"
        ]);

        animateElements(sectionElements, {
            className: "admin-motion-reveal",
            baseDelay: 20,
            step: 70,
            maxDelay: 720
        });

        animateElements(popElements, {
            className: "admin-motion-pop",
            baseDelay: 90,
            step: 45,
            maxDelay: 520
        });
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", setupAdminAnimations);
        return;
    }

    setupAdminAnimations();
})();
