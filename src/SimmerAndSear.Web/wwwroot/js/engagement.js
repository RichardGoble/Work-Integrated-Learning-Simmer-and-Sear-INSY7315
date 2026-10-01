(function () {
    'use strict';

    function getCsrfToken() {
        var el = document.querySelector('input[name="__RequestVerificationToken"]');
        return el ? el.value : '';
    }

    // ── 1. Contact Form ────────────────────────────────────────────────────────
    var contactForm = document.getElementById('contact-form');
    if (contactForm) {
        contactForm.addEventListener('submit', function (e) {
            e.preventDefault();
            var btn = document.getElementById('contact-submit-btn');
            var btnText = document.getElementById('contact-btn-text');
            var statusMsg = document.getElementById('contact-status-message');

            var fullName = (document.getElementById('contact-fullname') || {}).value || '';
            var email = (document.getElementById('contact-email') || {}).value || '';
            var phone = (document.getElementById('contact-phone') || {}).value || '';
            var inquiryType = (document.getElementById('contact-inquiry-type') || {}).value || 'General Inquiry';
            var message = (document.getElementById('contact-message') || {}).value || '';

            if (btn) btn.disabled = true;
            if (btnText) btnText.textContent = 'Sending...';
            if (statusMsg) statusMsg.style.display = 'none';

            var payload = {
                fullName: fullName.trim(),
                email: email.trim(),
                phone: phone.trim() || null,
                inquiryType: inquiryType.trim(),
                message: message.trim()
            };

            fetch('/api/contact', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'X-CSRF-TOKEN': getCsrfToken()
                },
                body: JSON.stringify(payload)
            })
            .then(function (res) {
                return res.json().then(function (data) { return { ok: res.ok, status: res.status, data: data }; });
            })
            .then(function (result) {
                if (result.ok) {
                    contactForm.reset();
                    if (statusMsg) {
                        statusMsg.style.display = 'block';
                        statusMsg.style.background = '#ecfdf5';
                        statusMsg.style.border = '1px solid #a7f3d0';
                        statusMsg.style.color = '#065f46';
                        statusMsg.textContent = 'Thank you for reaching out! Your message has been received and our team will be in touch shortly.';
                    }
                } else {
                    var errorText = (result.data && (result.data.title || result.data.detail)) || 'Failed to submit contact message. Please verify your details.';
                    if (statusMsg) {
                        statusMsg.style.display = 'block';
                        statusMsg.style.background = '#fee2e2';
                        statusMsg.style.border = '1px solid #fca5a5';
                        statusMsg.style.color = '#991b1b';
                        statusMsg.textContent = errorText;
                    }
                }
            })
            .catch(function () {
                // If fetch fails, fall back to native server post
                contactForm.submit();
            })
            .finally(function () {
                if (btn) btn.disabled = false;
                if (btnText) btnText.textContent = 'Send Message';
            });
        });
    }

    // ── 2. Consultation Booking Form ───────────────────────────────────────────
    var bookingForm = document.getElementById('booking-form');
    if (bookingForm) {
        bookingForm.addEventListener('submit', function (e) {
            e.preventDefault();
            var btn = document.getElementById('booking-submit-btn');
            var btnText = document.getElementById('booking-btn-text');
            var statusMsg = document.getElementById('booking-status-message');

            var fullNameInput = document.getElementById('booking-fullname');
            var emailInput = document.getElementById('booking-email');
            var phoneInput = document.getElementById('booking-phone');
            var dateInput = document.getElementById('booking-datetime');
            var purposeInput = document.getElementById('booking-purpose');
            var orderIdInput = document.getElementById('booking-order-id');

            var preferredDate = dateInput ? dateInput.value : '';
            if (!preferredDate) {
                alert('Please select a preferred date and time.');
                return;
            }

            var chosenTime = new Date(preferredDate);
            if (chosenTime <= new Date()) {
                alert('Preferred consultation date and time must be in the future.');
                return;
            }

            if (btn) btn.disabled = true;
            if (btnText) btnText.textContent = 'Booking Consultation...';
            if (statusMsg) statusMsg.style.display = 'none';

            var payload = {
                fullName: fullNameInput ? fullNameInput.value.trim() : null,
                email: emailInput ? emailInput.value.trim() : null,
                phone: phoneInput ? phoneInput.value.trim() : null,
                preferredDateTime: chosenTime.toISOString(),
                purpose: purposeInput ? purposeInput.value.trim() : '',
                orderId: orderIdInput && orderIdInput.value ? orderIdInput.value.trim() : null
            };

            fetch('/api/bookings', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'X-CSRF-TOKEN': getCsrfToken()
                },
                body: JSON.stringify(payload)
            })
            .then(function (res) {
                return res.json().then(function (data) { return { ok: res.ok, status: res.status, data: data }; });
            })
            .then(function (result) {
                if (result.ok) {
                    bookingForm.reset();
                    if (statusMsg) {
                        statusMsg.style.display = 'block';
                        statusMsg.style.background = '#ecfdf5';
                        statusMsg.style.border = '1px solid #a7f3d0';
                        statusMsg.style.color = '#065f46';
                        statusMsg.textContent = 'Your consultation request has been successfully booked! Our team will contact you to confirm the appointment.';
                    }
                } else {
                    var errorText = (result.data && (result.data.title || result.data.detail)) || 'Could not complete consultation booking. Please check the entered values.';
                    if (statusMsg) {
                        statusMsg.style.display = 'block';
                        statusMsg.style.background = '#fee2e2';
                        statusMsg.style.border = '1px solid #fca5a5';
                        statusMsg.style.color = '#991b1b';
                        statusMsg.textContent = errorText;
                    }
                }
            })
            .catch(function () {
                if (statusMsg) {
                    statusMsg.style.display = 'block';
                    statusMsg.style.background = '#fee2e2';
                    statusMsg.style.border = '1px solid #fca5a5';
                    statusMsg.style.color = '#991b1b';
                    statusMsg.textContent = 'A network error occurred. Please try again.';
                }
            })
            .finally(function () {
                if (btn) btn.disabled = false;
                if (btnText) btnText.textContent = 'Confirm Consultation Booking';
            });
        });
    }

    // ── 3. Product Reviews (Public Listing & Customer Submission) ─────────────
    var reviewsContainer = document.getElementById('product-reviews-container');
    if (reviewsContainer) {
        var productId = reviewsContainer.getAttribute('data-product-id');
        if (productId) {
            loadReviews(productId);
        }
    }

    function renderStars(rating) {
        var stars = '';
        for (var i = 1; i <= 5; i++) {
            stars += i <= rating ? '★' : '☆';
        }
        return stars;
    }

    function escapeHtml(str) {
        if (!str) return '';
        var div = document.createElement('div');
        div.textContent = str;
        return div.innerHTML;
    }

    function loadReviews(productId) {
        fetch('/api/products/' + encodeURIComponent(productId) + '/reviews')
            .then(function (res) {
                if (!res.ok) throw new Error('Failed to load reviews');
                return res.json();
            })
            .then(function (page) {
                var countSpan = document.getElementById('reviews-count');
                if (countSpan) countSpan.textContent = page.total || 0;

                reviewsContainer.innerHTML = '';
                if (!page.items || page.items.length === 0) {
                    var emptyDiv = document.createElement('div');
                    emptyDiv.style.color = 'var(--ss-ink-muted)';
                    emptyDiv.style.fontSize = '0.95rem';
                    emptyDiv.textContent = 'No reviews yet for this product. Be the first to share your thoughts!';
                    reviewsContainer.appendChild(emptyDiv);
                    return;
                }

                page.items.forEach(function (rev) {
                    var item = document.createElement('div');
                    item.style.background = 'var(--ss-surface)';
                    item.style.border = '1px solid var(--ss-border)';
                    item.style.padding = '1rem';
                    item.style.borderRadius = 'var(--ss-radius-sm)';

                    var header = document.createElement('div');
                    header.style.display = 'flex';
                    header.style.justifyContent = 'space-between';
                    header.style.marginBottom = '0.5rem';

                    var author = document.createElement('strong');
                    author.style.fontWeight = '600';
                    author.textContent = rev.clientName || 'Customer';

                    var stars = document.createElement('div');
                    stars.style.color = '#f59e0b';
                    stars.textContent = renderStars(rev.rating);

                    header.appendChild(author);
                    header.appendChild(stars);

                    var titleEl = document.createElement('div');
                    titleEl.style.fontWeight = '600';
                    titleEl.style.marginBottom = '0.25rem';
                    titleEl.style.color = 'var(--ss-ink)';
                    titleEl.textContent = rev.title;

                    var bodyEl = document.createElement('p');
                    bodyEl.style.margin = '0';
                    bodyEl.style.color = 'var(--ss-ink)';
                    bodyEl.textContent = rev.body;

                    item.appendChild(header);
                    item.appendChild(titleEl);
                    item.appendChild(bodyEl);
                    reviewsContainer.appendChild(item);
                });
            })
            .catch(function () {
                reviewsContainer.innerHTML = '<div style="color: var(--ss-ink-muted); font-size: 0.95rem;">Unable to load reviews at this time.</div>';
            });
    }

    var reviewForm = document.getElementById('product-review-form');
    if (reviewForm) {
        reviewForm.addEventListener('submit', function (e) {
            e.preventDefault();
            var prodId = reviewForm.getAttribute('data-product-id');
            var ratingVal = parseInt((document.getElementById('review-rating') || {}).value, 10) || 5;
            var titleVal = ((document.getElementById('review-title') || {}).value || '').trim();
            var bodyVal = ((document.getElementById('review-body') || {}).value || '').trim();
            var feedback = document.getElementById('review-feedback');
            var submitBtn = document.getElementById('review-submit-btn');

            if (submitBtn) submitBtn.disabled = true;
            if (feedback) feedback.style.display = 'none';

            var payload = {
                productId: prodId,
                rating: ratingVal,
                title: titleVal,
                body: bodyVal
            };

            fetch('/api/reviews', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'X-CSRF-TOKEN': getCsrfToken()
                },
                body: JSON.stringify(payload)
            })
            .then(function (res) {
                return res.json().then(function (data) { return { ok: res.ok, status: res.status, data: data }; });
            })
            .then(function (result) {
                if (result.ok) {
                    reviewForm.reset();
                    if (feedback) {
                        feedback.style.display = 'block';
                        feedback.style.background = '#ecfdf5';
                        feedback.style.border = '1px solid #a7f3d0';
                        feedback.style.color = '#065f46';
                        feedback.textContent = 'Thank you! Your review has been submitted and will appear once approved by our moderation team.';
                    }
                } else {
                    var msg = (result.data && (result.data.title || result.data.detail)) || 'Unable to submit review.';
                    if (feedback) {
                        feedback.style.display = 'block';
                        feedback.style.background = '#fee2e2';
                        feedback.style.border = '1px solid #fca5a5';
                        feedback.style.color = '#991b1b';
                        feedback.textContent = msg;
                    }
                }
            })
            .catch(function () {
                if (feedback) {
                    feedback.style.display = 'block';
                    feedback.style.background = '#fee2e2';
                    feedback.style.border = '1px solid #fca5a5';
                    feedback.style.color = '#991b1b';
                    feedback.textContent = 'A network error occurred while submitting your review.';
                }
            })
            .finally(function () {
                if (submitBtn) submitBtn.disabled = false;
            });
        });
    }

})();
