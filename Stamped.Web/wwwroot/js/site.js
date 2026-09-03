export function openInNewTab(url) {
    if (!url) return false;
    window.open(url, '_blank', 'noopener');
    return true;
}

export async function copyToClipboard(text) {
    try {
        await navigator.clipboard.writeText(text ?? '');
        return true;
    } catch {
        return false;
    }
}