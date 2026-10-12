window.dkpLootReserve = {
    copyText: async function (text) {
        if (!navigator.clipboard || !navigator.clipboard.writeText) {
            throw new Error('Clipboard access is not available in this browser.');
        }
        await navigator.clipboard.writeText(text);
    }
};
