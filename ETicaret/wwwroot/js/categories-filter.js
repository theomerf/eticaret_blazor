(function () {
    'use strict';

    document.addEventListener('DOMContentLoaded', function () {
        initializeCategoryFilter();
    });

    window.addEventListener('filtersApplied', function () {
        initializeCategoryFilter();
    });

    window.initializeCategoryFilter = function () {
        const urlParams = new URLSearchParams(window.location.search);
        const categoryId = urlParams.get('categoryId');

        document.querySelectorAll('.main-category-card.active, .sub-category-card.active').forEach(card => {
            card.classList.remove('active');
        });

        if (categoryId) {
            const categoryCards = document.querySelectorAll(`[data-category-id='${categoryId}']`);

            if (categoryCards.length > 0) {
                const firstCard = categoryCards[0];
                const isMainCategory = firstCard.classList.contains('main-category-card');

                categoryCards.forEach(card => card.classList.add('active'));

                if (isMainCategory) {
                    const categoryName = firstCard.getAttribute('data-category-name');
                    updateSelectedMainCategory(categoryName);
                    updateSubCategoriesFrontend(categoryId);
                    showSubCategoriesSection();
                } else {
                    const parentId = firstCard.getAttribute('data-parent-id');
                    if (parentId) {
                        const parentCards = document.querySelectorAll(`[data-category-id='${parentId}']`);
                        parentCards.forEach(card => card.classList.add('active'));

                        if (parentCards.length > 0) {
                            const parentName = parentCards[0].getAttribute('data-category-name');
                            updateSelectedMainCategory(parentName);
                            updateSubCategoriesFrontend(parentId);
                            showSubCategoriesSection();
                        }
                    }
                }
            }
        } else {
            const allCategoriesCards = document.querySelectorAll('.main-category-card:first-child');
            allCategoriesCards.forEach(card => card.classList.add('active'));

            hideSubCategoriesSection();
            updateSelectedMainCategory('Tüm Kategoriler');
        }
    }

    function showSubCategoriesSection() {
        const subcategoryTitles = document.querySelectorAll('#subcategory-title');
        subcategoryTitles.forEach(title => {
            const parentSection = title.closest('.border-t');
            if (parentSection) parentSection.style.display = 'block';
        });
    }

    function hideSubCategoriesSection() {
        const subcategoryTitles = document.querySelectorAll('#subcategory-title');
        subcategoryTitles.forEach(title => {
            const parentSection = title.closest('.border-t');
            if (parentSection) parentSection.style.display = 'none';
        });
    }

    function updateSelectedMainCategory(categoryName) {
        const elements = document.querySelectorAll('#selected-main-category');
        elements.forEach(el => {
            el.textContent = categoryName || 'Tüm Kategoriler';
        });
    }

    function updateSubCategoriesFrontend(parentCategoryId) {
        const containers = document.querySelectorAll('#subcategories-container');

        containers.forEach(container => {
            container.innerHTML = '';
        });

        if (!parentCategoryId) {
            hideSubCategoriesSection();
            return;
        }

        const allSubCategoriesPools = document.querySelectorAll('#all-subcategories');

        let hasSubCategories = false;

        allSubCategoriesPools.forEach((pool, poolIndex) => {
            const subCategories = pool.querySelectorAll(`.sub-category-card[data-parent-id='${parentCategoryId}']`);

            if (subCategories.length > 0) {
                hasSubCategories = true;
                const containerList = document.querySelectorAll('#subcategories-container');
                if (containerList[poolIndex]) {
                    subCategories.forEach(subCat => {
                        containerList[poolIndex].appendChild(subCat.cloneNode(true));
                    });
                }
            }
        });

        if (hasSubCategories) {
            showSubCategoriesSection();
        } else {
            hideSubCategoriesSection();
        }
    }

    function getCurrentFilters() {
        const filterForm = document.getElementById('filterForm');
        const formData = filterForm ? new FormData(filterForm) : new FormData();
        const urlParams = new URLSearchParams(window.location.search);

        const filters = {};

        for (let [key, value] of formData.entries()) {
            if (value && value !== '' && value !== '0') {
                filters[key] = value;
            }
        }

        const minPriceInput = document.getElementById('minPrice');
        const maxPriceInput = document.getElementById('maxPrice');

        if (minPriceInput && minPriceInput.value.trim() !== '') {
            filters.MinPrice = minPriceInput.value.trim();
        } else {
            delete filters.MinPrice;
        }

        if (maxPriceInput && maxPriceInput.value.trim() !== '') {
            filters.MaxPrice = maxPriceInput.value.trim();
        } else {
            delete filters.MaxPrice;
        }

        const sortBy = urlParams.get('sortBy');
        if (sortBy && sortBy !== '') {
            filters.sortBy = sortBy;
        }

        return filters;
    }

    function pascalToCamel(str) {
        if (!str) return str;
        return str[0].toLowerCase() + str.substring(1);
    }

    async function applyFilters(filters) {
        lockUI();

        try {
            const params = new URLSearchParams();

            Object.keys(filters).forEach(key => {
                const value = filters[key];
                if (value && value !== '' && value !== '0') {
                    params.set(pascalToCamel(key), value);
                }
            });

            const newURL = window.location.pathname +
                (params.toString() ? '?' + params.toString() : '');
            window.history.pushState({}, '', newURL);

            const response = await fetch(`/products/list?${params.toString()}`, {
                method: 'GET',
            });

            if (!response.ok) {
                throw new Error(`Ağ Hatası: ${response.status}`);
            }

            const result = await response.text();

            const productContainer = document.getElementById('productContainer');
            if (productContainer) {
                productContainer.innerHTML = result;
            }

            window.dispatchEvent(new CustomEvent('filtersApplied', {
                detail: { filters: params.toString() }
            }));
        } catch (error) {
            console.error('Filtreler uygulanırken hata oluştu:', error);
            showToast('Bir hata oluştu.', 'danger');
        } finally {
            document.getElementById('productContainer').scrollIntoView({
                behavior: 'smooth',
                block: 'start'
            });

            document.body.style.overflow = '';

            unlockUI();
        }
    }

    window.selectMainCategory = async function (event, categoryId, categoryName) {
        if (event) event.preventDefault();

        updateSelectedMainCategory(categoryName);
        updateSubCategoriesFrontend(categoryId);

        document.querySelectorAll('.main-category-card').forEach(card => {
            card.classList.remove('active');
            card.disabled = false;
            card.classList.remove('cursor-not-allowed');
        });

        if (categoryId) {
            const selectedCards = document.querySelectorAll(`.main-category-card[data-category-id='${categoryId}']`);
            selectedCards.forEach(card => card.classList.add('active'));
            selectedCards.forEach(card => card.disabled = true);
            selectedCards.forEach(card => card.classList.add('cursor-not-allowed'));
        } else {
            const containers = document.querySelectorAll('.main-category-card');
            const processed = new Set();
            containers.forEach(card => {
                const parent = card.parentElement;
                if (!processed.has(parent)) {
                    processed.add(parent);
                    const firstCard = parent.querySelector('.main-category-card');
                    if (firstCard) {
                        firstCard.classList.add('active');
                        firstCard.disabled = true;
                        firstCard.classList.add('cursor-not-allowed');
                    } 
                }
            });
        }

        document.querySelectorAll('.sub-category-card').forEach(card => {
            card.classList.remove('active');
            card.disabled = false;
            card.classList.remove('cursor-not-allowed');
        });

        const categoryInput = document.getElementById('selectedCategoryId');
        if (categoryInput) {
            categoryInput.value = categoryId || '';
        } else {
            const filterForm = document.getElementById('filterForm');
            if (filterForm) {
                const input = document.createElement('input');
                input.type = 'hidden';
                input.id = 'selectedCategoryId';
                input.name = 'CategoryId';
                input.value = categoryId || '';
                filterForm.appendChild(input);
            }
        }

        const currentFilters = getCurrentFilters();
        const newFilters = {
            ...currentFilters,
            categoryId: categoryId || '',
        };

        delete newFilters.mainCategoryId;
        delete newFilters.subCategoryId;

        await applyFilters(newFilters);

        closeMobileFilterIfOpen();
    };

    window.selectSubCategory = async function (event, subCategoryId, parentCategoryId) {
        if (event) event.preventDefault();

        document.querySelectorAll('.sub-category-card').forEach(card => {
            card.classList.remove('active');
            card.disabled = false;
            card.classList.remove('cursor-not-allowed');
        });

        if (subCategoryId) {
            const selectedLinks = document.querySelectorAll(`.category-link[data-sub-category-id='${subCategoryId}']`);
            selectedLinks.forEach(link => {
                const selectedCard = link.closest('.sub-category-card');
                if (selectedCard) {
                    selectedCard.classList.add('active');
                    selectedCard.disabled = true;
                    selectedCard.classList.add('cursor-not-allowed');
                }
            });
        }

        const categoryInput = document.getElementById('selectedCategoryId');
        const selectedId = subCategoryId || parentCategoryId || '';
        if (categoryInput) {
            categoryInput.value = selectedId;
        } else {
            const filterForm = document.getElementById('filterForm');
            if (filterForm) {
                const input = document.createElement('input');
                input.type = 'hidden';
                input.id = 'selectedCategoryId';
                input.name = 'CategoryId';
                input.value = selectedId;
                filterForm.appendChild(input);
            }
        }

        const currentFilters = getCurrentFilters();
        const newFilters = {
            ...currentFilters,
            categoryId: selectedId,
        };

        delete newFilters.mainCategoryId;
        delete newFilters.subCategoryId;

        await applyFilters(newFilters);

        closeMobileFilterIfOpen();
    };

    function closeMobileFilterIfOpen() {
        const mobileFilterModal = document.getElementById('mobileFilterModal');
        const mobileFilterContent = document.getElementById('mobileFilterContent');

        if (mobileFilterModal && !mobileFilterModal.classList.contains('hidden')) {
            if (mobileFilterContent) {
                mobileFilterContent.classList.add('translate-y-full');
                mobileFilterContent.style.transform = '';
            }
            setTimeout(() => {
                mobileFilterModal.classList.add('hidden');
                document.body.style.overflow = '';
            }, 300);
        }
    }

})();