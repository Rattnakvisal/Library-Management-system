document.addEventListener('DOMContentLoaded', function () {
    const header = document.querySelector('header');

    window.addEventListener('scroll', function () {
        if (window.scrollY > 50) {
            header.classList.add('scrolled');
        } else {
            header.classList.remove('scrolled');
        }
    });

    const toggleBtn = document.querySelector('.mobile-menu-toggle');
    const navLinks = document.querySelector('.nav-links');
    const icon = toggleBtn ? toggleBtn.querySelector('i') : null;

    if (toggleBtn && icon) {
        toggleBtn.addEventListener('click', function () {
            navLinks.classList.toggle('active');
            if (navLinks.classList.contains('active')) {
                icon.classList.remove('bi-list');
                icon.classList.add('bi-x-lg');
            } else {
                icon.classList.remove('bi-x-lg');
                icon.classList.add('bi-list');
            }
        });
    }

    // Optimistic badge update for visible feedback when clicking Add to cart.
    const cartForms = document.querySelectorAll('form[action*="/cart/add/"]');
    cartForms.forEach(function (form) {
        form.addEventListener('submit', function () {
            const badge = document.getElementById('userCartBadge');
            if (!badge) {
                return;
            }

            const current = Number(badge.textContent || 0);
            badge.textContent = String(current + 1);
            badge.classList.remove('d-none');
        });
    });
});
