(() => {
    const prefersReducedMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;

    function revealElements(elements, baseDelay = 0, step = 70, maxDelay = 560) {
        if (!elements.length) {
            return;
        }

        elements.forEach((element, index) => {
            const delay = Math.min(baseDelay + index * step, maxDelay);
            element.style.setProperty("--reveal-delay", `${delay}ms`);
        });

        if (prefersReducedMotion || typeof IntersectionObserver === "undefined") {
            elements.forEach((element) => element.classList.add("is-visible"));
            return;
        }

        const observer = new IntersectionObserver(
            (entries, io) => {
                entries.forEach((entry) => {
                    if (!entry.isIntersecting) {
                        return;
                    }

                    entry.target.classList.add("is-visible");
                    io.unobserve(entry.target);
                });
            },
            {
                threshold: 0.15,
                rootMargin: "0px 0px -8% 0px"
            }
        );

        elements.forEach((element) => observer.observe(element));
    }

    function setupEventPage() {
        const root = document.getElementById("eventPage");
        if (!root) {
            return;
        }

        root.classList.add("motion-enabled");
        const hero = root.querySelector(".event-hero");
        if (hero) {
            requestAnimationFrame(() => root.classList.add("is-ready"));
        }

        const reveals = Array.from(root.querySelectorAll(".page-title, .page-description, .event-card"));
        revealElements(reveals, 40, 70, 640);
    }

    function setupContactPage() {
        const root = document.getElementById("contactPage");
        if (!root) {
            return;
        }

        root.classList.add("motion-enabled");
        const reveals = Array.from(root.querySelectorAll(".contact-reveal"));
        revealElements(reveals, 20, 100, 380);
    }

    function setupAboutPage() {
        const root = document.getElementById("aboutPage");
        if (!root) {
            return;
        }

        root.classList.add("motion-enabled");
        const hero = root.querySelector(".about-hero");
        if (hero) {
            requestAnimationFrame(() => root.classList.add("is-ready"));
        }

        const reveals = Array.from(
            root.querySelectorAll(".about-content > h2, .about-content > p, .about-content > ul, .about-feature")
        );
        revealElements(reveals, 30, 75, 650);
    }

    function setupBookCategoryPage() {
        const root = document.getElementById("bookCategoryPage");
        if (!root) {
            return;
        }

        root.classList.add("motion-enabled");

        const headerReveals = Array.from(
            root.querySelectorAll(".category-heading, .category-subheading, .category-chip, .pagination-wrapper")
        );
        revealElements(headerReveals, 20, 45, 420);

        const cardReveals = Array.from(root.querySelectorAll(".category-card"));
        revealElements(cardReveals, 60, 70, 640);
    }

    function setupBookDetailPage() {
        const root = document.getElementById("bookDetailPage");
        if (!root) {
            return;
        }

        root.classList.add("motion-enabled");
        requestAnimationFrame(() => {
            root.classList.add("is-ready");
        });

        const relatedReveals = Array.from(root.querySelectorAll(".detail-related h3, .detail-related-card"));
        revealElements(relatedReveals, 40, 70, 600);
    }

    function setupBookmarkPage() {
        const root = document.getElementById("bookmarkPage");
        if (!root) {
            return;
        }

        root.classList.add("motion-enabled");
        const hero = root.querySelector(".bookmark-hero-wrap");
        if (hero) {
            requestAnimationFrame(() => root.classList.add("is-ready"));
        }

        const reveals = Array.from(
            root.querySelectorAll(".bookmark-title, .bookmark-card, .bookmark-pagination-wrap, .bookmark-empty")
        );
        revealElements(reveals, 35, 65, 680);
    }

    function setupHistoryPage() {
        const root = document.getElementById("historyPage");
        if (!root) {
            return;
        }

        root.classList.add("motion-enabled");
        const reveals = Array.from(root.querySelectorAll(".page-title, .summary-dropdown, .history-card"));
        revealElements(reveals, 30, 80, 620);
    }

    function setupProfilePage() {
        const root = document.getElementById("profilePage");
        if (!root) {
            return;
        }

        root.classList.add("motion-enabled");
        const hero = root.querySelector(".profile-hero");
        if (hero) {
            requestAnimationFrame(() => root.classList.add("is-ready"));
        }

        const sectionReveals = Array.from(root.querySelectorAll(".profile-reveal"));
        revealElements(sectionReveals, 20, 90, 520);

        const interestCards = Array.from(root.querySelectorAll(".interest-card"));
        revealElements(interestCards, 90, 60, 760);
    }

    document.addEventListener("DOMContentLoaded", () => {
        setupEventPage();
        setupContactPage();
        setupAboutPage();
        setupBookCategoryPage();
        setupBookDetailPage();
        setupBookmarkPage();
        setupHistoryPage();
        setupProfilePage();
    });
})();
