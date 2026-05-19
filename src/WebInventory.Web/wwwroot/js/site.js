(() => {
    const storageKey = 'webinventory-theme';
    const toggle = document.getElementById('themeToggle');

    if (!toggle) {
        return;
    }

    function getTheme() {
        return document.documentElement.getAttribute('data-bs-theme') || 'light';
    }

    function setTheme(theme) {
        document.documentElement.setAttribute('data-bs-theme', theme);
        localStorage.setItem(storageKey, theme);
        toggle.textContent = theme === 'dark' ? 'Light' : 'Dark';
    }

    setTheme(getTheme());

    toggle.addEventListener('click', () => {
        setTheme(getTheme() === 'dark' ? 'light' : 'dark');
    });
})();
