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