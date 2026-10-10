(() => {
    'use strict';
    const script = document.currentScript;
    const storageKey = 'tbr.uiLanguage';
    const textState = new WeakMap();
    const attributeState = new WeakMap();
    const attributes = ['placeholder', 'title', 'aria-label', 'alt'];
    const excluded = 'script, style, textarea, [data-localize="false"]';
    let language = 'th';
    let translations = {};
    let patterns = [];
    let observer;

    function translate(value) {
        if (language !== 'en' || !value.trim()) return value;
        const key = value.trim();
        const english = Object.hasOwn(translations, key) ? translations[key] : null;
        if (typeof english === 'string' && english.trim()) return value.replace(key, () => english);
        if (value.includes('\n')) return value.split('\n').map(translate).join('\n');
        if (key.endsWith(' - TBR')) return translate(value.slice(0, value.lastIndexOf(' - TBR'))) + ' - TBR';
        for (const pattern of patterns) {
            const match = pattern.regex.exec(key);
            if (match) return value.replace(key, () => pattern.en.replace(/\{(\d+)\}/g, (_, index) => match[Number(index) + 1]));
        }
        return value;
    }

    function isUiText(element) {
        if (!element || element.closest(excluded)) return false;
        // Database values, recipient addresses, and editable content are not UI labels.
        if (element.closest('[contenteditable="true"]')) return false;
        const option = element.closest('option');
        if (option && option.value !== '' && !['DataStatus', 'dataStatusFilter', 'pageSize'].includes(option.parentElement.id)) return false;
        if (element.closest('tbody') && !element.closest('.row-actions, .status-badge, td[colspan], button, label')) return false;
        return true;
    }

    function updateText(node) {
        if (!isUiText(node.parentElement)) return;
        const previous = textState.get(node);
        const source = previous && node.nodeValue === previous.result ? previous.source : node.nodeValue;
        const result = translate(source);
        if (node.nodeValue !== result) node.nodeValue = result;
        textState.set(node, { source, result });
    }

    function updateAttributes(element) {
        if (element.closest(excluded)) return;
        const state = attributeState.get(element) || {};
        for (const name of attributes) {
            if (!element.hasAttribute(name)) continue;
            const value = element.getAttribute(name);
            const previous = state[name];
            const source = previous && value === previous.result ? previous.source : value;
            const result = translate(source);
            if (value !== result) element.setAttribute(name, result);
            state[name] = { source, result };
        }
        attributeState.set(element, state);
    }

    function update(root) {
        if (root.nodeType === Node.TEXT_NODE) {
            updateText(root);
            return;
        }
        if (root.nodeType !== Node.ELEMENT_NODE) return;
        if (root.closest(excluded)) return;
        updateAttributes(root);
        root.querySelectorAll('[placeholder], [title], [aria-label], [alt]').forEach(updateAttributes);
        const walker = document.createTreeWalker(root, NodeFilter.SHOW_TEXT);
        while (walker.nextNode()) updateText(walker.currentNode);
    }

    function observe() {
        observer.observe(document.documentElement, {
            subtree: true, childList: true, characterData: true,
            attributes: true, attributeFilter: attributes
        });
    }

    function setLanguage(value) {
        language = value === 'en' ? 'en' : 'th';
        try { localStorage.setItem(storageKey, language); } catch { /* Storage may be disabled. */ }
        observer.disconnect();
        document.documentElement.lang = language;
        document.querySelectorAll('[data-language-selector]').forEach(button => {
            const label = language === 'th' ? 'Switch to English' : 'เปลี่ยนเป็นภาษาไทย';
            button.setAttribute('aria-label', label);
            button.title = label;
            button.querySelector('[data-language-label]').textContent = language.toUpperCase();
        });
        update(document.documentElement);
        observe();
    }

    async function initialize() {
        let defaultLanguage = 'th';
        try {
            const response = await fetch(script.dataset.mappingUrl, { cache: 'no-cache' });
            if (!response.ok) throw new Error('Language mapping unavailable');
            const config = await response.json();
            translations = config.translations || {};
            patterns = (config.patterns || []).filter(item => item.th && item.en?.trim()).map(item => ({
                regex: new RegExp('^' + item.th.split(/\{\d+\}/).map(part => part.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')).join('(\\d+)') + '$'),
                en: item.en
            }));
            defaultLanguage = config.defaultLanguage === 'en' ? 'en' : 'th';
        } catch {
            // Original Thai text remains usable if the mapping cannot be loaded.
        }
        observer = new MutationObserver(records => {
            observer.disconnect();
            for (const record of records) {
                if (record.type === 'childList') record.addedNodes.forEach(update);
                else update(record.target);
            }
            observe();
        });
        let preference = defaultLanguage;
        try { preference = localStorage.getItem(storageKey) || defaultLanguage; } catch { /* Use default. */ }
        setLanguage(preference);
        document.querySelectorAll('[data-language-selector]').forEach(button => {
            button.addEventListener('click', () => setLanguage(language === 'th' ? 'en' : 'th'));
        });
        window.addEventListener('storage', event => {
            if (event.key === storageKey) setLanguage(event.newValue || defaultLanguage);
        });
    }
    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', initialize);
    else initialize();
})();
