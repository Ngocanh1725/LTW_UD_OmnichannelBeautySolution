// ==========================================
// CANMAKE BEAUTY SOLUTION - OPTIMIZED STOREFRONT JS
// Alpine.js Components, 60 FPS Event Loop & Passive Listeners
// ==========================================

function storefrontApp() {
    return {
        cartOpen: false,
        quickViewOpen: false,
        mobileMenuOpen: false,
        init() {
            window.addEventListener('open-cart-drawer', () => { this.cartOpen = true; });
            window.addEventListener('close-cart-drawer', () => { this.cartOpen = false; });
            window.addEventListener('open-quickview-modal', () => { this.quickViewOpen = true; });
            window.addEventListener('close-quickview-modal', () => { this.quickViewOpen = false; });
        }
    };
}

function heroSliderComponent(totalSlides) {
    return {
        activeSlide: 0,
        timer: null,
        totalSlides: totalSlides || 1,
        init() {
            if (this.totalSlides > 1) {
                this.timer = setInterval(() => { this.next(); }, 6000);
            }
        },
        next() {
            this.activeSlide = (this.activeSlide + 1) % this.totalSlides;
        },
        prev() {
            this.activeSlide = (this.activeSlide - 1 + this.totalSlides) % this.totalSlides;
        },
        goTo(idx) {
            this.activeSlide = idx;
        }
    };
}

function categoryFilterComponent(config) {
    config = config || {};
    return {
        mobileFilterOpen: false,
        brandSearchQuery: '',
        minPriceVal: config.minPrice || 50000,
        maxPriceVal: config.maxPrice || 10000000,
        currentViewMode: config.viewMode || 'grid',
        get minPercent() {
            return Math.min(100, Math.max(0, ((this.minPriceVal - 50000) / (10000000 - 50000)) * 100));
        },
        get maxPercent() {
            return Math.min(100, Math.max(0, ((this.maxPriceVal - 50000) / (10000000 - 50000)) * 100));
        },
        formatPrice(val) {
            return new Intl.NumberFormat('vi-VN').format(val) + ' ₫';
        },
        onPriceSliderChange() {
            if (this.minPriceVal > this.maxPriceVal) {
                const temp = this.minPriceVal;
                this.minPriceVal = this.maxPriceVal;
                this.maxPriceVal = temp;
            }
            if (typeof submitFacetedFilter === 'function') {
                submitFacetedFilter();
            }
        }
    };
}

function quickViewItemComponent(initialData) {
    initialData = initialData || {};
    return {
        productId: initialData.productId || null,
        activeImg: initialData.firstImg || '',
        quantity: 1,
        selectedBatchType: 'fresh',
        freshPrice: initialData.freshPrice || 0,
        clearancePrice: initialData.clearancePrice || 0,
        selectedPrice: initialData.freshPrice || 0,
        selectedVariantName: initialData.variantName || '',
        selectBatch(type) {
            this.selectedBatchType = type;
            this.selectedPrice = (type === 'clearance') ? this.clearancePrice : this.freshPrice;
        },
        selectVariant(name, price, img) {
            this.selectedVariantName = name;
            this.freshPrice = price;
            this.clearancePrice = Math.round(price * 0.55 / 1000) * 1000;
            this.selectedPrice = (this.selectedBatchType === 'clearance') ? this.clearancePrice : this.freshPrice;
            if (img) this.activeImg = img;
        },
        addToCartFromQuickView(prodId) {
            const finalPid = prodId || this.productId;
            const batchNote = this.selectedBatchType === 'clearance' ? ' [Lô Cận Date -45%]' : ' [Lô Mới Nhất]';
            const finalName = this.selectedVariantName + batchNote;
            addToCart(finalPid, finalName, this.selectedPrice, this.activeImg, this.quantity);
            if (window.Alpine) {
                const bodyEl = document.querySelector('body');
                if (bodyEl && bodyEl.__x && bodyEl.__x.$data) {
                    bodyEl.__x.$data.quickViewOpen = false;
                    setTimeout(() => {
                        bodyEl.__x.$data.cartOpen = true;
                        if (window.htmx) {
                            htmx.trigger('body', 'reloadMiniCart');
                        }
                    }, 150);
                }
            }
        }
    };
}

let cartItems = [];
let wishlistItems = new Set();

// Khởi tạo Event Delegation toàn cục & Quản lý tương tác hiệu năng cao 60 FPS
document.addEventListener('DOMContentLoaded', () => {
    initGlobalEventDelegation();
    initLiveSearch();
    initMiniCartEvents();
    initScrollAnimations();
    initCountdownTimer();
    
    // 60 FPS Non-blocking Scroll Handler với passive listener và requestAnimationFrame
    let isScrollTicking = false;
    window.addEventListener('scroll', () => {
        if (!isScrollTicking) {
            window.requestAnimationFrame(() => {
                handleHeaderSticky();
                isScrollTicking = false;
            });
            isScrollTicking = true;
        }
    }, { passive: true });
    
    // Tích hợp Toast Notification cho các request từ HTMX
    document.body.addEventListener('htmx:afterRequest', function(evt) {
        if (evt.detail.successful) {
            const path = evt.detail.requestConfig?.path || '';
            if (path.includes('/Cart/Add')) {
                showStorefrontToast('Đã thêm sản phẩm vào giỏ hàng!', 'success');
            } else if (path.includes('/Account/Wishlist')) {
                showStorefrontToast('Đã cập nhật danh sách yêu thích!', 'info');
            } else if (path.includes('/Cart/RemoveItem')) {
                showStorefrontToast('Đã xóa sản phẩm khỏi giỏ hàng.', 'info');
            }
        }
    });
});

// Event Delegation toàn cục: Bắt mọi click từ thẻ sản phẩm (kể cả sau HTMX swap) mà không cần gắn lại listener
function initGlobalEventDelegation() {
    document.addEventListener('click', (e) => {
        // 1. Thêm vào giỏ hàng
        const addBtn = e.target.closest('[data-action="add-to-cart"]');
        if (addBtn) {
            e.preventDefault();
            e.stopPropagation();
            const pid = addBtn.dataset.productId || addBtn.dataset.id;
            const title = addBtn.dataset.title || addBtn.dataset.name || 'Sản phẩm';
            const price = parseFloat(addBtn.dataset.price) || 0;
            const image = addBtn.dataset.image || addBtn.dataset.imageUrl || '';
            const qty = parseInt(addBtn.dataset.quantity, 10) || 1;
            const batchId = addBtn.dataset.batchId || null;
            const variantId = addBtn.dataset.variantId || null;
            addToCart(pid, title, price, image, qty, batchId, variantId);
            return;
        }

        // 2. Mở QuickView
        const qvBtn = e.target.closest('[data-action="quick-view"]');
        if (qvBtn) {
            e.preventDefault();
            e.stopPropagation();
            const pid = qvBtn.dataset.productId || qvBtn.dataset.id;
            if (pid) openQuickView(pid);
            return;
        }

        // 3. Yêu thích (Wishlist)
        const wishBtn = e.target.closest('[data-action="toggle-wishlist"]');
        if (wishBtn) {
            e.preventDefault();
            e.stopPropagation();
            const pid = wishBtn.dataset.productId || wishBtn.dataset.id;
            if (pid) toggleWishlist(pid);
            return;
        }

        // 4. Mở / Đóng MiniCart Drawer
        const openCartBtn = e.target.closest('[data-action="open-mini-cart"]');
        if (openCartBtn) {
            e.preventDefault();
            openMiniCart();
            return;
        }

        const closeCartBtn = e.target.closest('[data-action="close-mini-cart"]');
        if (closeCartBtn) {
            e.preventDefault();
            closeMiniCart();
            return;
        }

        // 5. Đóng QuickView Modal
        const closeQvBtn = e.target.closest('[data-action="close-quick-view"]');
        if (closeQvBtn) {
            e.preventDefault();
            closeQuickViewModal();
            return;
        }
    });
}

function handleHeaderSticky() {
    const header = document.querySelector('.header-glass');
    if (header) {
        if (window.scrollY > 30) {
            header.classList.add('is-scrolled');
        } else {
            header.classList.remove('is-scrolled');
        }
    }
}

function initScrollAnimations() {
    const observer = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                entry.target.classList.add('animate-fade-in-up');
                observer.unobserve(entry.target);
            }
        });
    }, { threshold: 0.1 });

    const observeAll = () => {
        document.querySelectorAll('.animate-on-scroll:not(.animate-fade-in-up)').forEach(el => observer.observe(el));
    };

    observeAll();
    document.body.addEventListener('htmx:afterSwap', observeAll);
    document.body.addEventListener('htmx:load', observeAll);
}

// Live Search hiệu năng cao với Debounce 300ms và fetch API thực tế
function initLiveSearch() {
    const searchInput = document.getElementById('storefrontSearchInput');
    const dropdown = document.getElementById('liveSearchResults');
    const resultsList = document.getElementById('searchResultsList');
    const btnClear = document.getElementById('btnClearSearch');

    if (!searchInput || !dropdown || !resultsList) return;

    let liveSearchDebounceTimer = null;
    let currentAbortController = null;

    searchInput.addEventListener('input', (e) => {
        clearTimeout(liveSearchDebounceTimer);
        const query = e.target.value.trim();

        if (query.length === 0) {
            if (btnClear) btnClear.classList.add('hidden');
            dropdown.classList.remove('is-active');
            if (currentAbortController) currentAbortController.abort();
            return;
        }

        if (btnClear) btnClear.classList.remove('hidden');

        liveSearchDebounceTimer = setTimeout(async () => {
            if (currentAbortController) currentAbortController.abort();
            currentAbortController = new AbortController();

            try {
                resultsList.innerHTML = `
                    <div class="p-4 text-xs text-slate-400 text-center flex items-center justify-center gap-2">
                        <span class="w-4 h-4 border-2 border-magenta-accent border-t-transparent rounded-full animate-spin"></span>
                        <span>Đang tìm kiếm sản phẩm...</span>
                    </div>
                `;
                dropdown.classList.add('is-active');

                const response = await fetch(`/Product/Search?q=${encodeURIComponent(query)}`, {
                    headers: { 'HX-Target': 'searchResultsList' },
                    signal: currentAbortController.signal
                });

                if (response.ok) {
                    const html = await response.text();
                    resultsList.innerHTML = html;
                    dropdown.classList.add('is-active');
                } else {
                    resultsList.innerHTML = `<div class="p-4 text-xs text-slate-400 text-center">Không tìm thấy sản phẩm phù hợp.</div>`;
                }
            } catch (err) {
                if (err.name !== 'AbortError') {
                    resultsList.innerHTML = `<div class="p-4 text-xs text-slate-400 text-center">Không thể tải kết quả tìm kiếm.</div>`;
                }
            }
        }, 300);
    });

    if (btnClear) {
        btnClear.addEventListener('click', () => {
            searchInput.value = '';
            btnClear.classList.add('hidden');
            dropdown.classList.remove('is-active');
            searchInput.focus();
        });
    }

    document.addEventListener('click', (e) => {
        if (!searchInput.contains(e.target) && !dropdown.contains(e.target)) {
            dropdown.classList.remove('is-active');
        }
    });
}

function initMiniCartEvents() {
    const btnOpen = document.getElementById('btnOpenMiniCart');
    if (btnOpen) {
        btnOpen.addEventListener('click', openMiniCart);
    }
}

// MiniCart Drawer: Hợp nhất duy nhất 1 hàm, hỗ trợ Alpine và CustomEvent fallback
function openMiniCart() {
    try {
        const bodyEl = document.querySelector('body');
        if (bodyEl && bodyEl.__x && bodyEl.__x.$data) {
            bodyEl.__x.$data.cartOpen = true;
        } else {
            window.dispatchEvent(new CustomEvent('open-cart-drawer'));
        }
    } catch (e) {
        window.dispatchEvent(new CustomEvent('open-cart-drawer'));
    }
    if (window.htmx) {
        htmx.trigger('body', 'reloadMiniCart');
    }
}
window.openMiniCart = openMiniCart;

function closeMiniCart() {
    try {
        const bodyEl = document.querySelector('body');
        if (bodyEl && bodyEl.__x && bodyEl.__x.$data) {
            bodyEl.__x.$data.cartOpen = false;
        } else {
            window.dispatchEvent(new CustomEvent('close-cart-drawer'));
        }
    } catch (e) {
        window.dispatchEvent(new CustomEvent('close-cart-drawer'));
    }
}
window.closeMiniCart = closeMiniCart;

async function addToCart(arg1, arg2, arg3, arg4, arg5, arg6, arg7) {
    let productId = null;
    let title = 'Sản phẩm';
    let price = 0;
    let imageUrl = '';
    let quantity = 1;
    let batchId = null;
    let variantId = null;

    // Support multiple signatures:
    // 1. (event, productId, quantity)
    // 2. Options object: ({ productId, title, price, imageUrl, quantity, batchId, variantId })
    // 3. Positional: (productId, title, price, imageUrl, quantity, batchId, variantId)
    // 4. Compact: (productId, quantity, batchId, variantId)
    if (typeof arg1 === 'object' && arg1 !== null) {
        if ('preventDefault' in arg1 || 'target' in arg1) {
            arg1.preventDefault?.();
            arg1.stopPropagation?.();
            productId = arg2;
            quantity = arg3 || 1;
        } else {
            productId = arg1.productId || arg1.id;
            title = arg1.title || arg1.name || 'Sản phẩm';
            price = typeof arg1.price === 'number' ? arg1.price : (parseFloat(arg1.price) || 0);
            imageUrl = arg1.imageUrl || arg1.image || '';
            quantity = typeof arg1.quantity === 'number' ? arg1.quantity : (parseInt(arg1.quantity) || 1);
            batchId = arg1.batchId || null;
            variantId = arg1.variantId || null;
        }
    } else {
        productId = arg1;
        if (typeof arg2 === 'string') {
            title = arg2;
            price = typeof arg3 === 'number' ? arg3 : (parseFloat(arg3) || 0);
            imageUrl = arg4 || '';
            quantity = typeof arg5 === 'number' ? arg5 : (parseInt(arg5) || 1);
            batchId = arg6 || null;
            variantId = arg7 || null;
        } else if (typeof arg2 === 'number') {
            quantity = arg2;
            batchId = arg3 || null;
            variantId = arg4 || null;
        }
    }

    if (!productId) {
        console.warn('[addToCart] Missing productId');
        return { success: false, message: 'Missing productId' };
    }

    quantity = Math.max(1, parseInt(quantity) || 1);

    try {
        const tokenEl = document.querySelector('input[name="__RequestVerificationToken"]');
        const token = tokenEl ? tokenEl.value : '';
        const response = await fetch('/Cart/AddToCart', {
            method: 'POST',
            headers: { 
                'Content-Type': 'application/json',
                'RequestVerificationToken': token
            },
            body: JSON.stringify({ 
                productId: productId.toString(), 
                quantity: quantity,
                batchId: batchId || null,
                variantId: variantId || null,
                price: price,
                title: title,
                imageUrl: imageUrl
            })
        });

        if (!response.ok) throw new Error('HTTP ' + response.status);
        const result = await response.json();

        if (result.success) {
            // Cập nhật tất cả badge số lượng giỏ hàng trên Header & Drawer
            const newCount = result.totalItems !== undefined ? result.totalItems : (result.cartCount !== undefined ? result.cartCount : null);
            if (newCount !== null) {
                const badgeElements = document.querySelectorAll('#miniCartCountBadge, [data-cart-count], .cart-badge-count');
                badgeElements.forEach(badge => {
                    badge.innerText = newCount;
                    badge.classList.remove('hidden');
                });
            }

            // Cập nhật tổng tiền hiển thị
            const newTotal = result.cartTotalFormatted || result.subTotal;
            if (newTotal) {
                const totalElements = document.querySelectorAll('#miniCartHeaderTotal, [data-cart-total]');
                totalElements.forEach(el => { el.innerText = newTotal; });
            }

            // Đồng bộ qua HTMX
            if (window.htmx) {
                htmx.trigger('body', 'reloadMiniCart');
            }

            // Bắn Toast thông báo
            showStorefrontToast(result.message || `Đã thêm "${title}" vào giỏ hàng!`, 'success');
            
            // Trượt mở MiniCart Drawer mượt mà
            setTimeout(() => {
                openMiniCart();
            }, 200);
        } else {
            showStorefrontToast(result.message || 'Không thể thêm vào giỏ hàng.', 'error');
        }
    } catch (e) {
        console.warn('Kết nối Cart API thất bại, lưu tạm bộ nhớ client:', e);
        const existing = cartItems.find(i => i.id === productId);
        if (existing) {
            existing.quantity += quantity;
        } else {
            cartItems.push({ id: productId, title: title, price: price, imageUrl: imageUrl, quantity: quantity });
        }
        updateCartBadges();
        showStorefrontToast(`Đã thêm "${title}" vào giỏ hàng!`, 'success');
        setTimeout(() => {
            openMiniCart();
        }, 200);
    }
}
window.addToCart = addToCart;

function updateCartBadges() {
    const totalCount = cartItems.reduce((acc, curr) => acc + curr.quantity, 0);
    const subTotal = cartItems.reduce((acc, curr) => acc + (curr.quantity * curr.price), 0);

    const badgeHeader = document.getElementById('miniCartCountBadge');
    const totalHeader = document.getElementById('miniCartHeaderTotal');
    const drawerCount = document.getElementById('drawerItemsCount');
    const drawerSubTotal = document.getElementById('drawerSubTotal');

    if (badgeHeader) badgeHeader.innerText = totalCount;
    if (totalHeader) totalHeader.innerText = formatCurrency(subTotal);
    if (drawerCount) drawerCount.innerText = totalCount;
    if (drawerSubTotal) drawerSubTotal.innerText = formatCurrency(subTotal);

    updateFreeShippingMeter(subTotal);
}

function updateFreeShippingMeter(subTotal) {
    const threshold = 500000;
    const meterText = document.getElementById('shippingMeterText');
    const remainingEl = document.getElementById('shippingRemainingAmount');
    const progressBar = document.getElementById('shippingProgressBar');

    if (!meterText || !remainingEl || !progressBar) return;

    if (subTotal >= threshold) {
        meterText.innerHTML = `<i class="fa-solid fa-circle-check text-emerald-600 mr-1"></i> Chúc mừng! Bạn được <strong>FREESHIP</strong> toàn quốc!`;
        remainingEl.innerText = "0 ₫";
        progressBar.style.width = "100%";
        progressBar.style.background = "#10b981";
    } else {
        const remaining = threshold - subTotal;
        meterText.innerHTML = `<i class="fa-solid fa-truck-fast text-pink-600 mr-1"></i> Mua thêm để nhận <strong>FREESHIP</strong>`;
        remainingEl.innerText = formatCurrency(remaining);
        const percent = Math.min(100, Math.round((subTotal / threshold) * 100));
        progressBar.style.width = `${percent}%`;
        progressBar.style.background = "linear-gradient(90deg, #f59e0b, #ef4444)";
    }
}

function renderMiniCartItems() {
    const container = document.getElementById('miniCartItemsContainer');
    if (!container) return;

    if (cartItems.length === 0) {
        container.innerHTML = `
            <div class="h-full flex flex-col items-center justify-center text-center p-8 text-slate-400">
                <i class="fa-solid fa-bag-shopping text-5xl mb-3 text-pink-200"></i>
                <p class="text-sm font-semibold text-slate-600">Giỏ hàng của bạn đang trống</p>
                <p class="text-xs text-slate-400 mt-1">Hãy thêm các món mỹ phẩm yêu thích vào giỏ nhé!</p>
            </div>
        `;
        return;
    }

    container.innerHTML = cartItems.map(item => `
        <div class="flex items-center gap-3 p-3 rounded-2xl bg-slate-50 border border-slate-100">
            <img src="${item.imageUrl}" class="h-16 w-16 rounded-xl object-cover flex-shrink-0" alt="${item.title}" />
            <div class="flex-1 truncate">
                <h4 class="text-xs font-bold text-slate-800 truncate">${item.title}</h4>
                <div class="text-xs font-bold text-pink-600 font-mono-numeric mt-1">${formatCurrency(item.price)}</div>
                <div class="flex items-center gap-2 mt-2">
                    <button type="button" onclick="changeQuantity(${item.id}, -1)" class="stepper-btn"><i class="fa-solid fa-minus text-[9px]"></i></button>
                    <span class="text-xs font-bold font-mono-numeric px-2">${item.quantity}</span>
                    <button type="button" onclick="changeQuantity(${item.id}, 1)" class="stepper-btn"><i class="fa-solid fa-plus text-[9px]"></i></button>
                </div>
            </div>
            <button type="button" onclick="removeCartItem(${item.id})" class="text-slate-400 hover:text-rose-600 p-2 transition">
                <i class="fa-regular fa-trash-can text-sm"></i>
            </button>
        </div>
    `).join('');
}

function changeQuantity(id, delta) {
    const item = cartItems.find(i => i.id === id);
    if (!item) return;

    item.quantity += delta;
    if (item.quantity <= 0) {
        removeCartItem(id);
    } else {
        updateCartBadges();
        renderMiniCartItems();
    }
}

function removeCartItem(id) {
    cartItems = cartItems.filter(i => i.id !== id);
    updateCartBadges();
    renderMiniCartItems();
}

// QuickView Controller - Mở modal xem nhanh thông tin sản phẩm
async function openQuickView(productId) {
    if (!productId) return;

    // 1. Mở modal Alpine
    try {
        const bodyEl = document.querySelector('body');
        if (bodyEl && bodyEl.__x && bodyEl.__x.$data) {
            bodyEl.__x.$data.quickViewOpen = true;
        } else {
            window.dispatchEvent(new CustomEvent('open-quickview-modal', { detail: { productId: productId } }));
        }
    } catch (e) {
        window.dispatchEvent(new CustomEvent('open-quickview-modal', { detail: { productId: productId } }));
    }

    // Chờ 1 frame để Alpine kịp render template x-if vào DOM
    await new Promise(r => requestAnimationFrame(() => setTimeout(r, 20)));

    const container = document.getElementById('quickViewModalBody');
    if (!container) {
        // Fallback: nếu trang không có modal, điều hướng trực tiếp
        window.location.href = `/san-pham/${productId}`;
        return;
    }

    // 2. Hiển thị Skeleton Loader
    container.innerHTML = `
        <div class="p-8 sm:p-12 flex flex-col items-center justify-center min-h-[460px] space-y-4 text-center">
            <div class="w-12 h-12 rounded-full border-4 border-pink-100 border-t-magenta-accent animate-spin flex items-center justify-center"></div>
            <div class="text-xs font-bold text-charcoal uppercase tracking-widest animate-pulse">Đang tải thông tin sản phẩm...</div>
            <div class="text-[11px] text-slate-400">Đồng bộ hạn sử dụng FEFO & tồn kho showroom...</div>
        </div>
    `;

    // 3. Fetch dữ liệu HTML từ server
    try {
        const response = await fetch(`/Product/QuickView/${productId}`);
        if (!response.ok) throw new Error('Không thể tải thông tin sản phẩm');
        const html = await response.text();
        container.innerHTML = html;
        if (window.Alpine) {
            window.Alpine.initTree(container);
        }
    } catch (err) {
        console.warn('[QuickView] Lỗi tải dữ liệu:', err);
        container.innerHTML = `
            <div class="p-8 text-center text-slate-500 flex flex-col items-center justify-center min-h-[350px]">
                <i class="fa-solid fa-circle-exclamation text-3xl text-rose-500 mb-2"></i>
                <p class="text-sm font-semibold text-charcoal">Không thể nạp thông tin xem nhanh.</p>
                <p class="text-xs text-slate-400 mt-1">Vui lòng bấm vào nút bên dưới để xem trực tiếp trang sản phẩm.</p>
                <a href="/san-pham/${productId}" class="mt-4 px-6 py-2.5 rounded-full bg-magenta-accent text-white text-xs font-bold hover:bg-[#b0276a] transition shadow-md">
                    Xem Chi Tiết Sản Phẩm
                </a>
            </div>
        `;
    }
}
window.openQuickView = openQuickView;

function closeQuickViewModal() {
    try {
        const bodyEl = document.querySelector('body');
        if (bodyEl && bodyEl.__x && bodyEl.__x.$data) {
            bodyEl.__x.$data.quickViewOpen = false;
        } else {
            window.dispatchEvent(new CustomEvent('close-quickview-modal'));
        }
    } catch (e) {
        window.dispatchEvent(new CustomEvent('close-quickview-modal'));
    }
}
window.closeQuickViewModal = closeQuickViewModal;
window.closeQuickView = closeQuickViewModal;

function switchProductTab(category) {
    const tabs = ['all', 'skincare', 'makeup', 'haircare'];
    tabs.forEach(t => {
        const btn = document.getElementById(`tabBtn${capitalize(t)}`);
        if (btn) {
            if (t === category) {
                btn.className = "px-5 py-2 rounded-full text-xs font-bold bg-white text-slate-900 shadow-sm transition";
            } else {
                btn.className = "px-5 py-2 rounded-full text-xs font-bold text-slate-600 hover:text-slate-900 transition";
            }
        }
    });

    const cards = document.querySelectorAll('.product-card-luxury');
    cards.forEach(card => {
        const cardCat = card.getAttribute('data-category');
        if (category === 'all' || cardCat === category) {
            card.style.display = 'block';
        } else {
            card.style.display = 'none';
        }
    });
}

function capitalize(str) {
    return str.charAt(0).toUpperCase() + str.slice(1);
}

function initWishlistBadge() {
    try {
        const stored = JSON.parse(localStorage.getItem('omni_wishlist_ids') || '[]');
        if (Array.isArray(stored)) {
            stored.forEach(id => wishlistItems.add(Number(id)));
            const badge = document.getElementById('wishlistCountBadge');
            if (badge) badge.innerText = wishlistItems.size;
        }
    } catch (e) {}
}

document.addEventListener('DOMContentLoaded', () => {
    initWishlistBadge();
});

function toggleWishlist(productId) {
    const id = Number(productId);
    const badge = document.getElementById('wishlistCountBadge');
    let inWishlist = false;

    if (wishlistItems.has(id)) {
        wishlistItems.delete(id);
        inWishlist = false;
        showStorefrontToast("Đã xóa khỏi danh sách yêu thích.", "info");
    } else {
        wishlistItems.add(id);
        inWishlist = true;
        showStorefrontToast("Đã thêm vào danh sách yêu thích!", "success");
    }

    // Cập nhật LocalStorage
    try {
        localStorage.setItem('omni_wishlist_ids', JSON.stringify([...wishlistItems]));
    } catch (e) {}

    // Cập nhật badge
    if (badge) badge.innerText = wishlistItems.size;

    // Cập nhật trạng thái active của các nút heart tương ứng
    document.querySelectorAll(`[data-product-id="${id}"] .btn-wishlist-heart, button.btn-wishlist-heart[onclick*="${id}"]`).forEach(btn => {
        if (inWishlist) {
            btn.classList.add('is-active', 'text-rose-500');
        } else {
            btn.classList.remove('is-active', 'text-rose-500');
        }
    });

    // Đồng bộ lên server (nếu đã đăng nhập)
    fetch('/Wishlist/Toggle', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ productId: id })
    }).catch(() => {});
}

function showStorefrontToast(message, type = 'success') {
    const container = document.getElementById('storefrontToastContainer');
    if (!container) return;

    const toast = document.createElement('div');
    toast.className = 'toast-beauty-item flex items-center gap-3 text-xs';
    toast.innerHTML = `
        <i class="${type === 'success' ? 'fa-solid fa-circle-check text-pink-400' : 'fa-solid fa-circle-info text-blue-400'}"></i>
        <span>${message}</span>
    `;
    container.appendChild(toast);

    setTimeout(() => {
        toast.style.transition = 'opacity 0.4s ease, transform 0.4s ease';
        toast.style.opacity = '0';
        toast.style.transform = 'translateX(100%)';
        setTimeout(() => toast.remove(), 400);
    }, 3200);
}

let countdownIntervalId = null;
function initCountdownTimer() {
    const hEl = document.getElementById('countdownHours');
    const mEl = document.getElementById('countdownMinutes');
    const sEl = document.getElementById('countdownSeconds');

    // Không chạy timer nếu trang không có widget countdown
    if (!hEl || !mEl || !sEl) return;

    if (countdownIntervalId) {
        clearInterval(countdownIntervalId);
        countdownIntervalId = null;
    }

    let hours = parseInt(hEl.innerText, 10) || 8;
    let minutes = parseInt(mEl.innerText, 10) || 45;
    let seconds = parseInt(sEl.innerText, 10) || 20;

    countdownIntervalId = setInterval(() => {
        if (!document.getElementById('countdownHours')) {
            clearInterval(countdownIntervalId);
            countdownIntervalId = null;
            return;
        }

        if (seconds > 0) {
            seconds--;
        } else {
            seconds = 59;
            if (minutes > 0) {
                minutes--;
            } else {
                minutes = 59;
                if (hours > 0) hours--;
            }
        }

        hEl.innerText = hours.toString().padStart(2, '0');
        mEl.innerText = minutes.toString().padStart(2, '0');
        sEl.innerText = seconds.toString().padStart(2, '0');
    }, 1000);
}

function formatCurrency(num) {
    return new Intl.NumberFormat('vi-VN').format(num) + ' ₫';
}

// =========================================================================
// PHIÊN C0: SỰ KIỆN CLICK TOÀN CỤC CHO THẺ SẢN PHẨM (CARD-LEVEL CLICK DELEGATION)
// Cho phép click vào bất kỳ vùng nào của thẻ sản phẩm (ngoại trừ các nút bấm hành động)
// để chuyển hướng mượt mà đến trang chi tiết /san-pham/{slug}.
// =========================================================================
document.addEventListener('click', function (e) {
    const card = e.target.closest('.product-card-luxury, .fefo-flash-card, [data-product-url]');
    if (!card) return;

    // Bỏ qua nếu click trúng button, link a, input, select, textarea hoặc các nút hành động
    if (e.target.closest('button, a, input, select, textarea, [data-action], .btn-card-action')) return;

    // Lấy URL sản phẩm từ data-product-url hoặc tìm thẻ link <a> sản phẩm con
    const productUrl = card.getAttribute('data-product-url') ||
        card.querySelector('a[href*="/san-pham/"], a[href*="/Product/Detail/"]')?.getAttribute('href');

    if (productUrl) {
        window.location.href = productUrl;
    }
});


// =========================================================================
// TỰ ĐỘNG GẮN CSRF TOKEN CHO TẤT CẢ REQUEST HTMX
// =========================================================================
document.addEventListener('htmx:configRequest', function (event) {
    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
    if (token) {
        event.detail.headers['RequestVerificationToken'] = token;
    }
});
