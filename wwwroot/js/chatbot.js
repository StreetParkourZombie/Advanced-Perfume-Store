// ChatBot Widget JavaScript
class PerfumeChatBot {
    constructor() {
        this.isOpen = false;
        this.apiKey = 'perfume-bot-2024';
        this.messages = [];
        this.init();
    }

    init() {
        this.createWidget();
        this.bindEvents();
        this.addWelcomeMessage();
    }

    createWidget() {
        const widget = document.createElement('div');
        widget.className = 'chatbot-widget';
        widget.innerHTML = `
            <div class="chatbot-container" id="chatbot-container">
                <div class="chatbot-header">
                    <h4>🤖 PerfumeBot</h4>
                    <button class="chatbot-close" id="chatbot-close">×</button>
                </div>
                <div class="chatbot-messages" id="chatbot-messages">
                    <!-- Messages will be added here -->
                </div>
                <div class="chatbot-input-area">
                    <div class="chatbot-input-group">
                        <input type="text" class="chatbot-input" id="chatbot-input" 
                               placeholder="Nhập tin nhắn..." maxlength="500">
                        <button class="chatbot-send" id="chatbot-send">
                            <i class="fas fa-paper-plane"></i>
                        </button>
                    </div>
                </div>
            </div>
            <button class="chatbot-toggle" id="chatbot-toggle">
                <i class="fas fa-comments"></i>
            </button>
        `;

        document.body.appendChild(widget);
    }

    bindEvents() {
        const toggle = document.getElementById('chatbot-toggle');
        const close = document.getElementById('chatbot-close');
        const input = document.getElementById('chatbot-input');
        const send = document.getElementById('chatbot-send');

        toggle.addEventListener('click', () => this.toggleChat());
        close.addEventListener('click', () => this.closeChat());
        send.addEventListener('click', () => this.sendMessage());

        input.addEventListener('keypress', (e) => {
            if (e.key === 'Enter') {
                this.sendMessage();
            }
        });
    }

    toggleChat() {
        const container = document.getElementById('chatbot-container');
        const toggle = document.getElementById('chatbot-toggle');

        if (this.isOpen) {
            this.closeChat();
        } else {
            container.style.display = 'flex';
            toggle.innerHTML = '<i class="fas fa-times"></i>';
            this.isOpen = true;

            // Focus input
            setTimeout(() => {
                document.getElementById('chatbot-input').focus();
            }, 300);
        }
    }

    closeChat() {
        const container = document.getElementById('chatbot-container');
        const toggle = document.getElementById('chatbot-toggle');

        container.style.display = 'none';
        toggle.innerHTML = '<i class="fas fa-comments"></i>';
        this.isOpen = false;
    }

    addWelcomeMessage() {
        const welcomeMsg = `Chào bạn! 👋 Mình là PerfumeBot, trợ lý ảo của PerfumeStore.

Mình có thể giúp bạn:
🌸 Tư vấn nước hoa phù hợp
📦 Kiểm tra đơn hàng (nhập mã đơn hàng)
💳 Thông tin chính sách

Bạn cần hỗ trợ gì nhé?`;

        this.addMessage('bot', welcomeMsg);
    }

    async sendMessage() {
        const input = document.getElementById('chatbot-input');
        const send = document.getElementById('chatbot-send');
        const message = input.value.trim();

        if (!message) return;

        // Add user message
        this.addMessage('user', message);
        input.value = '';

        // Show typing indicator
        this.showTyping();
        send.disabled = true;

        try {
            const response = await fetch('/api/ChatBot/chat', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'X-API-Key': this.apiKey
                },
                body: JSON.stringify({
                    message: message,
                    userId: this.getUserId()
                })
            });

            const data = await response.json();

            // Remove typing indicator
            this.hideTyping();

            if (response.ok) {
                this.addMessage('bot', data.message);
            } else {
                this.addMessage('bot', data.error || 'Có lỗi xảy ra, bạn thử lại nhé! 😅');
            }
        } catch (error) {
            this.hideTyping();
            this.addMessage('bot', 'Lỗi kết nối. Bạn kiểm tra mạng và thử lại nhé! 🔌');
        } finally {
            send.disabled = false;
        }
    }

    addMessage(sender, message) {
        const messagesContainer = document.getElementById('chatbot-messages');
        const messageDiv = document.createElement('div');
        messageDiv.className = `chatbot-message ${sender}`;

        const contentDiv = document.createElement('div');
        contentDiv.className = 'chatbot-message-content';
        contentDiv.innerHTML = this.formatMessage(message);

        messageDiv.appendChild(contentDiv);
        messagesContainer.appendChild(messageDiv);

        // Scroll to bottom
        messagesContainer.scrollTop = messagesContainer.scrollHeight;

        // Store message
        this.messages.push({ sender, message, timestamp: new Date() });
    }

    formatMessage(message) {
        // Convert line breaks to <br>
        return message.replace(/\n/g, '<br>');
    }

    showTyping() {
        const messagesContainer = document.getElementById('chatbot-messages');
        const typingDiv = document.createElement('div');
        typingDiv.className = 'chatbot-message bot';
        typingDiv.id = 'typing-indicator';

        typingDiv.innerHTML = `
            <div class="chatbot-message-content">
                <div class="chatbot-typing">
                    PerfumeBot đang trả lời
                    <div class="chatbot-typing-dots">
                        <div class="chatbot-typing-dot"></div>
                        <div class="chatbot-typing-dot"></div>
                        <div class="chatbot-typing-dot"></div>
                    </div>
                </div>
            </div>
        `;

        messagesContainer.appendChild(typingDiv);
        messagesContainer.scrollTop = messagesContainer.scrollHeight;
    }

    hideTyping() {
        const typingIndicator = document.getElementById('typing-indicator');
        if (typingIndicator) {
            typingIndicator.remove();
        }
    }

    getUserId() {
        // Generate or get user ID from localStorage
        let userId = localStorage.getItem('perfume-chatbot-user-id');
        if (!userId) {
            userId = 'user-' + Date.now() + '-' + Math.random().toString(36).substr(2, 9);
            localStorage.setItem('perfume-chatbot-user-id', userId);
        }
        return userId;
    }
}

// Initialize ChatBot when DOM is loaded
document.addEventListener('DOMContentLoaded', function () {
    // Wait a bit for other scripts to load
    setTimeout(() => {
        new PerfumeChatBot();
    }, 1000);
});