// View toggle (cards / list) + name filter for the Application Pools page.
// Pure progressive enhancement: with JS off, the cards view is what you get.
(function () {
    "use strict";

    var STORAGE_KEY = "iisweb.poolsView";
    var validViews  = ["cards", "list"];

    var cards   = document.getElementById("poolsCards");
    var list    = document.getElementById("poolsList");
    var toggle  = document.getElementById("viewToggle");
    var search  = document.getElementById("poolSearch");
    var noRes   = document.getElementById("noResults");
    var counter = document.getElementById("poolCount");

    if (!cards || !list || !toggle) return;

    var cardItems = cards.querySelectorAll("[data-pool-item]");
    var listItems = list.querySelectorAll("[data-pool-item]");
    var totalPools = cardItems.length;

    function applyView(name) {
        if (validViews.indexOf(name) === -1) name = "cards";
        var isCards = name === "cards";
        cards.classList.toggle("d-none", !isCards);
        list.classList.toggle("d-none", isCards);
        var buttons = toggle.querySelectorAll("button[data-view]");
        buttons.forEach(function (b) {
            var match = b.dataset.view === name;
            b.classList.toggle("active", match);
            b.setAttribute("aria-pressed", match ? "true" : "false");
        });
        try { localStorage.setItem(STORAGE_KEY, name); } catch (e) { /* ignore */ }
    }

    toggle.addEventListener("click", function (e) {
        var btn = e.target.closest("button[data-view]");
        if (!btn) return;
        applyView(btn.dataset.view);
    });

    var initial = "cards";
    try {
        var stored = localStorage.getItem(STORAGE_KEY);
        if (stored && validViews.indexOf(stored) !== -1) initial = stored;
    } catch (e) { /* ignore */ }
    applyView(initial);

    if (search) {
        var debounceTimer = null;

        function applyFilter() {
            var q = (search.value || "").trim().toLowerCase();
            var visible = 0;

            cardItems.forEach(function (it) {
                var name = it.getAttribute("data-pool-name") || "";
                var ok = !q || name.indexOf(q) !== -1;
                it.classList.toggle("d-none", !ok);
                if (ok) visible++;
            });
            // Mirror the same hide/show on the list rows so both views stay in sync.
            listItems.forEach(function (it) {
                var name = it.getAttribute("data-pool-name") || "";
                var ok = !q || name.indexOf(q) !== -1;
                it.classList.toggle("d-none", !ok);
            });

            if (counter) {
                counter.textContent = q
                    ? "(" + visible + " of " + totalPools + ")"
                    : "(" + totalPools + ")";
            }
            if (noRes) noRes.classList.toggle("d-none", visible > 0);
        }

        search.addEventListener("input", function () {
            clearTimeout(debounceTimer);
            debounceTimer = setTimeout(applyFilter, 80);
        });

        // Re-apply on load in case the browser restored a previous value.
        applyFilter();
    }
})();
