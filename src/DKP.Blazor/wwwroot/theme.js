(function () {
    const storageKey = "dkp-theme";

    function resolve(preference) {
        return preference === "dark" ? "dark" : "light";
    }

    function apply(preference) {
        const resolved = resolve(preference);
        document.documentElement.dataset.theme = resolved;
        document.documentElement.style.colorScheme = resolved;
    }

    window.dkpTheme = {
        getPreference: function () {
            const value = window.localStorage.getItem(storageKey);
            return value === "dark" || value === "light" ? value : "light";
        },
        apply: apply,
        setPreference: function (preference) {
            if (!["light", "dark"].includes(preference)) preference = "light";
            window.localStorage.setItem(storageKey, preference);
            apply(preference);
        }
    };

    const preference = window.localStorage.getItem(storageKey) || "light";
    apply(preference);
})();
