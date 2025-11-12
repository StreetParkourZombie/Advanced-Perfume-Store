/**
 * SpinWheel JavaScript - Logic quay và animation đẹp
 */

class SpinWheel {
    constructor() {
        this.wheel = document.getElementById('spinWheel');
        this.spinBtn = document.querySelector('.wheel-center');
        this.resultModal = document.getElementById('resultModal');
        this.remainingSpinsEl = document.getElementById('remainingSpins');
        this.voucherList = document.getElementById('voucherList');
        
        this.isSpinning = false;
        this.currentRotation = 0;
        this.vouchers = [];
        this.currentVoucher = null;
        this.autoCloseTimeout = null;
        
        this.init();
    }

    init() {
        this.createVoucherPool();
        this.createWheel();
        this.createVoucherList();
        this.bindEvents();
        this.updateSpinButton();
        this.startBackgroundAnimations();
        
        // Debug info
        console.log('Wheel element:', this.wheel);
        console.log('Wheel style:', this.wheel.style);
    }

    createVoucherPool() {
        this.vouchers = [
            { id: 1, name: "Miễn phí ship", type: "freeship", code: "FREESHIP", color: "linear-gradient(135deg, #667eea 0%, #764ba2 100%)", probability: 12 },
            { id: 2, name: "Chúc may mắn lần sau", type: "none", code: "NONE", color: "linear-gradient(135deg, #f093fb 0%, #f5576c 100%)", probability: 10 },
            { id: 3, name: "Giảm 15%", type: "percent", code: "LUCKY15", color: "linear-gradient(135deg, #4facfe 0%, #00f2fe 100%)", probability: 15 },
            { id: 4, name: "Giảm 10%", type: "percent", code: "LUCKY10", color: "linear-gradient(135deg, #43e97b 0%, #38f9d7 100%)", probability: 20 },
            { id: 5, name: "Giảm 20%", type: "percent", code: "LUCKY20", color: "linear-gradient(135deg, #fa709a 0%, #fee140 100%)", probability: 18 },
            { id: 6, name: "Giảm 30%", type: "percent", code: "LUCKY30", color: "linear-gradient(135deg, #a8edea 0%, #fed6e3 100%)", probability: 12 },
            { id: 7, name: "Giảm 50.000đ", type: "amount", code: "CASH50K", color: "linear-gradient(135deg, #ff9a9e 0%, #fecfef 100%)", probability: 8 },
            { id: 8, name: "Giảm 100.000đ", type: "amount", code: "CASH100K", color: "linear-gradient(135deg, #ffecd2 0%, #fcb69f 100%)", probability: 5 }
        ];
    }

    createWheel() {
        // Clear existing sectors
        this.wheel.innerHTML = '';
        
        const sectorAngle = 360 / this.vouchers.length; // 45 degrees for 8 sectors
        
        this.vouchers.forEach((voucher, index) => {
            const sector = document.createElement('div');
            sector.className = 'wheel-sector';
            sector.style.transform = `rotate(calc(360deg / 8 * ${index}))`; // Mỗi slice xoay đều
            sector.style.background = voucher.color;
            
            const text = document.createElement('div');
            text.className = 'sector-text';
            text.innerHTML = `
                <div style="transform: rotate(${-index * sectorAngle + sectorAngle/2}deg);">
                    <div style="font-size: 11px; font-weight: 700; color: #fff; text-shadow: 0 2px 4px rgba(0,0,0,0.8);">
                        ${voucher.name}
                    </div>
                </div>
            `;
            
            sector.appendChild(text);
            this.wheel.appendChild(sector);
        });
    }

    createVoucherList() {
        if (!this.voucherList) return;
        
        this.voucherList.innerHTML = '';
        
        this.vouchers.forEach(voucher => {
            if (voucher.type === 'none') return;
            
            const voucherCard = document.createElement('div');
            voucherCard.className = 'voucher-card';
            voucherCard.innerHTML = `
                <div class="voucher-header">
                    <div class="voucher-name">${voucher.name}</div>
                    <div class="voucher-description">Tỷ lệ trúng: ${voucher.probability}%</div>
                </div>
                <div class="voucher-footer">
                    <div class="voucher-condition">${this.getVoucherCondition(voucher.type)}</div>
                </div>
            `;
            
            this.voucherList.appendChild(voucherCard);
        });
    }

    getVoucherCondition(type) {
        switch(type) {
            case 'amount': return 'Áp dụng cho đơn hàng từ 500.000₫';
            case 'percent': return 'Áp dụng cho tất cả sản phẩm';
            case 'freeship': return 'Áp dụng cho đơn hàng từ 200.000₫';
            default: return 'Áp dụng cho tất cả sản phẩm';
        }
    }

    bindEvents() {
        if (this.spinBtn) {
            console.log('Spin button found:', this.spinBtn);
            console.log('Spin button style:', window.getComputedStyle(this.spinBtn));
            
            this.spinBtn.addEventListener('click', (e) => {
                e.preventDefault();
                e.stopPropagation();
                console.log('Spin button clicked!');
                if (!this.isSpinning) {
                    this.spin();
                }
            });
            
            // Đã loại bỏ hover events
        } else {
            console.error('Spin button not found!');
        }

        // Close modal events
        const closeModalBtn = document.getElementById('closeModal');
        if (closeModalBtn) {
            closeModalBtn.addEventListener('click', (e) => {
                e.preventDefault();
                e.stopPropagation();
                console.log('Close modal button clicked');
                this.closeModal();
            });
        }

        const closeModalBtn2 = document.getElementById('closeModalBtn');
        if (closeModalBtn2) {
            closeModalBtn2.addEventListener('click', (e) => {
                e.preventDefault();
                e.stopPropagation();
                console.log('Close modal button 2 clicked');
                this.closeModal();
            });
        }

        if (this.resultModal) {
            this.resultModal.addEventListener('click', (e) => {
                if (e.target === this.resultModal || e.target.classList.contains('modal-backdrop')) {
                    this.closeModal();
                }
            });
        }

        // Apply voucher button
        const applyVoucherBtn = document.getElementById('applyVoucherBtn');
        if (applyVoucherBtn) {
            applyVoucherBtn.addEventListener('click', (e) => {
                e.preventDefault();
                e.stopPropagation();
                console.log('Apply voucher button clicked');
                console.log('Current voucher before apply:', this.currentVoucher);
                this.applyVoucher();
            });
        }

        // Save voucher button
        const saveVoucherBtn = document.getElementById('saveVoucherBtn');
        if (saveVoucherBtn) {
            saveVoucherBtn.addEventListener('click', (e) => {
                e.preventDefault();
                e.stopPropagation();
                console.log('Save voucher button clicked');
                this.saveVoucher();
            });
        }

        // Reset spins button
        const resetSpinsBtn = document.getElementById('resetSpinsBtn');
        if (resetSpinsBtn) {
            resetSpinsBtn.addEventListener('click', () => {
                this.resetSpins();
            });
        }

        // Fix SpinNumber button
        const fixSpinNumbersBtn = document.getElementById('fixSpinNumbersBtn');
        if (fixSpinNumbersBtn) {
            fixSpinNumbersBtn.addEventListener('click', () => {
                this.fixAllSpinNumbers();
            });
        }

        // Test session button
        const testSessionBtn = document.getElementById('testSessionBtn');
        if (testSessionBtn) {
            testSessionBtn.addEventListener('click', () => {
                this.testSession();
            });
        }
    }

    async spin() {
        if (this.isSpinning) return;
        
        const remaining = parseInt(this.remainingSpinsEl.textContent);
        if (remaining <= 0) {
            this.showMessage('Bạn đã hết lượt quay hôm nay!', 'warning');
            return;
        }

        this.isSpinning = true;
        this.updateSpinButton();
        this.addSparkleEffects();

        try {
            const response = await fetch('/SpinWheel/Spin', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value || ''
                }
            });

            const result = await response.json();
            
            if (result.success) {
                console.log('Spin result:', result);
                console.log('Angle received:', result.angle);
                this.animateSpin(result.angle);
                this.showResult(result);
                this.updateRemainingSpins(result.remainingSpins);
            } else {
                this.showMessage(result.message, 'error');
                this.isSpinning = false;
                this.updateSpinButton();
            }
        } catch (error) {
            console.error('Spin error:', error);
            this.showMessage('Có lỗi xảy ra, vui lòng thử lại!', 'error');
            this.isSpinning = false;
            this.updateSpinButton();
        }
    }

    animateSpin(finalAngle) {
        console.log('animateSpin called with angle:', finalAngle);
        console.log('Current rotation before:', this.currentRotation);
        
        // Thêm nhiều vòng quay để tạo hiệu ứng quay đẹp
        const extraRotations = 8; // Quay thêm 8 vòng (2880 độ)
        this.currentRotation += finalAngle + (extraRotations * 360);
        
        console.log('New rotation:', this.currentRotation);
        
        // Thêm transition để tạo animation mượt mà
        this.wheel.style.transition = 'transform 4s cubic-bezier(0.25, 0.46, 0.45, 0.94)';
        this.wheel.style.transform = `rotate(${this.currentRotation}deg)`;
        
        console.log('Applied transform:', this.wheel.style.transform);
        
        setTimeout(() => {
            this.isSpinning = false;
            this.updateSpinButton();
            this.removeSparkleEffects();
            // Reset transition sau khi animation kết thúc
            this.wheel.style.transition = '';
        }, 4000);
    }

    calculateSpinAngle(voucherId) {
        const random = Math.random();
        const spins = 5 + Math.floor(random * 3); // 5-7 vòng quay
        const sectorAngle = 360.0 / 8; // 8 sectors
        const targetAngle = (voucherId - 1) * sectorAngle + (sectorAngle / 2); // Giữa sector
        const finalAngle = spins * 360 + targetAngle;
        
        return finalAngle;
    }

    showResult(result) {
        setTimeout(() => {
            this.currentVoucher = result.voucher;
            console.log('Result voucher:', result.voucher);
            console.log('Current voucher set to:', this.currentVoucher);
            
            const resultContent = document.getElementById('resultContent');
            const applyBtn = document.getElementById('applyVoucherBtn');
            const saveBtn = document.getElementById('saveVoucherBtn');
            
            if (resultContent) {
                const iconClass = this.getVoucherIcon(result.voucher.type);
                resultContent.innerHTML = `
                    <div class="result-animation ${result.animation}">
                        <div class="result-icon">
                            <i class="${iconClass}"></i>
                        </div>
                        <h4 class="result-title">${result.voucher.name}</h4>
                        <p class="result-message">${result.message}</p>
                        ${result.voucher.type !== 'none' ? `
                            <div class="result-voucher">
                                <strong>Mã voucher:</strong> ${result.voucher.code || 'AUTO'}
                            </div>
                        ` : ''}
                    </div>
                `;
            }
            
            if (applyBtn) {
                applyBtn.style.display = result.voucher.type !== 'none' ? 'block' : 'none';
            }
            
            if (saveBtn) {
                saveBtn.style.display = result.voucher.type !== 'none' ? 'block' : 'none';
            }
            
            this.showModal();
        }, 4000);
    }

    getVoucherIcon(type) {
        switch(type) {
            case 'none': return 'fas fa-times-circle';
            case 'bonus': return 'fas fa-star';
            case 'freeship': return 'fas fa-truck';
            case 'percent': return 'fas fa-percentage';
            case 'amount': return 'fas fa-money-bill-wave';
            default: return 'fas fa-gift';
        }
    }

    showModal() {
        if (this.resultModal) {
            this.resultModal.style.display = 'flex';
            this.resultModal.style.pointerEvents = 'auto';
            
            // Ensure modal content is clickable
            const modalContent = this.resultModal.querySelector('.modal-content');
            if (modalContent) {
                modalContent.style.pointerEvents = 'auto';
            }
            
            // Clear any existing auto-close timeout
            if (this.autoCloseTimeout) {
                clearTimeout(this.autoCloseTimeout);
            }
            
            // Auto close modal after 30 seconds if user doesn't interact
            this.autoCloseTimeout = setTimeout(() => {
                if (this.resultModal && this.resultModal.style.display === 'flex') {
                    console.log('Auto-closing modal after 30 seconds');
                    this.closeModal();
                }
            }, 30000);
        }
    }

    closeModal() {
        if (this.resultModal) {
            this.resultModal.style.display = 'none';
            
            // Clear auto-close timeout
            if (this.autoCloseTimeout) {
                clearTimeout(this.autoCloseTimeout);
                this.autoCloseTimeout = null;
            }
            
            // Reset current voucher
            this.currentVoucher = null;
            // Update spin button state
            this.updateSpinButton();
            // Ẩn các nút trong modal
            const applyBtn = document.getElementById('applyVoucherBtn');
            const saveBtn = document.getElementById('saveVoucherBtn');
            if (applyBtn) applyBtn.style.display = 'none';
            if (saveBtn) saveBtn.style.display = 'none';
        }
    }

    applyVoucher() {
        if (!this.currentVoucher) {
            console.log('No current voucher to apply');
            return;
        }
        
        console.log('Applying voucher:', this.currentVoucher);
        console.log('Voucher code:', this.currentVoucher.code);
        
        // Lưu voucher code trước khi đóng modal
        const voucherCode = this.currentVoucher.code;
        
        console.log('Sending voucher to server:', { code: voucherCode });
        
        fetch('/SpinWheel/ApplyVoucher', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value || ''
            },
            body: JSON.stringify({ code: voucherCode })
        })
        .then(response => response.json())
        .then(result => {
            console.log('Apply voucher result:', result);
            if (result.success) {
                this.showMessage('✅ Voucher đã được áp dụng! Đang chuyển đến trang thanh toán...', 'success');
                
                // Chuyển thẳng đến trang thanh toán và tự động cuộn đến khối đơn hàng
                console.log('Redirecting to checkout with voucher:', voucherCode);
                window.location.href = '/Cart/Checkout?voucherApplied=true#order-summary';
            } else {
                this.showMessage(result.message, 'error');
            }
        })
        .catch(error => {
            console.error('Apply voucher error:', error);
            this.showMessage('Có lỗi xảy ra khi áp dụng voucher!', 'error');
        });
    }

    updateSpinButton() {
        if (!this.spinBtn) return;
        
        const remaining = parseInt(this.remainingSpinsEl.textContent);
        
        if (remaining > 0 && !this.isSpinning) {
            this.spinBtn.style.pointerEvents = 'auto';
            this.spinBtn.style.opacity = '1';
            this.spinBtn.innerHTML = `
                <i class="fas fa-play center-logo"></i>
                <div class="center-text">QUAY NGAY</div>
            `;
        } else if (this.isSpinning) {
            this.spinBtn.style.pointerEvents = 'none';
            this.spinBtn.style.opacity = '0.8';
            this.spinBtn.innerHTML = `
                <i class="fas fa-spinner fa-spin center-logo"></i>
                <div class="center-text">ĐANG QUAY</div>
            `;
        } else {
            this.spinBtn.style.pointerEvents = 'none';
            this.spinBtn.style.opacity = '0.6';
            this.spinBtn.innerHTML = `
                <i class="fas fa-lock center-logo"></i>
                <div class="center-text">HẾT LƯỢT</div>
            `;
        }
    }

    updateRemainingSpins(count) {
        if (this.remainingSpinsEl) {
            this.remainingSpinsEl.textContent = count;
        }
    }

    fetchRemainingSpins() {
        fetch('/SpinWheel/GetRemainingSpins')
            .then(response => response.json())
            .then(data => {
                this.updateRemainingSpins(data.remainingSpins);
            })
            .catch(error => {
                console.error('Fetch remaining spins error:', error);
            });
    }

    addSparkleEffects() {
        const sparkles = document.querySelectorAll('.sparkle');
        sparkles.forEach(sparkle => {
            sparkle.style.animation = 'sparkle 0.5s ease-in-out infinite';
        });
    }

    removeSparkleEffects() {
        const sparkles = document.querySelectorAll('.sparkle');
        sparkles.forEach(sparkle => {
            sparkle.style.animation = 'sparkle 2s ease-in-out infinite';
        });
    }

    startBackgroundAnimations() {
        // Start particle animation
        const particles = document.querySelector('.floating-particles');
        if (particles) {
            particles.style.animation = 'float 20s ease-in-out infinite';
        }
    }

    async resetSpins() {
        try {
            const response = await fetch('/SpinWheel/ResetMySpins', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value || ''
                }
            });

            const result = await response.json();
            
            if (result.success) {
                this.updateRemainingSpins(result.remainingSpins);
                this.updateSpinButton();
                this.showMessage(result.message, 'success');
            } else {
                this.showMessage(result.message, 'error');
            }
        } catch (error) {
            console.error('Reset spins error:', error);
            this.showMessage('Có lỗi xảy ra khi reset số lần quay!', 'error');
        }
    }

    async fixAllSpinNumbers() {
        try {
            const response = await fetch('/SpinWheel/FixAllSpinNumbers', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value || ''
                }
            });

            const result = await response.json();
            
            if (result.success) {
                this.fetchRemainingSpins();
                this.updateSpinButton();
                this.showMessage(result.message, 'success');
            } else {
                this.showMessage(result.message, 'error');
            }
        } catch (error) {
            console.error('Fix SpinNumber error:', error);
            this.showMessage('Có lỗi xảy ra khi sửa SpinNumber!', 'error');
        }
    }

    async testSession() {
        try {
            const response = await fetch('/SpinWheel/TestSession', {
                method: 'GET',
                headers: {
                    'Content-Type': 'application/json'
                }
            });

            const result = await response.json();
            console.log('Test session result:', result);
            
            if (result.success) {
                this.showMessage(`✅ Session OK: ${result.voucher.name} (${result.voucher.code})`, 'success');
            } else {
                this.showMessage(`❌ Session Error: ${result.message}`, 'error');
            }
        } catch (error) {
            console.error('Test session error:', error);
            this.showMessage('Có lỗi xảy ra khi test session!', 'error');
        }
    }

    saveVoucher() {
        if (!this.currentVoucher) {
            this.showMessage('Không có voucher để lưu!', 'error');
            return;
        }

        try {
            // Lấy danh sách voucher đã lưu từ localStorage
            let savedVouchers = JSON.parse(localStorage.getItem('savedVouchers') || '[]');

            // Thêm voucher mới vào danh sách
            const voucherToSave = {
                ...this.currentVoucher,
                savedAt: new Date().toISOString(),
                expiryDate: new Date(Date.now() + 30 * 24 * 60 * 60 * 1000).toISOString() // 30 ngày
            };
            
            savedVouchers.push(voucherToSave);
            
            // Lưu vào localStorage
            localStorage.setItem('savedVouchers', JSON.stringify(savedVouchers));
            
            this.showMessage(`✅ Đã lưu voucher "${this.currentVoucher.name}" thành công!`, 'success');
            
            // Ẩn nút lưu sau khi lưu thành công
            const saveBtn = document.getElementById('saveVoucherBtn');
            if (saveBtn) {
                saveBtn.style.display = 'none';
            }
            
        } catch (error) {
            console.error('Save voucher error:', error);
            this.showMessage('Có lỗi xảy ra khi lưu voucher!', 'error');
        }
    }

    showMessage(message, type) {
        // Create a simple notification
        const notification = document.createElement('div');
        notification.className = `alert alert-${type === 'error' ? 'danger' : type} alert-dismissible fade show`;
        notification.style.position = 'fixed';
        notification.style.top = '20px';
        notification.style.right = '20px';
        notification.style.zIndex = '9999';
        notification.innerHTML = `
            ${message}
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        `;
        
        document.body.appendChild(notification);
        
        setTimeout(() => {
            notification.remove();
        }, 5000);
    }
}

// Initialize when DOM is loaded
document.addEventListener('DOMContentLoaded', function() {
    new SpinWheel();
});