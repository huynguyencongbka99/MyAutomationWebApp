// AutoTech Hub - Client Scripts

document.addEventListener('DOMContentLoaded', () => {
    initTheme();
    initCodeCopyButtons();
    initReadingProgress();
    initTableOfContents();
    initResponsiveTables();
    initLikeButton();
    initMarkdownLivePreview();
});

/* 1. Dark/Light Theme Switcher */
function initTheme() {
    const defaultPref = document.documentElement.getAttribute('data-default-theme') || 'auto';
    const systemPref = window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
    const fallbackTheme = (defaultPref === 'dark' || defaultPref === 'light') ? defaultPref : systemPref;
    const savedTheme = localStorage.getItem('autotech_theme') || fallbackTheme;

    applyTheme(savedTheme);

    document.querySelectorAll('.theme-toggle-trigger, #theme-toggle-btn').forEach((btn) => {
        btn.addEventListener('click', () => {
            const currentTheme = document.documentElement.getAttribute('data-theme') || 'light';
            const newTheme = currentTheme === 'dark' ? 'light' : 'dark';
            applyTheme(newTheme);
            localStorage.setItem('autotech_theme', newTheme);
        });
    });
}

function applyTheme(theme) {
    document.documentElement.setAttribute('data-theme', theme);
    document.documentElement.setAttribute('data-bs-theme', theme);
    updateThemeIcons(theme);
}

function updateThemeIcons(theme) {
    document.querySelectorAll('#theme-toggle-icon, .theme-toggle-icon').forEach((icon) => {
        if (theme === 'dark') {
            icon.className = 'bi bi-sun-fill text-warning theme-toggle-icon';
        } else {
            icon.className = 'bi bi-moon-stars-fill text-primary theme-toggle-icon';
        }
    });
}

/* 2. Code Block Copy Buttons */
function initCodeCopyButtons() {
    const preBlocks = document.querySelectorAll('pre');
    preBlocks.forEach((pre) => {
        if (pre.querySelector('.code-copy-btn')) return;

        const copyBtn = document.createElement('button');
        copyBtn.type = 'button';
        copyBtn.className = 'code-copy-btn';
        copyBtn.innerHTML = '<i class="bi bi-clipboard"></i> Copy';
        copyBtn.title = 'Sao chép đoạn mã';

        copyBtn.addEventListener('click', async () => {
            const code = pre.querySelector('code') ? pre.querySelector('code').innerText : pre.innerText;
            try {
                await navigator.clipboard.writeText(code);
                copyBtn.innerHTML = '<i class="bi bi-check2 text-success"></i> Đã sao chép!';
                copyBtn.classList.add('btn-copied');
                setTimeout(() => {
                    copyBtn.innerHTML = '<i class="bi bi-clipboard"></i> Copy';
                    copyBtn.classList.remove('btn-copied');
                }, 2000);
            } catch (err) {
                console.error('Không thể copy: ', err);
            }
        });

        pre.appendChild(copyBtn);
    });
}

/* 3. Reading Progress Bar */
function initReadingProgress() {
    const progressBar = document.getElementById('reading-progress');
    const articleBody = document.getElementById('article-content-body');
    if (!progressBar || !articleBody) return;

    window.addEventListener('scroll', () => {
        const totalHeight = articleBody.clientHeight;
        const rect = articleBody.getBoundingClientRect();
        const startOffset = rect.top;
        const windowHeight = window.innerHeight;

        let progress = 0;
        if (startOffset < 0) {
            progress = Math.min(100, Math.max(0, (Math.abs(startOffset) / Math.max(1, totalHeight - windowHeight + 200)) * 100));
        }

        progressBar.style.width = `${progress}%`;
    });
}

/* 4. Table of Contents (TOC) Generator for Desktop & Mobile */
function initTableOfContents() {
    const desktopToc = document.getElementById('article-toc-list');
    const mobileToc = document.getElementById('article-toc-list-mobile');
    const articleBody = document.getElementById('article-content-body');
    if (!articleBody || (!desktopToc && !mobileToc)) return;

    const headings = articleBody.querySelectorAll('h2, h3');
    if (headings.length === 0) {
        const tocParent = document.getElementById('article-toc-wrapper');
        const mobileWrapper = document.getElementById('article-toc-mobile-wrapper');
        if (tocParent) tocParent.style.display = 'none';
        if (mobileWrapper) mobileWrapper.style.display = 'none';
        return;
    }

    const buildLinks = (container, isMobile) => {
        if (!container) return;
        container.innerHTML = '';
        headings.forEach((h, index) => {
            if (!h.id) {
                h.id = `section-heading-${index + 1}`;
            }

            const a = document.createElement('a');
            a.href = `#${h.id}`;
            a.textContent = h.textContent;
            if (h.tagName.toLowerCase() === 'h3') {
                a.className = 'toc-h3';
            }

            a.addEventListener('click', (e) => {
                e.preventDefault();
                const target = document.getElementById(h.id);
                if (target) {
                    target.scrollIntoView({ behavior: 'smooth' });
                }
                if (isMobile) {
                    const collapseEl = document.getElementById('mobileTocCollapse');
                    if (collapseEl && window.bootstrap) {
                        const bsCollapse = bootstrap.Collapse.getInstance(collapseEl);
                        if (bsCollapse) bsCollapse.hide();
                    }
                }
            });

            container.appendChild(a);
        });
    };

    buildLinks(desktopToc, false);
    buildLinks(mobileToc, true);

    // Active link highlighting on scroll
    if (desktopToc) {
        window.addEventListener('scroll', () => {
            let currentActive = '';
            headings.forEach((h) => {
                const rect = h.getBoundingClientRect();
                if (rect.top <= 120) {
                    currentActive = h.id;
                }
            });

            desktopToc.querySelectorAll('a').forEach((link) => {
                if (link.getAttribute('href') === `#${currentActive}`) {
                    link.classList.add('active');
                } else {
                    link.classList.remove('active');
                }
            });
        });
    }
}

/* 5. Wrap Article Tables for Mobile Horizontal Scroll */
function initResponsiveTables() {
    const tables = document.querySelectorAll('.article-content table');
    tables.forEach((table) => {
        if (table.parentElement && table.parentElement.classList.contains('table-responsive')) return;
        const wrapper = document.createElement('div');
        wrapper.className = 'table-responsive';
        table.parentNode.insertBefore(wrapper, table);
        wrapper.appendChild(table);
    });
}

/* 6. AJAX Like Button */
function initLikeButton() {
    const likeBtn = document.getElementById('btn-like-article');
    if (!likeBtn) return;

    likeBtn.addEventListener('click', async () => {
        const articleId = likeBtn.getAttribute('data-article-id');
        if (!articleId) return;

        try {
            const response = await fetch(`/Articles/ToggleLike/${articleId}`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                }
            });

            if (response.ok) {
                const data = await response.json();
                if (data.success) {
                    const countSpan = document.getElementById('like-count-display');
                    if (countSpan) countSpan.textContent = data.likes;
                    likeBtn.classList.add('btn-liked');
                    const heartIcon = likeBtn.querySelector('i');
                    if (heartIcon) heartIcon.className = 'bi bi-heart-fill text-danger';
                }
            }
        } catch (e) {
            console.error('Lỗi like bài viết: ', e);
        }
    });
}

/* 7. Live Markdown Preview in Create/Edit */
function initMarkdownLivePreview() {
    const textarea = document.getElementById('markdown-editor-input');
    const previewPane = document.getElementById('markdown-preview-pane');
    if (!textarea || !previewPane) return;

    let debounceTimer;
    const updatePreview = () => {
        const text = textarea.value;
        if (window.marked) {
            previewPane.innerHTML = window.marked.parse(text);
            initCodeCopyButtons();
            initResponsiveTables();
        } else {
            previewPane.innerText = text;
        }
    };

    textarea.addEventListener('input', () => {
        clearTimeout(debounceTimer);
        debounceTimer = setTimeout(updatePreview, 250);
    });

    updatePreview();
}
