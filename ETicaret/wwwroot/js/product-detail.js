function changeQuantity(amount) {
	var input = document.getElementById('productQuantity');
	if (!input) return;

	var current = parseInt(input.value) || 1;
	var min = parseInt(input.min) || 1;
	var max = parseInt(input.max) || 10;
	var next = current + amount;
	if (next >= min && next <= max) {
		input.value = next;
	}
}

function toggleAccordion(element) {
	const content = element.nextElementSibling;
	const icon = element.querySelector('.fa-chevron-down');

	if (content.classList.contains('hidden')) {
		content.classList.remove('hidden');
		if (icon) icon.classList.add('rotate-180');
	} else {
		content.classList.add('hidden');
		if (icon) icon.classList.remove('rotate-180');
	}
}

function openPhotoModal(imageSrc) {
	const modal = document.getElementById('photoModal');
	const modalImage = document.getElementById('modalImage');
	if (!modal || !modalImage) return;

	modalImage.src = imageSrc;
	modal.style.display = 'flex';
	modal.offsetHeight;
	modal.classList.add('show');
	document.addEventListener('keydown', handleEscapeKey);
	document.body.style.overflow = 'hidden';
}

function closePhotoModal() {
	const modal = document.getElementById('photoModal');
	if (!modal) return;

	modal.classList.remove('show');
	setTimeout(() => {
		modal.style.display = 'none';
		document.body.style.overflow = 'auto';
	}, 300);
	document.removeEventListener('keydown', handleEscapeKey);
}

function handleEscapeKey(event) {
	if (event.key === 'Escape') {
		closePhotoModal();
	}
}

function getFavoritesFromCookie() {
	const value = `; ${document.cookie}`;
	const parts = value.split(`; FavouriteProducts=`);
	if (parts.length === 2) {
		let cookieVal = parts.pop().split(';').shift();
		try {
			cookieVal = decodeURIComponent(cookieVal);
		} catch (e) {
			console.error('Cookie decode error', e);
		}
		return cookieVal.split('|').map(id => parseInt(id)).filter(id => !isNaN(id));
	}
	return [];
}

async function checkIfProductInCart(productId, cartBtn, variantId) {
	try {
		if (!variantId && variantId !== 0) return false;

		const response = await fetch('/api/cart');
		if (!response.ok) return false;

		const cart = await response.json();
		if (!cart || !cart.lines) return false;

		const cartLine = cart.lines.find(line => {
			return line.productId == productId && line.productVariantId == variantId;
		});

		const isInCart = !!cartLine;

		if (isInCart) {
			cartBtn.setAttribute('data-in-cart', 'true');
			cartBtn.setAttribute('data-cart-quantity', cartLine.quantity || 1);
			const btnContent = cartBtn.querySelector('.btn-content');
			const btnRemove = cartBtn.querySelector('.btn-remove');

			if (btnContent) btnContent.style.display = 'none';
			if (btnRemove) btnRemove.style.display = 'flex';

			cartBtn.classList.add('in-cart');

		} else {
			cartBtn.setAttribute('data-in-cart', 'false');
			cartBtn.removeAttribute('data-cart-quantity');
			const btnContent = cartBtn.querySelector('.btn-content');
			const btnRemove = cartBtn.querySelector('.btn-remove');

			if (btnContent) btnContent.style.display = 'flex';
			if (btnRemove) btnRemove.style.display = 'none';

			cartBtn.classList.remove('in-cart');
		}

		return isInCart;
	} catch (error) {
		console.error('Sepet kontrolü sırasında hata:', error);
		return false;
	}
}

function updateFavoriteButtonState(variantId) {
	const favButtons = document.querySelectorAll('.pd-fav-btn');
	if (favButtons.length === 0) return;

	const favorites = getFavoritesFromCookie();
	const isFav = favorites.includes(variantId);

	favButtons.forEach(btn => {
		btn.dataset.productVariantId = variantId;

		if (isFav) {
			btn.classList.remove('pd-add-to-favs-btn');
			btn.classList.add('pd-remove-from-favs-btn');
			btn.setAttribute('title', 'Favorilerden Kaldır');

			if (btn.closest('.lg\\:hidden')) {
				btn.className = "pd-fav-btn pd-remove-from-favs-btn bg-white/80 backdrop-blur-md p-2 rounded-full shadow-sm text-red-500 w-10 h-10 flex items-center justify-center border border-white/30";
				btn.innerHTML = '<i class="fas fa-heart text-lg"></i>';
			} else {
				btn.className = "pd-fav-btn pd-remove-from-favs-btn absolute top-5 right-5 bg-white/95 backdrop-blur-lg text-red-500 w-12 h-12 rounded-full flex items-center justify-center cursor-pointer transition-all duration-300 z-30 shadow-md border border-white/30 hover:bg-gradient-to-br hover:from-red-50 hover:to-red-100 hover:text-red-500 hover:scale-110 hover:rotate-6 hover:shadow-lg";
				btn.innerHTML = '<i class="fas fa-heart text-xl"></i>';
			}
		} else {
			btn.classList.remove('pd-remove-from-favs-btn');
			btn.classList.add('pd-add-to-favs-btn');
			btn.setAttribute('title', 'Favorilere Ekle');

			if (btn.closest('.lg\\:hidden')) {
				btn.className = "pd-fav-btn pd-add-to-favs-btn bg-white/80 backdrop-blur-md p-2 rounded-full shadow-sm text-gray-500 w-10 h-10 flex items-center justify-center border border-white/30";
				btn.innerHTML = '<i class="far fa-heart text-lg"></i>';
			} else {
				btn.className = "pd-fav-btn pd-add-to-favs-btn absolute top-5 right-5 bg-white/95 backdrop-blur-lg text-gray-600 w-12 h-12 rounded-full flex items-center justify-center cursor-pointer transition-all duration-300 z-30 shadow-md border border-white/30 hover:bg-gradient-to-br hover:from-red-50 hover:to-red-100 hover:text-red-500 hover:scale-110 hover:rotate-6 hover:shadow-lg";
				btn.innerHTML = '<i class="far fa-heart text-xl"></i>';
			}
		}
	});
}

async function updateVariantDetails(variantId) {
	try {
		const response = await fetch(`/api/products/variants/${variantId}`);

		if (!response.ok) {
			console.error('Varyant bilgisi alınamadı');
			return;
		}

		const result = await response.json();

		if (!result.success || !result.data) {
			console.error('Geçersiz varyant verisi');
			return;
		}

		const variant = result.data;

		const priceElements = document.querySelectorAll(
			'.text-3xl.font-black.bg-primary, ' + 
			'.safe-area-bottom .text-2xl, ' + 
			'h1 + div span.text-3xl' 
		);

		const displayPrice = variant.discountPrice || variant.price;
		priceElements.forEach(el => {
			el.innerText = new Intl.NumberFormat('tr-TR', {
				style: 'currency',
				currency: 'TRY'
			}).format(displayPrice);
		});

		if (variant.discount > 0) {
			const discountBadges = document.querySelectorAll('.text-xs.font-extrabold');
			discountBadges.forEach(badge => {
				if (badge.textContent.includes('%') || badge.textContent.includes('İNDİRİM')) {
					badge.textContent = `%${variant.discount} İNDİRİM`;
				}
			});
		}

		const allCartBtns = document.querySelectorAll('.pd-add-cart-btn, #addToCartBtn');

		if (variant.stock <= 0) {
			allCartBtns.forEach(btn => {
				btn.disabled = true;
				const txtSpan = btn.querySelector('.btn-text');
				if (txtSpan) txtSpan.innerText = "Stokta Yok";
				btn.classList.add('bg-gray-400', 'cursor-not-allowed', 'opacity-50');
				btn.classList.remove('bg-button', 'bg-gradient-to-br');
			});
		} else {
			allCartBtns.forEach(btn => {
				btn.disabled = false;
				const txtSpan = btn.querySelector('.btn-text');
				if (txtSpan) txtSpan.innerText = "Sepete Ekle";
				btn.classList.remove('bg-gray-400', 'cursor-not-allowed', 'opacity-50');
				btn.classList.add('bg-button');
			});
		}

		if (variant.images && variant.images.length > 0) {
			const mainImage = document.querySelector('.pd-main-image');
			const thumbnailContainer = document.querySelector('.pd-thumbnail')?.parentElement;

			const primaryImage = variant.images.find(img => img.isPrimary) || variant.images[0];
			if (mainImage && primaryImage) {
				mainImage.src = primaryImage.imageUrl;
				mainImage.alt = primaryImage.altText || variant.color || 'Product Image';
			}

			if (thumbnailContainer) {
				thumbnailContainer.innerHTML = '';
				variant.images.forEach((img, index) => {
					const thumb = document.createElement('img');
					thumb.src = img.imageUrl;
					thumb.alt = img.altText || `Thumbnail ${index + 1}`;
					thumb.className = `pd-thumbnail w-20 h-20 object-cover rounded-xl cursor-pointer transition-all duration-300 border-2 opacity-70 bg-gray-100 flex-shrink-0 hover:opacity-100 hover:-translate-y-1 hover:shadow-md ${index === 0 ? 'border-blue-600 opacity-100 shadow-[0_0_0_4px_rgba(0,102,255,0.2)] active' : 'border-transparent'}`;

					thumb.addEventListener('click', function () {
						if (mainImage) {
							mainImage.src = this.src;
							mainImage.alt = this.alt;
						}
						document.querySelectorAll('.pd-thumbnail').forEach(t => {
							t.classList.remove('active', 'border-blue-600', 'opacity-100', 'shadow-[0_0_0_4px_rgba(0,102,255,0.2)]');
							t.classList.add('border-transparent', 'opacity-70');
						});
						this.classList.add('active', 'border-blue-600', 'opacity-100', 'shadow-[0_0_0_4px_rgba(0,102,255,0.2)]');
						this.classList.remove('border-transparent', 'opacity-70');
					});

					thumbnailContainer.appendChild(thumb);
				});
			}
		}

		const specsContainers = document.querySelectorAll('#variant-specs, #mobile-specs-content');

		if (specsContainers.length > 0 && variant.specifications) {
			const selectorKeys = window.variantSelectors?.map(s => s.Key) || [];

			const technicalSpecs = Object.entries(variant.specifications)
				.filter(([key]) => !selectorKeys.includes(key));

			let html = '';
			if (technicalSpecs.length > 0) {
				technicalSpecs.forEach(([key, value], index) => {
					const desktopClass = index % 2 === 0 ? '' : 'bg-gray-50';
				});
			}

			specsContainers.forEach(container => {
				const isMobile = container.id === 'mobile-specs-content';
				let containerHtml = '';

				if (technicalSpecs.length > 0) {
					technicalSpecs.forEach(([key, value], index) => {
						if (isMobile) {
							const bgClass = index % 2 === 0 ? '' : 'bg-gray-50';
							containerHtml += `
								<div class="flex ${bgClass}">
									<div class="flex-1 py-3 px-4 text-xs font-bold text-gray-800 bg-blue-500/5 border-r border-gray-100 flex items-center">${key}</div>
									<div class="flex-[2] py-3 px-4 text-xs text-gray-600 font-medium flex items-center">${value}</div>
								</div>`;
						} else {
							const bgClass = index % 2 === 0 ? '' : 'bg-gray-50';
							containerHtml += `
								<div class="flex ${bgClass}">
									<div class="flex-1 py-5 px-6 font-bold text-gray-800 bg-blue-500/5 border-r border-gray-100">${key}</div>
									<div class="flex-[2] py-5 px-6 text-gray-600 font-medium">${value}</div>
								</div>`;
						}
					});
				}

				container.innerHTML = containerHtml;
			});
		}

		const vId = variant.productVariantId || variant.variantId || variant.ProductVariantId;
		if (typeof updateFavoriteButtonState === 'function') {
			updateFavoriteButtonState(vId);
		}

		const allCartBtns2 = document.querySelectorAll('.pd-add-cart-btn, #addToCartBtn');
		const pId = variant.productId || variant.ProductId || (allCartBtns2.length > 0 ? parseInt(allCartBtns2[0].getAttribute('data-product-id')) : 0);

		allCartBtns2.forEach(btn => {
			btn.setAttribute('data-product-variant-id', vId);

			if (pId > 0 && vId > 0) {
				checkIfProductInCart(pId, btn, vId);
			}
		});

	} catch (error) {
		console.error('Varyant güncelleme hatası:', error);
	}
}

function updateActiveStates(key, value) {
	document.querySelectorAll(`.pd-variant-option[data-key="${key}"]`).forEach(opt => {
		opt.classList.remove('active');

		if (opt.classList.contains('pd-color-option')) {
			opt.classList.remove('!border-blue-600', 'scale-110', '!shadow-[0_0_0_4px_rgba(37,99,235,0.3)]');
			opt.classList.add('border-gray-200');
			const check = opt.querySelector('.active-check');
			if (check) {
				check.classList.remove('!opacity-100');
				check.classList.add('opacity-0');
			}
		}

		if (opt.classList.contains('pd-text-option')) {
			opt.classList.remove('!border-blue-600', '!bg-gradient-to-br', '!from-blue-500', '!to-blue-600', '!text-white', '!shadow-lg', '-translate-y-1');
			opt.classList.add('border-gray-300', 'bg-white', 'text-gray-700');
		}
	});

	const selectedOpts = document.querySelectorAll(`.pd-variant-option[data-key="${key}"][data-value="${value}"]`);
	selectedOpts.forEach(selectedOpt => {
		selectedOpt.classList.add('active');

		if (selectedOpt.classList.contains('pd-color-option')) {
			selectedOpt.classList.remove('border-gray-200');
			selectedOpt.classList.add('!border-blue-600', 'scale-110', '!shadow-[0_0_0_4px_rgba(37,99,235,0.3)]');
			const check = selectedOpt.querySelector('.active-check');
			if (check) {
				check.classList.remove('opacity-0');
				check.classList.add('!opacity-100');
			}
		}

		if (selectedOpt.classList.contains('pd-text-option')) {
			selectedOpt.classList.remove('border-gray-300', 'bg-white', 'text-gray-700');
			selectedOpt.classList.add('!border-blue-600', '!bg-gradient-to-br', '!from-blue-500', '!to-blue-600', '!text-white', '!shadow-lg', '-translate-y-1');
		}
	});
}



(function () {

	async function initProductDetailPage() {
		const modal = document.getElementById('photoModal');
		if (modal) {
			modal.addEventListener('click', function (event) {
				if (event.target === this) {
					closePhotoModal();
				}
			});
		}

		const mainImage = document.querySelector('.pd-main-image');
		if (mainImage) {
			mainImage.style.cursor = 'pointer';
			mainImage.addEventListener('click', function () {
				openPhotoModal(this.src);
			});
		}

		const thumbnails = document.querySelectorAll('.pd-thumbnail');
		thumbnails.forEach(function (thumbnail) {
			thumbnail.style.cursor = 'pointer';
			thumbnail.addEventListener('click', function () {
				if (mainImage) {
					mainImage.src = this.src;
					mainImage.alt = this.alt || mainImage.alt;
				}
				thumbnails.forEach(function (thumb) {
					thumb.classList.remove('active');
					thumb.classList.remove('border-blue-600', 'opacity-100', 'shadow-[0_0_0_4px_rgba(0,102,255,0.2)]');
					thumb.classList.add('border-transparent', 'opacity-70');
				});
				this.classList.add('active');
				this.classList.remove('border-transparent', 'opacity-70');
				this.classList.add('border-blue-600', 'opacity-100', 'shadow-[0_0_0_4px_rgba(0,102,255,0.2)]');
			});
		});

		const prevBtn = document.getElementById('prev-image-btn');
		const nextBtn = document.getElementById('next-image-btn');

		if (prevBtn && nextBtn) {
			prevBtn.addEventListener('click', function (e) {
				e.stopPropagation();
				navigateImage(-1);
			});
			nextBtn.addEventListener('click', function (e) {
				e.stopPropagation();
				navigateImage(1);
			});
		}

		function navigateImage(direction) {
			const thumbnails = Array.from(document.querySelectorAll('.pd-thumbnail'));
			if (thumbnails.length === 0) return;

			const activeIndex = thumbnails.findIndex(t => t.classList.contains('active'));
			let nextIndex;

			if (activeIndex === -1) {
				nextIndex = 0;
			} else {
				nextIndex = (activeIndex + direction + thumbnails.length) % thumbnails.length;
			}

			thumbnails[nextIndex].click();

			thumbnails[nextIndex].scrollIntoView({ behavior: 'smooth', block: 'nearest', inline: 'center' });
		}

		const commentPhotos = document.querySelectorAll('.pd-comment-photo');
		commentPhotos.forEach(function (photo) {
			photo.style.cursor = 'pointer';
			photo.addEventListener('click', function () {
				openPhotoModal(this.src);
			});
		});

		const tabButtons = document.querySelectorAll('[data-tab-target]');
		const tabContents = document.querySelectorAll('.tab-pane');

		tabButtons.forEach(button => {
			button.addEventListener('click', () => {
				const targetSelector = button.getAttribute('data-tab-target');
				const targetContent = document.querySelector(targetSelector);

				if (!targetContent) return;

				tabButtons.forEach(btn => {
					btn.classList.remove('active');
				});

				button.classList.add('active');

				tabContents.forEach(content => {
					content.classList.remove('opacity-100');
					content.classList.add('opacity-0');
					content.classList.remove('block');
					content.classList.add('hidden');
				});

				targetContent.classList.remove('hidden');
				targetContent.classList.add('block');

				setTimeout(() => {
					targetContent.classList.remove('opacity-0');
					targetContent.classList.add('opacity-100');
				}, 20);
			});
		});

		const variantOptions = document.querySelectorAll('.pd-variant-option');
		const selectedVariantInput = document.getElementById('selectedVariantId');

		function updateVariantState(shouldUpdateUrl = true) {
			if (!window.productVariants || window.productVariants.length === 0) return;
			if (!window.variantSelectors || window.variantSelectors.length === 0) return;

			const selections = {};
			window.variantSelectors.forEach(sel => {
				const activeOpt = document.querySelector(`.pd-variant-option[data-key="${sel.Key}"].active`);
				if (activeOpt) {
					selections[sel.Key] = activeOpt.getAttribute('data-value');
				}
			});

			const matchedVariant = window.productVariants.find(v => {
				return window.variantSelectors.every(sel => {
					const selectedVal = selections[sel.Key];
					if (!selectedVal) return true;

					if (sel.Key === "Renk") return v.Color === selectedVal;
					if (sel.Key === "Beden") return v.Size === selectedVal;

					return v.Specifications && v.Specifications[sel.Key] === selectedVal;
				});
			});

			const allSelected = window.variantSelectors.every(sel => selections[sel.Key]);
			const fullMatchVariant = allSelected ? matchedVariant : null;

			function toCamelCase(str) {
				if (!str) return '';
				return str
					.split(' ')
					.map((word, index) => {
						if (index === 0) {
							return word.charAt(0).toLowerCase() + word.slice(1);
						}
						return word.charAt(0).toUpperCase() + word.slice(1);
					})
					.join('');
			}

			window.variantSelectors.forEach(targetSel => {
				const otherSelections = { ...selections };
				delete otherSelections[targetSel.Key];

				const possibleVariants = window.productVariants.filter(v => {
					return Object.entries(otherSelections).every(([key, val]) => {
						if (!val) return true;
						if (key === "Renk") return v.Color === val;
						if (key === "Beden") return v.Size === val;
						return v.Specifications && v.Specifications[key] === val;
					});
				});

				const availableValues = new Set();
				possibleVariants.forEach(v => {
					if (v.Stock <= 0) return;

					let val;
					if (targetSel.Key === "Renk") val = v.Color;
					else if (targetSel.Key === "Beden") val = v.Size;
					else val = v.Specifications ? v.Specifications[targetSel.Key] : null;

					if (val) availableValues.add(val);
				});

				const options = document.querySelectorAll(`.pd-variant-option[data-key="${targetSel.Key}"]`);
				options.forEach(opt => {
					const val = opt.getAttribute('data-value');
					if (!availableValues.has(val)) {
						opt.classList.add('opacity-40', 'cursor-not-allowed');
					} else {
						opt.classList.remove('opacity-40', 'cursor-not-allowed');
					}
				});
			});


			if (fullMatchVariant) {
				if (selectedVariantInput) selectedVariantInput.value = fullMatchVariant.ProductVariantId;

				updateVariantDetails(fullMatchVariant.ProductVariantId);

				if (shouldUpdateUrl) {
					const params = new URLSearchParams(window.location.search);
					Object.entries(selections).forEach(([key, val]) => {
						const camelKey = toCamelCase(key);
						if (val) params.set(camelKey, val);
						else params.delete(camelKey);
					});

					const newUrl = `${window.location.pathname}?${params.toString()}`;
					window.history.replaceState({ path: newUrl }, '', newUrl);
				}

			} else {
				if (selectedVariantInput) selectedVariantInput.value = "";
				const allCartBtns = document.querySelectorAll('.pd-add-cart-btn, #addToCartBtn');

				if (allSelected) {
					allCartBtns.forEach(btn => {
						btn.disabled = true;
						const txtSpan = btn.querySelector('.btn-text');
						if (txtSpan) txtSpan.innerText = "Seçenek Yok";
						else btn.innerText = "Seçenek Yok";
						btn.classList.add('bg-gray-400', 'cursor-not-allowed', 'opacity-50');
					});
				} else {
					allCartBtns.forEach(btn => {
						btn.disabled = true;
						const txtSpan = btn.querySelector('.btn-text');
						if (txtSpan) txtSpan.innerText = "Seçenekleri Belirleyin";
						else btn.innerText = "Seçenek Seçin";
						btn.classList.add('bg-gray-400', 'cursor-not-allowed', 'opacity-50');
					});
				}
			}
		}

		variantOptions.forEach(function (option) {
			option.addEventListener('click', function () {
				if (this.classList.contains('cursor-not-allowed')) return;

				const key = this.getAttribute('data-key');
				const value = this.getAttribute('data-value');

				updateActiveStates(key, value);

				updateVariantState();
			});
		});

		window.selectDefaultVariant = function () {
			if (!window.productVariants || window.productVariants.length === 0) return;

			const params = new URLSearchParams(window.location.search);
			let hasUrlSelection = false;

			if (window.variantSelectors) {
				window.variantSelectors.forEach(sel => {
					const camelKey = sel.Key.charAt(0).toLowerCase() + sel.Key.slice(1);
					const val = params.get(camelKey) || params.get(sel.Key);

					if (val) {
						updateActiveStates(sel.Key, val);
						hasUrlSelection = true;
					}
				});
			}

			if (!hasUrlSelection) {
				let defaultVariant = window.productVariants.find(v => v.IsDefault);
				if (!defaultVariant) defaultVariant = window.productVariants[0];

				if (defaultVariant && window.variantSelectors) {
					window.variantSelectors.forEach(sel => {
						let val;
						if (sel.Key === "Renk") val = defaultVariant.Color;
						else if (sel.Key === "Beden") val = defaultVariant.Size;
						else val = defaultVariant.Specifications ? defaultVariant.Specifications[sel.Key] : null;

						if (val) {
							updateActiveStates(sel.Key, val);
						}
					});
				}
			}

			updateVariantState(false);
		};

		selectDefaultVariant();

		document.addEventListener('click', async function (e) {
			const button = e.target.closest('.pd-add-to-favs-btn');
			if (!button) return;

			e.preventDefault();

			if (!window.isAuthenticated) {
				const currentUrl = window.location.pathname + window.location.search;
				window.location.href = `/account/login?returnUrl=${encodeURIComponent(currentUrl)}`;
				return;
			}

			const productVariantId = parseInt(button.dataset.productVariantId);
			if (!productVariantId || isNaN(productVariantId)) return;

			if (button.classList.contains('processing')) return;
			button.classList.add('processing');

			const badge = document.getElementById('favourites-summary');
			if (badge) badge.innerText = parseInt(badge.innerText || 0) + 1;

			try {
				const response = await fetch(`/api/account/favourites/add/${productVariantId}`, {
					method: 'POST',
					headers: { 'Content-Type': 'application/json' }
				});

				if (response.status === 401) {
					const currentUrl = window.location.pathname + window.location.search;
					window.location.href = `/account/login?returnUrl=${encodeURIComponent(currentUrl)}`;
					return;
				}

				if (!response.ok) throw new Error(`Ağ Hatası: ${response.status}`);

				const result = await response.json();

				if (result.success) {
					updateFavoriteButtonState(productVariantId);

					showToast(result.message, result.type);
				} else {
					showToast(result.message, result.type);
				}
			} catch (error) {
				console.error('Favorilere ekleme hatası:', error);
				showToast('Bir hata oluştu.', 'danger');
				if (badge) badge.innerText = Math.max(0, parseInt(badge.innerText || 0) - 1);
			} finally {
				button.classList.remove('processing');
			}
		});

		document.addEventListener('click', async function (e) {
			const button = e.target.closest('.pd-remove-from-favs-btn');
			if (!button) return;

			e.preventDefault();

			if (!window.isAuthenticated) {
				const currentUrl = window.location.pathname + window.location.search;
				window.location.href = `/account/login?returnUrl=${encodeURIComponent(currentUrl)}`;
				return;
			}

			const productVariantId = parseInt(button.dataset.productVariantId);
			if (!productVariantId || isNaN(productVariantId)) return;

			if (button.classList.contains('processing')) return;
			button.classList.add('processing');

			const badge = document.getElementById('favourites-summary');
			if (badge) badge.innerText = Math.max(0, parseInt(badge.innerText || 0) - 1);

			try {
				const response = await fetch(`/api/account/favourites/remove/${productVariantId}`, {
					method: 'POST',
					headers: { 'Content-Type': 'application/json' }
				});

				if (response.status === 401) {
					const currentUrl = window.location.pathname + window.location.search;
					window.location.href = `/account/login?returnUrl=${encodeURIComponent(currentUrl)}`;
					return;
				}

				if (!response.ok) throw new Error(`Ağ Hatası: ${response.status}`);

				const result = await response.json();

				if (result.success) {
					updateFavoriteButtonState(productVariantId);
					showToast(result.message, result.type);
				} else {
					showToast(result.message, result.type);
					if (badge) badge.innerText = parseInt(badge.innerText || 0) + 1;
				}
			} catch (error) {
				console.error('Favorilerden çıkarma hatası:', error);
				showToast('Bir hata oluştu.', 'danger');
				if (badge) badge.innerText = parseInt(badge.innerText || 0) + 1;
			} finally {
				button.classList.remove('processing');
			}
		});


	}

	document.addEventListener('click', async function (event) {
		const clickedBtn = event.target.closest('.pd-add-cart-btn');
		if (!clickedBtn) return;

		if (clickedBtn.classList.contains('loading') || clickedBtn.classList.contains('removing')) {
			return;
		}

		event.preventDefault();

		const productId = clickedBtn.getAttribute('data-product-id');
		const allButtons = document.querySelectorAll(`.pd-add-cart-btn[data-product-id="${productId}"]`);

		const isInCart = clickedBtn.getAttribute('data-in-cart') === 'true';

		if (isInCart) {
			allButtons.forEach(btn => {
				const btnContent = btn.querySelector('.btn-content');
				const btnLoading = btn.querySelector('.btn-loading');
				const btnRemove = btn.querySelector('.btn-remove');
				const btnRemoveLoading = btn.querySelector('.btn-remove-loading');

				if (btnContent) btnContent.style.display = 'none';
				if (btnLoading) btnLoading.style.display = 'none';
				if (btnRemove) btnRemove.style.display = 'none';
				if (btnRemoveLoading) btnRemoveLoading.style.display = 'flex';
				btn.classList.add('removing');
				btn.disabled = true;
			});

			try {
				const variantInput = document.getElementById('selectedVariantId');
				let variantId = variantInput?.value ? parseInt(variantInput.value) : null;
				const response = await fetch(`/api/cart/items/${productId}/variants/${variantId}`, {
					method: 'DELETE',
					headers: {
						'Content-Type': 'application/json'
					}
				});

				if (!response.ok) throw new Error('Ağ hatası');

				const result = await response.json();
				if (result.success) {
					allButtons.forEach(btn => {
						const btnContent = btn.querySelector('.btn-content');
						const btnLoading = btn.querySelector('.btn-loading');
						const btnRemove = btn.querySelector('.btn-remove');
						const btnRemoveLoading = btn.querySelector('.btn-remove-loading');

						if (btnContent) btnContent.style.display = 'flex';
						if (btnLoading) btnLoading.style.display = 'none';
						if (btnRemove) btnRemove.style.display = 'none';
						if (btnRemoveLoading) btnRemoveLoading.style.display = 'none';
						btn.classList.remove('removing', 'in-cart');
						btn.setAttribute('data-in-cart', 'false');
						btn.disabled = false;
					});

					if (result.cart && Array.isArray(result.cart.lines)) {
						if (typeof setCounter === 'function') {
							setCounter('cart-summary', result.cart.lines.length);
						}
					} else if (typeof dispatchCartUpdatedEvent === 'function') {
						dispatchCartUpdatedEvent();
					}

					if (typeof showToast === 'function') showToast(result.message || 'Ürün sepetten kaldırıldı!', 'success');
				} else {
					allButtons.forEach(btn => {
						const btnContent = btn.querySelector('.btn-content');
						const btnRemove = btn.querySelector('.btn-remove');
						resetButtonState(btn, btnContent, btnRemove, true);
					});
					if (typeof showToast === 'function') showToast('Hata oluştu', 'danger');
				}
			} catch (error) {
				console.error(error);
				allButtons.forEach(btn => {
					const btnContent = btn.querySelector('.btn-content');
					const btnRemove = btn.querySelector('.btn-remove');
					resetButtonState(btn, btnContent, btnRemove, true);
				});
				if (typeof showToast === 'function') showToast('Hata oluştu', 'danger');
			}
			return;
		}

		allButtons.forEach(btn => {
			const btnContent = btn.querySelector('.btn-content');
			const btnLoading = btn.querySelector('.btn-loading');
			const btnRemove = btn.querySelector('.btn-remove');
			const btnRemoveLoading = btn.querySelector('.btn-remove-loading');

			if (btnContent) btnContent.style.display = 'none';
			if (btnLoading) btnLoading.style.display = 'flex';
			if (btnRemove) btnRemove.style.display = 'none';
			if (btnRemoveLoading) btnRemoveLoading.style.display = 'none';
			btn.classList.add('loading');
			btn.disabled = true;
		});

		try {
			const quantityInput = document.getElementById('productQuantity');
			const variantInput = document.getElementById('selectedVariantId');
			let quantity = parseInt(quantityInput?.value || 1);
			let variantId = variantInput?.value ? parseInt(variantInput.value) : null;

			if (isNaN(quantity) || quantity < 1) quantity = 1;

			if (window.productVariants && window.productVariants.length > 0 && !variantId) {
				if (typeof showToast === 'function') showToast('Lütfen renk ve beden seçiniz.', 'warning');

				allButtons.forEach(btn => {
					const btnContent = btn.querySelector('.btn-content');
					const btnLoading = btn.querySelector('.btn-loading');
					if (btnContent) btnContent.style.display = 'flex';
					if (btnLoading) btnLoading.style.display = 'none';
					btn.classList.remove('loading');
					btn.disabled = false;
				});
				return;
			}

			const response = await fetch(`/api/cart/items`, {
				method: 'POST',
				headers: { 'Content-Type': 'application/json' },
				body: JSON.stringify({
					productId: parseInt(productId),
					quantity: quantity,
					productVariantId: variantId
				})
			});

			if (!response.ok) throw new Error('Ağ hatası');

			const result = await response.json();
			if (result.success) {
				allButtons.forEach(btn => {
					const btnContent = btn.querySelector('.btn-content');
					const btnLoading = btn.querySelector('.btn-loading');
					const btnRemove = btn.querySelector('.btn-remove');
					const btnRemoveLoading = btn.querySelector('.btn-remove-loading');

					if (btnContent) btnContent.style.display = 'none';
					if (btnLoading) btnLoading.style.display = 'none';
					if (btnRemove) btnRemove.style.display = 'flex';
					if (btnRemoveLoading) btnRemoveLoading.style.display = 'none';
					btn.classList.remove('loading');
					btn.classList.add('in-cart');
					btn.setAttribute('data-in-cart', 'true');
					btn.setAttribute('data-cart-quantity', quantity);
					btn.disabled = false;
				});

				if (result.cart && Array.isArray(result.cart.lines)) {
					if (typeof setCounter === 'function') {
						setCounter('cart-summary', result.cart.lines.length);
					}
				} else if (typeof dispatchCartUpdatedEvent === 'function') {
					dispatchCartUpdatedEvent();
				}

				if (typeof showToast === 'function') showToast(result.message || `Ürün sepete eklendi!`, 'success');
			} else {
				allButtons.forEach(btn => {
					const btnContent = btn.querySelector('.btn-content');
					const btnRemove = btn.querySelector('.btn-remove');
					resetButtonState(btn, btnContent, btnRemove, false);
				});
				if (typeof showToast === 'function') showToast(result.message || 'Hata oluştu', 'danger');
			}
		} catch (error) {
			console.error(error);
			allButtons.forEach(btn => {
				const btnContent = btn.querySelector('.btn-content');
				const btnRemove = btn.querySelector('.btn-remove');
				resetButtonState(btn, btnContent, btnRemove, false);
			});
			if (typeof showToast === 'function') showToast(error.message || 'Hata oluştu', 'danger');
		}
	});

	function resetButtonState(btn, content, remove, wasInCart) {
		const loading = btn.querySelector('.btn-loading');
		const removeLoading = btn.querySelector('.btn-remove-loading');

		loading.style.display = 'none';
		removeLoading.style.display = 'none';
		btn.classList.remove('loading', 'removing');
		btn.disabled = false;

		if (wasInCart) {
			content.style.display = 'none';
			remove.style.display = 'flex';
			btn.classList.add('in-cart');
		} else {
			content.style.display = 'flex';
			remove.style.display = 'none';
			btn.classList.remove('in-cart');
		}
	}

		if (document.readyState === 'loading') {
		document.addEventListener('DOMContentLoaded', initProductDetailPage);
	} else {
		initProductDetailPage();
	}
})();
