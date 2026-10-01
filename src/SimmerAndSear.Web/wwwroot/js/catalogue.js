

(function () {
    'use strict';

    let csrfToken = null;
    const pendingWishlist = new WeakSet();

    async function getCsrfToken() {
        if (csrfToken) return csrfToken;

        const response = await fetch('/api/accounts/csrf', { credentials: 'same-origin' });
        if (!response.ok) throw new Error('Could not obtain an antiforgery token.');

        csrfToken = (await response.json()).token;
        return csrfToken;
    }

    async function addToCart(productId, quantity) {
        const response = await fetch('/api/cart/items', {
            method: 'POST',
            credentials: 'same-origin',
            headers: {
                'X-CSRF-TOKEN': await getCsrfToken(),
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ productId: productId, quantity: quantity })
        });

        if (!response.ok) {
            const problem = await response.json().catch(() => null);
            throw new Error(problem && problem.title ? problem.title : 'Could not add that to your cart.');
        }

        return response.json();
    }

    async function sendWishlist(method, url, body, failure) {
        const headers = { 'X-CSRF-TOKEN': await getCsrfToken() };
        if (body) headers['Content-Type'] = 'application/json';

        const response = await fetch(url, {
            method: method,
            credentials: 'same-origin',
            headers: headers,
            body: body ? JSON.stringify(body) : undefined
        });

        if (response.status === 401) {
            window.location.href = '/Auth/Login?ReturnUrl=' + encodeURIComponent(window.location.pathname + window.location.search);
            return null;
        }

        if (!response.ok) {
            const problem = await response.json().catch(() => null);
            throw new Error(problem && problem.title ? problem.title : failure);
        }

        return response.json();
    }

    function addToWishlist(productId) {
        return sendWishlist('POST', '/api/wishlist/items', { productId: productId }, 'Could not add that to your wishlist.');
    }

    function removeFromWishlist(productId) {
        return sendWishlist('DELETE', '/api/wishlist/items/' + encodeURIComponent(productId), null, 'Could not remove that from your wishlist.');
    }

    function markWishlisted(heart, wishlisted) {
        heart.classList.toggle('is-wishlisted', wishlisted);
        heart.setAttribute('aria-pressed', wishlisted ? 'true' : 'false');
        heart.title = wishlisted ? 'Remove from Wishlist' : 'Add to Wishlist';
    }

    function updateWishlistBadge(count) {
        if (typeof count !== 'number') return;

        const link = document.querySelector('a[href="/Wishlist"]');
        if (!link) return;

        let badge = link.querySelector('.nav-badge');

        if (count === 0) {
            if (badge) badge.remove();
            return;
        }

        if (!badge) {
            badge = document.createElement('span');
            badge.className = 'nav-badge';
            link.append(badge);
        }

        badge.textContent = count;
        badge.setAttribute('aria-label', count + (count === 1 ? ' item' : ' items') + ' saved to your wishlist');
    }

    function confirmOn(button, cart) {
        const original = button.innerHTML;
        const count = cart && typeof cart.totalQuantity === 'number' ? cart.totalQuantity : null;

        button.innerHTML = count === null ? 'Added' : 'Added (' + count + ' in cart)';
        button.disabled = true;

        window.setTimeout(function () {
            button.innerHTML = original;
            button.disabled = false;
        }, 1800);
    }


    function step(button) {
        const target = document.getElementById(button.getAttribute('data-qty-target'));
        if (!target) return;

        const max = parseInt(target.getAttribute('data-max'), 10) || 1;
        const by = parseInt(button.getAttribute('data-qty-step'), 10) || 0;
        const current = parseInt(target.textContent.trim(), 10) || 1;

        target.textContent = Math.max(1, Math.min(max, current + by));
    }

    function showImage(index) {
        const thumbs = Array.prototype.slice.call(document.querySelectorAll('[data-gallery-pick]'));
        const main = document.getElementById('gallery-main');
        if (!main || thumbs.length === 0) return;

        const wrapped = (index + thumbs.length) % thumbs.length;
        const chosen = thumbs[wrapped];

        main.src = chosen.getAttribute('data-full');
        main.alt = chosen.getAttribute('data-alt') || '';

        thumbs.forEach(function (thumb, i) {
            thumb.style.border = i === wrapped ? '2px solid var(--ss-brand)' : '2px dashed #c2c9d6';
        });
    }

    function currentImageIndex() {
        const thumbs = Array.prototype.slice.call(document.querySelectorAll('[data-gallery-pick]'));
        const active = thumbs.findIndex(function (t) { return t.style.border.indexOf('solid') !== -1; });
        return active === -1 ? 0 : active;
    }

    document.addEventListener('click', function (event) {
        const stepper = event.target.closest('[data-qty-step]');
        if (stepper) {
            step(stepper);
            return;
        }

        const thumb = event.target.closest('[data-gallery-pick]');
        if (thumb) {
            showImage(parseInt(thumb.getAttribute('data-gallery-pick'), 10) || 0);
            return;
        }

        const arrow = event.target.closest('[data-gallery-step]');
        if (arrow) {
            showImage(currentImageIndex() + (parseInt(arrow.getAttribute('data-gallery-step'), 10) || 0));
            return;
        }

        const heart = event.target.closest('[data-add-to-wishlist]');
        if (heart) {
            const wishlistProductId = heart.getAttribute('data-add-to-wishlist');
            event.preventDefault();
            if (!wishlistProductId || pendingWishlist.has(heart)) return;

            pendingWishlist.add(heart);

            const wishlisted = heart.classList.contains('is-wishlisted');
            const request = wishlisted ? removeFromWishlist(wishlistProductId) : addToWishlist(wishlistProductId);

            request
                .then(function (wishlist) {
                    if (!wishlist) return;
                    markWishlisted(heart, !wishlisted);
                    updateWishlistBadge(wishlist.count);
                })
                .catch(function (error) { window.alert(error.message); })
                .finally(function () { pendingWishlist.delete(heart); });
            return;
        }

        const button = event.target.closest('[data-add-to-cart]');
        if (!button || button.disabled) return;

        const productId = button.getAttribute('data-add-to-cart');
        if (!productId) return;

        const quantityField = button.getAttribute('data-quantity-from');
        const quantity = quantityField
            ? parseInt(document.getElementById(quantityField).textContent.trim(), 10) || 1
            : parseInt(button.getAttribute('data-quantity'), 10) || 1;

        event.preventDefault();
        button.disabled = true;

        addToCart(productId, quantity)
            .then(function (cart) {
                button.disabled = false;
                confirmOn(button, cart);
            })
            .catch(function (error) {
                button.disabled = false;
                window.alert(error.message);
            });
    });
})();
