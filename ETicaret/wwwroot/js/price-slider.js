let desktopPriceSlider = null;
let mobilePriceSlider = null;
let productFilterReference = null;
let currentFilters = {
    minPrice: 0,
    maxPrice: 100000,
};

let isUpdatingDesktopFromInput = false;
let isUpdatingMobileFromInput = false;

const urlParams = new URLSearchParams(window.location.search);
currentFilters.minPrice = parseInt(urlParams.get('minPrice')) || 0;
currentFilters.maxPrice = parseInt(urlParams.get('maxPrice')) || 100000;

window.setProductFilterReference = function (dotNetHelper) {
    productFilterReference = dotNetHelper;
};

async function initializePriceSlider() {
    const priceRangeElement = document.getElementById('priceRange');
    const minPriceInput = document.getElementById('minPrice');
    const maxPriceInput = document.getElementById('maxPrice');
    const minPriceDisplay = document.getElementById('minPriceDisplay');
    const maxPriceDisplay = document.getElementById('maxPriceDisplay');

    const sliderMin = currentFilters.minPrice != null && currentFilters.minPrice !== ''
        ? Number(currentFilters.minPrice)
        : 0;

    const sliderMax = currentFilters.maxPrice != null && currentFilters.maxPrice !== ''
        ? Number(currentFilters.maxPrice)
        : 100000;

    if (priceRangeElement && typeof noUiSlider !== 'undefined') {
        if (desktopPriceSlider) {
            desktopPriceSlider.destroy();
        }

        desktopPriceSlider = noUiSlider.create(priceRangeElement, {
            start: [sliderMin, sliderMax],
            connect: true,
            step: 50,
            range: {
                min: 0,
                max: 100000
            },
            format: {
                to: v => Math.round(v),
                from: v => Number(v)
            },
            behaviour: 'tap-drag',
        });

        desktopPriceSlider.on('update', function (values) {
            if (isUpdatingDesktopFromInput) return;

            const minVal = Math.round(values[0]);
            const maxVal = Math.round(values[1]);

            if (minPriceDisplay) minPriceDisplay.textContent = minVal;
            if (maxPriceDisplay) maxPriceDisplay.textContent = maxVal;
            if (minPriceInput) {
                minPriceInput.value = minVal === 0 ? '' : minVal;
                minPriceInput.dispatchEvent(new Event('input', { bubbles: true }));
            }
            if (maxPriceInput) {
                maxPriceInput.value = maxVal === 100000 ? '' : maxVal;
                maxPriceInput.dispatchEvent(new Event('input', { bubbles: true }));
            }
        });

        desktopPriceSlider.on('change', async function (values) {
            const minVal = Math.round(values[0]);
            const maxVal = Math.round(values[1]);

            if (productFilterReference) {
                await productFilterReference.invokeMethodAsync(
                    'UpdatePriceFromSlider',
                    minVal === 0 ? null : minVal,
                    maxVal === 100000 ? null : maxVal
                );
            }
        });

        let desktopInputDebounce = null;

        function updateDesktopSliderFromInputs() {
            if (!desktopPriceSlider) return;

            const minVal = minPriceInput && minPriceInput.value !== ''
                ? Math.max(0, Math.min(100000, parseInt(minPriceInput.value) || 0))
                : 0;
            const maxVal = maxPriceInput && maxPriceInput.value !== ''
                ? Math.max(0, Math.min(100000, parseInt(maxPriceInput.value) || 100000))
                : 100000;

            const safeMin = Math.min(minVal, maxVal);
            const safeMax = Math.max(minVal, maxVal);

            isUpdatingDesktopFromInput = true;
            desktopPriceSlider.set([safeMin, safeMax]);

            if (minPriceDisplay) minPriceDisplay.textContent = safeMin;
            if (maxPriceDisplay) maxPriceDisplay.textContent = safeMax;

            isUpdatingDesktopFromInput = false;
        }

        if (minPriceInput) {
            minPriceInput.addEventListener('input', function () {
                clearTimeout(desktopInputDebounce);
                desktopInputDebounce = setTimeout(updateDesktopSliderFromInputs, 300);
            });
        }

        if (maxPriceInput) {
            maxPriceInput.addEventListener('input', function () {
                clearTimeout(desktopInputDebounce);
                desktopInputDebounce = setTimeout(updateDesktopSliderFromInputs, 300);
            });
        }
    }

    if (minPriceDisplay) minPriceDisplay.textContent = sliderMin;
    if (maxPriceDisplay) maxPriceDisplay.textContent = sliderMax;
}


async function initializeMobilePriceSlider() {
    const priceRangeElement = document.getElementById('mobilePriceRange');
    const minPriceInput = document.getElementById('mobileMinPrice');
    const maxPriceInput = document.getElementById('mobileMaxPrice');
    const minPriceDisplay = document.getElementById('mobileMinPriceDisplay');
    const maxPriceDisplay = document.getElementById('mobileMaxPriceDisplay');

    const sliderMin = currentFilters.minPrice != null && currentFilters.minPrice !== ''
        ? Number(currentFilters.minPrice)
        : 0;

    const sliderMax = currentFilters.maxPrice != null && currentFilters.maxPrice !== ''
        ? Number(currentFilters.maxPrice)
        : 100000;

    if (priceRangeElement && typeof noUiSlider !== 'undefined') {
        if (mobilePriceSlider) {
            mobilePriceSlider.destroy();
        }

        mobilePriceSlider = noUiSlider.create(priceRangeElement, {
            start: [sliderMin, sliderMax],
            connect: true,
            step: 50,
            range: {
                min: 0,
                max: 100000
            },
            format: {
                to: v => Math.round(v),
                from: v => Number(v)
            },
            behaviour: 'tap-drag',
        });

        mobilePriceSlider.on('update', function (values) {
            if (isUpdatingMobileFromInput) return;

            const minVal = Math.round(values[0]);
            const maxVal = Math.round(values[1]);

            if (minPriceDisplay) minPriceDisplay.textContent = minVal;
            if (maxPriceDisplay) maxPriceDisplay.textContent = maxVal;
            if (minPriceInput) {
                minPriceInput.value = minVal === 0 ? '' : minVal;
                minPriceInput.dispatchEvent(new Event('input', { bubbles: true }));
            }
            if (maxPriceInput) {
                maxPriceInput.value = maxVal === 100000 ? '' : maxVal;
                maxPriceInput.dispatchEvent(new Event('input', { bubbles: true }));
            }
        });

        mobilePriceSlider.on('change', async function (values) {
            const minVal = Math.round(values[0]);
            const maxVal = Math.round(values[1]);

            if (productFilterReference) {
                await productFilterReference.invokeMethodAsync(
                    'UpdatePriceFromSlider',
                    minVal === 0 ? null : minVal,
                    maxVal === 100000 ? null : maxVal
                );
            }
        });

        let mobileInputDebounce = null;

        function updateMobileSliderFromInputs() {
            if (!mobilePriceSlider) return;

            const minVal = minPriceInput && minPriceInput.value !== ''
                ? Math.max(0, Math.min(100000, parseInt(minPriceInput.value) || 0))
                : 0;
            const maxVal = maxPriceInput && maxPriceInput.value !== ''
                ? Math.max(0, Math.min(100000, parseInt(maxPriceInput.value) || 100000))
                : 100000;

            const safeMin = Math.min(minVal, maxVal);
            const safeMax = Math.max(minVal, maxVal);

            isUpdatingMobileFromInput = true;
            mobilePriceSlider.set([safeMin, safeMax]);

            if (minPriceDisplay) minPriceDisplay.textContent = safeMin;
            if (maxPriceDisplay) maxPriceDisplay.textContent = safeMax;

            isUpdatingMobileFromInput = false;
        }

        if (minPriceInput) {
            minPriceInput.addEventListener('input', function () {
                clearTimeout(mobileInputDebounce);
                mobileInputDebounce = setTimeout(updateMobileSliderFromInputs, 300);
            });
        }

        if (maxPriceInput) {
            maxPriceInput.addEventListener('input', function () {
                clearTimeout(mobileInputDebounce);
                mobileInputDebounce = setTimeout(updateMobileSliderFromInputs, 300);
            });
        }
    }

    if (minPriceDisplay) minPriceDisplay.textContent = sliderMin;
    if (maxPriceDisplay) maxPriceDisplay.textContent = sliderMax;
}


document.addEventListener('DOMContentLoaded', function () {
    initializePriceSlider();
    initializeMobilePriceSlider();
});


window.priceSliderAPI = {
    setSliders: function (minVal, maxVal) {
        const min = minVal !== null && minVal !== '' ? parseInt(minVal) : 0;
        const max = maxVal !== null && maxVal !== '' ? parseInt(maxVal) : 100000;

        if (desktopPriceSlider) {
            desktopPriceSlider.set([min, max]);
        }
        if (mobilePriceSlider) {
            mobilePriceSlider.set([min, max]);
        }
    },

    resetSliders: function () {
        if (desktopPriceSlider) {
            desktopPriceSlider.set([0, 100000]);
        }
        if (mobilePriceSlider) {
            mobilePriceSlider.set([0, 100000]);
        }
    },

    syncFromInputs: function () {
        const minPriceInput = document.getElementById('minPrice');
        const maxPriceInput = document.getElementById('maxPrice');
        const mobileMinPriceInput = document.getElementById('mobileMinPrice');
        const mobileMaxPriceInput = document.getElementById('mobileMaxPrice');

        const minVal = minPriceInput && minPriceInput.value !== ''
            ? parseInt(minPriceInput.value) : 0;
        const maxVal = maxPriceInput && maxPriceInput.value !== ''
            ? parseInt(maxPriceInput.value) : 100000;

        if (desktopPriceSlider) {
            desktopPriceSlider.set([minVal, maxVal]);
        }

        const mobileMinVal = mobileMinPriceInput && mobileMinPriceInput.value !== ''
            ? parseInt(mobileMinPriceInput.value) : 0;
        const mobileMaxVal = mobileMaxPriceInput && mobileMaxPriceInput.value !== ''
            ? parseInt(mobileMaxPriceInput.value) : 100000;

        if (mobilePriceSlider) {
            mobilePriceSlider.set([mobileMinVal, mobileMaxVal]);
        }
    },

    getDesktopSlider: function () {
        return desktopPriceSlider;
    },

    getMobileSlider: function () {
        return mobilePriceSlider;
    }
};