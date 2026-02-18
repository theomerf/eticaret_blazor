document.addEventListener('DOMContentLoaded', function () {
	initNotificationsPage();
});

function initNotificationsPage() {
	document.addEventListener('click', async function (e) {
		if (e.target.closest('#mark-all-read')) {
			e.preventDefault();
			const button = e.target.closest('#mark-all-read');
			if (button.disabled) return;

			button.disabled = true;
			const originalContent = button.innerHTML;

			try {
				const response = await fetch('/api/account/notifications/mark-all-read', {
					method: 'PUT',
					headers: {
						'Content-Type': 'application/json'
					}
				});

				if (!response.ok) {
					throw new Error('Ağ hatası');
				}

				const result = await response.json();
				if (result.success) {
					if (typeof showToast === 'function') {
						showToast(result.message || 'Tüm bildirimler okundu olarak işaretlendi.', 'success');
					}

					const unreadCards = document.querySelectorAll('.notif-card.unread');
					unreadCards.forEach(function (card) {
						card.classList.remove('unread');
						card.classList.add('read');

						const statusBadge = card.querySelector('.notif-badge');
						if (statusBadge) {
							statusBadge.classList.remove('badge-unread');
							statusBadge.classList.add('badge-read');
							statusBadge.textContent = 'Okundu';
						}

						const markReadBtn = card.querySelector('#mark-notification-read');
						if (markReadBtn) {
							markReadBtn.remove();
						}
					});

					const countElements = document.querySelectorAll('.notif-count');
					countElements.forEach(function (el) {
						el.textContent = '0';
					});

					button.remove();
				} else {
					if (typeof showToast === 'function') {
						showToast(result.message || 'İşlem başarısız.', 'warning');
					}
				}
			} catch (error) {
				console.error('Hata:', error);
				if (typeof showToast === 'function') {
					showToast('Bir hata oluştu. Lütfen tekrar deneyin.', 'danger');
				}
			} finally {
				button.disabled = false;
			}
		}
	});

	document.addEventListener('click', async function (e) {
		if (e.target.closest('#remove-all-notifications')) {
			e.preventDefault();
			const button = e.target.closest('#remove-all-notifications');
			if (button.disabled) return;

			button.disabled = true;

			try {
				const response = await fetch('/api/account/notifications/remove-all', {
					method: 'DELETE',
					headers: {
						'Content-Type': 'application/json'
					}
				});

				if (!response.ok) {
					throw new Error('Ağ hatası');
				}

				const result = await response.json();
				if (result.success) {
					if (typeof showToast === 'function') {
						showToast(result.message || 'Tüm bildirimler silindi.', 'success');
					}

					const countElements = document.querySelectorAll('.notif-count');
					countElements.forEach(function (el) {
						el.textContent = '0';
					});

					const cards = document.querySelectorAll('.notif-card');
					const notificationsList = document.querySelector('.notif-list');

					cards.forEach(function (card, index) {
						card.classList.add('opacity-0', 'transition-opacity', 'duration-500');

						setTimeout(function () {
							card.remove();

							if (notificationsList && document.querySelectorAll('.notif-card').length === 0) {
								notificationsList.innerHTML = `
									<div class="notif-empty">
										<div class="w-20 h-20 bg-slate-100 rounded-full flex items-center justify-center text-slate-400 text-3xl mx-auto mb-5">
											<i class="fas fa-bell-slash"></i>
										</div>
										<h4 class="text-[#1e293b] font-bold text-2xl mb-2">Bildirim Yok</h4>
										<p class="text-slate-500">Henüz herhangi bir bildiriminiz bulunmuyor.</p>
									</div>`;
							}
						}, 400 + (index * 100));
					});
				} else {
					if (typeof showToast === 'function') {
						showToast(result.message || 'İşlem başarısız.', 'warning');
					}
				}
			} catch (error) {
				console.error('Hata:', error);
				if (typeof showToast === 'function') {
					showToast('Bir hata oluştu. Lütfen tekrar deneyin.', 'danger');
				}
			} finally {
				button.disabled = false;
			}
		}
	});

	document.addEventListener('click', async function (e) {
		if (e.target.closest('#mark-notification-read')) {
			e.preventDefault();
			const button = e.target.closest('#mark-notification-read');
			const notificationId = button.getAttribute('data-notification-id');
			if (!notificationId || button.disabled) return;

			button.disabled = true;

			try {
				const response = await fetch(`/api/account/notifications/mark-read/${notificationId}`, {
					method: 'PUT',
					headers: {
						'Content-Type': 'application/json'
					}
				});

				if (!response.ok) {
					throw new Error('Ağ hatası');
				}

				const result = await response.json();
				if (result.success) {
					if (typeof showToast === 'function') {
						showToast(result.message || 'Bildirim okundu olarak işaretlendi.', 'success');
					}

					const card = button.closest('.notif-card');
					if (card) {
						card.classList.remove('unread');
						card.classList.add('read');

						const statusBadge = card.querySelector('.notif-badge');
						if (statusBadge) {
							statusBadge.classList.remove('badge-unread');
							statusBadge.classList.add('badge-read');
							statusBadge.textContent = 'Okundu';
						}

						button.remove();

						const countElements = document.querySelectorAll('.notif-count');
						countElements.forEach(function (el) {
							const currentCount = parseInt(el.textContent) || 0;
							if (currentCount > 1) {
								el.textContent = (currentCount - 1).toString();
							} else {
								el.textContent = '0';
								const allReadBtn = document.getElementById('mark-all-read');
								if (allReadBtn) allReadBtn.remove();
							}
						});
					}
				} else {
					if (typeof showToast === 'function') {
						showToast(result.message || 'İşlem başarısız.', 'warning');
					}
				}
			} catch (error) {
				console.error('Hata:', error);
				if (typeof showToast === 'function') {
					showToast('Bir hata oluştu. Lütfen tekrar deneyin.', 'danger');
				}
			} finally {
				button.disabled = false;
			}
		}
	});

	document.addEventListener('click', async function (e) {
		if (e.target.closest('#remove-notification')) {
			e.preventDefault();
			const button = e.target.closest('#remove-notification');
			const notificationId = button.getAttribute('data-notification-id');
			if (!notificationId || button.classList.contains('processing')) return;

			button.classList.add('processing');
			const notificationCard = button.closest('.notif-card');
			const isUnread = button.classList.contains('unread') || notificationCard?.classList.contains('unread');

			try {
				const response = await fetch(`/api/account/notifications/remove/${notificationId}`, {
					method: 'DELETE',
					headers: {
						'Content-Type': 'application/json'
					}
				});

				if (!response.ok) {
					throw new Error('Ağ hatası');
				}

				const result = await response.json();
				if (result.success) {
					if (typeof showToast === 'function') {
						showToast(result.message || 'Bildirim silindi.', 'success');
					}

					if (notificationCard) {
						notificationCard.classList.add('opacity-0', 'transition-opacity', 'duration-500');

						setTimeout(function () {
							notificationCard.remove();

							const notificationsList = document.querySelector('.notif-list');
							if (notificationsList && document.querySelectorAll('.notif-card').length === 0) {
								notificationsList.innerHTML = `
									<div class="notif-empty">
										<div class="w-20 h-20 bg-slate-100 rounded-full flex items-center justify-center text-slate-400 text-3xl mx-auto mb-5">
											<i class="fas fa-bell-slash"></i>
										</div>
										<h4 class="text-[#1e293b] font-bold text-2xl mb-2">Bildirim Yok</h4>
										<p class="text-slate-500">Henüz herhangi bir bildiriminiz bulunmuyor.</p>
									</div>`;
							}
						}, 500);
					}

					if (isUnread) {
						const countElements = document.querySelectorAll('.notif-count');
						countElements.forEach(function (el) {
							const currentCount = parseInt(el.textContent) || 0;
							if (currentCount > 1) {
								el.textContent = (currentCount - 1).toString();
							} else {
								el.textContent = '0';
								const allReadBtn = document.getElementById('mark-all-read');
								if (allReadBtn) allReadBtn.remove();
							}
						});
					}
				} else {
					if (typeof showToast === 'function') {
						showToast(result.message || 'İşlem başarısız.', 'warning');
					}
				}
			} catch (error) {
				console.error('Hata:', error);
				if (typeof showToast === 'function') {
					showToast('Bir hata oluştu.', 'danger');
				}
			} finally {
				button.classList.remove('processing');
			}
		}
	});
}
