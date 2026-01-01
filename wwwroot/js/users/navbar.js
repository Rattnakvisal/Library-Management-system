window.addEventListener('load', function () {
    var header = document.querySelector('header');

    document.addEventListener('scroll', function () {
        if (window.pageYOffset > 50) {
            header.classList.add('scrolled');
        } else {
            header.classList.remove('scrolled');
        }
    });
});