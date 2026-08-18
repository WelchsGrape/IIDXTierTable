window.iidxStorage = {
    set: function (key, value) {
        localStorage.setItem(key, value);
    },
    get: function (key) {
        return localStorage.getItem(key);
    },
    isMobile: function () {
        return window.matchMedia('(max-width: 767px)').matches;
    },
    remove: function (key) {
        localStorage.removeItem(key);
    }
};
