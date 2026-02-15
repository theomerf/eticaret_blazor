// Toast

let toastIdCounter = 0;
const activeToasts = new Map();
const MAX_TOASTS = 5;

const toastConfig = {
    success: {
        icon: 'fa-check',
        iconBg: 'bg-emerald-100',
        iconColor: 'text-emerald-600',
        borderColor: 'border-emerald-500',
        titleColor: 'text-emerald-800',
        defaultTitle: 'Başarılı'
    },
    error: {
        icon: 'fa-xmark',
        iconBg: 'bg-red-100',
        iconColor: 'text-red-600',
        borderColor: 'border-red-500',
        titleColor: 'text-red-800',
        defaultTitle: 'Hata'
    },
    warning: {
        icon: 'fa-exclamation',
        iconBg: 'bg-amber-100',
        iconColor: 'text-amber-600',
        borderColor: 'border-amber-500',
        titleColor: 'text-amber-800',
        defaultTitle: 'Uyarı'
    },
    info: {
        icon: 'fa-info',
        iconBg: 'bg-blue-100',
        iconColor: 'text-blue-600',
        borderColor: 'border-blue-500',
        titleColor: 'text-blue-800',
        defaultTitle: 'Bilgi'
    }
};

function ensureToastContainer() {
    let container = document.getElementById('toast-container');
    if (!container) {
        container = document.createElement('div');
        container.id = 'toast-container';
        container.className = 'fixed top-4 right-4 z-[9999] flex flex-col gap-3 w-full max-w-md pointer-events-none px-4 sm:px-0';
        document.body.appendChild(container);
    }
    return container;
}

function showToast(message, type = 'success', options = {}) {
    if (type === 'danger') type = 'error';
    const config = toastConfig[type] || toastConfig.success;

    const settings = {
        autohide: true,
        delay: 4000,
        title: config.defaultTitle,
        showTime: true,
        ...options
    };

    const container = ensureToastContainer();

    if (activeToasts.size >= MAX_TOASTS) {
        const oldestToastId = activeToasts.keys().next().value;
        hideToast(oldestToastId);
    }

    const toastId = ++toastIdCounter;
    const currentTime = new Date().toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit' });

    const toastHTML = `
        <div id="toast-${toastId}" class="pointer-events-auto relative w-full overflow-hidden rounded-2xl bg-white/90 p-4 shadow-[0_8px_30px_rgba(0,0,0,0.12)] ring-1 ring-black/5 backdrop-blur-xl transition-all duration-500 ease-out transform translate-x-full opacity-0 border-l-4 ${config.borderColor} group">
            <div class="flex items-start gap-4">
                <div class="flex-shrink-0">
                    <div class="flex h-10 w-10 items-center justify-center rounded-full ${config.iconBg} ${config.iconColor} shadow-sm ring-1 ring-black/5">
                        <i class="fa-solid ${config.icon} text-lg"></i>
                    </div>
                </div>
                <div class="w-0 flex-1 pt-0.5">
                    <div class="flex items-center justify-between mb-1">
                        <p class="text-sm font-bold ${config.titleColor}">${settings.title}</p>
                        ${settings.showTime ? `<p class="text-[10px] text-gray-400 font-medium">${currentTime}</p>` : ''}
                    </div>
                    <p class="text-sm text-gray-600 leading-relaxed font-medium">${message}</p>
                </div>
                <div class="flex-shrink-0 ml-2">
                    <button type="button" onclick="hideToast(${toastId})" class="inline-flex rounded-md bg-white text-gray-400 hover:text-gray-500 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:ring-offset-2 transition-colors">
                        <span class="sr-only">Kapat</span>
                        <i class="fa-solid fa-xmark h-5 w-5"></i>
                    </button>
                </div>
            </div>
            <!-- Progress Bar (Optional Visual Flair) -->
             ${settings.autohide ? `<div class="absolute bottom-0 left-0 h-1 bg-gradient-to-r from-transparent via-${config.iconColor.split('-')[1]}-400 to-transparent w-full opacity-30 animate-[progress_${settings.delay}ms_linear]"></div>` : ''}
        </div>
    `;

    const tempDiv = document.createElement('div');
    tempDiv.innerHTML = toastHTML.trim();
    const toastEl = tempDiv.firstChild;

    container.appendChild(toastEl);

    activeToasts.set(toastId, {
        element: toastEl,
        timeout: null
    });

    let touchStartX = 0;
    let touchCurrentX = 0;
    let isSwiping = false;

    toastEl.addEventListener('touchstart', (e) => {
        touchStartX = e.touches[0].clientX;
        isSwiping = false;
        toastEl.style.transition = 'none'; 
    }, { passive: true });

    toastEl.addEventListener('touchmove', (e) => {
        touchCurrentX = e.touches[0].clientX;
        const diffX = touchCurrentX - touchStartX;

        if (diffX > 0) {
            e.preventDefault();
            isSwiping = true;
            toastEl.style.transform = `translateX(${diffX}px)`;
            toastEl.style.opacity = `${1 - (diffX / 300)}`;
        }
    }, { passive: false });

    toastEl.addEventListener('touchend', () => {
        if (!isSwiping) {
            toastEl.style.transition = '';
            return;
        }

        const diffX = touchCurrentX - touchStartX;

        if (diffX > 100) {
            toastEl.style.transition = 'all 0.2s ease-out';
            toastEl.style.transform = 'translateX(100%)';
            toastEl.style.opacity = '0';
            setTimeout(() => {
                hideToast(toastId);
            }, 200);
        } else {
            toastEl.style.transition = 'all 0.3s cubic-bezier(0.4, 0, 0.2, 1)';
            toastEl.style.transform = '';
            toastEl.style.opacity = '';

            setTimeout(() => {
                toastEl.style.transition = '';
            }, 300);
        }

        isSwiping = false;
        touchStartX = 0;
        touchCurrentX = 0;
    });

    requestAnimationFrame(() => {
        toastEl.offsetHeight;
        toastEl.classList.remove('translate-x-full', 'opacity-0');
        toastEl.classList.add('translate-x-0', 'opacity-100');
    });

    if (settings.autohide) {
        const timeout = setTimeout(() => {
            hideToast(toastId);
        }, settings.delay);
        activeToasts.get(toastId).timeout = timeout;
    }

    return toastId;
}

function hideToast(toastId) {
    const toastData = activeToasts.get(toastId);
    if (!toastData) return;

    const { element, timeout } = toastData;

    if (timeout) clearTimeout(timeout);

    element.classList.remove('translate-x-0', 'opacity-100');
    element.classList.add('translate-x-full', 'opacity-0');

    setTimeout(() => {
        if (element && element.parentNode) {
            element.remove();
        }
        activeToasts.delete(toastId);
    }, 500);
}

function clearAllToasts() {
    activeToasts.forEach((_, id) => hideToast(id));
}

window.showToast = showToast;
window.hideToast = hideToast;
window.clearAllToasts = clearAllToasts;

// Cookie Helper
function setCookie(name, value, days) {
    let expires = "";
    if (days) {
        let date = new Date();
        date.setTime(date.getTime() + (days * 24 * 60 * 60 * 1000));
        expires = "; expires=" + date.toUTCString();
    }
    document.cookie = name + "=" + (value || "") + expires + "; path=/; SameSite=Lax";
}
window.setCookie = setCookie;

// UI Helpers
function lockUI() {
    document.body.classList.add('loading');
    const overlay = document.getElementById('loadingOverlay');
    if (overlay) overlay.style.display = 'flex';
}

function unlockUI() {
    document.body.classList.remove('loading');
    const overlay = document.getElementById('loadingOverlay');
    if (overlay) overlay.style.display = 'none';
}
window.lockUI = lockUI;
window.unlockUI = unlockUI;

// Toggle Body Scroll Helper
window.toggleBodyScroll = function (isLocked) {
    const header = document.querySelector('header');
    if (isLocked) {
        document.body.style.overflow = 'hidden';
        if (header) header.style.zIndex = '1';
    } else {
        document.body.style.overflow = '';
        if (header) header.style.zIndex = '';
    }
};

// Modal Helper
window.modalHelper = {
    show: function (modalId) {
        const modalElement = document.getElementById(modalId);
        if (modalElement) {
            const modal = new bootstrap.Modal(modalElement);
            modal.show();
        }
    },
    hide: function (modalId) {
        const modalElement = document.getElementById(modalId);
        if (modalElement) {
            const modal = bootstrap.Modal.getInstance(modalElement);
            if (modal) {
                modal.hide();
            }
        }
    }
};


// Image Preview Helper
window.initializeImagePreview = function (inputId, previewId) {
    const input = document.getElementById(inputId);
    const preview = document.getElementById(previewId);

    if (!input || !preview) return;

    input.addEventListener('change', function () {
        const file = this.files[0];
        if (file) {
            if (!file.type.startsWith('image/')) {
                showToast('Lütfen geçerli bir resim dosyası seçin', 'warning');
                return;
            }
            const reader = new FileReader();
            reader.onload = function (e) {
                let existingImg = preview.querySelector('img');
                if (existingImg) {
                    existingImg.src = e.target.result;
                } else {
                    preview.innerHTML = `<img src="${e.target.result}" alt="Preview" class="w-full h-full object-cover rounded-lg" />`;
                }
            };
            reader.onerror = () => showToast('Resim yüklenirken hata oluştu', 'error');
            reader.readAsDataURL(file);
        }
    });
};

// Bottom Sheet Swipe Helper
window.initBottomSheetSwipe = function (contentId, modalId, options = {}) {
    const settings = {
        threshold: 100,
        closeDelay: 300,
        handleHeight: 60,
        ...options
    };

    const content = document.getElementById(contentId);
    const modal = document.getElementById(modalId);

    if (!content || !modal) return;
    if (content.dataset.swipeInitialized === 'true') return;

    content.dataset.swipeInitialized = 'true';

    let startY = 0;
    let currentY = 0;
    let isDragging = false;
    let isFromHandle = false;

    content.addEventListener('touchstart', function (e) {
        const touch = e.touches[0];
        const rect = content.getBoundingClientRect();
        isFromHandle = (touch.clientY - rect.top) <= settings.handleHeight;

        const scrollable = content.querySelector('.overflow-y-auto');
        if (!isFromHandle && scrollable && scrollable.scrollTop > 0) {
            startY = 0;
            return;
        }

        startY = touch.clientY;
        currentY = startY;
        isDragging = false;
    }, { passive: true });

    content.addEventListener('touchmove', function (e) {
        if (!startY) return;
        currentY = e.touches[0].clientY;
        const diff = currentY - startY;

        if (diff > 0) {
            const scrollable = content.querySelector('.overflow-y-auto');
            if (!isFromHandle && scrollable && scrollable.scrollTop > 0) {
                content.style.transform = '';
                startY = 0;
                return;
            }

            isDragging = true;
            if (e.cancelable) e.preventDefault();
            content.style.transform = `translateY(${diff}px)`;
        } else {
            content.style.transform = '';
        }
    }, { passive: false });

    content.addEventListener('touchend', function () {
        const diff = currentY - startY;
        if (isDragging && diff > settings.threshold) {
            content.classList.add('translate-y-full');
            content.style.transform = '';
            setTimeout(() => {
                modal.classList.add('hidden');
                document.body.style.overflow = '';
            }, settings.closeDelay);
        } else {
            content.style.transform = '';
        }
        startY = 0; isDragging = false;
    });
};

document.addEventListener('DOMContentLoaded', function () {
    const bottomSheets = document.querySelectorAll('[data-bottom-sheet]');
    bottomSheets.forEach(sheet => {
        const contentId = sheet.getAttribute('data-bottom-sheet-content');
        const modalId = sheet.getAttribute('data-bottom-sheet-modal');
        const threshold = parseInt(sheet.getAttribute('data-bottom-sheet-threshold')) || 100;
        if (contentId && modalId) {
            window.initBottomSheetSwipe(contentId, modalId, { threshold });
        }
    });
});

// Scroll to element helper
window.scrollToElement = (elementId) => {
    const element = document.getElementById(elementId);
    if (element) {
        element.scrollIntoView({ behavior: 'smooth', block: 'start' });
    }
};

// Custom alert helper
window.showConfirm = function (message, title = "Onay") {
    return new Promise(resolve => {
        const modal = document.getElementById("confirmModal");
        if (!modal) {
            resolve(confirm(message));
            return;
        }

        const box = modal.querySelector("div");

        document.getElementById("confirmTitle").innerText = title;
        document.getElementById("confirmMessage").innerText = message;

        modal.classList.remove("hidden");
        modal.classList.add("flex");

        requestAnimationFrame(() => {
            box.classList.remove("scale-95", "opacity-0");
        });

        const okBtn = document.getElementById("confirmOk");
        const cancelBtn = document.getElementById("confirmCancel");

        const cleanup = (result) => {
            box.classList.add("scale-95", "opacity-0");

            okBtn.onclick = null;
            cancelBtn.onclick = null;
            modal.onclick = null;

            setTimeout(() => {
                modal.classList.add("hidden");
                modal.classList.remove("flex");
                resolve(result);
            }, 200);
        };

        okBtn.onclick = () => cleanup(true);
        cancelBtn.onclick = () => cleanup(false);

        modal.onclick = (e) => {
            if (e.target === modal) {
                cleanup(false);
            }
        };
    });
};

// Impersonation banner offset helper
function updateImpersonationOffset() {
    const banner = document.querySelector('.impersonation-banner');
    if (!banner) {
        document.body.classList.remove('is-impersonating');
        document.body.style.removeProperty('--impersonation-offset');
        return;
    }

    const height = Math.ceil(banner.getBoundingClientRect().height);
    document.body.classList.add('is-impersonating');
    document.body.style.setProperty('--impersonation-offset', `${height}px`);
}

function initImpersonationOffsetObserver() {
    updateImpersonationOffset();

    const observer = new MutationObserver(() => updateImpersonationOffset());
    observer.observe(document.body, { childList: true, subtree: true });

    window.addEventListener('resize', () => updateImpersonationOffset());
}

document.addEventListener('DOMContentLoaded', initImpersonationOffsetObserver);

