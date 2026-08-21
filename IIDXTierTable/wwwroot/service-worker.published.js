// 주의! 오프라인 지원을 포함하여 애플리케이션을 배포하기 전에 관련 주의 사항을 충분히 이해했는지 확인하세요.
// 자세한 내용은 https://aka.ms/blazor-offline-considerations 을 참고하세요.

self.importScripts('./service-worker-assets.js');
self.addEventListener('install', event => event.waitUntil(onInstall(event)));
self.addEventListener('activate', event => event.waitUntil(onActivate(event)));
self.addEventListener('fetch', event => event.respondWith(onFetch(event)));

const cacheNamePrefix = 'offline-cache-';
const cacheName = `${cacheNamePrefix}${self.assetsManifest.version}`;
const offlineAssetsInclude = [ /\.dll$/, /\.pdb$/, /\.wasm/, /\.html/, /\.js$/, /\.json$/, /\.css$/, /\.woff$/, /\.png$/, /\.jpe?g$/, /\.gif$/, /\.ico$/, /\.blat$/, /\.dat$/, /\.webmanifest$/ ];
const offlineAssetsExclude = [ /^service-worker\.js$/ ];

// Service Worker의 scope를 기준으로 리소스 경로를 계산합니다.
// 이렇게 하면 /IIDXTierTable/ 같은 GitHub Pages 하위 경로에서도 앱이 정상적으로 동작합니다.
const baseUrl = new URL('./', self.registration.scope);
const manifestUrlList = self.assetsManifest.assets.map(asset => new URL(asset.url, baseUrl).href);

async function onInstall(event) {
    console.info('Service worker: Install');

    // 리소스 manifest에서 조건에 맞는 모든 항목을 가져와 캐시합니다.
    const assetsRequests = self.assetsManifest.assets
        .filter(asset => offlineAssetsInclude.some(pattern => pattern.test(asset.url)))
        .filter(asset => !offlineAssetsExclude.some(pattern => pattern.test(asset.url)))
        .map(asset => new Request(new URL(asset.url, baseUrl), { integrity: asset.hash, cache: 'no-cache' }));
    await caches.open(cacheName).then(cache => cache.addAll(assetsRequests));
}

async function onActivate(event) {
    console.info('Service worker: Activate');

    // 사용하지 않는 캐시를 삭제합니다.
    const cacheKeys = await caches.keys();
    await Promise.all(cacheKeys
        .filter(key => key.startsWith(cacheNamePrefix) && key !== cacheName)
        .map(key => caches.delete(key)));
}

async function onFetch(event) {
    const requestUrl = new URL(event.request.url);
    const isApiRequest = event.request.method === 'GET'
        && requestUrl.pathname.includes('/api/');

    if (isApiRequest) {
        return networkFirstApi(event.request);
    }

    let cachedResponse = null;
    if (event.request.method === 'GET') {
        // 오프라인 리소스에 대한 요청이 아니라면 모든 탐색 요청에 대해 캐시된 index.html을 제공하려고 시도합니다.
        // 서버에서 렌더링해야 하는 URL이 있다면 다음 조건에서 해당 URL을 제외하세요.
        const shouldServeIndexHtml = event.request.mode === 'navigate'
            && !manifestUrlList.some(url => url === event.request.url);

        const request = shouldServeIndexHtml ? new URL('index.html', baseUrl).href : event.request;
        const cache = await caches.open(cacheName);
        cachedResponse = await cache.match(request);
    }

    if (cachedResponse) {
        return cachedResponse;
    }

    try {
        return await fetch(event.request);
    } catch (error) {
        // 거부된 FetchEvent Promise를 처리되지 않은 상태로 남기지 않습니다.
        // 탐색 요청인 경우 캐시된 앱 셸이 있으면 사용합니다.
        if (event.request.mode === 'navigate') {
            const cache = await caches.open(cacheName);
            const offlinePage = await cache.match(new URL('index.html', baseUrl).href);
            if (offlinePage) {
                return offlinePage;
            }
        }

        // Response.error()는 브라우저 콘솔에 FetchEvent 네트워크 오류 경고를 발생시키므로,
        // 정상적인 HTTP 오류 응답으로 변환합니다.
        return new Response('네트워크에 연결할 수 없습니다.', {
            status: 503,
            statusText: 'Service Unavailable',
            headers: { 'Content-Type': 'text/plain; charset=utf-8' }
        });
    }
}

async function networkFirstApi(request) {
    const cache = await caches.open(cacheName);

    try {
        const response = await fetch(request);
        if (response.ok) {
            await cache.put(request, response.clone());
        }

        return response;
    } catch (error) {
        const cachedResponse = await cache.match(request);
        if (cachedResponse) {
            return cachedResponse;
        }

        return new Response('API에 연결할 수 없습니다.', {
            status: 503,
            statusText: 'Service Unavailable',
            headers: { 'Content-Type': 'text/plain; charset=utf-8' }
        });
    }
}
