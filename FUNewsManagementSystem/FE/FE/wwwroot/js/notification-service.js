// wwwroot/js/notification-service.js
class NotificationService {
    constructor(hubUrl, apiUrl) {
        this.hubUrl = hubUrl;
        this.apiUrl = apiUrl;
        this.connection = null;
        this.notifications = [];
        this.maxNotifications = 50;
        this.listeners = [];
    }

    // Initialize SignalR Connection
    async initialize() {
        this.connection = new signalR.HubConnectionBuilder()
            .withUrl(this.hubUrl)
            .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
            .configureLogging(signalR.LogLevel.Information)
            .build();

        this.setupEventHandlers();
        await this.startConnection();
    }

    // Setup Event Handlers
    setupEventHandlers() {
        // Receive new notification
        this.connection.on("ReceiveNotification", (notification) => {
            console.log("📢 New notification:", notification);
            this.addNotification(notification);
            this.notifyListeners('new', notification);
        });

        // Connection events
        this.connection.onreconnecting(() => {
            console.log("🔄 SignalR Reconnecting...");
            this.notifyListeners('reconnecting');
        });

        this.connection.onreconnected(() => {
            console.log("✅ SignalR Reconnected");
            this.loadNotifications();
            this.notifyListeners('reconnected');
        });

        this.connection.onclose(() => {
            console.log("❌ SignalR Disconnected");
            this.notifyListeners('disconnected');
            setTimeout(() => this.startConnection(), 5000);
        });
    }

    // Start Connection
    async startConnection() {
        try {
            await this.connection.start();
            console.log("✅ SignalR Connected");
            await this.loadNotifications();
            this.notifyListeners('connected');
        } catch (err) {
            console.error("❌ SignalR Connection Error:", err);
            setTimeout(() => this.startConnection(), 5000);
        }
    }

    // Load Notifications from API
    async loadNotifications(limit = 10) {
        try {
            const response = await fetch(`${this.apiUrl}?limit=${limit}`);
            if (!response.ok) throw new Error('Failed to load notifications');

            const data = await response.json();
            this.notifications = data.notifications || [];
            this.notifyListeners('loaded', this.notifications);
            return data;
        } catch (err) {
            console.error("Load notifications error:", err);
            return { notifications: [], unreadCount: 0, totalCount: 0 };
        }
    }

    // Add Notification
    addNotification(notification) {
        this.notifications.unshift(notification);
        if (this.notifications.length > this.maxNotifications) {
            this.notifications.pop();
        }
        this.notifyListeners('update', this.notifications);
    }

    // Mark as Read
    async markAsRead(notificationId) {
        try {
            const response = await fetch(`${this.apiUrl}/${notificationId}/read`, {
                method: 'PUT'
            });

            if (response.ok) {
                const notification = this.notifications.find(n => n.id === notificationId);
                if (notification) {
                    notification.isRead = true;
                    this.notifyListeners('update', this.notifications);
                }
            }
            return response.ok;
        } catch (err) {
            console.error("Mark as read error:", err);
            return false;
        }
    }

    // Mark All as Read
    async markAllAsRead() {
        try {
            const response = await fetch(`${this.apiUrl}/read-all`, {
                method: 'PUT'
            });

            if (response.ok) {
                this.notifications.forEach(n => n.isRead = true);
                this.notifyListeners('update', this.notifications);
            }
            return response.ok;
        } catch (err) {
            console.error("Mark all as read error:", err);
            return false;
        }
    }

    // Delete Notification
    async deleteNotification(notificationId) {
        try {
            const response = await fetch(`${this.apiUrl}/${notificationId}`, {
                method: 'DELETE'
            });

            if (response.ok) {
                this.notifications = this.notifications.filter(n => n.id !== notificationId);
                this.notifyListeners('update', this.notifications);
            }
            return response.ok;
        } catch (err) {
            console.error("Delete notification error:", err);
            return false;
        }
    }

    // Create Notification
    async createNotification(notification) {
        try {
            const response = await fetch(this.apiUrl, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(notification)
            });

            if (response.ok) {
                return await response.json();
            }
            return null;
        } catch (err) {
            console.error("Create notification error:", err);
            return null;
        }
    }

    // Get Notifications
    getNotifications() {
        return this.notifications;
    }

    // Get Unread Count
    getUnreadCount() {
        return this.notifications.filter(n => !n.isRead).length;
    }

    // Subscribe to events
    subscribe(callback) {
        this.listeners.push(callback);
        return () => {
            this.listeners = this.listeners.filter(cb => cb !== callback);
        };
    }

    // Notify Listeners
    notifyListeners(event, data = null) {
        this.listeners.forEach(callback => {
            try {
                callback(event, data);
            } catch (err) {
                console.error("Listener error:", err);
            }
        });
    }

    // Disconnect
    async disconnect() {
        if (this.connection) {
            await this.connection.stop();
        }
    }
}

// Export for use
if (typeof module !== 'undefined' && module.exports) {
    module.exports = NotificationService;
}