(function () {
    const initial = window.webInventorySession;
    if (!initial || !initial.isAuthenticated) {
        return;
    }

    async function checkSession() {
        try {
            const response = await fetch('/Session/State', {
                headers: { 'X-Requested-With': 'XMLHttpRequest' },
                cache: 'no-store'
            });

            if (!response.ok || response.redirected) {
                window.location.href = '/';
                return;
            }

            const state = await response.json();
            if (!state.isAuthenticated) {
                window.location.href = '/';
                return;
            }

            if (initial.isAdmin && !state.isAdmin) {
                window.location.href = '/';
                return;
            }

            if (state.isAdmin !== initial.isAdmin) {
                window.location.reload();
            }
        } catch {
            // Ignore transient network errors; the next poll will retry.
        }
    }

    window.setInterval(checkSession, 5000);
})();
