document.addEventListener("DOMContentLoaded", function () {
  // Handle all password toggle buttons
  document.querySelectorAll(".toggle-password").forEach(function (toggleBtn) {
    toggleBtn.addEventListener("click", function () {
      const target = toggleBtn.getAttribute("data-target");
      const passwordInput = document.querySelector(target) || toggleBtn.parentElement.querySelector("input[type='password'], input[type='text']");
      const toggleIcon = toggleBtn.querySelector("i");

      if (passwordInput && toggleIcon) {
        const isHidden = passwordInput.type === "password";
        passwordInput.type = isHidden ? "text" : "password";

        toggleIcon.classList.toggle("bi-eye");
        toggleIcon.classList.toggle("bi-eye-slash");

        toggleBtn.setAttribute(
          "aria-label",
          isHidden ? "Hide password" : "Show password",
        );
      }
    });

    // Handle keyboard toggle (Enter/Space)
    toggleBtn.addEventListener("keypress", function (e) {
      if (e.key === "Enter" || e.key === " ") {
        e.preventDefault();
        toggleBtn.click();
      }
    });
  });

  function applyLoadingState(form) {
    const submitButton =
      form.querySelector('button[type="submit"]') ||
      document.getElementById("login-submit") ||
      document.getElementById("register-submit");

    if (!submitButton) {
      return;
    }

    submitButton.disabled = true;
    submitButton.classList.add("btn-loading");
  }

  document.querySelectorAll("form").forEach(function (form) {
    form.addEventListener("submit", function (event) {
      if (event.defaultPrevented) {
        return;
      }

      if (typeof form.checkValidity === "function" && !form.checkValidity()) {
        return;
      }

      if (window.jQuery && typeof window.jQuery(form).valid === "function" && !window.jQuery(form).valid()) {
        return;
      }

      applyLoadingState(form);
    });
  });
});
