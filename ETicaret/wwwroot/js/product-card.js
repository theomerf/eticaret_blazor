(function () {
    'use strict';

    let cartItems = new Set(); // Sepetteki ürün ID'leri
    const CART_VERSION_KEY = 'cart_version';

    function getCookie(name) {
        const value = `; ${document.cookie}`;
        const parts = value.split(`; ${name}=`);
        if (parts.length === 2) return parts.pop().split(';').shift();
        return null;
    }

    function getFavoritesFromCookie() {
        const cookie = getCookie('FavouriteProducts');
        if (!cookie || cookie === '') return [];

        return cookie.split('|')
            .filter(id => id && id.trim() !== '')
            .map(id => parseInt(id.trim()))
            .filter(id => !isNaN(id) && id > 0);
    }

    async function postJSON(url, data) {
        const response = await fetch(url, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(data)
        });

        if (!response.ok) {
            throw new Error(`Ağ hatası: ${response.status}`);
        }

        return response.json();
    }

    async function deleteJSON(url) {
        const response = await fetch(url, {
            method: 'DELETE',
            headers: {
                'Content-Type': 'application/json',
            }
        });

        if (!response.ok) {
            throw new Error(`Ağ hatası: ${response.status}`);
        }

        return response.json();
    }

    function redirectToLogin() {
        const currentUrl = window.location.pathname + window.location.search;
        window.location.href = `/account/login?returnUrl=${encodeURIComponent(currentUrl)}`;
    }


    async function initCartState() {
        try {
            const response = await fetch('/api/cart');

            if (response.ok) {
                const cart = await response.json();

                localStorage.setItem(CART_VERSION_KEY, cart.version);
                cartItems = new Set(cart.lines.map(line => line.productId));

                syncAllButtons();

                updateCartCounter(cart.lines.length);
            }
        } catch (error) {
            console.error('Sepet yüklenirken hata oluştu:', error);
        }
    }

    function syncAllButtons() {
        document.querySelectorAll('.add-to-cart-btn').forEach(btn => {
            const productId = parseInt(btn.dataset.productId);

            if (cartItems.has(productId)) {
                convertToRemoveButton(btn, productId);
            }
        });

        document.querySelectorAll('.remove-from-cart-btn').forEach(btn => {
            const productId = parseInt(btn.dataset.productId);

            if (!cartItems.has(productId)) {
                convertToAddButton(btn, productId);
            }
        });
    }

    function convertToRemoveButton(button, productId) {
        const cartActionDiv = button.closest('.cart-action');
        if (cartActionDiv) {
            const isSmall = cartActionDiv.classList.contains('small-card-action');

            if (isSmall) {
                cartActionDiv.innerHTML = `
                <button type="button" class="remove-from-cart-btn w-full py-2 text-[0.6rem] border-none rounded-xl bg-gradient-to-br from-red-500 to-red-700 text-white font-bold flex items-center justify-center gap-1 transition-all shadow-md uppercase cursor-pointer hover:scale-[1.02]" data-product-id="${productId}">
                    <span class="mr-1">Çıkar</span>
                    <i class="fas fa-trash-alt text-[10px]"></i>
                </button>`;
            } else {
                cartActionDiv.innerHTML = `
                <button type="button"
                    class="remove-from-cart-btn w-[90%] px-2 py-2 sm:px-4 sm:py-3 border-none rounded-xl bg-gradient-to-br from-red-500 via-red-600 to-red-700 text-white font-bold text-[0.6rem] sm:text-base flex items-center justify-center gap-1 sm:gap-2.5 transition-all duration-[400ms] [transition-timing-function:cubic-bezier(0.175,0.885,0.32,1.275)] shadow-[0_10px_25px_rgba(239,68,68,0.3),inset_0_1px_0_rgba(255,255,255,0.2)] uppercase tracking-wide cursor-pointer relative overflow-hidden before:content-[''] before:absolute before:top-0 before:-left-full before:w-full before:h-full before:bg-gradient-to-r before:from-transparent before:via-white/20 before:to-transparent before:transition-[left] before:duration-500 before:ease-out hover:before:left-full hover:bg-gradient-to-br hover:from-red-600 hover:via-red-700 hover:to-red-800 hover:shadow-[0_15px_35px_rgba(239,68,68,0.4),inset_0_1px_0_rgba(255,255,255,0.3)] hover:-translate-y-1 hover:scale-[1.02] [&.processing]:opacity-60 [&.processing]:pointer-events-none [&.processing]:animate-[processing_1.5s_infinite]"
                    data-product-id="${productId}">
                    <span class="hidden sm:inline mr-1 sm:mr-2 [text-shadow:0_1px_2px_rgba(0,0,0,0.1)]">Sepetten Çıkar</span>
                    <span class="sm:hidden mr-0.5 [text-shadow:0_1px_2px_rgba(0,0,0,0.1)]">Çıkar</span>
                    <span class="inline-flex items-center transition-transform duration-300 group-hover:scale-110 group-hover:rotate-[5deg]">
                        <i class="fas fa-trash-alt text-xs sm:text-lg [filter:drop-shadow(0_1px_2px_rgba(0,0,0,0.1))]"></i>
                    </span>
                </button>`;
            }
        }
    }

    function convertToAddButton(button, productId) {
        const cartActionDiv = button.closest('.cart-action');
        if (cartActionDiv) {
            const isSmall = cartActionDiv.classList.contains('small-card-action');

            if (isSmall) {
                cartActionDiv.innerHTML = `
                <button type="button" class="add-to-cart-btn w-full py-2 text-[0.6rem] border-none rounded-xl bg-button text-white font-bold flex items-center justify-center gap-1 transition-all shadow-md uppercase cursor-pointer hover:scale-[1.02]" data-product-id="${productId}">
                    <span class="mr-1">Ekle</span>
                    <i class="fas fa-cart-plus text-[10px]"></i>
                </button>`;
            } else {
                cartActionDiv.innerHTML = `
                <button type="button"
                    class="add-to-cart-btn w-[90%] px-2 py-2 sm:px-4 sm:py-3 border-none rounded-xl bg-button text-white font-bold text-[0.6rem] sm:text-base flex items-center justify-center gap-1 sm:gap-2.5 transition-all duration-[400ms] [transition-timing-function:cubic-bezier(0.175,0.885,0.32,1.275)] shadow-[0_10px_25px_rgba(59,130,246,0.3),inset_0_1px_0_rgba(255,255,255,0.2)] uppercase tracking-wide cursor-pointer relative overflow-hidden before:content-[''] before:absolute before:top-0 before:-left-full before:w-full before:h-full before:bg-gradient-to-r before:from-transparent before:via-white/20 before:to-transparent before:transition-[left] before:duration-500 before:ease-out hover:before:left-full hover:bg-button-hover hover:shadow-[0_15px_35px_rgba(59,130,246,0.4),inset_0_1px_0_rgba(255,255,255,0.3)] hover:-translate-y-1 hover:scale-[1.02] [&.processing]:opacity-60 [&.processing]:pointer-events-none [&.processing]:animate-[processing_1.5s_infinite]"
                    data-product-id="${productId}">
                    <span class="hidden sm:inline mr-1 sm:mr-2 [text-shadow:0_1px_2px_rgba(0,0,0,0.1)]">Sepete Ekle</span>
                    <span class="sm:hidden mr-0.5 [text-shadow:0_1px_2px_rgba(0,0,0,0.1)]">Ekle</span>
                    <span class="inline-flex items-center transition-transform duration-300 group-hover:scale-110 group-hover:rotate-[5deg]">
                        <i class="fas fa-cart-plus text-xs sm:text-lg [filter:drop-shadow(0_1px_2px_rgba(0,0,0,0.1))]"></i>
                    </span>
                </button>`;
            }
        }
    }

    function updateCartCounter(count) {
        const badge = document.getElementById('cart-summary');
        if (badge) {
            badge.textContent = count;
        }
    }

    // Favori işlemleri

    document.addEventListener('click', async function (e) {
        const button = e.target.closest('.add-to-favs-btn');
        if (!button) return;

        e.preventDefault();

        const productId = parseInt(button.dataset.productId);
        if (!productId || isNaN(productId)) return;

        if (button.classList.contains('processing')) return;
        button.classList.add('processing');

        updateCounter('favourites-summary', +1);

        try {
            const response = await fetch(`/api/account/favourites/add/${productId}`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                }
            });

            if (response.status === 401) {
                redirectToLogin();
                return;
            }

            if (!response.ok) {
                throw new Error(`Ağ Hatası: ${response.status}`);
            }

            const result = await response.json();

            if (result.success) {
                const isSmall = button.classList.contains('w-6');
                const isDetailMain = button.classList.contains('w-12');
                const isMobileDetail = button.classList.contains('w-10');

                if (isDetailMain) {
                    button.innerHTML = '<i class="fas fa-heart text-xl"></i>';
                } else if (isMobileDetail) {
                    button.innerHTML = '<i class="fas fa-heart text-lg"></i>';
                } else if (isSmall) {
                    button.innerHTML = '<i class="fas fa-heart text-[8px] sm:text-xs"></i>';
                } else {
                    button.innerHTML = '<i class="fas fa-heart text-xs sm:text-lg"></i>';
                }

                if (isDetailMain) {
                    button.className = "remove-from-favs-btn absolute top-5 right-5 bg-white/95 backdrop-blur-lg text-red-500 w-12 h-12 rounded-full flex items-center justify-center cursor-pointer transition-all duration-300 z-30 shadow-md border border-white/30 hover:bg-gradient-to-br hover:from-red-50 hover:to-red-100 hover:text-red-500 hover:scale-110 hover:rotate-6 hover:shadow-lg";
                } else if (isMobileDetail) {
                    button.className = "remove-from-favs-btn bg-white/80 backdrop-blur-md p-2 rounded-full shadow-sm text-red-500 w-10 h-10 flex items-center justify-center border border-white/30";
                } else if (isSmall) {
                    button.className = "remove-from-favs-btn w-6 h-6 sm:w-8 sm:h-8 rounded-full bg-gradient-to-br from-red-50 to-red-100 flex items-center justify-center border-none shadow-sm text-red-500 transition-all duration-300 backdrop-blur-[10px] border border-white/30 hover:scale-110 hover:shadow-md";
                } else {
                    button.className = "remove-from-favs-btn w-7 h-7 sm:w-11 sm:h-11 rounded-full bg-gradient-to-br from-red-50 to-red-100 flex items-center justify-center border-none shadow-[0_8px_16px_rgba(0,0,0,0.1),inset_0_1px_0_rgba(255,255,255,0.8)] text-red-500 transition-all duration-300 [transition-timing-function:cubic-bezier(0.175,0.885,0.32,1.275)] cursor-pointer backdrop-blur-[10px] border border-white/30 lg:hover:scale-110 lg:hover:rotate-[5deg] hover:shadow-[0_12px_24px_rgba(239,68,68,0.2)]";
                }

                button.setAttribute('title', 'Favorilerden Kaldır');
            }

            showToast(result.message, result.type);

        } catch (error) {
            console.error('Favorilere ekleme hatası:', error);
            showToast('Bir hata oluştu.', 'danger');
            updateCounter('favourites-summary', -1);
        } finally {
            button.classList.remove('processing');
        }
    });

    document.addEventListener('click', async function (e) {
        const button = e.target.closest('.remove-from-favs-btn');
        if (!button) return;

        e.preventDefault();

        const productId = parseInt(button.dataset.productId);
        if (!productId || isNaN(productId)) return;

        if (button.classList.contains('processing')) return;
        button.classList.add('processing');

        updateCounter('favourites-summary', -1);

        try {
            const response = await fetch(`/api/account/favourites/remove/${productId}`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                }
            });

            if (response.status === 401) {
                redirectToLogin();
                return;
            }

            if (!response.ok) {
                throw new Error(`Ağ Hatası: ${response.status}`);
            }

            const result = await response.json();

            if (result.success) {
                const favoritesPage = document.querySelector('.fav-page');
                if (favoritesPage) {
                    const productCard = button.closest('.product-wrapper');

                    if (productCard) {
                        productCard.classList.add('opacity-0', 'transition-opacity', 'duration-500');

                        setTimeout(() => {
                            productCard.remove();

                            const remainingProducts = document.querySelectorAll('.product-wrapper');
                            if (remainingProducts.length === 0) {
                                const productsWrapper = document.querySelector('.fav-wrapper');
                                if (productsWrapper) {
                                    productsWrapper.parentElement.innerHTML = `
                                        <div class="fav-empty">
                                            <div class="fav-empty-icon">
                                                <i class="fa fa-heart-broken"></i>
                                            </div>
                                            <h4 class="text-slate-800 font-bold text-2xl mb-2">Henüz favori ürününüz bulunmuyor</h4>
                                            <p class="text-slate-500 max-w-md mx-auto mb-8">Beğendiğiniz ürünleri favorilerinize ekleyerek daha sonra kolayca ulaşabilirsiniz. </p>
                                            <a href="/Products/Index" class="fav-btn-shop">
                                                <i class="fa fa-shopping-bag"></i> Alışverişe Başla
                                            </a>
                                        </div>
                                    `;
                                }
                            }
                        }, 500);
                    }
                } else {
                    const isSmall = button.classList.contains('w-6');
                    const isDetailMain = button.classList.contains('w-12');
                    const isMobileDetail = button.classList.contains('w-10');

                    if (isDetailMain) {
                        button.innerHTML = '<i class="far fa-heart text-xl"></i>';
                    } else if (isMobileDetail) {
                        button.innerHTML = '<i class="far fa-heart text-lg"></i>';
                    } else if (isSmall) {
                        button.innerHTML = '<i class="far fa-heart text-[8px] sm:text-xs"></i>';
                    } else {
                        button.innerHTML = '<i class="far fa-heart text-xs sm:text-lg"></i>';
                    }

                    if (isDetailMain) {
                        button.className = "add-to-favs-btn absolute top-5 right-5 bg-white/95 backdrop-blur-lg text-gray-600 w-12 h-12 rounded-full flex items-center justify-center cursor-pointer transition-all duration-300 z-30 shadow-md border border-white/30 hover:bg-gradient-to-br hover:from-red-50 hover:to-red-100 hover:text-red-500 hover:scale-110 hover:rotate-6 hover:shadow-lg";
                    } else if (isMobileDetail) {
                        button.className = "add-to-favs-btn bg-white/80 backdrop-blur-md p-2 rounded-full shadow-sm text-gray-500 w-10 h-10 flex items-center justify-center border border-white/30";
                    } else if (isSmall) {
                        button.className = "add-to-favs-btn w-6 h-6 sm:w-8 sm:h-8 rounded-full bg-gradient-to-br from-white to-slate-50 flex items-center justify-center border-none shadow-sm text-slate-500 transition-all duration-300 backdrop-blur-[10px] border border-white/30 hover:scale-110 hover:shadow-md hover:bg-gradient-to-br hover:from-red-50 hover:to-red-100 hover:text-red-500";
                    } else {
                        button.className = "add-to-favs-btn w-7 h-7 sm:w-11 sm:h-11 rounded-full bg-gradient-to-br from-white to-slate-50 flex items-center justify-center border-none shadow-[0_8px_16px_rgba(0,0,0,0.1),inset_0_1px_0_rgba(255,255,255,0.8)] text-slate-500 transition-all duration-300 [transition-timing-function:cubic-bezier(0.175,0.885,0.32,1.275)] cursor-pointer backdrop-blur-[10px] border border-white/30 lg:hover:scale-110 lg:hover:rotate-[5deg] hover:shadow-[0_12px_24px_rgba(0,0,0,0.15)] hover:bg-gradient-to-br hover:from-red-50 hover:to-red-100 hover:text-red-500";
                    }

                    button.setAttribute('title', 'Favorilere Ekle');
                }
            }

            showToast(result.message, result.type);

        } catch (error) {
            console.error('Favorilerden kaldırma hatası:', error);
            showToast('Bir hata oluştu.', 'danger');
            updateCounter('favourites-summary', +1);
        } finally {
            button.classList.remove('processing');
        }
    });

    // Sepet işlemleri

    document.addEventListener('click', async function (e) {
        const button = e.target.closest('.add-to-cart-btn');
        if (!button) return;

        e.preventDefault();

        const productId = parseInt(button.dataset.productId);

        if (isNaN(productId)) return;

        if (button.classList.contains('processing')) return;
        button.classList.add('processing');

        updateCounter('cart-summary', +1);

        try {
            const data = {
                productId: productId,
                quantity: 1
            };

            const response = await postJSON('/api/cart/items', data);

            if (response.success) {
                cartItems.add(productId);

                convertToRemoveButton(button, productId);

                showToast(response.message, response.type);
            } else {
                showToast(response.message, response.type);
            }
        } catch (error) {
            console.error('Sepete ekleme hatası:', error);
            updateCounter('cart-summary', -1);
            showToast('Sepete eklenirken bir hata oluştu.', 'danger');
        } finally {
            button.classList.remove('processing');
        }
    });

    document.addEventListener('click', async function (e) {
        const button = e.target.closest('.remove-from-cart-btn');
        if (!button) return;

        e.preventDefault();

        const productId = parseInt(button.dataset.productId);
        if (!productId || isNaN(productId)) return;

        if (button.classList.contains('processing')) return;
        button.classList.add('processing');

        updateCounter('cart-summary', -1);

        try {
            const response = await deleteJSON('/api/cart/items/' + productId);

            if (response.success) {
                cartItems.delete(productId);

                convertToAddButton(button, productId);

                showToast(response.message, response.type);
            } else {
                showToast(response.message, response.type);
            }
        } catch (error) {
            console.error('Sepetten kaldırma hatası:', error);
            updateCounter('cart-summary', +1);
            showToast('Sepetten çıkarılırken bir hata oluştu.', 'danger');
        } finally {
            button.classList.remove('processing');
        }
    });

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initCartState);
    } else {
        initCartState();
    }

    document.addEventListener('visibilitychange', function () {
        if (!document.hidden) {
            try {
                const response = await fetch('/api/cart/version');

                if (response.ok) {
                    const version = await response.json();
                    if (version !== localStorage.getItem('cartVersion')) {
                        initCartState();
                    }
                }
            } catch (error) {
                console.error('Sepet versiyonu yüklenirken hata oluştu:', error);
            }

        }
    });
})();