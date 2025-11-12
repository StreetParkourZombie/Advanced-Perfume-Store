// Review Form Functionality
document.addEventListener('DOMContentLoaded', function () {
    let selectedRating = 0;

    // Handle open review form button
    document.addEventListener('click', function (e) {
        if (e.target.classList.contains('btn-rate-now')) {
            e.preventDefault();
            openReviewForm();
        }

        // Handle close form
        if (e.target.closest('.btn-close-form')) {
            e.preventDefault();
            closeReviewForm();
        }

        // Handle star rating clicks
        if (e.target.closest('.review-form .star-item')) {
            e.preventDefault();
            const starItem = e.target.closest('.review-form .star-item');
            const rating = parseInt(starItem.getAttribute('data-rating'));
            selectRating(rating);
        }

    });

    // Handle form submission
    document.addEventListener('submit', function (e) {
        if (e.target.id === 'reviewForm') {
            e.preventDefault();
            handleFormSubmission(e.target);
        }
    });

    // Handle textarea character count
    document.addEventListener('input', function (e) {
        if (e.target.id === 'reviewContent') {
            updateCharCount(e.target);
        }
    });

    // Handle star hover events
    document.addEventListener('mouseenter', function (e) {
        if (e.target && e.target.closest && e.target.closest('.review-form .star-item')) {
            const starItem = e.target.closest('.review-form .star-item');
            const rating = parseInt(starItem.getAttribute('data-rating'));
            highlightStars(rating);
        }
    }, true);

    document.addEventListener('mouseleave', function (e) {
        if (e.target && e.target.closest && e.target.closest('.review-form .star-rating')) {
            resetStarHighlight();
        }
    }, true);

    // Handle overlay click to close
    document.addEventListener('click', function (e) {
        if (e.target.classList.contains('review-form-overlay')) {
            closeReviewForm();
        }
    });

    // Handle escape key to close
    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape') {
            closeReviewForm();
        }
    });
});

function openReviewForm() {
    const overlay = document.getElementById('reviewFormOverlay');
    if (overlay) {
        overlay.style.display = 'flex';
        document.body.style.overflow = 'hidden'; // Prevent background scrolling

        // Focus on textarea
        const textarea = document.getElementById('reviewContent');
        if (textarea) {
            setTimeout(() => textarea.focus(), 100);
        }
    }
}

function closeReviewForm() {
    const overlay = document.getElementById('reviewFormOverlay');
    if (overlay) {
        overlay.style.display = 'none';
        document.body.style.overflow = ''; // Restore scrolling

        // Reset form
        resetForm();
    }
}

function selectRating(rating) {
    selectedRating = rating;
    const hiddenInput = document.getElementById('selectedRating');
    if (hiddenInput) {
        hiddenInput.value = rating;
    }

    // Update star display - stars from 1 to rating should be yellow
    const starItems = document.querySelectorAll('.review-form .star-item');
    starItems.forEach((item, index) => {
        const star = item.querySelector('.star');
        const label = item.querySelector('.rating-label');

        // Index is 0-based, rating is 1-based
        // So if rating is 3, we want to highlight items at index 0, 1, 2
        if (index < rating) {
            item.classList.add('active');
            star.style.color = '#ffc107';
            label.style.color = '#333';
            label.style.fontWeight = '600';
        } else {
            item.classList.remove('active');
            star.style.color = '#ddd';
            label.style.color = '#666';
            label.style.fontWeight = '500';
        }
    });
}

function highlightStars(rating) {
    const starItems = document.querySelectorAll('.review-form .star-item');
    starItems.forEach((item, index) => {
        const star = item.querySelector('.star');
        const label = item.querySelector('.rating-label');

        // Index is 0-based, rating is 1-based
        // So if rating is 3, we want to highlight items at index 0, 1, 2
        if (index < rating) {
            star.style.color = '#ffc107';
            label.style.color = '#333';
            label.style.fontWeight = '600';
        } else {
            star.style.color = '#ddd';
            label.style.color = '#666';
            label.style.fontWeight = '500';
        }
    });
}

function resetStarHighlight() {
    const starItems = document.querySelectorAll('.review-form .star-item');
    starItems.forEach((item, index) => {
        const star = item.querySelector('.star');
        const label = item.querySelector('.rating-label');

        if (item.classList.contains('active')) {
            // Keep active stars yellow
            star.style.color = '#ffc107';
            label.style.color = '#333';
            label.style.fontWeight = '600';
        } else {
            // Reset non-active stars
            star.style.color = '#ddd';
            label.style.color = '#666';
            label.style.fontWeight = '500';
        }
    });
}

function updateCharCount(textarea) {
    const charCount = document.getElementById('charCount');
    if (charCount) {
        const length = textarea.value.length;
        charCount.textContent = `${length} ký tự (Tối thiểu 5)`;

        if (length < 5) {
            charCount.classList.add('invalid');
        } else {
            charCount.classList.remove('invalid');
        }
    }
}

function handleFormSubmission(form) {
    const textarea = document.getElementById('reviewContent');
    const nameInput = document.getElementById('reviewerName');
    const productIdInput = document.getElementById('productId');
    const submitBtn = form.querySelector('.btn-submit-review');

    // Validate form
    if (!textarea.value.trim() || textarea.value.trim().length < 5) {
        alert('Vui lòng nhập ít nhất 5 ký tự cho đánh giá.');
        textarea.focus();
        return;
    }

    if (selectedRating === 0) {
        alert('Vui lòng chọn đánh giá sao.');
        return;
    }

    // Disable submit button
    submitBtn.disabled = true;
    submitBtn.textContent = 'ĐANG GỬI...';

    // Build form data
    const tokenInput = form.querySelector('input[name="__RequestVerificationToken"]');
    const payload = new URLSearchParams();
    payload.append('productId', productIdInput ? productIdInput.value : '0');
    payload.append('rating', String(selectedRating));
    payload.append('content', textarea.value.trim());

    fetch('/Comments/Add', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8',
            'RequestVerificationToken': tokenInput ? tokenInput.value : ''
        },
        body: payload.toString()
    })
        .then(async (res) => {
            const data = await res.json().catch(() => ({}));
            if (!res.ok) {
                throw new Error(data.message || 'Gửi đánh giá thất bại');
            }
            alert(data.message || 'Bình luận đã được gửi. Vui lòng chờ phê duyệt.');
            closeReviewForm();
        })
        .catch((err) => {
            alert(err.message || 'Có lỗi xảy ra. Vui lòng thử lại.');
        })
        .finally(() => {
            submitBtn.disabled = false;
            submitBtn.textContent = 'GỬI ĐÁNH GIÁ';
        });
}

function resetForm() {
    const form = document.getElementById('reviewForm');
    if (form) {
        form.reset();
        selectedRating = 0;

        // Reset star display
        const starItems = document.querySelectorAll('.review-form .star-item');
        starItems.forEach(item => {
            item.classList.remove('active');
            const star = item.querySelector('.star');
            const label = item.querySelector('.rating-label');
            star.style.color = '#ddd';
            label.style.color = '#666';
            label.style.fontWeight = '500';
        });

        // Reset hidden input
        const hiddenInput = document.getElementById('selectedRating');
        if (hiddenInput) {
            hiddenInput.value = '0';
        }

        // Reset char count
        const textarea = document.getElementById('reviewContent');
        if (textarea) {
            updateCharCount(textarea);
        }
    }
}