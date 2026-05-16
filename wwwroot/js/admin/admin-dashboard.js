// Human Comment: Admin shell controls for nav visibility and per-page search.
document.addEventListener("DOMContentLoaded", () => {
    const shell = document.getElementById("adminShell");
    const toggle = document.getElementById("adminNavToggle");
    const search = document.getElementById("adminPageSearch");
    const clear = document.getElementById("adminSearchClear");

    if (shell && toggle) {
        toggle.addEventListener("click", () => {
            const isCollapsed = shell.classList.toggle("nav-collapsed");
            toggle.setAttribute("aria-expanded", String(!isCollapsed));
            toggle.textContent = isCollapsed ? "☰ Show Menu" : "☰ Menu";
        });
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
});
