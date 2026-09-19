// Mobile Menu Toggle
        document.getElementById('mobile-menu-toggle').addEventListener('click', function() {
            var menu = document.getElementById('nav-links-menu');
            menu.classList.toggle('open');
            this.setAttribute('aria-expanded', menu.classList.contains('open'));
        });

        // Accessibility Preferences
        function togglePreference(className) {
            document.documentElement.classList.toggle(className);
        }
    