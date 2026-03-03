document.addEventListener("DOMContentLoaded", function () {
  const toggleBtn = document.querySelector(".toggle-password");
  const toggleIcon =
    document.getElementById("togglePassword") ||
    (toggleBtn && toggleBtn.querySelector("i"));
  const passwordInput = document.querySelector(".password");

  if (toggleBtn && passwordInput) {
    toggleBtn.addEventListener("click", function () {
      const isHidden = passwordInput.type === "password";
      passwordInput.type = isHidden ? "text" : "password";

      if (toggleIcon) {
        toggleIcon.classList.toggle("bi-eye");
        toggleIcon.classList.toggle("bi-eye-slash");
      }

      toggleBtn.setAttribute(
        "aria-label",
        isHidden ? "Hide password" : "Show password",
      );
    });
  }

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
