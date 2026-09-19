function showTab(tabId) {
        document.querySelectorAll('.admin-tab-content').forEach(el => el.style.display = 'none');
        document.querySelectorAll('.admin-tab-btn').forEach(btn => {
            btn.classList.remove('btn-secondary');
            btn.classList.add('btn-outline');
        });
        document.getElementById('tab-' + tabId).style.display = 'block';
        document.getElementById('btn-' + tabId).classList.remove('btn-outline');
        document.getElementById('btn-' + tabId).classList.add('btn-secondary');
    }
    
    // Ensure the orders tab is showing initially
    window.onload = function() {
        showTab('orders');
    }
