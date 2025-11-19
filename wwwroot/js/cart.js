// Cart functionality
document.addEventListener('DOMContentLoaded', function() {
    // Initialize cart count on page load
    updateCartCount();
    
    // Handle add to cart forms
    const addToCartForms = document.querySelectorAll('.add-to-cart-form');
    addToCartForms.forEach(form => {
        form.addEventListener('submit', function(e) {
            e.preventDefault();
            addToCart(this);
        });
    });
});

// Add product to cart via AJAX
function addToCart(form) {
    const formData = new FormData(form);
    const button = form.querySelector('button[type="submit"]');
    const originalText = button.innerHTML;
    
    // Show loading state
    button.innerHTML = '<i class="bi bi-hourglass-split me-1"></i>Đang thêm...';
    button.disabled = true;
    
    // Add AJAX header
    const xhr = new XMLHttpRequest();
    xhr.open('POST', form.action, true);
    xhr.setRequestHeader('X-Requested-With', 'XMLHttpRequest');
    
    xhr.onreadystatechange = function() {
        if (xhr.readyState === 4) {
            if (xhr.status === 200) {
                try {
                    const response = JSON.parse(xhr.responseText);
                    if (response.success) {
                        // Show success message
                        showToast(response.message, 'success');
                        
                        // Update cart count
                        updateCartCount();
                        
                        // Add animation to button
                        button.innerHTML = '<i class="bi bi-check-circle me-1"></i>Đã thêm!';
                        button.classList.add('btn-success');
                        button.classList.remove('btn-orange');
                        
                        // Reset button after 2 seconds
                        setTimeout(() => {
                            button.innerHTML = originalText;
                            button.classList.remove('btn-success');
                            button.classList.add('btn-orange');
                            button.disabled = false;
                        }, 2000);
                    } else {
                        // Hiển thị thông báo lỗi từ server
                        showToast(response.message || 'Có lỗi xảy ra khi thêm sản phẩm!', 'error');
                        button.innerHTML = originalText;
                        button.disabled = false;
                    }
                } catch (e) {
                    console.error('Error parsing response:', e);
                    showToast('Có lỗi xảy ra!', 'error');
                    button.innerHTML = originalText;
                    button.disabled = false;
                }
            } else {
                showToast('Có lỗi xảy ra khi thêm sản phẩm!', 'error');
                button.innerHTML = originalText;
                button.disabled = false;
            }
        }
    };
    
    xhr.send(formData);
}

// Update cart count in header
function updateCartCount() {
    fetch('/Cart/GetCartCount')
        .then(response => response.json())
        .then(data => {
            // Sử dụng cartCount từ response (API trả về cartCount, không phải count)
            const count = data.cartCount || data.count || 0;
            
            // Update mobile cart count
            const mobileCount = document.getElementById('mobile-cart-count');
            if (mobileCount) {
                mobileCount.textContent = count;
                // Hiển thị badge nếu có sản phẩm, ẩn nếu không có
                if (count > 0) {
                    mobileCount.style.display = 'block';
                    mobileCount.classList.add('cart-badge-animation');
                    setTimeout(() => mobileCount.classList.remove('cart-badge-animation'), 600);
                } else {
                    mobileCount.style.display = 'none';
                }
            }
            
            // Update desktop cart count
            const desktopCount = document.getElementById('desktop-cart-count');
            if (desktopCount) {
                desktopCount.textContent = count;
                // Hiển thị badge nếu có sản phẩm, ẩn nếu không có
                if (count > 0) {
                    desktopCount.style.display = 'block';
                    desktopCount.classList.add('cart-badge-animation');
                    setTimeout(() => desktopCount.classList.remove('cart-badge-animation'), 600);
                } else {
                    desktopCount.style.display = 'none';
                }
            }
            
            // Update tất cả các element có class cart-count (fallback)
            const allCartCounts = document.querySelectorAll('.cart-count');
            allCartCounts.forEach(element => {
                element.textContent = count;
                if (count > 0) {
                    element.style.display = 'block';
                } else {
                    element.style.display = 'none';
                }
            });
        })
        .catch(error => {
            console.error('Error updating cart count:', error);
        });
}

// Show toast notification
function showToast(message, type = 'info') {
    // Create toast container if it doesn't exist
    let toastContainer = document.getElementById('toast-container');
    if (!toastContainer) {
        toastContainer = document.createElement('div');
        toastContainer.id = 'toast-container';
        toastContainer.style.cssText = `
            position: fixed;
            top: 20px;
            right: 20px;
            z-index: 9999;
            max-width: 300px;
        `;
        document.body.appendChild(toastContainer);
    }
    
    // Create toast element
    const toast = document.createElement('div');
    toast.className = `alert alert-${type === 'success' ? 'success' : type === 'error' ? 'danger' : 'info'} alert-dismissible fade show`;
    toast.style.cssText = `
        margin-bottom: 10px;
        box-shadow: 0 4px 12px rgba(0,0,0,0.15);
        border: none;
        border-radius: 8px;
    `;
    
    toast.innerHTML = `
        <i class="bi bi-${type === 'success' ? 'check-circle' : type === 'error' ? 'exclamation-triangle' : 'info-circle'} me-2"></i>
        ${message}
        <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
    `;
    
    toastContainer.appendChild(toast);
    
    // Auto remove after 3 seconds
    setTimeout(() => {
        if (toast.parentNode) {
            toast.remove();
        }
    }, 3000);
}

// Add cart animation styles
const style = document.createElement('style');
style.textContent = `
    .add-to-cart-form button {
        transition: all 0.3s ease;
    }
    
    .add-to-cart-form button:hover {
        transform: translateY(-1px);
    }
    
    .add-to-cart-form button:active {
        transform: translateY(0);
    }
    
    .cart-badge-animation {
        animation: cartPulse 0.6s ease-in-out;
    }
    
    @keyframes cartPulse {
        0% { transform: scale(1); }
        50% { transform: scale(1.2); }
        100% { transform: scale(1); }
    }
`;
document.head.appendChild(style);


