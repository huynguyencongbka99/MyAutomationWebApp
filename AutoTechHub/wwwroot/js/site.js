// AutoTech Hub - Client Scripts

document.addEventListener('DOMContentLoaded', () => {
    initTheme();
    initCodeCopyButtons();
    initReadingProgress();
    initTableOfContents();
    initLikeButton();
    initMarkdownLivePreview();
});

/* 1. Dark/Light Theme Switcher */
function initTheme() {
    const savedTheme = localStorage.getItem('autotech_theme') || 
                       (window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light');
    
    document.documentElement.setAttribute('data-theme', savedTheme);
    updateThemeIcon(savedTheme);

    const themeToggleBtn = document.getElementById('theme-toggle-btn');
    if (themeToggleBtn) {
        themeToggleBtn.addEventListener('click', () => {
            const currentTheme = document.documentElement.getAttribute('data-theme') || 'light';
            const newTheme = currentTheme === 'dark' ? 'light' : 'dark';
            
            document.documentElement.setAttribute('data-theme', newTheme);
            localStorage.setItem('autotech_theme', newTheme);
            updateThemeIcon(newTheme);
        });
    }
}

function updateThemeIcon(theme) {
    const icon = document.getElementById('theme-toggle-icon');
    if (!icon) return;
    if (theme === 'dark') {
        icon.className = 'bi bi-sun-fill text-warning';
    } else {
        icon.className = 'bi bi-moon-stars-fill text-primary';
    }
}

/* 2. Code Block Copy Buttons */
function initCodeCopyButtons() {
    const preBlocks = document.querySelectorAll('pre');
    preBlocks.forEach((pre) => {
        // Prevent duplicate buttons
        if (pre.querySelector('.code-copy-btn')) return;

        const copyBtn = document.createElement('button');
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
            progress = Math.min(100, Math.max(0, (Math.abs(startOffset) / (totalHeight - windowHeight + 200)) * 100));
        }

        progressBar.style.width = `${progress}%`;
    });
}

/* 4. Table of Contents (TOC) Generator */
function initTableOfContents() {
    const tocContainer = document.getElementById('article-toc-list');
    const articleBody = document.getElementById('article-content-body');
    if (!tocContainer || !articleBody) return;

    const headings = articleBody.querySelectorAll('h2, h3');
    if (headings.length === 0) {
        const tocParent = document.getElementById('article-toc-wrapper');
        if (tocParent) tocParent.style.display = 'none';
        return;
    }

    tocContainer.innerHTML = '';
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
            document.getElementById(h.id).scrollIntoView({ behavior: 'smooth' });
        });

        tocContainer.appendChild(a);
    });

    // Active link highlighting on scroll
    window.addEventListener('scroll', () => {
        let currentActive = '';
        headings.forEach((h) => {
            const rect = h.getBoundingClientRect();
            if (rect.top <= 120) {
                currentActive = h.id;
            }
        });

        tocContainer.querySelectorAll('a').forEach((link) => {
            if (link.getAttribute('href') === `#${currentActive}`) {
                link.classList.add('active');
            } else {
                link.classList.remove('active');
            }
        });
    });
}

/* 5. AJAX Like Button */
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

/* 6. Live Markdown Preview in Create/Edit */
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
        } else {
            // Basic fallback preview
            previewPane.innerText = text;
        }
    };

    textarea.addEventListener('input', () => {
        clearTimeout(debounceTimer);
        debounceTimer = setTimeout(updatePreview, 250);
    });

    // Initial render
    updatePreview();
}
