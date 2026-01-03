document.addEventListener('DOMContentLoaded', function () {
    const toggleBtn = document.querySelector('.toggle-password');
    const toggleIcon = document.getElementById('togglePassword') || (toggleBtn && toggleBtn.querySelector('i'));
    const passwordInput = document.querySelector('.password');

    if (!toggleBtn || !passwordInput) return; // nothing to do if elements are missing

    toggleBtn.addEventListener('click', function () {
        const isHidden = passwordInput.type === 'password';
        passwordInput.type = isHidden ? 'text' : 'password';

        if (toggleIcon) {
            toggleIcon.classList.toggle('bi-eye');
            toggleIcon.classList.toggle('bi-eye-slash');
        }

        toggleBtn.setAttribute('aria-label', isHidden ? 'Hide password' : 'Show password');
    });
});