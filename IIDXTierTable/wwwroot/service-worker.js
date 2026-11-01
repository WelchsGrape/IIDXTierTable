// 오프라인 캐시를 사용하지 않습니다.
// 이전에 설치된 서비스 워커와 캐시를 정리하기 위한 파일입니다.
self.addEventListener('install', () => self.skipWaiting());
self.addEventListener('activate', event => event.waitUntil((async () => {
    const keys = await caches.keys();
    await Promise.all(keys.map(key => caches.delete(key)));
    await self.registration.unregister();
    const clients = await self.clients.matchAll({ type: 'window' });
    clients.forEach(client => client.navigate(client.url));
})()));
