if (typeof window.showTab !== 'function') {
    window.showTab = function(tabId) {
        document.querySelectorAll('.admin-tab-content').forEach(function(el) { el.style.display = 'none'; });
        document.querySelectorAll('.admin-tab-btn').forEach(function(b) {
            b.classList.remove('btn-secondary');
            b.classList.add('btn-outline');
        });
        var targetContent = document.getElementById('tab-' + tabId);
        var targetBtn = document.getElementById('btn-' + tabId);
        if (targetContent) targetContent.style.display = 'block';
        if (targetBtn) {
            targetBtn.classList.remove('btn-outline');
            targetBtn.classList.add('btn-secondary');
        }
        if (history.replaceState) {
            history.replaceState(null, null, '#' + tabId);
        } else {
            window.location.hash = tabId;
        }
    };
}

const pageActions = {
    'action1': function(event) { showTab('orders') },
    'action2': function(event) { showTab('bulk') },
    'action3': function(event) { showTab('inquiries') },
    'action4': function(event) { showTab('products') },
    'action5': function(event) { showTab('attributes') },
    'action6': function(event) { showTab('journal') },
    'action7': function(event) { showTab('gallery') },
    'action8': function(event) { showTab('reviews') },
    'action9': function(event) { showTab('partners') },
    'action10': function(event) { showTab('settings') },
    'action11': function(event) { showTab('admins') },
    'action12': function(event) { showTab('audit') },
    'action13': function(event) { document.getElementById('form-add-product').style.display='block'; document.getElementById('products-grid').style.display='none'; },
    'action14': function(event) { document.getElementById('form-add-product').style.display='none'; document.getElementById('products-grid').style.display='block'; },
    'action15': function(event) { document.getElementById('form-add-journal').style.display='block'; document.getElementById('journal-grid').style.display='none'; },
    'action16': function(event) { document.getElementById('form-add-journal').style.display='none'; document.getElementById('journal-grid').style.display='block'; },
    'action17': function(event) { document.getElementById('form-add-gallery-image').style.display='block'; document.getElementById('gallery-container').style.display='none'; },
    'action18': function(event) { document.getElementById('form-add-gallery-image').style.display='none'; document.getElementById('gallery-container').style.display='flex'; },
    'action19': function(event) { document.getElementById('form-add-partner').style.display='block'; document.getElementById('partners-grid').style.display='none'; },
    'action20': function(event) { document.getElementById('form-add-partner').style.display='none'; document.getElementById('partners-grid').style.display='block'; },
    'action21': function(event) { event.preventDefault(); window.location.href='/Cart/OrderConfirmation'; },
    'action22': function(event) { event.preventDefault(); alert('Message sent (UI mock)'); },
    'action23': function(event) { togglePreference('dark-mode') },
    'action24': function(event) { togglePreference('high-contrast') },
    'action25': function(event) { togglePreference('large-text') },
    'action26': function(event) { togglePreference('dyslexia-font') },
    'action27': function(event) { togglePreference('reduce-animations') },
};
for (const eventName of ['click', 'submit']) {
    document.addEventListener(eventName, function(event) {
        const target = event.target.closest(`[data-${eventName}-action]`);
        if (!target) return;
        const handler = pageActions[target.getAttribute(`data-${eventName}-action`)];
        if (handler) handler.call(target, event);
    });
}