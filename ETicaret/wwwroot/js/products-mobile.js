document.addEventListener('click', function (e) {
    if (e.target.closest('#mobileFilterBtn')) {
        e.preventDefault();
        e.stopPropagation();
        openMobileFilter();
        return;
    }

    if (e.target.closest('#closeMobileFilterModal')) {
        closeMobileFilter();
        return;
    }

    if (e.target.id === 'mobileFilterBackdrop') {
        closeMobileFilter();
        return;
    }

    if (e.target.closest('#mobileSortBtn')) {
        e.preventDefault();
        e.stopPropagation();
        openMobileSort();
        return;
    }

    if (e.target.closest('#closeMobileSortModal')) {
        closeMobileSort();
        return;
    }

    if (e.target.id === 'mobileSortBackdrop') {
        closeMobileSort();
        return;
    }
});

function openMobileFilter() {
    const modal = document.getElementById('mobileFilterModal');
    const content = document.getElementById('mobileFilterContent');

    if (!modal || !content) return;

    modal.classList.remove('hidden');
    document.body.style.overflow = 'hidden';

    const scrollable = content.querySelector('.overflow-y-auto');
    if (scrollable) scrollable.scrollTop = 0;
    content.style.transform = '';

    setTimeout(() => {
        content.classList.remove('translate-y-full');
    }, 10);
}

function closeMobileFilter() {
    const modal = document.getElementById('mobileFilterModal');
    const content = document.getElementById('mobileFilterContent');

    if (!content) return;

    content.classList.add('translate-y-full');
    content.style.transform = '';

    setTimeout(() => {
        if (modal) modal.classList.add('hidden');
        document.body.style.overflow = '';
    }, 300);
}

function openMobileSort() {
    const modal = document.getElementById('mobileSortModal');
    const content = document.getElementById('mobileSortContent');

    if (!modal || !content) return;

    modal.classList.remove('hidden');
    document.body.style.overflow = 'hidden';

    setTimeout(() => {
        content.classList.remove('translate-y-full');
    }, 10);
}

function closeMobileSort() {
    const modal = document.getElementById('mobileSortModal');
    const content = document.getElementById('mobileSortContent');

    if (!content) return;

    content.classList.add('translate-y-full');
    content.style.transform = '';

    setTimeout(() => {
        if (modal) modal.classList.add('hidden');
        document.body.style.overflow = '';
    }, 300);
}

function initMobileSwipes() {
    if (typeof window.initBottomSheetSwipe === 'function') {
        const filterContent = document.getElementById('mobileFilterContent');
        const sortContent = document.getElementById('mobileSortContent');

        if (filterContent) {
            window.initBottomSheetSwipe('mobileFilterContent', 'mobileFilterModal', {
                checkScroll: true,
                threshold: 100
            });
        }

        if (sortContent) {
            window.initBottomSheetSwipe('mobileSortContent', 'mobileSortModal', {
                checkScroll: false,
                threshold: 100
            });
        }
    }
}

document.addEventListener('DOMContentLoaded', initMobileSwipes);

// Blazor Mobile Filter Functions
window.initBlazorMobileFilters = function () {
    const openBtn = document.querySelector('[data-blazor-filter-open]');
    const closeBtn = document.getElementById('closeBlazorMobileFilterModal');
    const backdrop = document.getElementById('blazorMobileFilterBackdrop');
    const modal = document.getElementById('blazorMobileFilterModal');
    const content = document.getElementById('blazorMobileFilterContent');

    function openFilter() {
        if (!modal || !content) return;
        modal.classList.remove('hidden');
        document.body.style.overflow = 'hidden';
        const scrollable = content.querySelector('.overflow-y-auto');
        if (scrollable) scrollable.scrollTop = 0;
        content.style.transform = '';
        setTimeout(() => content.classList.remove('translate-y-full'), 10);
    }

    function closeFilter() {
        if (!content) return;
        content.classList.add('translate-y-full');
        content.style.transform = '';
        setTimeout(() => {
            if (modal) modal.classList.add('hidden');
            document.body.style.overflow = '';
        }, 300);
    }

    if (openBtn) openBtn.addEventListener('click', openFilter);
    if (closeBtn) closeBtn.addEventListener('click', closeFilter);
    if (backdrop) backdrop.addEventListener('click', closeFilter);

    // Swipe gesture
    if (typeof window.initBottomSheetSwipe === 'function') {
        window.initBottomSheetSwipe('blazorMobileFilterContent', 'blazorMobileFilterModal', {
            checkScroll: true,
            threshold: 100
        });
    }
};

window.closeBlazorMobileFilter = function () {
    const content = document.getElementById('blazorMobileFilterContent');
    const modal = document.getElementById('blazorMobileFilterModal');
    if (content) {
        content.classList.add('translate-y-full');
        content.style.transform = '';
        setTimeout(() => {
            if (modal) modal.classList.add('hidden');
            document.body.style.overflow = '';
        }, 300);
    }
};

// Blazor Mobile Sort Functions
window.initBlazorMobileSort = function () {
    const openBtn = document.querySelector('[data-blazor-sort-open]');
    const closeBtn = document.getElementById('closeBlazorMobileSortModal');
    const backdrop = document.getElementById('blazorMobileSortBackdrop');
    const modal = document.getElementById('blazorMobileSortModal');
    const content = document.getElementById('blazorMobileSortContent');

    function openSort() {
        if (!modal || !content) return;
        modal.classList.remove('hidden');
        document.body.style.overflow = 'hidden';
        setTimeout(() => content.classList.remove('translate-y-full'), 10);
    }

    function closeSort() {
        if (!content) return;
        content.classList.add('translate-y-full');
        content.style.transform = '';
        setTimeout(() => {
            if (modal) modal.classList.add('hidden');
            document.body.style.overflow = '';
        }, 300);
    }

    if (openBtn) openBtn.addEventListener('click', openSort);
    if (closeBtn) closeBtn.addEventListener('click', closeSort);
    if (backdrop) backdrop.addEventListener('click', closeSort);

    // Swipe gesture
    if (typeof window.initBottomSheetSwipe === 'function') {
        window.initBottomSheetSwipe('blazorMobileSortContent', 'blazorMobileSortModal', {
            checkScroll: false,
            threshold: 100
        });
    }
};

window.closeBlazorMobileSort = function () {
    const content = document.getElementById('blazorMobileSortContent');
    const modal = document.getElementById('blazorMobileSortModal');
    if (content) {
        content.classList.add('translate-y-full');
        content.style.transform = '';
        setTimeout(() => {
            if (modal) modal.classList.add('hidden');
            document.body.style.overflow = '';
        }, 300);
    }
};
