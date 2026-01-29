(function () {
    'use strict';

    // ============================================
    // State Management
    // ============================================
    const state = {
        currentPage: Number(new URLSearchParams(location.search).get("page")) || 1,
        initialPage: Number(new URLSearchParams(location.search).get("page")) || 1,
        isLoading: false,
        hasMore: true,
        isPageReload: false,
        hasScrolledYet: false,

        pageStates: new Map(),
        visiblePages: new Set(),

        filters: {
            searchTerm: '',
            minPrice: null,
            maxPrice: null,
            brand: '',
            isShowCase: false,
            isDiscount: false,
            sortBy: '',
            categoryId: null
        }
    };

    // ============================================
    // DOM Elementleri
    // ============================================
    let container;
    let sentinel;
    container = document.getElementById("cardsContainer");
    sentinel = document.getElementById("scrollSentinel");

    if (!container || !sentinel) {
        console.error('Gereken elementler bulunamadı');
        return;
    }

    // ============================================
    // Helperlar
    // ============================================
    function pascalToCamel(str) {
        if (!str) return str;
        return str[0].toLowerCase() + str.substring(1);
    }

    function getCursorFromLastCard() {
        const cards = container.querySelectorAll(".product-card");
        if (!cards.length) return null;

        const lastCard = cards[cards.length - 1];

        return {
            price: lastCard.dataset.cursorPrice,
            id: lastCard.dataset.cursorId,
            rating: lastCard.dataset.cursorRating,
            reviewCount: lastCard.dataset.cursorReviewCount
        };
    }

    function updateFiltersFromURL() {
        const params = new URLSearchParams(location.search);

        state.filters = {
            searchTerm: params.get('searchTerm') || '',
            minPrice: params.get('minPrice') || null,
            maxPrice: params.get('maxPrice') || null,
            brand: params.get('brand') || '',
            isShowCase: params.get('isShowCase') === 'true',
            isDiscount: params.get('isDiscount') === 'true',
            sortBy: params.get('sortBy') || '',
            categoryId: params.get('categoryId') || null
        };

        state.currentPage = Number(params.get('page')) || 1;
    }

    function buildFetchURL(page, cursor = null) {
        const url = new URL("/products/cards", location.origin);

        url.searchParams.set("page", page);

        if (cursor) {
            if (cursor.price) {
                const rawPrice = cursor.price;
                const normalizedPrice = rawPrice.replace(",", ".");
                const cursorPrice = Number(normalizedPrice).toFixed(2);

                url.searchParams.set("cursorPrice", cursorPrice);
            }
            if (cursor.id) url.searchParams.set("cursorId", cursor.id);
            if (cursor.rating) url.searchParams.set("cursorRating", cursor.rating);
            if (cursor.reviewCount) url.searchParams.set("cursorReviewCount", cursor.reviewCount);
        }

        Object.entries(state.filters).forEach(([key, value]) => {
            if (value !== null && value !== '' && value !== 'default' && value !== false) {
                url.searchParams.set(pascalToCamel(key), value);
            }
        });

        return url;
    }

    function updateURLWithPage(page, replaceState = true) {
        const url = new URL(location.href);
        Object.entries(state.filters).forEach(([key, value]) => {
            if (value !== null && value !== '' && value !== 'default' && value !== false) {
                url.searchParams.set(pascalToCamel(key), value);
            }
        });

        if (page === 1) {
            url.searchParams.delete('page');
        } else {
            url.searchParams.set('page', page);
        }

        if (replaceState) {
            history.replaceState({ page, scrollY: window.scrollY }, '', url);
        } else {
            history.pushState({ page, scrollY: window.scrollY }, '', url);
        }
    }

    // ============================================
    // Sayfa Görünürlüğü Tespiti
    // ============================================
    function detectVisiblePage() {
        const cards = container.querySelectorAll(".product-card");
        if (!cards.length) return state.currentPage;

        const pageSize = 9;
        const pageOffset = state.initialPage - 1;

        const isAtBottom = (window.innerHeight + window.scrollY) >= (document.body.scrollHeight - 100);

        if (isAtBottom && !state.hasMore) {
            const lastCardIndex = cards.length - 1;
            const calculatedPage = Math.floor(lastCardIndex / pageSize) + 1 + pageOffset;
            return calculatedPage;
        }

        let firstVisibleCardIndex = 0;

        for (let i = 0; i < cards.length; i++) {
            const card = cards[i];
            const rect = card.getBoundingClientRect();

            if (rect.bottom > 0) {
                firstVisibleCardIndex = i;
                break;
            }
        }

        const calculatedPage = Math.floor(firstVisibleCardIndex / pageSize) + 1 + pageOffset;
        return calculatedPage;
    }

    // ============================================
    // Scroll Takibi
    // ============================================
    let lastScrollTime = 0;
    const SCROLL_THROTTLE_MS = 100;
    let lastDetectedPage = state.currentPage;

    function handleScroll() {
        const now = Date.now();

        if (!state.hasScrolledYet) {
            state.hasScrolledYet = true;
        }

        if (now - lastScrollTime >= SCROLL_THROTTLE_MS) {
            lastScrollTime = now;

            const visiblePage = detectVisiblePage();

            if (visiblePage !== lastDetectedPage) {
                lastDetectedPage = visiblePage;
                state.currentPage = visiblePage;
                updateURLWithPage(visiblePage, true);
            }
        }
    }

    window.addEventListener('scroll', handleScroll, { passive: true });

    // ============================================
    // Daha Fazla Ürün Yükle
    // ============================================
    async function loadMoreProducts() {
        if (state.isLoading || !state.hasMore) {
            console.log('Daha fazla yükleme engellendi:', { isLoading: state.isLoading, hasMore: state.hasMore });
            return;
        }

        container = document.getElementById("cardsContainer");
        sentinel = document.getElementById("scrollSentinel");

        state.isLoading = true;
        const nextPage = state.currentPage + 1;
        sentinel.innerHTML = `            
            <div class="text-center">
                <div class="spinner-border text-primary" role="status">
                    <span class="visually-hidden">Yükleniyor...</span>
                </div>
                <p class="text-gray-500 mt-2 text-sm">Daha fazla ürün yükleniyor...</p>
            </div>
            `;

        try {
            const cursor = getCursorFromLastCard();
            const url = buildFetchURL(nextPage, cursor);

            const response = await fetch(url, { method: 'GET' });

            if (!response.ok) {
                throw new Error(`Ağ Hatası: ${response.status}`);
            }

            const hasMore = response.headers.get('X-Has-More') === 'true';
            state.hasMore = hasMore;

            const html = await response.text();

            if (!html.trim()) {
                state.hasMore = false;
                observer.disconnect();

                sentinel.innerHTML = `
                    <div class="text-center mt-10">
                        <i class="fas fa-check-circle text-green-500 text-3xl"></i>
                        <p class="text-gray-600">Tüm ürünler yüklendi</p>
                    </div>
                `;
                return;
            }

            container.insertAdjacentHTML("beforeend", html);

            const newCursor = getCursorFromLastCard();
            state.pageStates.set(nextPage, {
                cursor: newCursor,
                cardCount: container.querySelectorAll('.product-card').length
            });

            if (!state.hasMore) {
                observer.disconnect();
                sentinel.innerHTML = `
                    <div class="text-center mt-10">
                        <i class="fas fa-check-circle text-green-500 text-3xl"></i>
                        <p class="text-gray-600">Tüm ürünler yüklendi</p>
                    </div>
                `;
            }

        } catch (error) {
            console.error('Daha fazla ürün yüklenirken hata oluştu:', error);
            state.hasMore = false;
            observer.disconnect();

            sentinel.innerHTML = `
                <div class="text-center mt-10">
                    <i class="fas fa-exclamation-circle text-red-500 text-3xl"></i>
                    <p class="text-gray-600">Ürünler yüklenirken bir hata oluştu</p>
                    <button onclick="location.reload()" class="mt-3 px-4 py-2 bg-primary text-white rounded-lg">
                        Tekrar Dene
                    </button>
                </div>
            `;
        } finally {
            state.isLoading = false;
            if (state.hasMore && sentinel.querySelector('.spinner-border')) {
                sentinel.innerHTML = ``;
            }
        }
    }

    // ============================================
    // Kesişim Gözlemleme
    // ============================================
    const observer = new IntersectionObserver(
        ([entry]) => {
            if (entry.isIntersecting && !state.isPageReload && state.hasMore && !state.isLoading) {
                loadMoreProducts();
            }
        },
        {
            rootMargin: '400px'
        }
    );

    // ============================================
    // Filtre Değişme Handlerı
    // ============================================
    window.addEventListener('filtersApplied', (event) => {
        state.currentPage = 1;
        state.initialPage = 1;
        state.hasMore = true;
        state.isLoading = false;
        state.pageStates.clear();
        state.visiblePages.clear();
        state.isPageReload = true; // Geçici olarak true
        state.hasScrolledYet = false;
        lastDetectedPage = 1;

        container = document.getElementById("cardsContainer");
        sentinel = document.getElementById("scrollSentinel");

        updateFiltersFromURL();
        updateURLWithPage(1, false);

        observer.disconnect();

        setTimeout(() => {
            const sentinel = document.getElementById('scrollSentinel');
            if (sentinel) {
                observer.observe(sentinel);
            }
            state.isPageReload = false;
        }, 100);
    });

    // ============================================
    // Tarayıcı Geri/İleri Navigasyonu
    // ============================================
    window.addEventListener('popstate', (event) => {
        location.reload();
    });

    // ============================================
    // Sıralama Handlerı
    // ============================================
    document.addEventListener('change', async (event) => {
        const isDesktopSort = event.target.id === 'sortOptions';
        const isMobileSort = event.target.name === 'SortBy';

        if (!isDesktopSort && !isMobileSort) return;

        const sortValue = event.target.value;
        state.filters.sortBy = sortValue;

        state.currentPage = 1;
        state.initialPage = 1;
        state.hasMore = true;
        state.isLoading = false;
        state.pageStates.clear();
        state.isPageReload = true;
        state.hasScrolledYet = false;
        lastDetectedPage = 1;

        if (typeof lockUI === 'function') lockUI();

        try {
            const url = buildFetchURL(1, null);
            const response = await fetch(url, { method: 'GET' });

            if (!response.ok) throw new Error(`Ağ Hatası: ${response.status}`);

            const hasMore = response.headers.get('X-Has-More') === 'true';
            state.hasMore = hasMore;

            const html = await response.text();
            container.innerHTML = html;

            updateURLWithPage(1, false);
            updateSortUI(sortValue);

            window.scrollTo({ top: 0, behavior: 'smooth' });

            const cursor = getCursorFromLastCard();
            state.pageStates.set(1, {
                cursor,
                cardCount: container.querySelectorAll('.product-card').length
            });

            observer.disconnect();

            setTimeout(() => {
                const sentinel = document.getElementById('scrollSentinel');
                if (sentinel) {
                    observer.observe(sentinel);
                }
                state.isPageReload = false;
            }, 100);


            if (isMobileSort) {
                const modal = document.getElementById('mobileSortModal');
                const content = document.getElementById('mobileSortContent');
                if (content) content.classList.add('translate-y-full');
                setTimeout(() => {
                    if (modal) modal.classList.add('hidden');
                    document.body.style.overflow = '';
                }, 300);
            }

        } catch (error) {
            console.error('Sıralama uygulanırken hata oluştu:', error);
            if (typeof showToast === 'function') {
                showToast('Bir hata oluştu.', 'danger');
            }
        } finally {
            if (typeof unlockUI === 'function') unlockUI();
        }
    });

    function updateSortUI(sortValue) {
        const desktopSelect = document.getElementById('sortOptions');
        if (desktopSelect) desktopSelect.value = sortValue || 'default';

        const mobileSortBtn = document.getElementById('mobileSortBtn');
        if (mobileSortBtn) {
            let badge = mobileSortBtn.querySelector('.bg-red-500');
            if (sortValue && sortValue !== 'default') {
                if (!badge) {
                    mobileSortBtn.insertAdjacentHTML('beforeend', '<span class="absolute top-1.5 right-1.5 w-2.5 h-2.5 bg-red-500 border-2 border-white rounded-full animate-pulse"></span>');
                }
            } else {
                if (badge) badge.remove();
            }
        }

        const labels = document.querySelectorAll('.mobile-sort-label');
        labels.forEach(label => {
            const input = label.querySelector('input[name="SortBy"]');
            const checkIcon = label.querySelector('.check-icon');

            if (input && input.value === (sortValue || 'default')) {
                input.checked = true;
                label.classList.add('border-blue-500', 'bg-blue-50', 'sort-active');
                label.classList.remove('border-gray-200', 'bg-white');
                if (checkIcon) checkIcon.classList.remove('hidden');
            } else {
                label.classList.remove('border-blue-500', 'bg-blue-50', 'sort-active');
                label.classList.add('border-gray-200', 'bg-white');
                if (checkIcon) checkIcon.classList.add('hidden');
            }
        });
    }

    window.toggleAccordion = function(targetId, button) {
        const target = document.getElementById(targetId);
        const icon = button.querySelector('.fa-chevron-down');

        if (target.classList.contains('show')) {
            target.classList.remove('show');
            button.classList.add('collapsed');
            icon.style.transform = 'rotate(0deg)';
        } else {
            target.classList.add('show');
            button.classList.remove('collapsed');
            icon.style.transform = 'rotate(180deg)';
        }
    }

    document.addEventListener('DOMContentLoaded', function () {
        document.querySelectorAll('.custom-accordion-button:not(.collapsed)').forEach(button => {
            const icon = button.querySelector('.fa-chevron-down');
            if (icon) icon.style.transform = 'rotate(180deg)';
        });
    });

    // ============================================
    // Başlatma
    // ============================================
    if ('scrollRestoration' in history) {
        history.scrollRestoration = 'manual';
    }

    window.scrollTo(0, 0);

    updateFiltersFromURL();

    const initialCursor = getCursorFromLastCard();
    if (initialCursor) {
        state.pageStates.set(state.currentPage, {
            cursor: initialCursor,
            cardCount: container.querySelectorAll('.product-card').length
        });
    }

    history.replaceState({
        page: state.currentPage,
        scrollY: 0
    }, '', location.href);

    // Observer'ı başlat
    if (sentinel) {
        observer.observe(sentinel);
    }
})();