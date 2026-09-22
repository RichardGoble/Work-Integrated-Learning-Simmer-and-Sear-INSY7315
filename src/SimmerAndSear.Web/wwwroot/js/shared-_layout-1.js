// Mobile Menu Toggle
var mobileToggle = document.getElementById('mobile-menu-toggle');
if (mobileToggle) {
    mobileToggle.addEventListener('click', function() {
        var menu = document.getElementById('nav-links-menu');
        if (menu) {
            menu.classList.toggle('open');
            this.setAttribute('aria-expanded', menu.classList.contains('open'));
        }
    });
}

// Accessibility Preferences
function togglePreference(className) {
    document.documentElement.classList.toggle(className);
}