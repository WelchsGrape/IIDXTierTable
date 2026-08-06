async function registerServiceWorker() {
    if (!('serviceWorker' in navigator)) {
        return;
    }

    let refreshing = false;
    navigator.serviceWorker.addEventListener('controllerchange', () => {
        if (refreshing) {
            return;
        }
        refreshing = true;
        window.location.reload();
    });

    try {
        const registration = await navigator.serviceWorker.register('service-worker.js', { updateViaCache: 'none' });

        async function checkVersion() {
            try {
                const response = await fetch('service-worker-assets.js', { cache: 'no-cache' });
                if (!response.ok) {
                    return;
                }
                const manifest = await response.json();
                const latestVersion = manifest.version;
                const currentVersion = localStorage.getItem('appVersion');

                if (currentVersion !== latestVersion) {
                    localStorage.setItem('appVersion', latestVersion);
                    if (registration.waiting) {
                        registration.waiting.postMessage({ type: 'SKIP_WAITING' });
                    } else if (registration.installing) {
                        registration.installing.addEventListener('statechange', (event) => {
                            if (event.target.state === 'installed') {
                                registration.waiting?.postMessage({ type: 'SKIP_WAITING' });
                            }
                        });
                    } else {
                        await registration.update();
                    }
                }
            } catch (error) {
                console.warn('버전 체크 실패:', error);
            }
        }

        registration.addEventListener('updatefound', () => {
            const newWorker = registration.installing;
            if (!newWorker) {
                return;
            }
            newWorker.addEventListener('statechange', () => {
                if (newWorker.state === 'installed' && navigator.serviceWorker.controller) {
                    newWorker.postMessage({ type: 'SKIP_WAITING' });
                }
            });
        });

        await checkVersion();
    } catch (error) {
        console.warn('Service worker registration failed:', error);
    }
}

registerServiceWorker();
