window.ClockIT = window.ClockIT || {};

ClockIT.LiveSearch = (function () {

    let config = {
        formId: "searchForm",
        tableContainerId: "tableContainer",
        debounceDelay: 400
    };

    let debounceTimer;

    function getForm() {
        return document.getElementById(config.formId);
    }

    function getTableContainer() {
        return document.getElementById(config.tableContainerId);
    }

    function serializeForm(form) {
        return new URLSearchParams(new FormData(form)).toString();
    }

    function updateTable(url) {

        const tableContainer = getTableContainer();

        if (!tableContainer)
            return;

        const overlay = document.getElementById("loadingOverlay");

        if (overlay)
            overlay.classList.remove("d-none");

        fetch(url, {
            headers: {
                "X-Requested-With": "XMLHttpRequest"
            }
        })
            .then(response => {

                if (!response.ok)
                    throw new Error("Request failed.");

                return response.text();

            })
            .then(html => {

                const wrapper = document.createElement("div");

                wrapper.innerHTML = html;

                tableContainer.innerHTML = "";

                if (overlay)
                    tableContainer.appendChild(overlay);

                tableContainer.appendChild(wrapper);

                history.replaceState({}, "", url);

                wirePagination();

                if (overlay)
                    overlay.classList.add("d-none");
            })
            .catch(error => {

                if (overlay)
                    overlay.classList.add("d-none");

                console.error(error);
            });

    }

    function performSearch() {

        const form = getForm();

        if (!form)
            return;

        const url = form.action + "?" + serializeForm(form);

        updateTable(url);

    }

    function debounceSearch() {

        clearTimeout(debounceTimer);

        debounceTimer = setTimeout(function () {

            performSearch();

        }, config.debounceDelay);

    }

    function wireSearch() {

        const searchInput = document.getElementById("searchTerm");

        const statusFilter = document.getElementById("statusFilter");

        if (searchInput) {

            searchInput.addEventListener("keyup", debounceSearch);

        }

        if (statusFilter) {

            statusFilter.addEventListener("change", performSearch);

        }

    }

    function wirePagination() {

        document.querySelectorAll(".ajax-page").forEach(link => {

            link.addEventListener("click", function (e) {

                e.preventDefault();

                updateTable(this.href);

            });

        });

    }

    function init(options) {

        config = { ...config, ...options };

        wireSearch();

        wirePagination();

    }

    return {

        init: init

    };

})();