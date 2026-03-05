document.addEventListener('DOMContentLoaded', function () {
    function setupSearchSuggestions() {
        const searchForm = document.querySelector('.search-box');
        if (!searchForm) {
            return;
        }

        const searchInput = searchForm.querySelector('input[name="q"]');
        const suggestionsPanel = searchForm.querySelector('.search-suggestions');
        if (!searchInput || !suggestionsPanel) {
            return;
        }

        let debounceTimer = null;
        let activeRequest = null;

        function hideSuggestions() {
            searchForm.classList.remove('is-open');
            suggestionsPanel.classList.add('d-none');
            suggestionsPanel.innerHTML = '';
        }

        function createGroupHeader(text) {
            const header = document.createElement('div');
            header.className = 'search-suggestion-group';
            header.textContent = text;
            return header;
        }

        function createCategoryItem(name) {
            const link = document.createElement('a');
            link.className = 'search-suggestion-item search-suggestion-item-category';
            link.href = '/book?category=' + encodeURIComponent(name);

            const title = document.createElement('div');
            title.className = 'search-suggestion-title';
            title.textContent = name || 'Unnamed category';

            const meta = document.createElement('div');
            meta.className = 'search-suggestion-author';
            meta.textContent = 'Category';

            link.appendChild(title);
            link.appendChild(meta);
            return link;
        }

        function createBookItem(item) {
            const link = document.createElement('a');
            link.className = 'search-suggestion-item search-suggestion-item-book';
            link.href = '/book/' + item.id;

            const title = document.createElement('div');
            title.className = 'search-suggestion-title';
            title.textContent = item.title || 'Untitled';

            const author = document.createElement('div');
            author.className = 'search-suggestion-author';
            const authorText = item.author || 'Unknown author';
            const categoryText = item.category ? ' • ' + item.category : '';
            author.textContent = authorText + categoryText;

            link.appendChild(title);
            link.appendChild(author);
            return link;
        }

        function createViewAllItem(query) {
            const link = document.createElement('a');
            link.className = 'search-suggestion-item search-suggestion-view-all';
            link.href = '/book?q=' + encodeURIComponent(query);

            const label = document.createElement('div');
            label.className = 'search-suggestion-title';
            label.textContent = 'View all results for "' + query + '"';

            link.appendChild(label);
            return link;
        }

        function renderEmptyState(query) {
            suggestionsPanel.innerHTML = '';

            const empty = document.createElement('div');
            empty.className = 'search-suggestion-empty';
            empty.textContent = 'No matching books or categories';
            suggestionsPanel.appendChild(empty);

            suggestionsPanel.appendChild(createViewAllItem(query));
            searchForm.classList.add('is-open');
            suggestionsPanel.classList.remove('d-none');
        }

        function renderSuggestions(payload, query) {
            suggestionsPanel.innerHTML = '';

            const categories = Array.isArray(payload && payload.categories) ? payload.categories : [];
            const books = Array.isArray(payload && payload.books) ? payload.books : [];
            const hasCategories = categories.length > 0;
            const hasBooks = books.length > 0;

            if (!hasCategories && !hasBooks) {
                renderEmptyState(query);
                return;
            }

            if (hasCategories) {
                suggestionsPanel.appendChild(createGroupHeader('Categories'));
                categories.forEach(function (item) {
                    suggestionsPanel.appendChild(createCategoryItem(item.name));
                });
            }

            if (hasBooks) {
                suggestionsPanel.appendChild(createGroupHeader('Books'));
                books.forEach(function (item) {
                    suggestionsPanel.appendChild(createBookItem(item));
                });
            }

            suggestionsPanel.appendChild(createViewAllItem(query));

            searchForm.classList.add('is-open');
            suggestionsPanel.classList.remove('d-none');
        }

        function fetchSuggestions(query) {
            if (!query || query.length < 1) {
                hideSuggestions();
                return;
            }

            if (activeRequest) {
                activeRequest.abort();
            }

            activeRequest = new AbortController();
            fetch('/book/suggest?q=' + encodeURIComponent(query), {
                method: 'GET',
                headers: {
                    'X-Requested-With': 'XMLHttpRequest'
                },
                signal: activeRequest.signal
            })
                .then(function (response) {
                    if (!response.ok) {
                        return [];
                    }

                    return response.json();
                })
                .then(function (data) {
                    if ((searchInput.value || '').trim() !== query) {
                        return;
                    }

                    renderSuggestions(data, query);
                })
                .catch(function (error) {
                    if (error && error.name === 'AbortError') {
                        return;
                    }

                    hideSuggestions();
                });
        }

        searchInput.addEventListener('input', function () {
            const query = (searchInput.value || '').trim();
            if (debounceTimer) {
                clearTimeout(debounceTimer);
            }

            debounceTimer = setTimeout(function () {
                fetchSuggestions(query);
            }, 220);
        });

        searchInput.addEventListener('focus', function () {
            const query = (searchInput.value || '').trim();
            if (query.length >= 1) {
                fetchSuggestions(query);
            }
        });

        searchInput.addEventListener('keydown', function (event) {
            if (event.key === 'Escape') {
                hideSuggestions();
            }
        });

        document.addEventListener('click', function (event) {
            if (!searchForm.contains(event.target)) {
                hideSuggestions();
            }
        });

        searchForm.addEventListener('submit', function () {
            hideSuggestions();
        });
    }

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

    const reservationToggle = document.getElementById('userReservationToggle');
    const reservationBadge = document.getElementById('userReservationBadge');
    let isMarkingReservationRead = false;

    function hasUnreadReservationNotifications() {
        if (!reservationBadge || reservationBadge.classList.contains('d-none')) {
            return false;
        }

        return Number(reservationBadge.textContent || 0) > 0;
    }

    function clearReservationBadge() {
        if (!reservationBadge) {
            return;
        }

        reservationBadge.textContent = '0';
        reservationBadge.classList.add('d-none');
    }

    function markReservationNotificationsRead() {
        if (isMarkingReservationRead || !hasUnreadReservationNotifications()) {
            return;
        }

        isMarkingReservationRead = true;
        clearReservationBadge();

        fetch('/notifications/reservations/mark-read', {
            method: 'POST',
            headers: {
                'X-Requested-With': 'XMLHttpRequest'
            }
        })
            .catch(function () {
                // Keep UI responsive even if request fails.
            })
            .finally(function () {
                isMarkingReservationRead = false;
            });
    }

    if (reservationToggle) {
        reservationToggle.addEventListener('click', markReservationNotificationsRead);

        const reservationDropdown = reservationToggle.closest('.dropdown');
        if (reservationDropdown) {
            reservationDropdown.addEventListener('show.bs.dropdown', markReservationNotificationsRead);
        }
    }

    setupSearchSuggestions();
});
