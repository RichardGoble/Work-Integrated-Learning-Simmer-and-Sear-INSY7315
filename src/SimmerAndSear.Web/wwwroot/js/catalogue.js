

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

    document.addEventListener('click', function (event) {
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
