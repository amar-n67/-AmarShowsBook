document.addEventListener("DOMContentLoaded", () => {
    const shell = document.getElementById("adminShell");
    const toggle = document.getElementById("adminNavToggle");
    const search = document.getElementById("adminPageSearch");
    const clear = document.getElementById("adminSearchClear");
    const exportButton = document.querySelector("[data-admin-export]");

    // This script is shared by admin pages for sidebar state, page search, and watermarked exports.
    if (shell && toggle) {
        const refreshToggleLabel = () => {
            const icon = toggle.querySelector(".admin-sidebar-toggle-icon");
            const isCompact = window.matchMedia("(max-width: 820px)").matches;
            const classOn = shell.classList.contains("nav-collapsed");
            const navVisible = isCompact ? classOn : !classOn;
            toggle.setAttribute("aria-expanded", String(navVisible));
            toggle.title = navVisible ? "Hide Sidebar" : "Show Sidebar";
            toggle.setAttribute("aria-label", navVisible ? "Hide Sidebar" : "Show Sidebar");
            if (icon) {
                icon.textContent = navVisible ? "‹" : "›";
            }
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

    const normalizeExportText = (value) => String(value || "")
        .replace(/\s+/g, " ")
        .trim();

    const escapeHtml = (value) => normalizeExportText(value)
        .replace(/&/g, "&amp;")
        .replace(/</g, "&lt;")
        .replace(/>/g, "&gt;")
        .replace(/"/g, "&quot;")
        .replace(/'/g, "&#39;");

    const isVisibleExportElement = (element) => {
        if (!element || element.hidden) {
            return false;
        }

        const style = window.getComputedStyle(element);
        return style.display !== "none" && style.visibility !== "hidden";
    };

    const getAdminPageName = () => {
        const title = document.querySelector(".admin-page-header h1, .admin-header h1, h1, h2");
        return normalizeExportText(title?.textContent || document.title || "admin-data")
            .replace(/\s+-\s+Admin Panel$/i, "")
            .replace(/[^a-z0-9]+/gi, "-")
            .replace(/^-+|-+$/g, "")
            .toLowerCase() || "admin-data";
    };

    const getExportWatermark = async () => {
        const fallback = `${window.location.origin}/images/brand/showtime-export-watermark.png`;

        try {
            const response = await fetch("/images/brand/showtime-export-watermark.png", { cache: "force-cache" });

            if (!response.ok) {
                return fallback;
            }

            const blob = await response.blob();

            return await new Promise((resolve) => {
                const reader = new FileReader();
                reader.onload = () => resolve(reader.result || fallback);
                reader.onerror = () => resolve(fallback);
                reader.readAsDataURL(blob);
            });
        }
        catch {
            return fallback;
        }
    };

    const buildWatermarkedWorkbookHtml = (rows, watermarkSource) => {
        const pageName = escapeHtml(getAdminPageName().replace(/-/g, " "));
        const msoPageName = normalizeExportText(getAdminPageName().replace(/-/g, " "))
            .replace(/&/g, "&&")
            .replace(/"/g, "'")
            .replace(/[\\\r\n]/g, " ");
        const msoPrintedBy = normalizeExportText(document.body?.dataset.printUser || "User")
            .replace(/&/g, "&&")
            .replace(/"/g, "'")
            .replace(/[\\\r\n]/g, " ");
        const columnCount = Math.max(1, ...rows.map((row) => row.length));
        const tableRows = rows.map((row, index) => {
            const isBlank = row.every((cell) => !normalizeExportText(cell));
            const previousBlank = index === 0 || rows[index - 1].every((cell) => !normalizeExportText(cell));
            const isHeader = !isBlank && previousBlank;

            if (isBlank) {
                return `<tr class="export-gap"><td colspan="${columnCount}"></td></tr>`;
            }

            const cells = row.map((cell) => `<td>${escapeHtml(cell)}</td>`).join("");
            return `<tr class="${isHeader ? "export-heading" : ""}">${cells}</tr>`;
        }).join("");

        return `<!doctype html>
<html>
<head>
<meta charset="utf-8">
<style>
@page {
    mso-page-orientation: landscape;
    margin: .35in;
    mso-header-data: "&LshowTime&C${msoPageName}&RWatermarked Export";
    mso-footer-data: "&LUser: ${msoPrintedBy}&CPage &P of &N&RPrinted: &D &T";
}
body {
    margin: 0;
    color: #111827;
    font-family: Arial, sans-serif;
}
.export-sheet {
    position: relative;
    min-height: 720px;
    padding: 24px;
}
.export-watermark {
    position: fixed;
    left: 50%;
    top: 50%;
    width: 430px;
    max-width: 56%;
    transform: translate(-50%, -50%) rotate(-18deg);
    opacity: .16;
    z-index: 0;
}
.export-content {
    position: relative;
    z-index: 1;
}
h1 {
    margin: 0 0 4px;
    font-size: 24px;
}
.export-meta {
    margin: 0 0 18px;
    color: #475569;
    font-size: 12px;
    font-weight: 700;
}
table {
    width: 100%;
    border-collapse: collapse;
    background: transparent;
}
td {
    border: 1px solid #cbd5e1;
    padding: 7px 9px;
    vertical-align: top;
    font-size: 12px;
    mso-number-format: "\\@";
}
.export-heading td {
    background: #111827;
    color: #ffffff;
    font-weight: 700;
    text-align: center;
}
.export-gap td {
    height: 18px;
    border: 0;
    background: transparent;
}
</style>
</head>
<body>
<div class="export-sheet">
    <img class="export-watermark" src="${watermarkSource}" alt="">
    <div class="export-content">
        <h1>showTime ${pageName}</h1>
        <table>${tableRows}</table>
    </div>
</div>
</body>
</html>`;
    };

    const downloadWatermarkedWorkbook = async (rows) => {
        const timestamp = new Date()
            .toISOString()
            .slice(0, 19)
            .replace(/[-:T]/g, "");
        const filename = `${getAdminPageName()}-${timestamp}.xls`;
        const watermarkSource = await getExportWatermark();
        const workbookHtml = buildWatermarkedWorkbookHtml(rows, watermarkSource);
        const blob = new Blob([`\uFEFF${workbookHtml}`], { type: "application/vnd.ms-excel;charset=utf-8;" });
        const url = URL.createObjectURL(blob);
        const link = document.createElement("a");

        link.href = url;
        link.download = filename;
        document.body.appendChild(link);
        link.click();
        link.remove();
        URL.revokeObjectURL(url);
    };

    const getTableExportRows = () => {
        const tables = [...document.querySelectorAll("table")]
            .filter((table) => isVisibleExportElement(table))
            .filter((table) => table.querySelector("tbody tr"));
        const rows = [];

        tables.forEach((table, tableIndex) => {
            const headers = [...table.querySelectorAll("thead th")]
                .map((cell, index) => ({
                    index,
                    text: normalizeExportText(cell.textContent)
                }))
                .filter((header) => header.text && !/^actions?$/i.test(header.text));
            const headerIndexes = headers.map((header) => header.index);
            const tableRows = [...table.querySelectorAll("tbody tr")]
                .filter((row) => isVisibleExportElement(row));

            if (!headers.length || !tableRows.length) {
                return;
            }

            if (tableIndex > 0 && rows.length) {
                rows.push([]);
            }

            rows.push(headers.map((header) => header.text));
            tableRows.forEach((row) => {
                const cells = [...row.children];
                rows.push(headerIndexes.map((index) => normalizeExportText(cells[index]?.textContent)));
            });
        });

        return rows;
    };

    const getCardExportRows = () => {
        const cards = [
            ...document.querySelectorAll(".access-card"),
            ...document.querySelectorAll("[data-admin-searchable]")
        ].filter((item, index, items) => items.indexOf(item) === index)
            .filter((item) => !item.closest("table"))
            .filter((item) => isVisibleExportElement(item));

        if (!cards.length) {
            return [];
        }

        return [
            ["Record", "Details"],
            ...cards.map((card, index) => [
                String(index + 1),
                normalizeExportText(card.textContent)
            ])
        ];
    };

    exportButton?.addEventListener("click", async () => {
        const previousLabel = exportButton.textContent;
        const serverExportUrl = exportButton.dataset.adminExportUrl || "";

        if (serverExportUrl) {
            exportButton.textContent = "Exporting...";
            exportButton.setAttribute("aria-busy", "true");
            exportButton.disabled = true;

            window.setTimeout(() => {
                window.location.href = serverExportUrl;
                window.setTimeout(() => {
                    exportButton.textContent = previousLabel;
                    exportButton.removeAttribute("aria-busy");
                    exportButton.disabled = false;
                }, 1800);
            }, 80);
            return;
        }

        const rows = getTableExportRows();
        const exportRows = rows.length ? rows : getCardExportRows();

        if (!exportRows.length) {
            exportButton.textContent = "No visible data";
            window.setTimeout(() => {
                exportButton.textContent = previousLabel;
            }, 1400);
            return;
        }

        exportButton.textContent = "Exporting...";
        exportButton.setAttribute("aria-busy", "true");
        exportButton.disabled = true;

        try {
            await downloadWatermarkedWorkbook(exportRows);
            exportButton.textContent = "Exported";
        }
        catch {
            exportButton.textContent = "Export failed";
        }
        finally {
            exportButton.removeAttribute("aria-busy");
            exportButton.disabled = false;
        }

        window.setTimeout(() => {
            exportButton.textContent = previousLabel;
        }, 1400);
    });

    document.querySelectorAll("[data-page-filter]").forEach((panel) => {
        const searchBox = panel.querySelector("[data-page-filter-search]");
        const statusBox = panel.querySelector("[data-page-filter-status]");
        const methodBox = panel.querySelector("[data-page-filter-method]");
        const priorityBox = panel.querySelector("[data-page-filter-priority]");
        const sortBox = panel.querySelector("[data-page-filter-sort]");
        const resetButton = panel.querySelector("[data-page-filter-reset]");
        const targetSelector = panel.getAttribute("data-page-filter-target") || "tbody tr";
        const tableCard = [...document.querySelectorAll(".card")]
            .find((card) => card.querySelector("table"));
        const preferredAnchor = document.querySelector("[data-page-filter-anchor]");
        const anchor = preferredAnchor
            || document.querySelector(".table-wrapper")
            || document.querySelector(".access-card-grid")
            || tableCard;

        if (anchor && anchor.parentElement) {
            anchor.parentElement.insertBefore(panel, anchor);
        }

        const normalize = (value) => String(value || "").trim().toLowerCase();
        const priorityRank = {
            high: 4,
            medium: 3,
            normal: 2,
            low: 1
        };
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

        const getRowText = (row) => normalize(row.dataset.filterSearch || row.textContent);
        const getRowTime = (row) => Number(row.dataset.sortTime || 0);
        const getRowNumber = (row, key) => Number(row.dataset[key] || 0);
        const compareRows = (left, right, sort) => {
            const leftPriority = priorityRank[normalize(left.dataset.filterPriority)] || 0;
            const rightPriority = priorityRank[normalize(right.dataset.filterPriority)] || 0;
            const leftStatus = normalize(left.dataset.filterStatus);
            const rightStatus = normalize(right.dataset.filterStatus);
            const leftSource = normalize(left.dataset.filterMethod);
            const rightSource = normalize(right.dataset.filterMethod);
            const leftTitle = normalize(left.dataset.sortTitle || left.dataset.filterSearch || left.textContent);
            const rightTitle = normalize(right.dataset.sortTitle || right.dataset.filterSearch || right.textContent);
            const leftTime = getRowTime(left);
            const rightTime = getRowTime(right);

            if (sort === "oldest") {
                return leftTime - rightTime || getRowText(left).localeCompare(getRowText(right));
            }

            if (sort === "priority") {
                return rightPriority - leftPriority || rightTime - leftTime;
            }

            if (sort === "status") {
                return leftStatus.localeCompare(rightStatus) || rightTime - leftTime;
            }

            if (sort === "source") {
                return leftSource.localeCompare(rightSource) || rightTime - leftTime;
            }

            if (sort === "newest") {
                return rightTime - leftTime || getRowText(left).localeCompare(getRowText(right));
            }

            if (sort === "title") {
                return leftTitle.localeCompare(rightTitle) || leftTime - rightTime;
            }

            if (sort === "tickets") {
                return getRowNumber(right, "sortTickets") - getRowNumber(left, "sortTickets") || leftTime - rightTime;
            }

            if (sort === "amount") {
                return getRowNumber(right, "sortAmount") - getRowNumber(left, "sortAmount") || leftTime - rightTime;
            }

            return 0;
        };

        const sortRows = () => {
            const sort = normalize(sortBox?.value);

            if (!sort) {
                return;
            }

            const groups = new Map();

            getRows().forEach((row) => {
                const parent = row.parentElement;

                if (!parent) {
                    return;
                }

                if (!groups.has(parent)) {
                    groups.set(parent, []);
                }

                groups.get(parent).push(row);
            });

            groups.forEach((rows, parent) => {
                rows
                    .sort((left, right) => compareRows(left, right, sort))
                    .forEach((row) => parent.appendChild(row));
            });
        };

        const applyPanelFilters = () => {
            const query = normalize(searchBox?.value);
            const status = normalize(statusBox?.value);
            const method = normalize(methodBox?.value);
            const priority = normalize(priorityBox?.value);

            getRows().forEach((row) => {
                const text = getRowText(row);
                const matchesSearch = !query || text.includes(query);
                const matchesStatus = matchesFilter(row, "filterStatus", status, text);
                const matchesMethod = matchesFilter(row, "filterMethod", method, text);
                const matchesPriority = matchesFilter(row, "filterPriority", priority, text);
                row.hidden = !(matchesSearch && matchesStatus && matchesMethod && matchesPriority);
            });

            sortRows();
        };

        searchBox?.addEventListener("input", applyPanelFilters);
        statusBox?.addEventListener("change", applyPanelFilters);
        methodBox?.addEventListener("change", applyPanelFilters);
        priorityBox?.addEventListener("change", applyPanelFilters);
        sortBox?.addEventListener("change", applyPanelFilters);
        resetButton?.addEventListener("click", () => {
            if (searchBox) searchBox.value = "";
            if (statusBox) statusBox.value = "";
            if (methodBox) methodBox.value = "";
            if (priorityBox) priorityBox.value = "";
            if (sortBox) sortBox.value = "";
            applyPanelFilters();
            searchBox?.focus();
        });
    });
});
