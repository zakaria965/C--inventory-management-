// Sidebar Toggle Functionality
document.addEventListener('DOMContentLoaded', function() {
    const sidebar = document.getElementById('sidebar');
    const toggleButton = document.getElementById('sidebarToggle');
    
    if (!sidebar || !toggleButton) {
        return;
    }
    
    // Check if sidebar state is saved in localStorage
    const savedState = localStorage.getItem('sidebarHidden');
    if (savedState === 'true') {
        sidebar.classList.add('hidden');
    }
    
    // Toggle sidebar on button click
    toggleButton.addEventListener('click', function() {
        sidebar.classList.toggle('hidden');
        
        // Save state to localStorage
        const isHidden = sidebar.classList.contains('hidden');
        localStorage.setItem('sidebarHidden', isHidden.toString());
    });
    
    // Close sidebar when clicking outside on mobile
    document.addEventListener('click', function(event) {
        if (window.innerWidth <= 768) {
            const isClickInsideSidebar = sidebar.contains(event.target);
            const isClickOnToggle = toggleButton.contains(event.target);
            
            if (!isClickInsideSidebar && !isClickOnToggle && !sidebar.classList.contains('hidden')) {
                sidebar.classList.add('hidden');
                localStorage.setItem('sidebarHidden', 'true');
            }
        }
    });
    
    // Handle window resize
    window.addEventListener('resize', function() {
        if (window.innerWidth > 768) {
            // On larger screens, restore sidebar if it was hidden
            const savedState = localStorage.getItem('sidebarHidden');
            if (savedState === 'false') {
                sidebar.classList.remove('hidden');
            }
        }
    });
});

// Logout confirmation functions (will be overridden by layout script if needed)
if (typeof window.confirmLogout === 'undefined') {
    window.confirmLogout = function(event) {
        if (event) {
            event.preventDefault();
            event.stopPropagation();
        }
        const modal = document.getElementById('logoutModal');
        if (modal) {
            modal.style.display = 'flex';
        }
        return false;
    };
}

if (typeof window.closeLogoutModal === 'undefined') {
    window.closeLogoutModal = function() {
        const modal = document.getElementById('logoutModal');
        if (modal) {
            modal.style.display = 'none';
        }
    };
}

if (typeof window.proceedLogout === 'undefined') {
    window.proceedLogout = function() {
        window.location.href = '/Account/Logout';
    };
}

// Dark Mode Toggle Functionality
function initDarkMode() {
    const darkModeToggle = document.getElementById('darkModeToggle');
    const darkModeIcon = document.getElementById('darkModeIcon');
    const darkModeText = document.getElementById('darkModeText');
    const body = document.body;

    if (!darkModeToggle || !darkModeIcon) {
        return;
    }

    // Check if dark mode is saved in localStorage
    const darkModeState = localStorage.getItem('darkMode');
    if (darkModeState === 'enabled') {
        body.classList.add('dark-mode');
        darkModeToggle.classList.add('active');
        darkModeIcon.classList.remove('fa-moon');
        darkModeIcon.classList.add('fa-sun');
        if (darkModeText) {
            darkModeText.textContent = 'Light Mode';
        }
    } else {
        if (darkModeText) {
            darkModeText.textContent = 'Dark Mode';
        }
    }

    // Toggle dark mode - use onclick to ensure it works
    darkModeToggle.onclick = function(e) {
        e.preventDefault();
        e.stopPropagation();
        
        body.classList.toggle('dark-mode');
        darkModeToggle.classList.toggle('active');
        
        if (body.classList.contains('dark-mode')) {
            darkModeIcon.classList.remove('fa-moon');
            darkModeIcon.classList.add('fa-sun');
            localStorage.setItem('darkMode', 'enabled');
            if (darkModeText) {
                darkModeText.textContent = 'Light Mode';
            }
        } else {
            darkModeIcon.classList.remove('fa-sun');
            darkModeIcon.classList.add('fa-moon');
            localStorage.setItem('darkMode', 'disabled');
            if (darkModeText) {
                darkModeText.textContent = 'Dark Mode';
            }
        }
    };
}

// Initialize when DOM is ready
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', initDarkMode);
} else {
    initDarkMode();
}

// Also try after a short delay in case elements load later
setTimeout(initDarkMode, 200);

