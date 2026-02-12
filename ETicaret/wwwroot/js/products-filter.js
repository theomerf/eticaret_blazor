document.addEventListener('DOMContentLoaded', async function () {
    await initProductFilters();
});

async function initProductFilters() {
    let isInitialized = false;
    let debounceTimer = null;

    let initialFormStates = {
        filterForm: '',
        mobileFilterForm: ''
    };

    function getFormState(formId) {
        const form = document.getElementById(formId);
        if (!form) return '';
        const formData = new FormData(form);
        const state = {};
        for (let [key, value] of formData.entries()) {
            state[key] = value;
        }
        return JSON.stringify(state);
    }

    function updateApplyButtonsState() {
        const currentDesktopState = getFormState('filterForm');
        const desktopBtn = document.getElementById('applyFiltersBtn');
        if (desktopBtn) {
            const hasChanged = currentDesktopState !== initialFormStates.filterForm;
            setButtonState(desktopBtn, hasChanged);
        }

        const currentMobileState = getFormState('mobileFilterForm');
        const mobileBtn = document.getElementById('mobileApplyFiltersBtn');
        if (mobileBtn) {
            const hasChanged = currentMobileState !== initialFormStates.mobileFilterForm;
            setButtonState(mobileBtn, hasChanged);
        }
    }

    function setButtonState(btn, enabled) {
        if (enabled) {
            btn.disabled = false;
            btn.classList.remove('opacity-50', 'cursor-not-allowed', 'pointer-events-none');
        } else {
            btn.disabled = true;
            btn.classList.add('opacity-50', 'cursor-not-allowed', 'pointer-events-none');
        }
    }

    function captureInitialStates() {
        initialFormStates.filterForm = getFormState('filterForm');
        initialFormStates.mobileFilterForm = getFormState('mobileFilterForm');
        updateApplyButtonsState();
    }

    let currentFilters = {
        searchTerm: '',
        minPrice: null,
        maxPrice: null,
        brand: '',
        isShowCase: false,
        isDiscount: false
    };

    function isTrue(value) {
        return value && value.toLowerCase() === 'true';
    }

    function initializeFiltersFromURL() {
        const urlParams = new URLSearchParams(window.location.search);

        currentFilters.searchTerm = urlParams.get('searchTerm') || '';
        currentFilters.minPrice = urlParams.get('minPrice') ? parseInt(urlParams.get('minPrice')) : null;
        currentFilters.maxPrice = urlParams.get('maxPrice') ? parseInt(urlParams.get('maxPrice')) : null;
        currentFilters.brand = urlParams.get('brand') || '';
        currentFilters.isShowCase = isTrue(urlParams.get('isShowCase'));
        currentFilters.isDiscount = isTrue(urlParams.get('isDiscount'));

        const searchTerm = document.getElementById('searchTerm');
        const minPrice = document.getElementById('minPrice');
        const maxPrice = document.getElementById('maxPrice');
        const bestSellers = document.getElementById('bestSellers');
        const discountedItems = document.getElementById('discountedItems');

        const mobileSearchTerm = document.getElementById('mobileSearchTerm');
        const mobileMinPrice = document.getElementById('mobileMinPrice');
        const mobileMaxPrice = document.getElementById('mobileMaxPrice');
        const mobileBestSellers = document.getElementById('mobileBestSellers');
        const mobileDiscountedItems = document.getElementById('mobileDiscountedItems');

        // Desktop
        if (searchTerm && searchTerm.value !== currentFilters.searchTerm) searchTerm.value = currentFilters.searchTerm;

        const minVal = currentFilters.minPrice !== null ? String(currentFilters.minPrice) : '';
        if (minPrice && minPrice.value !== minVal) minPrice.value = minVal;

        const maxVal = currentFilters.maxPrice !== null ? String(currentFilters.maxPrice) : '';
        if (maxPrice && maxPrice.value !== maxVal) maxPrice.value = maxVal;

        // Mobile
        if (mobileSearchTerm && mobileSearchTerm.value !== currentFilters.searchTerm) mobileSearchTerm.value = currentFilters.searchTerm;
        if (mobileMinPrice && mobileMinPrice.value !== minVal) mobileMinPrice.value = minVal;
        if (mobileMaxPrice && mobileMaxPrice.value !== maxVal) mobileMaxPrice.value = maxVal;

        if (currentFilters.brand) {
            const brandRadio = document.querySelector(`input[name="Brand"][value="${currentFilters.brand}"]`);
            if (brandRadio) brandRadio.checked = true;

            const mobileBrandRadio = document.getElementById(`mobileBrand${currentFilters.brand}`);
            if (mobileBrandRadio) mobileBrandRadio.checked = true;
        } else {
            const brandAll = document.getElementById('brandAll');
            if (brandAll) brandAll.checked = true;

            const mobileBrandAll = document.getElementById('mobileBrandAll');
            if (mobileBrandAll) mobileBrandAll.checked = true;
        }

        if (bestSellers) bestSellers.checked = currentFilters.isShowCase;
        if (discountedItems) discountedItems.checked = currentFilters.isDiscount;
        if (mobileBestSellers) mobileBestSellers.checked = currentFilters.isShowCase;
        if (mobileDiscountedItems) mobileDiscountedItems.checked = currentFilters.isDiscount;
    }


    function pascalToCamel(str) {
        if (!str) return str;
        return str[0].toLowerCase() + str.substring(1);
    }

    async function debouncedApplyFilters() {
        clearTimeout(debounceTimer);
        debounceTimer = setTimeout(() => {
            applyFilters();
        }, 500);
    }

    debouncedApplyFilters.cancel = function () {
        clearTimeout(debounceTimer);
    };

    async function applyFilters(formId = 'filterForm') {
        if (typeof lockUI === 'function') lockUI();

        const isMobile = formId === 'mobileFilterForm';
        let filterForm = document.getElementById(formId);

        if (!filterForm) {
            filterForm = document.getElementById('filterForm') || document.getElementById('mobileFilterForm');
        }

        if (!filterForm) {
            if (typeof unlockUI === 'function') unlockUI();
            return;
        }

        const formData = new FormData(filterForm);
        const params = new URLSearchParams();
        const addedKeys = new Set();
        const urlParams = new URLSearchParams(window.location.search);

        for (let [key, value] of formData.entries()) {
            if (value && value !== '' && value !== '0') {
                const camelKey = pascalToCamel(key);

                if (camelKey === 'minPrice' || camelKey === 'maxPrice') continue;

                if ((camelKey === 'isShowCase' || camelKey === 'isDiscount') && value.toLowerCase() === 'false') {
                    continue;
                }

                params.append(camelKey, value);
                addedKeys.add(camelKey);
            }
        }

        const prefix = isMobile ? 'mobile' : '';
        const minPriceInput = document.getElementById(isMobile ? 'mobileMinPrice' : 'minPrice');
        const maxPriceInput = document.getElementById(isMobile ? 'mobileMaxPrice' : 'maxPrice');

        const minVal = minPriceInput ? minPriceInput.value.trim() : '';
        const maxVal = maxPriceInput ? maxPriceInput.value.trim() : '';

        if (minVal !== '' && minVal !== '0') {
            params.set('minPrice', minVal);
            addedKeys.add('minPrice');
        }

        if (maxVal !== '' && maxVal !== '100000') {
            params.set('maxPrice', maxVal);
            addedKeys.add('maxPrice');
        }

        if (urlParams.get('sortBy') && !addedKeys.has('sortBy')) {
            params.append('sortBy', urlParams.get('sortBy'));
        }

        const newURL = window.location.pathname + (params.toString() ? '?' + params.toString() : '');

        window.history.pushState({}, '', newURL);

        try {
            const abortController = new AbortController();

            const response = await fetch(`/products/list?${params.toString()}`, {
                method: 'GET',
                signal: abortController.signal
            });

            if (!response.ok) {
                throw new Error('Ağ hatası: ' + response.status);
            }

            const result = await response.text();

            if (productContainer) {
                productContainer.innerHTML = result;
            }

            // URL üzerindeki güncel filtreleri form elemanlarına yansıt ve 
            // bu durumu "başlangıç durumu" (uygulanmış durum) olarak kaydet.
            // Bu sayede buton "Disable" durumuna geçer (Form == Initial/Applied).
            initializeFiltersFromURL();
            captureInitialStates();

            window.dispatchEvent(new CustomEvent('filtersApplied', {
                detail: { filters: params.toString() }
            }));

            const resetButton = document.getElementById("resetAllFilters");
            if (resetButton) {
                resetButton.classList.remove('d-none');
                resetButton.classList.add('d-block');
            }
        } catch (error) {
            console.error('Filtreler uygulanırken hata oluştu:', error);
            window.location.reload();
        } finally {
            if (typeof unlockUI === 'function') unlockUI();

            const applyBtn = document.querySelector('.filter-apply-btn');
            if (applyBtn) {
                applyBtn.innerHTML = '<i class="fas fa-search me-2"></i>Filtreleri Uygula';
            }

            const productContainerEl = document.getElementById('productContainer');
            if (productContainerEl) {
                productContainerEl.scrollIntoView({
                    behavior: 'smooth',
                    block: 'start'
                });
            }

            document.body.style.overflow = '';
        }
    }

    const searchTermInput = document.getElementById('searchTerm');
    if (searchTermInput) {
        searchTermInput.addEventListener('input', function () {
            toggleSearchClear();

            const value = this.value.trim();

            if (value.length >= 2) {
                debouncedApplyFilters();
            }

            if (value.length === 0) {
                debouncedApplyFilters.cancel();
                applyFiltersImmediately();
            }
        });
    }

    document.addEventListener("click", async function (event) {
        const btn = event.target.closest("#clearSearch");
        if (!btn) return;

        debouncedApplyFilters.cancel();
        applyFiltersImmediately();
    });

    function applyFiltersImmediately() {
        const searchInput = document.getElementById('searchTerm');
        if (searchInput) {
            searchInput.value = '';
            applyFilters();
            captureInitialStates();
        }
    }

    function toggleSearchClear() {
        const searchInput = document.getElementById('searchTerm');
        const clearBtn = document.getElementById('clearSearch');

        if (searchInput && clearBtn) {
            if (searchInput.value.length > 0) {
                clearBtn.style.display = 'block';
            } else {
                clearBtn.style.display = 'none';
            }
        }
    }

    const clearSearchBtn = document.getElementById('clearSearch');
    if (clearSearchBtn) {
        clearSearchBtn.addEventListener('click', function () {
            const searchInput = document.getElementById('searchTerm');
            if (searchInput) {
                searchInput.value = '';
                searchInput.dispatchEvent(new Event('input', { bubbles: true }));
            }
        });
    }

    const brandSearch = document.getElementById('brandSearch');
    if (brandSearch) {
        brandSearch.addEventListener('input', function () {
            const searchTerm = this.value.toLowerCase();
            const brandItems = document.querySelectorAll('.brand-item');

            brandItems.forEach(function (item) {
                const label = item.querySelector('label span');
                if (label) {
                    const brandText = label.textContent.toLowerCase();
                    if (brandText.includes(searchTerm) || searchTerm === '') {
                        item.style.display = 'block';
                    } else {
                        item.style.display = 'none';
                    }
                }
            });
        });
    }

    const applyFiltersBtn = document.getElementById('applyFiltersBtn');
    if (applyFiltersBtn) {
        applyFiltersBtn.addEventListener('click', async function () {
            await applyFilters('filterForm');
        });
    }

    ['filterForm', 'mobileFilterForm'].forEach(formId => {
        const form = document.getElementById(formId);
        if (form) {
            form.addEventListener('input', updateApplyButtonsState);
            form.addEventListener('change', updateApplyButtonsState);
        }
    });

    document.addEventListener('click', async function (event) {
        const btn = event.target.closest('#mobileApplyFiltersBtn');
        if (!btn) return;

        event.preventDefault();

        await applyFilters('mobileFilterForm');

        const mobileFilterModal = document.getElementById('mobileFilterModal');
        const mobileFilterContent = document.getElementById('mobileFilterContent');
        if (mobileFilterContent) {
            mobileFilterContent.classList.add('translate-y-full');
            mobileFilterContent.style.transform = '';
        }
        setTimeout(() => {
            if (mobileFilterModal) {
                mobileFilterModal.classList.add('hidden');
            }
            document.body.style.overflow = '';
        }, 300);
    });

    document.addEventListener('click', async function (event) {
        const anchor = event.target.closest("a.productlist-clear-all");
        if (!anchor) return;

        event.preventDefault();

        await clearAllFilters();
    });

    async function clearAllFilters() {
        const searchTerm = document.getElementById('searchTerm');
        const minPrice = document.getElementById('minPrice');
        const maxPrice = document.getElementById('maxPrice');
        const brandAll = document.getElementById('brandAll');
        const bestSellers = document.getElementById('bestSellers');
        const discountedItems = document.getElementById('discountedItems');
        const brandSearch = document.getElementById('brandSearch');

        const mobileSearchTerm = document.getElementById('mobileSearchTerm');
        const mobileMinPrice = document.getElementById('mobileMinPrice');
        const mobileMaxPrice = document.getElementById('mobileMaxPrice');
        const mobileBrandAll = document.getElementById('mobileBrandAll');
        const mobileBestSellers = document.getElementById('mobileBestSellers');
        const mobileDiscountedItems = document.getElementById('mobileDiscountedItems');
        const mobileBrandSearch = document.getElementById('mobileBrandSearch');

        if (searchTerm) searchTerm.value = '';
        if (minPrice) minPrice.value = '';
        if (maxPrice) maxPrice.value = '';
        if (brandAll) brandAll.checked = true;
        if (bestSellers) bestSellers.checked = false;
        if (discountedItems) discountedItems.checked = false;
        if (brandSearch) brandSearch.value = '';

        if (mobileSearchTerm) mobileSearchTerm.value = '';
        if (mobileMinPrice) mobileMinPrice.value = '';
        if (mobileMaxPrice) mobileMaxPrice.value = '';
        if (mobileBrandAll) mobileBrandAll.checked = true;
        if (mobileBestSellers) mobileBestSellers.checked = false;
        if (mobileDiscountedItems) mobileDiscountedItems.checked = false;
        if (mobileBrandSearch) mobileBrandSearch.value = '';

        const catInputs = document.querySelectorAll('input[name="CategoryId"], input[name="categoryId"], #selectedCategoryId, #mainCat, #subCat');
        catInputs.forEach(input => input.value = '');

        document.querySelectorAll('.main-category-card.active, .sub-category-card.active').forEach(el => el.classList.remove('active'));
        const firstMainCat = document.querySelector('.main-category-card');
        if (firstMainCat) firstMainCat.classList.add('active');

        if (window.priceSliderAPI) {
            window.priceSliderAPI.resetSliders();
        }

        const brandItems = document.querySelectorAll('.brand-item');
        brandItems.forEach(item => item.style.display = 'block');

        toggleSearchClear();
        const mobileClearBtn = document.getElementById('mobileClearSearch');
        if (mobileClearBtn) mobileClearBtn.style.display = 'none';

        await applyFilters();
    }

    document.addEventListener('click', async function (event) {
        const anchor = event.target.closest("a.productlist-filter-remove");
        if (!anchor) return;

        event.preventDefault();

        const href = anchor.getAttribute("href");

        if (typeof lockUI === 'function') lockUI();

        try {
            const tempUrl = new URL(href, window.location.origin);
            const normalizedParams = new URLSearchParams();

            for (const [key, val] of tempUrl.searchParams.entries()) {
                if (val && val !== '') {
                    normalizedParams.append(pascalToCamel(key), val);
                }
            }

            const newUrl = new URL(`${window.location.pathname}?${normalizedParams.toString()}`, window.location.origin);
            window.history.pushState({}, '', newUrl);

            const searchParamVal = newUrl.searchParams.get('searchTerm');
            if (searchTerm) searchTerm.value = searchParamVal || '';

            const minPriceParam = newUrl.searchParams.get('minPrice');
            const maxPriceParam = newUrl.searchParams.get('maxPrice');

            if (minPrice) minPrice.value = minPriceParam || '';
            if (maxPrice) maxPrice.value = maxPriceParam || '';
            if (mobileMinPrice) mobileMinPrice.value = minPriceParam || '';
            if (mobileMaxPrice) mobileMaxPrice.value = maxPriceParam || '';

            const mobileSearchTerm = document.getElementById('mobileSearchTerm');
            if (mobileSearchTerm) mobileSearchTerm.value = searchParamVal || '';

            const brandParam = newUrl.searchParams.get('brand') || '';

            if (brandParam) {
                const brandRadio = document.querySelector(`input[name="Brand"][value="${brandParam}"]`);
                if (brandRadio) brandRadio.checked = true;

                const mobileBrandRadio = document.getElementById(`mobileBrand${brandParam}`);
                if (mobileBrandRadio) mobileBrandRadio.checked = true;
            } else {
                const brandAll = document.getElementById('brandAll');
                if (brandAll) brandAll.checked = true;

                const mobileBrandAll = document.getElementById('mobileBrandAll');
                if (mobileBrandAll) mobileBrandAll.checked = true;
            }

            const mobileBestSellers = document.getElementById('mobileBestSellers');
            const mobileDiscountedItems = document.getElementById('mobileDiscountedItems');

            if (mobileBestSellers) mobileBestSellers.checked = isTrue(newUrl.searchParams.get('isShowCase'));
            if (mobileDiscountedItems) mobileDiscountedItems.checked = isTrue(newUrl.searchParams.get('isDiscount'));

            const mobileBrandSearch = document.getElementById('mobileBrandSearch');
            if (mobileBrandSearch) mobileBrandSearch.value = '';

            if (bestSellers) {
                bestSellers.checked = isTrue(newUrl.searchParams.get('isShowCase'));
            }

            if (discountedItems) {
                discountedItems.checked = isTrue(newUrl.searchParams.get('isDiscount'));
            }

            if (brandSearch) brandSearch.value = '';

            toggleSearchClear();

            if (window.priceSliderAPI) {
                const minVal = minPriceParam ? parseInt(minPriceParam) : 0;
                const maxVal = maxPriceParam ? parseInt(maxPriceParam) : 100000;
                window.priceSliderAPI.setSliders(minVal, maxVal);
            }

            const response = await fetch(`/products/list?${newUrl.searchParams.toString()}`, {
                method: 'GET',
            });

            if (!response.ok) {
                throw new Error(`Ağ hatası: ${response.status}`);
            }

            const result = await response.text();

            const productContainer = document.getElementById('productContainer');
            if (productContainer) {
                productContainer.innerHTML = result;
            }

            window.dispatchEvent(new CustomEvent('filtersApplied', {
                detail: { filters: newUrl.searchParams.toString() }
            }));

        } catch (error) {
            console.error('Filtre temizlenirken hata oluştu:', error);
            if (typeof showToast === 'function') {
                showToast('Bir hata oluştu. ', 'danger');
            }
        } finally {
            if (typeof unlockUI === 'function') unlockUI();
            const productContainerEl = document.getElementById('productContainer');
            if (productContainerEl) {
                productContainerEl.scrollIntoView({
                    behavior: 'smooth',
                    block: 'start'
                });
            }
        }
    });

    const switchCards = document.querySelectorAll('.custom-switch-card');
    switchCards.forEach(function (card) {
        card.addEventListener('mouseenter', function () {
            this.classList.add('shadow-sm', 'border-primary');
        });

        card.addEventListener('mouseleave', function () {
            this.classList.remove('shadow-sm', 'border-primary');
        });
    });

    initializeFiltersFromURL();
    await initializePriceSlider();
    toggleSearchClear();
    captureInitialStates();
    isInitialized = true;
}