(() => {
    function setupWishlistButtons(root) {
        const buttons = root.querySelectorAll(".wishlist-btn");
        if (!buttons.length) {
            return;
        }

        buttons.forEach((button, index) => {
            const configuredId = button.getAttribute("data-id");
            const bookId = configuredId && configuredId.trim().length > 0
                ? configuredId.trim()
                : `wishlist-item-${index}`;

            if (!configuredId) {
                button.setAttribute("data-id", bookId);
            }

            const icon = button.querySelector("i");
            let favorites = JSON.parse(localStorage.getItem("favorites") || "[]");

            if (favorites.includes(bookId)) {
                button.classList.add("active");
                if (icon) {
                    icon.classList.add("text-danger");
                }
            }

            button.addEventListener("click", function () {
                favorites = JSON.parse(localStorage.getItem("favorites") || "[]");

                if (favorites.includes(bookId)) {
                    favorites = favorites.filter((id) => id !== bookId);
                    this.classList.remove("active");
                    if (icon) {
                        icon.classList.remove("text-danger");
                    }
                } else {
                    favorites.push(bookId);
                    this.classList.add("active");
                    if (icon) {
                        icon.classList.add("text-danger");
                    }
                }

                localStorage.setItem("favorites", JSON.stringify(favorites));
            });
        });
    }

    function setupHomeAnimations(root) {
        const hero = root.querySelector(".home-hero");
        if (hero) {
            requestAnimationFrame(() => {
                hero.classList.add("is-visible");
            });
        }

        const sections = root.querySelectorAll(".home-section");
        if (!sections.length) {
            return;
        }

        sections.forEach((section) => {
            const revealTargets = section.querySelectorAll(".book-card, .genre-card-wrapper");
            revealTargets.forEach((item, index) => {
                item.classList.add("reveal-card");
                item.style.setProperty("--stagger-delay", `${Math.min(index * 70, 560)}ms`);
            });
        });

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
                threshold: 0.16,
                rootMargin: "0px 0px -8% 0px"
            }
        );

        sections.forEach((section) => observer.observe(section));
    }

    document.addEventListener("DOMContentLoaded", () => {
        const homeRoot = document.getElementById("homePage");
        if (!homeRoot) {
            return;
        }

        setupWishlistButtons(homeRoot);
        setupHomeAnimations(homeRoot);
    });
})();
