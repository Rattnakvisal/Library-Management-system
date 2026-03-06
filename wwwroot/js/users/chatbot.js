(() => {
    function setupStaticChatbot() {
        const chatbot = document.getElementById("homeChatbot");
        if (!chatbot || chatbot.dataset.initialized === "1") {
            return;
        }

        chatbot.dataset.initialized = "1";

        const toggleButton = chatbot.querySelector("#homeChatbotToggle");
        const panel = chatbot.querySelector("#homeChatbotPanel");
        const closeButton = chatbot.querySelector("#homeChatbotClose");
        const messages = chatbot.querySelector("#homeChatbotMessages");
        const quickActions = chatbot.querySelector("#homeChatbotQuickActions");
        const form = chatbot.querySelector("#homeChatbotForm");
        const input = chatbot.querySelector("#homeChatbotInput");

        if (
            !toggleButton ||
            !panel ||
            !closeButton ||
            !messages ||
            !quickActions ||
            !form ||
            !input
        ) {
            return;
        }

        const staticReplies = {
            greeting:
                "Hi. I can help with hours, reservations, borrowing, fines, events, profile, and policies.",
            hours: "Library hours: Monday-Friday 8:00 AM-8:00 PM, Saturday-Sunday 9:00 AM-5:00 PM.",
            borrow:
                "Borrowing limit is up to 5 books at a time. The default borrowing duration is 14 days.",
            reserve:
                "To reserve a book: open a book detail page, add it to cart, then go to /cart and click Proceed Request.",
            fine:
                "Late return fee is $1.00 per late day. You can check overdue and fine details in /history.",
            history:
                "Open /history to see borrowed, overdue, and returned books with fine payment status.",
            event: "Open /event to see upcoming library events and schedules.",
            account:
                "Use /login to sign in. After login, you can manage account details from /profile.",
            profile:
                "Use /profile to update your profile and /bookmark to manage favorite books.",
            review:
                "You can submit a 1-5 star review from each book detail page using the Feedback button.",
            policy: "Library policy is available at /about/policies.",
            search:
                "Use /book to browse or search by title, author, and category. Category filters are available on the Book page.",
            contact:
                "Use /contact to send feedback or support questions to library staff.",
            location:
                "Library location: ACLEDA University campus library building.",
            resetPassword:
                "Open /Identity/Account/ForgotPassword, then use Telegram OTP to finish your reset.",
        };

        const intentKeywords = [
            {
                intent: "greeting",
                keywords: ["hello", "hi", "hey", "good morning", "good afternoon"],
            },
            {
                intent: "hours",
                keywords: ["hour", "opening", "open time", "close time"],
            },
            {
                intent: "reserve",
                keywords: [
                    "reserve",
                    "reservation",
                    "request book",
                    "proceed request",
                    "cart request",
                ],
            },
            {
                intent: "borrow",
                keywords: [
                    "borrow limit",
                    "borrowing limit",
                    "how many books",
                    "borrow period",
                    "duration",
                ],
            },
            {
                intent: "fine",
                keywords: ["fine", "late fee", "late return", "penalty", "overdue fee"],
            },
            {
                intent: "history",
                keywords: ["history", "borrow history", "overdue", "returned books"],
            },
            {
                intent: "event",
                keywords: ["event", "events", "schedule", "activity"],
            },
            {
                intent: "resetPassword",
                keywords: [
                    "reset password",
                    "forgot password",
                    "password reset",
                    "telegram otp",
                    "otp code",
                    "send otp",
                    "/start",
                ],
            },
            {
                intent: "account",
                keywords: ["account", "login", "sign in", "register"],
            },
            {
                intent: "profile",
                keywords: ["profile", "bookmark", "favorite", "avatar", "cover image"],
            },
            {
                intent: "review",
                keywords: ["review", "rating", "star", "book feedback"],
            },
            {
                intent: "policy",
                keywords: ["policy", "rules", "terms", "copyright"],
            },
            {
                intent: "contact",
                keywords: ["contact", "support", "help", "feedback message"],
            },
            {
                intent: "location",
                keywords: ["where", "location", "address", "campus"],
            },
            {
                intent: "search",
                keywords: ["search", "find book", "book list", "category", "browse"],
            },
        ];

        const fallbackReply =
            "I can help with hours, reservations, borrow limits, reset password OTP, fines, events, profile, policies, and support. Try asking: How do I reset my password?";

        const appendMessageElement = (item) => {
            messages.appendChild(item);
            messages.scrollTop = messages.scrollHeight;
        };

        const addMessage = (text, type) => {
            const item = document.createElement("div");
            item.className = `home-chatbot__msg home-chatbot__msg--${type}`;
            item.textContent = text;
            appendMessageElement(item);
        };

        const addResetPasswordGuideMessage = () => {
            const item = document.createElement("div");
            item.className = "home-chatbot__msg home-chatbot__msg--bot home-chatbot__msg--guide";

            const title = document.createElement("p");
            title.className = "home-chatbot__guide-title";
            title.textContent = "How to reset password with Telegram OTP:";
            item.appendChild(title);

            const steps = [
                "Open Telegram bot @AUBLIBRARYCONTACT_BOT and tap /start.",
                "Send your phone number (Cambodia local or +855 format).",
                "Open /Identity/Account/ForgotPassword and submit your new password.",
                "Enter the OTP sent to Telegram to complete reset.",
            ];

            const list = document.createElement("ol");
            list.className = "home-chatbot__guide-list";
            steps.forEach((step) => {
                const li = document.createElement("li");
                li.textContent = step;
                list.appendChild(li);
            });
            item.appendChild(list);

            const links = document.createElement("div");
            links.className = "home-chatbot__guide-links";

            const openBotLink = document.createElement("a");
            openBotLink.href = "https://t.me/AUBLIBRARYCONTACT_BOT";
            openBotLink.target = "_blank";
            openBotLink.rel = "noopener noreferrer";
            openBotLink.textContent = "Open Telegram Bot";
            links.appendChild(openBotLink);

            const openResetLink = document.createElement("a");
            openResetLink.href = "/Identity/Account/ForgotPassword";
            openResetLink.textContent = "Open Reset Password Page";
            links.appendChild(openResetLink);

            item.appendChild(links);

            const image = document.createElement("img");
            image.className = "home-chatbot__guide-image";
            image.src = "/images/image.png";
            image.alt = "Telegram /start QR for AUBLIBRARYCONTACT_BOT";
            image.loading = "lazy";
            item.appendChild(image);

            appendMessageElement(item);
        };

        const sendBotReplyByIntent = (intent) => {
            if (intent === "resetPassword") {
                addResetPasswordGuideMessage();
                return;
            }

            addMessage(staticReplies[intent] || fallbackReply, "bot");
        };

        const openPanel = () => {
            chatbot.classList.add("is-open");
            panel.hidden = false;
            toggleButton.setAttribute("aria-expanded", "true");

            if (!chatbot.dataset.seeded) {
                addMessage(
                    "Hello, I am your library assistant. Ask about reservations, borrowing, reset password OTP, fines, events, or support.",
                    "bot",
                );
                chatbot.dataset.seeded = "1";
            }
        };

        const closePanel = () => {
            chatbot.classList.remove("is-open");
            toggleButton.setAttribute("aria-expanded", "false");
            panel.hidden = true;
        };

        const getIntent = (text) => {
            const normalized = (text || "").trim().toLowerCase();
            if (!normalized) {
                return "";
            }

            for (const entry of intentKeywords) {
                const isMatch = entry.keywords.some((keyword) =>
                    normalized.includes(keyword),
                );
                if (isMatch) {
                    return entry.intent;
                }
            }

            return "";
        };

        const sendUserMessage = (text) => {
            const cleaned = (text || "").trim();
            if (!cleaned) {
                return;
            }

            addMessage(cleaned, "user");
            const intent = getIntent(cleaned);
            sendBotReplyByIntent(intent);
        };

        toggleButton.addEventListener("click", () => {
            if (chatbot.classList.contains("is-open")) {
                closePanel();
                return;
            }

            openPanel();
            input.focus();
        });

        closeButton.addEventListener("click", closePanel);

        quickActions.addEventListener("click", (event) => {
            const target = event.target;
            if (!(target instanceof HTMLButtonElement)) {
                return;
            }

            const intent = target.dataset.intent || "";
            const label = target.textContent
                ? target.textContent.trim()
                : "Question";
            addMessage(label, "user");
            sendBotReplyByIntent(intent);
        });

        form.addEventListener("submit", (event) => {
            event.preventDefault();
            const value = input.value;
            input.value = "";
            sendUserMessage(value);
            input.focus();
        });

        document.addEventListener("click", (event) => {
            if (!chatbot.classList.contains("is-open")) {
                return;
            }

            const target = event.target;
            if (target instanceof Node && chatbot.contains(target)) {
                return;
            }

            closePanel();
        });

        window.addEventListener("keydown", (event) => {
            if (event.key === "Escape") {
                closePanel();
            }
        });
    }

    document.addEventListener("DOMContentLoaded", setupStaticChatbot);
})();
