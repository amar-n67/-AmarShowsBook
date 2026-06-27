// Human Comment: Admin shell controls for nav visibility and per-page search.
document.addEventListener("DOMContentLoaded", () => {
    const shell = document.getElementById("adminShell");
    const toggle = document.getElementById("adminNavToggle");
    const search = document.getElementById("adminPageSearch");
    const clear = document.getElementById("adminSearchClear");

    if (shell && toggle) {
        const refreshToggleLabel = () => {
            const isCompact = window.matchMedia("(max-width: 820px)").matches;
            const classOn = shell.classList.contains("nav-collapsed");
            const navVisible = isCompact ? classOn : !classOn;
            toggle.setAttribute("aria-expanded", String(navVisible));
            toggle.textContent = navVisible ? "Menu" : "Show Menu";
        };

        toggle.addEventListener("click", () => {
            shell.classList.toggle("nav-collapsed");
            refreshToggleLabel();
        });

        window.addEventListener("resize", refreshToggleLabel);
        refreshToggleLabel();
    }

    const searchableItems = [
        ...document.querySelectorAll(".admin-table tbody tr"),
        ...document.querySelectorAll(".dashboard-card"),
        ...document.querySelectorAll(".stat-card"),
        ...document.querySelectorAll("[data-admin-searchable]")
    ].map((item) => ({
        item,
        text: item.textContent?.toLowerCase() ?? ""
    }));

    let searchTimer;

    const filterPage = () => {
        if (!search) {
            return;
        }

        const query = search.value.trim().toLowerCase();

        searchableItems.forEach(({ item, text }) => {
            item.hidden = query.length > 0 && !text.includes(query);
        });
    };

    search?.addEventListener("input", () => {
        window.clearTimeout(searchTimer);
        searchTimer = window.setTimeout(filterPage, 120);
    });

    clear?.addEventListener("click", () => {
        if (!search) {
            return;
        }

        search.value = "";
        filterPage();
        search.focus();
    });

    document.querySelectorAll("[data-page-filter]").forEach((panel) => {
        const searchBox = panel.querySelector("[data-page-filter-search]");
        const statusBox = panel.querySelector("[data-page-filter-status]");
        const methodBox = panel.querySelector("[data-page-filter-method]");
        const resetButton = panel.querySelector("[data-page-filter-reset]");
        const targetSelector = panel.getAttribute("data-page-filter-target") || "tbody tr";
        const tableCard = [...document.querySelectorAll(".card")]
            .find((card) => card.querySelector("table"));
        const anchor = document.querySelector(".table-wrapper")
            || document.querySelector(".access-card-grid")
            || tableCard;

        if (anchor && anchor.parentElement) {
            anchor.parentElement.insertBefore(panel, anchor);
        }

        const getRows = () => [...document.querySelectorAll(targetSelector)]
            .filter((row) => !panel.contains(row))
            .filter((row) => !row.closest(".stats-grid"))
            .filter((row) => !row.closest("form"))
            .filter((row, index, rows) => rows.indexOf(row) === index);

        const applyPanelFilters = () => {
            const query = (searchBox?.value || "").trim().toLowerCase();
            const status = (statusBox?.value || "").trim().toLowerCase();
            const method = (methodBox?.value || "").trim().toLowerCase();

            getRows().forEach((row) => {
                const text = row.textContent?.toLowerCase() || "";
                const matchesSearch = !query || text.includes(query);
                const matchesStatus = !status || text.includes(status);
                const matchesMethod = !method || text.includes(method);
                row.hidden = !(matchesSearch && matchesStatus && matchesMethod);
            });
        };

        searchBox?.addEventListener("input", applyPanelFilters);
        statusBox?.addEventListener("change", applyPanelFilters);
        methodBox?.addEventListener("change", applyPanelFilters);
        resetButton?.addEventListener("click", () => {
            if (searchBox) searchBox.value = "";
            if (statusBox) statusBox.value = "";
            if (methodBox) methodBox.value = "";
            applyPanelFilters();
            searchBox?.focus();
        });
    });
});
