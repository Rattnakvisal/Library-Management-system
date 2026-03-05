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

        if (!toggleButton || !panel || !closeButton || !messages || !quickActions || !form || !input) {
            return;
        }

        const staticReplies = {
            hours: "Library hours: Monday-Friday 8:00 AM-8:00 PM, Saturday-Sunday 9:00 AM-5:00 PM.",
            borrow: "Borrowing limit: up to 5 books at a time. The standard borrowing period is 14 days.",
            fine: "Late return fee is $1.00 per late day.",
            contact: "Support: use the Contact page or ask library staff at the front desk during opening hours.",
            location: "Library location: ACLEDA University campus library building."
        };

        const fallbackReply =
            "I can help with opening hours, borrow limit, late fee, contact support, and location.";

        const addMessage = (text, type) => {
            const item = document.createElement("div");
            item.className = `home-chatbot__msg home-chatbot__msg--${type}`;
            item.textContent = text;
            messages.appendChild(item);
            messages.scrollTop = messages.scrollHeight;
        };

        const openPanel = () => {
            chatbot.classList.add("is-open");
            panel.hidden = false;
            toggleButton.setAttribute("aria-expanded", "true");

            if (!chatbot.dataset.seeded) {
                addMessage("Hello, I am your static library assistant. Ask me a quick question.", "bot");
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

            if (normalized.includes("hour") || normalized.includes("time") || normalized.includes("open")) {
                return "hours";
            }

            if (normalized.includes("borrow") || normalized.includes("limit") || normalized.includes("book")) {
                return "borrow";
            }

            if (normalized.includes("fine") || normalized.includes("late") || normalized.includes("fee")) {
                return "fine";
            }

            if (normalized.includes("contact") || normalized.includes("support") || normalized.includes("help")) {
                return "contact";
            }

            if (normalized.includes("where") || normalized.includes("location") || normalized.includes("address")) {
                return "location";
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
            addMessage(staticReplies[intent] || fallbackReply, "bot");
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
            const label = target.textContent ? target.textContent.trim() : "Question";
            addMessage(label, "user");
            addMessage(staticReplies[intent] || fallbackReply, "bot");
        });

        form.addEventListener("submit", (event) => {
            event.preventDefault();
            const value = input.value;
            input.value = "";
            sendUserMessage(value);
            input.focus();
        });
    }

    document.addEventListener("DOMContentLoaded", setupStaticChatbot);
})();
