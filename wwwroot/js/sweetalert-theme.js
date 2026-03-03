(function () {
    if (typeof Swal === "undefined") {
        return;
    }

    const baseOptions = {
        customClass: {
            popup: "swal2-popup",
            title: "swal2-title",
            htmlContainer: "swal2-html-container"
        },
        confirmButtonColor: "#0f2b48",
        cancelButtonColor: "#6b7785",
        buttonsStyling: true
    };

    if (typeof Swal.mixin === "function") {
        window.LibrarySwal = Swal.mixin(baseOptions);
    } else {
        window.LibrarySwal = Swal;
    }
})();
