// Adds a JS confirmation step on top of native form submission for the
// destructive/disruptive actions (Start/Stop/Recycle).
// Defence in depth only: server-side performs all real authorization checks.
(function () {
    "use strict";
    document.addEventListener("submit", function (ev) {
        var form = ev.target;
        if (!form || !form.dataset || !form.dataset.confirm) return;
        if (!window.confirm(form.dataset.confirm)) {
            ev.preventDefault();
            ev.stopPropagation();
            return false;
        }
    }, true);
})();
