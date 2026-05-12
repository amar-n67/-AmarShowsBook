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
        });
    }

    const searchableItems = () => [
        ...document.querySelectorAll(".admin-table tbody tr"),
        ...document.querySelectorAll(".dashboard-card"),
        ...document.querySelectorAll(".stat-card"),
        ...document.querySelectorAll("[data-admin-searchable]")
    ];

    const filterPage = () => {
        if (!search) {
            return;
        }

        const query = search.value.trim().toLowerCase();

        searchableItems().forEach((item) => {
            const text = item.textContent?.toLowerCase() ?? "";
            item.hidden = query.length > 0 && !text.includes(query);
        });
    };

    search?.addEventListener("input", filterPage);

    clear?.addEventListener("click", () => {
        if (!search) {
            return;
        }

        search.value = "";
        filterPage();
        search.focus();
    });
});
