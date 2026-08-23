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
        const priorityBox = panel.querySelector("[data-page-filter-priority]");
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

        const normalize = (value) => String(value || "").trim().toLowerCase();
        const getFilterValues = (row, key) => normalize(row.dataset[key])
            .split("|")
            .map((value) => value.trim())
            .filter(Boolean);
        const matchesFilter = (row, key, filterValue, text) => {
            if (!filterValue) {
                return true;
            }

            const values = getFilterValues(row, key);
            return values.length > 0
                ? values.includes(filterValue)
                : text.includes(filterValue);
        };

        const getRows = () => [...document.querySelectorAll(targetSelector)]
            .filter((row) => !panel.contains(row))
            .filter((row) => !row.closest(".stats-grid"))
            .filter((row) => !row.matches(".admin-detail-panel[data-admin-searchable]"))
            .filter((row, index, rows) => rows.indexOf(row) === index);

        const applyPanelFilters = () => {
            const query = normalize(searchBox?.value);
            const status = normalize(statusBox?.value);
            const method = normalize(methodBox?.value);
            const priority = normalize(priorityBox?.value);

            getRows().forEach((row) => {
                const text = normalize(row.dataset.filterSearch || row.textContent);
                const matchesSearch = !query || text.includes(query);
                const matchesStatus = matchesFilter(row, "filterStatus", status, text);
                const matchesMethod = matchesFilter(row, "filterMethod", method, text);
                const matchesPriority = matchesFilter(row, "filterPriority", priority, text);
                row.hidden = !(matchesSearch && matchesStatus && matchesMethod && matchesPriority);
            });
        };

        searchBox?.addEventListener("input", applyPanelFilters);
        statusBox?.addEventListener("change", applyPanelFilters);
        methodBox?.addEventListener("change", applyPanelFilters);
        priorityBox?.addEventListener("change", applyPanelFilters);
        resetButton?.addEventListener("click", () => {
            if (searchBox) searchBox.value = "";
            if (statusBox) statusBox.value = "";
            if (methodBox) methodBox.value = "";
            if (priorityBox) priorityBox.value = "";
            applyPanelFilters();
            searchBox?.focus();
        });
    });
});
