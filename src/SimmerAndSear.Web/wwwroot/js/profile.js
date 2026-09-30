
(function () {
    'use strict';

    document.addEventListener('submit', function (event) {
        const form = event.target.closest('[data-confirm-deactivate]');
        if (!form) return;

        const ok = window.confirm(
            'Deactivate your account? You will be signed out, and you can sign back in at any time to reactivate it.');

        if (!ok) event.preventDefault();
    });
})();
