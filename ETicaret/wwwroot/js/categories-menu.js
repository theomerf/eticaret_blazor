document.addEventListener('DOMContentLoaded', function () {
	initCategoriesMenu();
});

function initCategoriesMenu() {
	const categoryList = document.getElementById('categoriesMainList');
	const categoryItems = categoryList.querySelectorAll('li');
	const subcategoriesContainer = document.getElementById('subcategoriesContainer');
	window.subcategoryPanels = subcategoriesContainer.querySelectorAll('[data-category]');

	const urlParams = new URLSearchParams(window.location.search);
	const categoryIdFromUrl = urlParams.get('categoryId');

	let activeMainCategoryId = null;

	if (categoryIdFromUrl) {
		const subcategoryElement = subcategoriesContainer.querySelector(`[data-subcategory="${categoryIdFromUrl}"]`);

		if (subcategoryElement) {
			activeMainCategoryId = subcategoryElement.getAttribute('data-parent-category');
		} else {
			const mainCategoryElement = categoryList.querySelector(`[data-category="${categoryIdFromUrl}"]`);
			if (mainCategoryElement) {
				activeMainCategoryId = categoryIdFromUrl;
			}
		}
	}

	if (!activeMainCategoryId && categoryItems.length > 0) {
		const firstItem = categoryItems[0];
		activeMainCategoryId = firstItem.getAttribute('data-category');
	}

	if (activeMainCategoryId) {
		highlightMainCategory(activeMainCategoryId);
		showSubcategories(activeMainCategoryId);
	}

	categoryItems.forEach(item => {
		item.addEventListener('mouseenter', function () {
			const categoryId = this.getAttribute('data-category');
			highlightMainCategory(categoryId);
			showSubcategories(categoryId);
		});
	});

	const megaDropdown = document.getElementById('categoriesMegaDropdown');
	if (megaDropdown) {
		megaDropdown.addEventListener('mouseleave', function () {
			if (activeMainCategoryId) {
				highlightMainCategory(activeMainCategoryId);
				showSubcategories(activeMainCategoryId);
			}
		});
	}
}

function highlightMainCategory(categoryId) {
	const categoryList = document.getElementById('categoriesMainList');
	const categoryItems = categoryList.querySelectorAll('li');

	categoryItems.forEach(cat => cat.classList.remove('bg-blue-50'));

	const targetCategory = categoryList.querySelector(`[data-category="${categoryId}"]`);
	if (targetCategory) {
		targetCategory.classList.add('bg-blue-50');
	}
}

function showSubcategories(categoryId) {
	subcategoryPanels.forEach(panel => {
		panel.classList.remove('block');
		panel.classList.add('hidden');
	});

	const targetPanel = document.querySelector(`#subcategoriesContainer [data-category="${categoryId}"]`);
	if (targetPanel) {
		targetPanel.classList.remove('hidden');
		targetPanel.classList.add('block');
	}
}