// Select logic on page load
document.addEventListener('DOMContentLoaded', function () {
    // Address
    const firstAddress = document.querySelector('.address-radio:checked');
    if (firstAddress) {
        selectAddress(parseInt(firstAddress.value));
    }

    // Payment
    const checkedPayment = document.querySelector('input[name="PaymentMethod"]:checked');
    if (checkedPayment) selectPayment(checkedPayment);

    // Shipping
    const checkedShipping = document.querySelector('input[name="ShippingMethod"]:checked');
    if (checkedShipping) selectShipping(checkedShipping);

    updateTotal();
});

// Select address function
function selectAddress(addressId) {
    document.getElementById('selectedAddressId').value = addressId;

    // Update visual state
    document.querySelectorAll('.address-card').forEach(card => {
        const cardAddressId = parseInt(card.dataset.addressId);
        const innerDiv = card.querySelector('div');
        const check = card.querySelector('.address-check');

        if (cardAddressId === addressId) {
            innerDiv.classList.add('border-primary', 'bg-blue-50/30');
            innerDiv.classList.remove('border-gray-100');
            if (check) {
                check.classList.add('border-primary', 'bg-primary');
                check.classList.remove('border-gray-300');
            }
        } else {
            innerDiv.classList.remove('border-primary', 'bg-blue-50/30');
            innerDiv.classList.add('border-gray-100');
            if (check) {
                check.classList.remove('border-primary', 'bg-primary');
                check.classList.add('border-gray-300');
            }
        }
    });
}

// Select Payment Method Visual Update
function selectPayment(inputElement) {
    // Reset all
    document.querySelectorAll('input[name="PaymentMethod"]').forEach(input => {
        const container = input.nextElementSibling;
        const icon = container.querySelector('i');
        const text = container.querySelector('span');

        container.classList.remove('border-primary', 'bg-blue-50/10');
        container.classList.add('border-gray-100', 'bg-white');

        icon.classList.remove('text-primary');
        icon.classList.add('text-gray-600');

        text.classList.remove('text-primary');
        text.classList.add('text-gray-700');
    });

    // Set active
    const container = inputElement.nextElementSibling;
    const icon = container.querySelector('i');
    const text = container.querySelector('span');

    container.classList.remove('border-gray-100', 'bg-white');
    container.classList.add('border-primary', 'bg-blue-50/10');

    icon.classList.remove('text-gray-600');
    icon.classList.add('text-primary');

    text.classList.remove('text-gray-700');
    text.classList.add('text-primary');
}

// Select Shipping Method Visual Update
function selectShipping(inputElement) {
    // Reset all
    document.querySelectorAll('input[name="ShippingMethod"]').forEach(input => {
        const container = input.nextElementSibling;
        container.classList.remove('border-primary', 'bg-blue-50/10');
        container.classList.add('border-gray-100', 'bg-white');
    });

    // Set active
    const container = inputElement.nextElementSibling;
    container.classList.remove('border-gray-100', 'bg-white');
    container.classList.add('border-primary', 'bg-blue-50/10');

    // Recalculate totals
    updateShippingCost(inputElement.value);
}

function updateShippingCost(value) {
    const shippingCostEl = document.getElementById('shippingCost');
    const subtotalText = document.getElementById('subtotal').textContent.replace('₺', '').replace('.', '').replace(',', '.');
    const subtotal = parseFloat(subtotalText) || 0;

    // 0 = Standard, 1 = Express, 2 = HandlingOnly
    if (value === '0') {
        if (subtotal >= 500) {
            shippingCostEl.textContent = 'Ücretsiz';
            shippingCostEl.className = 'font-medium text-green-600';
        } else {
            shippingCostEl.textContent = '29,99₺';
            shippingCostEl.className = 'font-medium';
        }
    } else if (value === '1') {
        shippingCostEl.textContent = '49,99₺';
        shippingCostEl.className = 'font-medium';
    } else if (value === '2') {
        shippingCostEl.textContent = 'Ücretsiz';
        shippingCostEl.className = 'font-medium text-green-600';
    }

    updateTotal();
}

function applyCoupon() {
    const code = document.getElementById('couponCode').value;
    const messageEl = document.getElementById('couponMessage');

    if (!code) {
        messageEl.className = 'mt-2 text-sm text-red-600';
        messageEl.textContent = 'Lütfen bir kupon kodu girin.';
        return;
    }

    // AJAX call to validate coupon
    fetch('/api/orders/validate-coupon', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
        },
        body: JSON.stringify({
            couponCode: code,
            orderAmount: parseFloat(document.getElementById('subtotal').textContent.replace('₺', '').replace('.', '').replace(',', '.'))
        })
    })
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                messageEl.className = 'mt-2 text-sm text-green-600';
                messageEl.textContent = data.message;
                document.getElementById('discountRow').style.display = 'flex';
                document.getElementById('discountAmount').textContent = `-${data.discountAmount.toFixed(2).replace('.', ',')}₺`;
                updateTotal();
            } else {
                messageEl.className = 'mt-2 text-sm text-red-600';
                messageEl.textContent = data.message;
            }
        })
        .catch(error => {
            messageEl.className = 'mt-2 text-sm text-red-600';
            messageEl.textContent = 'Kupon doğrulanırken bir hata oluştu.';
        });
}

function updateTotal() {
    const subtotalText = document.getElementById('subtotal').textContent.replace('₺', '').replace('.', '').replace(',', '.');
    const subtotal = parseFloat(subtotalText) || 0;

    const shippingText = document.getElementById('shippingCost').textContent.replace('₺', '').replace('.', '').replace(',', '.').replace('Ücretsiz', '0');
    const shipping = parseFloat(shippingText) || 0;

    const discountText = document.getElementById('discountAmount').textContent.replace('₺', '').replace('.', '').replace(',', '.').replace('-', '');
    const discount = parseFloat(discountText) || 0;

    const netAmount = subtotal - discount + shipping;
    const tax = netAmount * 0.18;
    const total = netAmount + tax;

    document.getElementById('taxAmount').textContent = tax.toFixed(2).replace('.', ',') + '₺';
    document.getElementById('totalAmount').innerHTML = total.toFixed(2).replace('.', ',') + '<span class="text-lg align-top">₺</span>';
}

// Attach event listeners to inputs
document.querySelectorAll('input[name="PaymentMethod"]').forEach(radio => {
    radio.addEventListener('change', function () { selectPayment(this); });
});

document.querySelectorAll('input[name="ShippingMethod"]').forEach(radio => {
    radio.addEventListener('change', function () { selectShipping(this); });
});

// Form validation before submit
document.getElementById('checkoutForm').addEventListener('submit', function (e) {
    const addressId = parseInt(document.getElementById('selectedAddressId').value);
    if (!addressId || addressId <= 0) {
        e.preventDefault();
        showToast('Lütfen bir teslimat adresi seçin.', 'error');
        return false;
    }

    // Disable submit button to prevent double submission
    document.getElementById('submitButton').disabled = true;
    document.getElementById('submitButton').innerHTML = '<i class="fas fa-spinner fa-spin"></i> <span>İşleniyor...</span>';
});