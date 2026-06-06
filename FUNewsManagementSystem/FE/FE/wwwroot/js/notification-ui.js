// wwwroot/js/notification-ui.js
class NotificationUI {
    constructor(service) {
        this.service = service;
        this.elements = {};
        this.toasts = new Map();
    }

    // Initialize UI
    initialize() {
        this.cacheElements();
        this.attachEventListeners();
        this.subscribeToService();
    }

    // Cache DOM Elements
    cacheElements() {
        this.elements = {
            badge: document.getElementById('notificationBadge'),
            count: document.getElementById('notificationCount'),
            list: document.getElementById('notificationList'),
            markAllBtn: document.getElementById('markAllReadBtn'),
            toastContainer: document.getElementById('toastContainer'),
            notificationBtn: document.getElementById('notificationBtn')
        };
    }

    // Attach Event Listeners
    attachEventListeners() {
        // Mark all as read
        if (this.elements.markAllBtn) {
            this.elements.markAllBtn.addEventListener('click', async (e) => {
                e.stopPropagation();
                await this.service.markAllAsRead();
            });
        }

        // Close dropdown when clicking outside
        document.addEventListener('click', (e) => {
            if (!e.target.closest('.dropdown')) {
                // Dropdown will close automatically by Bootstrap
            }
        });
    }

    // Subscribe to Service Events
    subscribeToService() {
        this.service.subscribe((event, data) => {
            switch (event) {
                case 'new':
                    this.handleNewNotification(data);
                    break;
                case 'update':
                case 'loaded':
                    this.updateUI();
                    break;
                case 'reconnecting':
                    this.showConnectionStatus('Đang kết nối lại...', 'warning');
                    break;
                case 'reconnected':
                    this.showConnectionStatus('Đã kết nối', 'success');
                    setTimeout(() => this.hideConnectionStatus(), 2000);
                    break;
                case 'disconnected':
                    this.showConnectionStatus('Mất kết nối', 'danger');
                    break;
                case 'connected':
                    this.hideConnectionStatus();
                    break;
            }
        });
    }

    // Handle New Notification
    handleNewNotification(notification) {
        this.updateUI();
        this.showToast(notification);
        this.animateBadge();
        this.playNotificationSound();
    }

    // Update UI
    updateUI() {
        const notifications = this.service.getNotifications();
        const unreadCount = this.service.getUnreadCount();

        // Update badge
        if (this.elements.badge) {
            this.elements.badge.textContent = unreadCount;
            this.elements.badge.style.display = unreadCount > 0 ? 'inline-block' : 'none';
        }

        if (this.elements.count) {
            this.elements.count.textContent = unreadCount;
        }

        // Update list
        this.renderNotificationList(notifications);
    }

    // Render Notification List
    renderNotificationList(notifications) {
        if (!this.elements.list) return;

        if (notifications.length === 0) {
            this.elements.list.innerHTML = this.getEmptyStateHTML();
            return;
        }

        this.elements.list.innerHTML = notifications
            .slice(0, 10)
            .map(n => this.getNotificationItemHTML(n))
            .join('');

        // Attach event listeners to items
        this.attachItemEventListeners();
    }

    // Get Empty State HTML
    getEmptyStateHTML() {
        return `
            <div class="empty-state">
                <i class="bi bi-bell-slash"></i>
                <p class="mb-0">Không có thông báo</p>
            </div>
        `;
    }

    // Get Notification Item HTML
    getNotificationItemHTML(notification) {
        const iconClass = this.getIconClass(notification.type);
        const timeAgo = this.formatTimeAgo(notification.createdAt);

        return `
            <div class="notification-item ${notification.isRead ? '' : 'unread'}" 
                 data-id="${notification.id}"
                 data-link="${notification.link || ''}">
                <div class="d-flex">
                    <div class="notification-icon ${notification.type}">
                        <i class="bi ${notification.icon || iconClass}"></i>
                    </div>
                    <div class="notification-content ms-3">
                        <div class="notification-title">${this.escapeHtml(notification.title)}</div>
                        <p class="notification-message">${this.escapeHtml(notification.message)}</p>
                        <div class="notification-time">
                            <i class="bi bi-clock"></i>
                            <span>${timeAgo}</span>
                        </div>
                    </div>
                    <div class="notification-actions">
                        ${!notification.isRead ? `
                            <button class="mark-read-btn" title="Đánh dấu đã đọc">
                                <i class="bi bi-check"></i>
                            </button>
                        ` : ''}
                        <button class="delete-btn" title="Xóa">
                            <i class="bi bi-trash"></i>
                        </button>
                    </div>
                </div>
            </div>
        `;
    }

    // Attach Item Event Listeners
    attachItemEventListeners() {
        document.querySelectorAll('.notification-item').forEach(item => {
            // Click on item
            item.addEventListener('click', async (e) => {
                if (e.target.closest('.notification-actions')) return;

                const id = item.dataset.id;
                const link = item.dataset.link;
                const notification = this.service.getNotifications().find(n => n.id === id);

                if (notification && !notification.isRead) {
                    await this.service.markAsRead(id);
                }

                if (link && link !== '#' && link !== '') {
                    window.location.href = link;
                }
            });

            // Mark as read button
            const markReadBtn = item.querySelector('.mark-read-btn');
            if (markReadBtn) {
                markReadBtn.addEventListener('click', async (e) => {
                    e.stopPropagation();
                    const id = item.dataset.id;
                    await this.service.markAsRead(id);
                });
            }

            // Delete button
            const deleteBtn = item.querySelector('.delete-btn');
            if (deleteBtn) {
                deleteBtn.addEventListener('click', async (e) => {
                    e.stopPropagation();
                    const id = item.dataset.id;
                    if (confirm('Bạn có chắc muốn xóa thông báo này?')) {
                        await this.service.deleteNotification(id);
                    }
                });
            }
        });
    }

    // Show Toast
    showToast(notification) {
        if (!this.elements.toastContainer) return;

        const toastId = `toast-${Date.now()}`;
        const toast = document.createElement('div');
        toast.id = toastId;
        toast.className = `toast-notification ${notification.type}`;
        toast.innerHTML = `
            <div class="notification-icon ${notification.type}">
                <i class="bi ${notification.icon || this.getIconClass(notification.type)}"></i>
            </div>
            <div class="flex-grow-1">
                <div class="notification-title">${this.escapeHtml(notification.title)}</div>
                <p class="notification-message mb-0">${this.escapeHtml(notification.message)}</p>
            </div>
            <button class="toast-close">
                <i class="bi bi-x"></i>
            </button>
        `;

        // Close button handler
        toast.querySelector('.toast-close').addEventListener('click', () => {
            this.removeToast(toastId);
        });

        // Click to navigate
        if (notification.link && notification.link !== '#') {
            toast.style.cursor = 'pointer';
            toast.addEventListener('click', (e) => {
                if (!e.target.closest('.toast-close')) {
                    window.location.href = notification.link;
                }
            });
        }

        this.elements.toastContainer.appendChild(toast);
        this.toasts.set(toastId, toast);

        // Auto remove after 5 seconds
        setTimeout(() => this.removeToast(toastId), 5000);
    }

    // Remove Toast
    removeToast(toastId) {
        const toast = this.toasts.get(toastId);
        if (toast) {
            toast.classList.add('removing');
            setTimeout(() => {
                toast.remove();
                this.toasts.delete(toastId);
            }, 300);
        }
    }

    // Animate Badge
    animateBadge() {
        if (this.elements.badge) {
            this.elements.badge.classList.add('notification-badge-pulse');
            setTimeout(() => {
                this.elements.badge.classList.remove('notification-badge-pulse');
            }, 2000);
        }
    }

    // Play Notification Sound
    playNotificationSound() {
        try {
            const audio = new Audio('data:audio/wav;base64,UklGRnoGAABXQVZFZm10IBAAAAABAAEAQB8AAEAfAAABAAgAZGF0YQoGAACBhYqFbF1fdJivrJBhNjVgodDbq2EcBj+a2/LDciUFLIHO8tiJNwgZaLvt559NEAxQp+PwtmMcBjiR1/LMeSwFJHfH8N2QQAoUXrTp66hVFApGn+DyvmwhBTGH0fPTgjMGHm7A7+OZSA0PVqzn77BdGAg+ltryxnMnBSuBzvLYiTcIGWi77OfQahkHPJTS8cpzJwUpfs/y2Yo3CBlon+zy1Y05CBld');
            audio.volume = 0.3;
            audio.play().catch(err => console.log('Sound play failed:', err));
        } catch (err) {
            console.log('Sound not supported:', err);
        }
    }

    // Show Connection Status
    showConnectionStatus(message, type) {
        if (!this.elements.list) return;

        let statusBar = document.getElementById('connectionStatus');
        if (!statusBar) {
            statusBar = document.createElement('div');
            statusBar.id = 'connectionStatus';
            statusBar.className = 'connection-status';
            this.elements.list.parentElement.insertBefore(statusBar, this.elements.list);
        }

        statusBar.className = `connection-status ${type}`;
        statusBar.innerHTML = `
            <i class="bi bi-${type === 'warning' ? 'arrow-repeat' : type === 'success' ? 'check-circle' : 'x-circle'}"></i>
            ${message}
        `;
        statusBar.style.display = 'block';
    }

    // Hide Connection Status
    hideConnectionStatus() {
        const statusBar = document.getElementById('connectionStatus');
        if (statusBar) {
            setTimeout(() => {
                statusBar.style.display = 'none';
            }, 2000);
        }
    }

    // Get Icon Class by Type
    getIconClass(type) {
        const icons = {
            success: 'bi-check-circle',
            warning: 'bi-exclamation-triangle',
            danger: 'bi-x-circle',
            info: 'bi-info-circle'
        };
        return icons[type] || 'bi-bell';
    }

    // Format Time Ago
    formatTimeAgo(dateString) {
        const date = new Date(dateString);
        const now = new Date();
        const diff = Math.floor((now - date) / 1000);

        if (diff < 60) return 'Vừa xong';
        if (diff < 3600) return `${Math.floor(diff / 60)} phút trước`;
        if (diff < 86400) return `${Math.floor(diff / 3600)} giờ trước`;
        if (diff < 604800) return `${Math.floor(diff / 86400)} ngày trước`;

        const options = { year: 'numeric', month: 'short', day: 'numeric' };
        return date.toLocaleDateString('vi-VN', options);
    }

    // Escape HTML
    escapeHtml(text) {
        const map = {
            '&': '&amp;',
            '<': '&lt;',
            '>': '&gt;',
            '"': '&quot;',
            "'": '&#039;'
        };
        return text.replace(/[&<>"']/g, m => map[m]);
    }

    // Clear All Toasts
    clearAllToasts() {
        this.toasts.forEach((toast, id) => {
            this.removeToast(id);
        });
    }

    // Destroy
    destroy() {
        this.clearAllToasts();
    }
}

// Initialize when DOM is ready
document.addEventListener('DOMContentLoaded', () => {
    // Configuration
    const config = {
        hubUrl: 'https://localhost:7000/hubs/notifications', // Thay đổi theo backend của bạn
        apiUrl: 'https://localhost:7000/api/notification'
    };

    // Initialize Service
    const notificationService = new NotificationService(config.hubUrl, config.apiUrl);

    // Initialize UI
    const notificationUI = new NotificationUI(notificationService);
    notificationUI.initialize();

    // Start service
    notificationService.initialize();

    // Make service globally accessible for testing
    window.notificationService = notificationService;
    window.notificationUI = notificationUI;

    // Test button (if exists)
    const testBtn = document.getElementById('testNotificationBtn');
    if (testBtn) {
        testBtn.addEventListener('click', async () => {
            const types = ['success', 'warning', 'danger', 'info'];
            const titles = [
                'Bài viết mới',
                'Cập nhật hệ thống',
                'Lỗi quan trọng',
                'Thông tin'
            ];
            const messages = [
                'Bài viết "Tin tức công nghệ" đã được xuất bản',
                'Hệ thống sẽ bảo trì vào 2h sáng mai',
                'Phát hiện lỗi nghiêm trọng trong module đăng nhập',
                'Có 5 bình luận mới cần xem xét'
            ];
            const icons = ['bi-file-text', 'bi-gear', 'bi-exclamation-triangle', 'bi-info-circle'];

            const randomIndex = Math.floor(Math.random() * types.length);
            const type = types[randomIndex];

            const testNotification = {
                title: titles[randomIndex],
                message: messages[randomIndex],
                type: type,
                icon: icons[randomIndex],
                link: '#',
                createdAt: new Date().toISOString(),
                isRead: false
            };

            await notificationService.createNotification(testNotification);
        });
    }
});