(() => {
    function setupShareButtons(root) {
        const buttons = root.querySelectorAll(".share-book-btn");
        if (!buttons.length) {
            return;
        }

        const toAbsoluteUrl = (url) => {
            try {
                return new URL(url, window.location.origin).href;
            } catch {
                return window.location.href;
            }
        };

        const flashShareState = (button, titleText) => {
            const previousTitle = button.getAttribute("title") || "Share";
            button.setAttribute("title", titleText);
            button.classList.add("text-success");

            window.setTimeout(() => {
                button.setAttribute("title", previousTitle);
                button.classList.remove("text-success");
            }, 1400);
        };

        const copyToClipboard = async (text) => {
            if (navigator.clipboard && window.isSecureContext) {
                await navigator.clipboard.writeText(text);
                return true;
            }

            const helper = document.createElement("textarea");
            helper.value = text;
            helper.setAttribute("readonly", "");
            helper.style.position = "fixed";
            helper.style.opacity = "0";
            document.body.appendChild(helper);
            helper.select();

            const copied = document.execCommand("copy");
            document.body.removeChild(helper);
            return copied;
        };

        buttons.forEach((button) => {
            button.addEventListener("click", async () => {
                const shareUrl = toAbsoluteUrl(button.dataset.shareUrl || window.location.href);
                const shareTitle = button.dataset.shareTitle || document.title;
                const shareAuthor = button.dataset.shareAuthor;
                const shareText = shareAuthor ? `${shareTitle} by ${shareAuthor}` : shareTitle;

                try {
                    if (typeof navigator.share === "function") {
                        await navigator.share({
                            title: shareTitle,
                            text: shareText,
                            url: shareUrl
                        });
                        flashShareState(button, "Shared");
                        return;
                    }

                    const copied = await copyToClipboard(shareUrl);
                    flashShareState(button, copied ? "Link copied" : "Copy failed");
                } catch (error) {
                    if (error && typeof error === "object" && "name" in error && error.name === "AbortError") {
                        return;
                    }

                    try {
                        const copied = await copyToClipboard(shareUrl);
                        flashShareState(button, copied ? "Link copied" : "Copy failed");
                    } catch {
                        flashShareState(button, "Copy failed");
                    }
                }
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

        setupShareButtons(homeRoot);
        setupHomeAnimations(homeRoot);
    });
})();
