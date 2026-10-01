//References
//MDN Web Docs, n.d. Using the Fetch API [Source code].
//Available at: <https://developer.mozilla.org/en-US/docs/Web/API/Fetch_API/Using_Fetch> [Accessed 21 September 2026].

//Microsoft, 2026. Prevent Cross-Site Request Forgery (XSRF/CSRF) attacks in ASP.NET Core [Source code].
//Available at: <https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery?view=aspnetcore-10.0> [Accessed 21 September 2026].

(function () {
    'use strict';

    let csrfToken = null;
    let busy = false;

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
            headers: { 'X-CSRF-TOKEN': await getCsrfToken() }
        });

        if (!response.ok) {
            const problem = await response.json().catch(() => null);
            throw new Error(problem && problem.title ? problem.title : 'Something went wrong.');
        }
    }

    async function apply(work) {
        if (busy) return;
        busy = true;

        try {
            await work();
            window.location.reload();
        } catch (error) {
            busy = false;
            window.alert(error.message);
        }
    }

    async function requestQuote(form) {
        const field = (name) => form.elements[name].value;

        const response = await fetch('/api/wishlist/quote-request', {
            method: 'POST',
            credentials: 'same-origin',
            headers: {
                'X-CSRF-TOKEN': await getCsrfToken(),
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                name: field('name'),
                email: field('email'),
                company: field('company'),
                notes: field('notes')
            })
        });

        if (!response.ok) {
            const problem = await response.json().catch(() => null);
            throw new Error(problem && problem.title ? problem.title : 'Could not send your quote request.');
        }

        return response.json();
    }

    document.addEventListener('submit', function (event) {
        const form = event.target.closest('[data-wishlist-quote]');
        if (!form) return;

        event.preventDefault();
        if (busy) return;
        busy = true;

        requestQuote(form)
            .then(function (order) {
                window.location.href = '/Cart/OrderConfirmation/' + encodeURIComponent(order.id);
            })
            .catch(function (error) {
                busy = false;
                window.alert(error.message);
            });
    });

    document.addEventListener('click', function (event) {
        const moveButton = event.target.closest('[data-wishlist-move]');
        if (moveButton) {
            const productId = moveButton.getAttribute('data-wishlist-move');
            apply(() => send('POST', '/api/wishlist/items/' + encodeURIComponent(productId) + '/move-to-cart'));
            return;
        }

        const removeButton = event.target.closest('[data-wishlist-remove]');
        if (removeButton) {
            const productId = removeButton.getAttribute('data-wishlist-remove');
            apply(() => send('DELETE', '/api/wishlist/items/' + encodeURIComponent(productId)));
            return;
        }

        const clearButton = event.target.closest('[data-wishlist-clear]');
        if (clearButton) {
            if (!window.confirm('Remove everything from your wishlist?')) return;
            apply(() => send('DELETE', '/api/wishlist'));
        }
    });
})();
