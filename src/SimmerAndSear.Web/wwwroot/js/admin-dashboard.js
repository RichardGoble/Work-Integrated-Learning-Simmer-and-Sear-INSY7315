(() => {
    const forms = {
        product: ['products-grid', '/Admin/Products/Create', 'Add New Product'],
        journal: ['journal-grid', '/Admin/Journal/Create', 'Add New Entry'],
        partner: ['partners-grid', '/Admin/Partners/Create', 'Add Partner']
    };
    const attributes = {
        cookwareType: ['types', 'filter-type', 'type-list'],
        material: ['materials', 'filter-material', 'mat-list'],
        brand: ['brands', 'filter-brand', 'brand-list'],
        sizeLabel: ['sizes', 'filter-size', 'size-list'],
        category: ['ranges', 'filter-range', 'cat-list']
    };

    function showForm(key, visible, data) {
        const panel = document.getElementById('form-add-' + key);
        const form = panel.querySelector('form');
        form.reset();
        panel.querySelector('[role="alert"]')?.remove();
        form.action = forms[key][1];
        panel.querySelector('h3').textContent = forms[key][2];
        if (data) {
            form.action = '/Admin/' + (key === 'product' ? 'Products' : 'Journal') + '/' + encodeURIComponent(data.id) + '/edit';
            panel.querySelector('h3').textContent = key === 'product' ? 'Edit Product' : 'Edit Entry';
            for (const input of form.elements) {
                if (input.type !== 'file' && data[input.name] !== undefined) input.value = data[input.name] ?? '';
            }
            if (key === 'journal') form.elements.content.value = data.body;
        }
        if (key === 'product' || key === 'journal') form.elements.imageFile.required = !data;
        panel.style.display = visible ? 'block' : 'none';
        document.getElementById(forms[key][0]).style.display = visible ? 'none' : 'block';
        if (visible) form.querySelector('input:not([type="hidden"])')?.focus();
    }

    function filterAttributes(value) {
        document.getElementById('select-attribute-filter').value = value;
        for (const [section] of Object.values(attributes)) {
            document.getElementById('attr-sec-' + section).style.display = value === 'all' || value === section ? 'block' : 'none';
        }
    }

    function showError(container, message) {
        let error = container.querySelector('[role="alert"]');
        if (!error) {
            error = document.createElement('p');
            error.setAttribute('role', 'alert');
            error.style.color = '#b91c1c';
            container.prepend(error);
        }
        error.textContent = message;
    }

    async function request(url, options) {
        let response;
        try {
            response = await fetch(url, { ...options, headers: { Accept: 'application/json', ...options?.headers } });
        } catch {
            throw new Error('The connection was interrupted. Check that the website is running and reselect the image. Check the catalogue before retrying, as the save may have completed.');
        }
        if (response.status === 401 || response.status === 403) {
            throw new Error('Sign in with an administrator account before saving.');
        }
        if (!response.ok) {
            const error = await response.json().catch(() => ({}));
            throw new Error(error.message || error.title || 'Unable to save changes. Please try again.');
        }
        if (response.redirected && new URL(response.url).pathname.toLowerCase().includes('/auth/')) {
            throw new Error('Your session has expired. Sign in again before saving.');
        }
        return response;
    }

    document.addEventListener('click', async event => {
        const button = event.target.closest('[data-toggle-form], [data-filter-attributes], [data-add-attribute], [data-edit-product], [data-edit-journal]');
        if (!button) return;
        event.preventDefault();
        if (button.dataset.toggleForm) {
            const key = button.dataset.toggleForm;
            showForm(key, document.getElementById('form-add-' + key).style.display === 'none');
        } else if (button.dataset.filterAttributes) {
            filterAttributes(button.dataset.filterAttributes === 'all' ? 'all' : document.getElementById('select-attribute-filter').value);
        } else if (button.dataset.addAttribute) {
            const field = button.dataset.addAttribute;
            const input = document.getElementById(button.dataset.input);
            const section = document.getElementById('attr-sec-' + attributes[field][0]);
            if (!input.value.trim()) { input.focus(); return; }
            const body = new FormData();
            body.set('field', field);
            body.set('value', input.value.trim());
            body.set('__RequestVerificationToken', document.querySelector('[name="__RequestVerificationToken"]').value);
            button.disabled = true;
            try {
                const response = await request('/Admin/Attributes', { method: 'POST', body });
                const { value } = await response.json();
                const badges = section.querySelector('div[style*="flex-wrap: wrap"]');
                if (![...badges.children].some(b => b.textContent.trim().toLowerCase() === value.toLowerCase())) {
                    const badge = document.createElement('div');
                    badge.style.cssText = 'background: #f1f5f9; padding: 0.4rem 0.8rem; border-radius: 50px; font-size: 0.85rem; color: #334155;';
                    badge.textContent = value;
                    badges.append(badge);
                }
                for (const id of attributes[field].slice(1)) {
                    const select = document.getElementById(id);
                    if (![...select.options].some(o => o.value.toLowerCase() === value.toLowerCase())) select.append(new Option(value, value));
                }
                section.querySelector('[role="alert"]')?.remove();
                input.value = '';
            } catch (error) { showError(section, error.message); }
            finally { button.disabled = false; }
        } else {
            const key = button.dataset.editProduct ? 'product' : 'journal';
            const id = button.dataset.editProduct || button.dataset.editJournal;
            try {
                const response = await request('/Admin/' + (key === 'product' ? 'Products' : 'Journal') + '/' + encodeURIComponent(id));
                showForm(key, true, await response.json());
            } catch (error) { showError(button.closest('.admin-tab-content'), error.message); }
        }
    });

    document.addEventListener('change', event => {
        if (event.target.matches('[data-submit-on-change]')) event.target.form.requestSubmit();
        if (event.target.matches('[data-attribute-filter]')) filterAttributes(event.target.value);
    });

    document.addEventListener('submit', async event => {
        const form = event.target;
        if (form.dataset.confirm && !window.confirm(form.dataset.confirm)) { event.preventDefault(); return; }
        if (!form.matches('[data-admin-save]')) return;
        event.preventDefault();
        if (!form.reportValidity()) return;
        for (const input of form.querySelectorAll('input[type="file"]')) {
            if ([...input.files].some(file => file.size > 10 * 1024 * 1024)) {
                showError(form, 'Choose an image smaller than 10 MB.');
                return;
            }
        }
        const submit = form.querySelector('[type="submit"]');
        if (submit.disabled) return;
        submit.disabled = true;
        try {
            const response = await request(form.action, { method: 'POST', body: new FormData(form) });
            const result = await response.json();
            if (!result.success) throw new Error('The save could not be confirmed. Please check the catalogue before retrying.');
            window.history.replaceState(null, '', '/Admin#' + (form.action.includes('/Products/') ? 'products' : form.action.includes('/Journal/') ? 'journal' : 'gallery'));
            window.location.reload();
        } catch (error) { showError(form, error.message); }
        finally { submit.disabled = false; }
    });

    function restoreTab() {
        const tab = window.location.hash.slice(1);
        window.showTab(document.getElementById('tab-' + tab) ? tab : 'orders');
    }
    window.addEventListener('hashchange', restoreTab);
    window.addEventListener('DOMContentLoaded', restoreTab);
})();
