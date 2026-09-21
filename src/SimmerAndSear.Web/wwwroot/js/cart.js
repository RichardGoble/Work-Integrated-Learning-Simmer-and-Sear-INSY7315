

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

    async function send(method, url) {
        const response = await fetch(url, {
            method: method,
            credentials: 'same-origin',
            headers: {
                'X-CSRF-TOKEN': await getCsrfToken(),
                'Content-Type': 'application/json'
            },
            body: method === 'DELETE' ? undefined : JSON.stringify({})
        });

        if (!response.ok) {
          
            const problem = await response.json().catch(() => null);
            throw new Error(problem && problem.title ? problem.title : 'Something went wrong.');
        }

        return response.json();
    }

    async function setQuantity(productId, quantity) {
        const response = await fetch('/api/cart/items/' + encodeURIComponent(productId), {
            method: 'PUT',
            credentials: 'same-origin',
            headers: {
                'X-CSRF-TOKEN': await getCsrfToken(),
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ quantity: quantity })
        });

        if (!response.ok) {
            const problem = await response.json().catch(() => null);
            throw new Error(problem && problem.title ? problem.title : 'Something went wrong.');
        }

        return response.json();
    }

    function disable(button, busy) {
        if (!button) return;
        button.disabled = busy;
        button.style.opacity = busy ? '0.5' : '';
    }

   
    async function apply(button, work) {
        disable(button, true);

        try {
            await work();
            window.location.reload();
        } catch (error) {
            disable(button, false);
            window.alert(error.message);
        }
    }

    document.addEventListener('click', function (event) {
        const quantityButton = event.target.closest('[data-cart-quantity]');
        if (quantityButton) {
            const productId = quantityButton.getAttribute('data-cart-quantity');
            const quantity = parseInt(quantityButton.getAttribute('data-quantity'), 10);
            apply(quantityButton, () => setQuantity(productId, quantity));
            return;
        }

        const removeButton = event.target.closest('[data-cart-remove]');
        if (removeButton) {
            const productId = removeButton.getAttribute('data-cart-remove');
            apply(removeButton, () => send('DELETE', '/api/cart/items/' + encodeURIComponent(productId)));
            return;
        }

        const clearButton = event.target.closest('[data-cart-clear]');
        if (clearButton) {
            if (!window.confirm('Remove everything from your cart?')) return;
            apply(clearButton, () => send('DELETE', '/api/cart'));
        }
    });
})();
