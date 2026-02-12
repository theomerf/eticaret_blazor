document.addEventListener('DOMContentLoaded', function () {
    initNavbar();
    initDropdowns();
    initMobileMenu();
    initMobileCategories();
    initMobileSearch();
});

function initNavbar() {
    const mainNavbar = document.getElementById('mainNavbar');
    const categoryNavbar = document.getElementById('categoryNavbar');
    const navbarWrapper = document.getElementById('navbarWrapper');
    const categoriesDropdown = document.getElementById('categoriesMegaDropdown');

    if (!mainNavbar || !categoryNavbar || !navbarWrapper) return;

    let isCompact = false;

    window.addEventListener('scroll', function () {
        const scrollTop = window.scrollY;

        if (scrollTop > 20 && !isCompact) {
            isCompact = true;

            mainNavbar.classList.remove('lg:py-5', 'py-2.5');
            mainNavbar.classList.add('lg:py-3', 'py-2');

            categoryNavbar.style.setProperty('margin-top', `-${categoryNavbar.offsetHeight}px`);
            categoryNavbar.style.opacity = '0';
            categoryNavbar.style.pointerEvents = 'none';
            categoryNavbar.style.zIndex = '1';

            if (categoriesDropdown) {
                categoriesDropdown.classList.add('hidden');
            }

        } else if (scrollTop <= 20 && isCompact) {
            isCompact = false;

            mainNavbar.classList.remove('lg:py-3', 'py-2');
            mainNavbar.classList.add('lg:py-5', 'py-2.5');

            categoryNavbar.style.marginTop = '0';
            categoryNavbar.style.opacity = '1';
            categoryNavbar.style.pointerEvents = 'auto';
            categoryNavbar.style.zIndex = '';
        }
    });

    window.addEventListener('resize', () => {
        if (isCompact) {
            categoryNavbar.style.setProperty('margin-top', `-${categoryNavbar.offsetHeight}px`);
        }
    });
}

function initDropdowns() {
    const userDropdownWrapper = document.getElementById('userDropdown');

    if (userDropdownWrapper) {
        const dropdownMenu = document.getElementById('userDropdownMenu');

        if (dropdownMenu) {
            let hideTimeout;

            userDropdownWrapper.addEventListener('mouseenter', function () {
                if (window.innerWidth >= 640) {
                    clearTimeout(hideTimeout);
                    openDropdown(dropdownMenu);
                }
            });

            userDropdownWrapper.addEventListener('mouseleave', function () {
                if (window.innerWidth >= 640) {
                    hideTimeout = setTimeout(() => {
                        closeDropdown(dropdownMenu);
                    }, 200);
                }
            });

            userDropdownWrapper.addEventListener('click', function (e) {
                if (window.innerWidth < 640) {
                    e.stopPropagation();
                    if (dropdownMenu.classList.contains('hidden')) {
                        openDropdown(dropdownMenu);
                    } else {
                        closeDropdown(dropdownMenu);
                    }
                }
            });
        }
    }

    const categoryDropdownWrapper = document.getElementById('categoryDropdownWrapper');
    const categoriesDropdown = document.getElementById('categoriesMegaDropdown');
    const categoryNavbar = document.getElementById('categoryNavbar');

    if (categoryDropdownWrapper && categoriesDropdown && categoryNavbar) {
        let hideTimeout;

        function positionDropdown() {
            const navbarRect = categoryNavbar.getBoundingClientRect();
            categoriesDropdown.style.top = (navbarRect.bottom) + 'px';
        }

        categoryDropdownWrapper.addEventListener('mouseenter', function () {
            if (window.innerWidth >= 1024) {
                clearTimeout(hideTimeout);
                positionDropdown();
                openDropdown(categoriesDropdown);
            }
        });

        categoryDropdownWrapper.addEventListener('mouseleave', function () {
            if (window.innerWidth >= 1024) {
                hideTimeout = setTimeout(() => {
                    closeDropdown(categoriesDropdown);
                }, 200);
            }
        });

        categoriesDropdown.addEventListener('mouseenter', function () {
            if (window.innerWidth >= 1024) {
                clearTimeout(hideTimeout);
            }
        });

        categoriesDropdown.addEventListener('mouseleave', function () {
            if (window.innerWidth >= 1024) {
                hideTimeout = setTimeout(() => {
                    closeDropdown(categoriesDropdown);
                }, 200);
            }
        });

        window.addEventListener('scroll', function () {
            if (!categoriesDropdown.classList.contains('hidden')) {
                positionDropdown();
            }
        });

        window.addEventListener('resize', function () {
            if (!categoriesDropdown.classList.contains('hidden')) {
                positionDropdown();
            }
        });
    }

    document.addEventListener('click', function (e) {
        const userMenu = document.getElementById('userDropdownMenu');
        const userWrapper = document.getElementById('userDropdown');

        if (userMenu && !userMenu.classList.contains('hidden')) {
            if (!userWrapper.contains(e.target)) {
                closeDropdown(userMenu);
            }
        }
    });
}

function initMobileMenu() {
    const menuToggle = document.getElementById('mobileMenuToggle');
    if (!menuToggle) return;
}

function initMobileCategories() {
    const categoriesBtn = document.getElementById('mobileCategoriesBtn');
    const modal = document.getElementById('mobileCategoriesModal');
    const content = document.getElementById('mobileCategoriesContent');
    const backdrop = document.getElementById('mobileCategoriesBackdrop');
    const closeBtn = document.getElementById('closeMobileCategoriesModal');

    if (!categoriesBtn || !modal) return;

    categoriesBtn.addEventListener('click', function (e) {
        e.preventDefault();
        e.stopPropagation();
        openCategoriesModal();
    });

    closeBtn?.addEventListener('click', function () {
        closeCategoriesModal();
    });

    backdrop?.addEventListener('click', function () {
        closeCategoriesModal();
    });

    if (typeof window.initBottomSheetSwipe === 'function') {
        window.initBottomSheetSwipe('mobileCategoriesContent', 'mobileCategoriesModal', {
            checkScroll: true,
            threshold: 100
        });
    }

    function openCategoriesModal() {
        modal.classList.remove('hidden');
        document.body.style.overflow = 'hidden';

        setTimeout(() => {
            content.classList.remove('translate-y-full');
        }, 10);
    }

    function closeCategoriesModal() {
        content.classList.add('translate-y-full');
        content.style.transform = '';

        setTimeout(() => {
            modal.classList.add('hidden');
            document.body.style.overflow = '';
        }, 300);
    }
}

function initMobileSearch() {
    const searchToggle = document.getElementById('mobileSearchToggle');
    const searchContainer = document.getElementById('mobileSearchContainer');

    if (!searchToggle || !searchContainer) return;

    searchToggle.addEventListener('click', function (e) {
        e.stopPropagation();
        searchContainer.classList.toggle('hidden');

        if (!searchContainer.classList.contains('hidden')) {
            const input = searchContainer.querySelector('input');
            input?.focus();
        }
    });
}

function openDropdown(dropdownMenu) {
    if (!dropdownMenu) return;

    const allDropdowns = [
        document.getElementById('userDropdownMenu'),
        document.getElementById('categoriesMegaDropdown')
    ];

    allDropdowns.forEach(menu => {
        if (menu && menu !== dropdownMenu && !menu.classList.contains('hidden')) {
            closeDropdown(menu);
        }
    });

    dropdownMenu.classList.remove('hidden');
    void dropdownMenu.offsetWidth;
}

function closeDropdown(dropdownMenu) {
    if (!dropdownMenu || dropdownMenu.classList.contains('hidden')) return;
    dropdownMenu.classList.add('hidden');
}

window.updateCounter = function(selector, delta) {
    const badge = document.getElementById(selector);
    if (!badge) return;

    const current = parseInt(badge.textContent) || 0;
    badge.textContent = Math.max(0, current + delta);
}

window.setCounter = function (selector, value) {
    const badge = document.getElementById(selector);
    if (!badge) return;

    badge.textContent = Math.max(0, value);
}