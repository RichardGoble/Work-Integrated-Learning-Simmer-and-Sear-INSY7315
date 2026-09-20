

(function () {
    'use strict';

    let csrfToken = null;

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
