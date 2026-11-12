(function () {
    document.addEventListener('DOMContentLoaded', function () {
        var book = document.querySelector('.sf-book');
        if (!book) return;

        var reduceMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
        var TURN_MS = 480;

        function turnThenGo(action) {
            if (reduceMotion) { action(); return; }
            book.classList.add('is-turning');
            window.setTimeout(action, TURN_MS);
        }

        var form = book.querySelector('form');
        if (form) {
            form.addEventListener('submit', function (e) {
                if (book.classList.contains('is-turning')) return;
                e.preventDefault();
                turnThenGo(function () { form.submit(); });
            });
        }

        document.querySelectorAll('.sf-switch a').forEach(function (link) {
            link.addEventListener('click', function (e) {
                var href = link.getAttribute('href');
                if (!href) return;
                e.preventDefault();
                turnThenGo(function () { window.location.href = href; });
            });
        });
    });
})();