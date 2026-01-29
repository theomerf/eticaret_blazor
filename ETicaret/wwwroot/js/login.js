// ══════════════════════════════════════════════════════════════════
// LOGIN/REGISTER SAYFASI
// ══════════════════════════════════════════════════════════════════

(function () {
    'use strict';

    // DOM Elementleri
    const elements = {
        loginForm: document.getElementById('loginForm'),
        registerForm: document.getElementById('registerForm'),
        loginTabBtn: document.getElementById('loginTabBtn'),
        registerTabBtn: document.getElementById('registerTabBtn'),
        loginPassword: document.getElementById('login-password'),
        registerPassword: document.getElementById('register-password'),
        toggleLoginPassword: document.getElementById('toggle-login-password'),
        toggleRegisterPassword: document.getElementById('toggle-register-password'),
        toggleRegisterPasswordConfirm: document.getElementById('toggle-register-password-confirm')
    };

    // ══════════════════════════════════════════════════════════════
    // ŞİFRE GÖSTERME/GİZLEME
    // ══════════════════════════════════════════════════════════════

    function setupPasswordToggle() {
        if (elements.toggleLoginPassword) {
            elements.toggleLoginPassword.addEventListener('click', () => {
                togglePasswordVisibility('login-password', 'login-eye-icon');
            });
        }

        if (elements.toggleRegisterPassword) {
            elements.toggleRegisterPassword.addEventListener('click', () => {
                togglePasswordVisibility('register-password', 'register-eye-icon');
            });
        }

        if (elements.toggleRegisterPasswordConfirm) {
            elements.toggleRegisterPasswordConfirm.addEventListener('click', () => {
                togglePasswordVisibility('register-password-confirm', 'register-confirm-eye-icon');
            });
        }
    }

    function togglePasswordVisibility(inputId, iconId) {
        const input = document.getElementById(inputId);
        const icon = document.getElementById(iconId);

        if (!input || !icon) return;

        if (input.type === 'password') {
            input.type = 'text';
            icon.classList.remove('fa-eye');
            icon.classList.add('fa-eye-slash');
        } else {
            input.type = 'password';
            icon.classList.remove('fa-eye-slash');
            icon.classList.add('fa-eye');
        }
    }

    // ══════════════════════════════════════════════════════════════
    // FORM GÖNDERİMİ - YÜKLEME DURUMU
    // ══════════════════════════════════════════════════════════════

    function setupFormSubmit() {
        if (elements.loginForm) {
            elements.loginForm.addEventListener('submit', function () {
                if (this.checkValidity()) {
                    setTimeout(() => {
                        showLoadingState(this, 'Giriş yapılıyor...');
                    }, 10);
                }
            });
        }

        if (elements.registerForm) {
            elements.registerForm.addEventListener('submit', function () {
                if (this.checkValidity()) {
                    setTimeout(() => {
                        showLoadingState(this, 'Kayıt yapılıyor...');
                    }, 10);
                }
            });
        }
    }

    function showLoadingState(form, text) {
        const submitBtn = form.querySelector('button[type="submit"]');
        if (!submitBtn || submitBtn.disabled) return;

        submitBtn.disabled = true;
        submitBtn.innerHTML = `
            <svg class="animate-spin -ml-1 mr-3 h-5 w-5 text-white inline-block" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
              <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
              <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
            </svg>
            ${text}
        `;
    }

    // ══════════════════════════════════════════════════════════════
    // TAB GEÇİŞLERİNDE HATALARI TEMİZLE
    // ══════════════════════════════════════════════════════════════

    function setupTabSwitching() {
        const tabBtns = [elements.loginTabBtn, elements.registerTabBtn];

        tabBtns.forEach(btn => {
            if (!btn) return;

            btn.addEventListener('click', () => {
                const targetId = btn.getAttribute('data-target-pane');
                const targetPane = document.getElementById(targetId);

                if (!targetPane) return;

                // Tab butonlarını güncelle
                tabBtns.forEach(b => b?.classList.remove('active'));
                btn.classList.add('active');

                // Panelleri güncelle
                document.querySelectorAll('#authTabContent > div').forEach(pane => {
                    pane.classList.add('hidden');
                    pane.classList.remove('block');
                });

                targetPane.classList.remove('hidden');
                targetPane.classList.add('block');

                clearValidationErrors();
            });
        });
    }

    function clearValidationErrors() {
        document.querySelectorAll('.validation-summary-errors').forEach(el => {
            const ul = el.querySelector('ul');
            if (ul) {
                ul.innerHTML = '';
            }
            el.classList.add('hidden');
            el.classList.remove('block');
        });

        document.querySelectorAll('.was-validated').forEach(form => {
            form.classList.remove('was-validated');
        });

        document.querySelectorAll('.alert').forEach(el => {
            if (!el.classList.contains('validation-summary-errors')) {
                el.remove();
            }
        });
    }

    // ══════════════════════════════════════════════════════════════
    // BOŞ VALIDATION SUMMARY'LERİ GİZLE
    // ══════════════════════════════════════════════════════════════

    function hideEmptyValidationSummaries() {
        document.querySelectorAll('.validation-summary-errors, .validation-summary-valid').forEach(el => {
            const ul = el.querySelector('ul');
            // li elements that actually have text and are not the dummy empty li ASP.NET adds
            const errorItems = ul ? Array.from(ul.querySelectorAll('li')).filter(li => li.innerText.trim() !== "" && li.style.display !== 'none') : [];
            const hasErrors = errorItems.length > 0;

            if (!hasErrors) {
                el.classList.add('hidden');
                el.classList.remove('block');
            } else {
                el.classList.add('block');
                el.classList.remove('hidden');

                // Shake if it's the active pane (especially login)
                const authCard = document.getElementById('authCard');
                if (authCard && !el.closest('.hidden')) {
                    authCard.classList.add('animate-shake');
                    setTimeout(() => {
                        authCard.classList.remove('animate-shake');
                    }, 500);
                }
            }
        });
    }

    // ══════════════════════════════════════════════════════════════
    // CLIENT-SIDE VALIDATION (Basit)
    // ══════════════════════════════════════════════════════════════

    function setupBasicValidation() {
        const phoneInput = document.querySelector('input[name="Register.PhoneNumber"]');
        if (phoneInput) {
            phoneInput.addEventListener('input', function () {
                this.value = this.value.replace(/[^\d\s\-\(\)\+]/g, '');
            });
        }

        const usernameInput = document.querySelector('input[name="Register.UserName"]');
        if (usernameInput) {
            usernameInput.addEventListener('input', function () {
                this.value = this.value.replace(/[çğıöşüÇĞİÖŞÜ]/g, '');
                this.value = this.value.replace(/\s/g, '');
            });
        }

        const birthDateInput = document.querySelector('input[name="Register.BirthDate"]');
        if (birthDateInput) {
            const today = new Date();
            const maxDate = new Date(today.getFullYear() - 18, today.getMonth(), today.getDate());
            const minDate = new Date(today.getFullYear() - 120, today.getMonth(), today.getDate());

            birthDateInput.max = maxDate.toISOString().split('T')[0];
            birthDateInput.min = minDate.toISOString().split('T')[0];
        }
    }

    // ══════════════════════════════════════════════════════════════
    // ŞİFRE GÜÇLÜK GÖSTERGESİ
    // ══════════════════════════════════════════════════════════════

    function setupPasswordStrength() {
        if (!elements.registerPassword) return;

        const existingIndicator = elements.registerPassword
            .parentElement
            .parentElement
            .querySelector('.auth-strength-container');

        if (existingIndicator) return;

        const strengthIndicator = document.createElement('div');
        strengthIndicator.className = 'auth-strength-container mt-3 opacity-0 transition-opacity duration-300';
        strengthIndicator.innerHTML = `
            <div class="auth-progress">
                <div class="auth-progress-bar" id="strengthBar" role="progressbar" style="width: 0%"></div>
            </div>
            <small class="auth-strength-text" id="strengthText"></small>
        `;

        elements.registerPassword.parentElement.parentElement.appendChild(strengthIndicator);

        elements.registerPassword.addEventListener('input', function () {
            const strength = calculatePasswordStrength(this.value);
            updateStrengthIndicator(strength);

            if (this.value.length > 0) {
                strengthIndicator.classList.add('opacity-100');
                strengthIndicator.classList.remove('opacity-0');
            } else {
                strengthIndicator.classList.add('opacity-0');
                strengthIndicator.classList.remove('opacity-100');
            }
        });
    }

    function calculatePasswordStrength(password) {
        if (!password) return { level: 0, text: '', color: '' };

        let score = 0;

        if (password.length >= 8) score += 25;
        if (password.length >= 12) score += 25;
        if (/[a-z]/.test(password)) score += 15;
        if (/[A-Z]/.test(password)) score += 15;
        if (/\d/.test(password)) score += 10;
        if (/[^a-zA-Z0-9]/.test(password)) score += 10;

        if (score < 30) return { level: 25, text: 'Çok zayıf', color: 'bg-red-500' };
        if (score < 50) return { level: 50, text: 'Zayıf', color: 'bg-amber-500' };
        if (score < 75) return { level: 75, text: 'Orta', color: 'bg-sky-500' };
        return { level: 100, text: 'Güçlü', color: 'bg-emerald-500' };
    }

    function updateStrengthIndicator(strength) {
        const bar = document.getElementById('strengthBar');
        const text = document.getElementById('strengthText');

        if (bar && text) {
            bar.style.width = strength.level + '%';
            bar.className = 'auth-progress-bar ' + strength.color;
            text.textContent = strength.text;
        }
    }

    // ══════════════════════════════════════════════════════════════
    // CAPS LOCK UYARISI
    // ══════════════════════════════════════════════════════════════

    function setupCapsLockWarning() {
        const passwordInputs = [elements.loginPassword, elements.registerPassword];

        passwordInputs.forEach(input => {
            if (!input) return;

            input.addEventListener('keyup', function (e) {
                if (!e.getModifierState) return;
                const capsLockOn = e.getModifierState('CapsLock');
                showCapsLockWarning(this, capsLockOn);
            });

            input.addEventListener('blur', function () {
                showCapsLockWarning(this, false);
            });
        });
    }

    function showCapsLockWarning(input, show) {
        const container = input.parentElement.parentElement;
        let warning = container.querySelector('.auth-caps-warning');

        if (show) {
            if (!warning) {
                warning = document.createElement('small');
                warning.className = 'auth-caps-warning';
                warning.innerHTML = '<i class="fas fa-exclamation-triangle"></i>Caps Lock açık';
                container.appendChild(warning);
            }
        } else {
            warning?.remove();
        }
    }


    // ══════════════════════════════════════════════════════════════
    // ÇİFT GÖNDERİMİ ÖNLE
    // ══════════════════════════════════════════════════════════════

    function preventDoubleSubmit() {
        const forms = [elements.loginForm, elements.registerForm];

        forms.forEach(form => {
            if (!form) return;

            form.addEventListener('submit', function (e) {
                const submitBtn = this.querySelector('button[type="submit"]');
                if (submitBtn && submitBtn.disabled) {
                    e.preventDefault();
                    return false;
                }
            });
        });
    }

    // ══════════════════════════════════════════════════════════════
    // KLAVYE KISAYOLLARI
    // ══════════════════════════════════════════════════════════════

    function setupKeyboardShortcuts() {
        document.addEventListener('keydown', function (e) {
            // Alt + L = Login
            if (e.altKey && e.key === 'l') {
                e.preventDefault();
                elements.loginTabBtn?.click();
                setTimeout(() => {
                    elements.loginForm?.querySelector('input')?.focus();
                }, 100);
            }

            // Alt + R = Register
            if (e.altKey && e.key === 'r') {
                e.preventDefault();
                elements.registerTabBtn?.click();
                setTimeout(() => {
                    elements.registerForm?.querySelector('input')?.focus();
                }, 100);
            }
        });
    }

    // ══════════════════════════════════════════════════════════════
    // BAŞLAT
    // ══════════════════════════════════════════════════════════════

    function init() {
        hideEmptyValidationSummaries();
        setupPasswordToggle();
        setupFormSubmit();
        setupTabSwitching();
        setupBasicValidation();
        setupPasswordStrength();
        setupCapsLockWarning();
        preventDoubleSubmit();
        setupKeyboardShortcuts();
    }

    // DOM Ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }

})();