let cartItems = [];
let wishlistItems = new Set();

const mockProducts = [
    {
        id: 1,
        name: "Nước Tẩy Trang L'Oréal Paris Micellar 400ml",
        price: 189000,
        originalPrice: 219000,
        category: "skincare",
        brand: "L'Oréal Paris",
        image: "https://images.unsplash.com/photo-1556228720-195a672e8a03?w=500&h=500&fit=crop",
        unit: "Chai 400ml",
        batchFar: "LOT-LOR-26A01 (Hạn dùng: 2028 - An toàn)",
        batchNear: "LOT-LOR-24E09 (Cận date 38 ngày - Sale 40%)",
        desc: "Công nghệ Micellar dịu nhẹ làm sạch sâu lớp trang điểm cứng đầu và bụi mịn PM2.5."
    },
    {
        id: 2,
        name: "Kem Rửa Mặt Dưỡng Ẩm Hada Labo 80g",
        price: 85000,
        originalPrice: 99000,
        category: "skincare",
        brand: "Rohto-Mentholatum",
        image: "https://images.unsplash.com/photo-1556228722-d0b5be7490bf?w=500&h=500&fit=crop",
        unit: "Tuýp 80g",
        batchFar: "LOT-ROH-26B02 (Hạn dùng: 2028 - An toàn)",
        batchNear: "LOT-ROH-24F10 (Cận date 38 ngày - Sale 35%)",
        desc: "Phức hợp HA, SHA, Nano HA dưỡng ẩm đa tầng, rửa sạch sâu không gây căng rát."
    },
    {
        id: 3,
        name: "Serum Phục Hồi La Roche-Posay Hyalu B5 30ml",
        price: 890000,
        originalPrice: 985000,
        category: "skincare",
        brand: "L'Oréal Paris",
        image: "https://images.unsplash.com/photo-1620916566398-39f1143ab7be?w=500&h=500&fit=crop",
        unit: "Chai 30ml",
        batchFar: "LOT-LRP-26C03 (Hạn dùng: 2028 - An toàn)",
        batchNear: "LOT-LRP-24G11 (Cận date 38 ngày - Sale 30%)",
        desc: "Chứa Panthenol B5 5% và Pure Hyaluronic Acid giúp tái tạo hàng rào ẩm cho da yếu nhạy cảm."
    },
    {
        id: 4,
        name: "Sữa Chống Nắng Skin Aqua Tone Up UV Milk 50g",
        price: 165000,
        originalPrice: 185000,
        category: "skincare",
        brand: "Rohto-Mentholatum",
        image: "https://images.unsplash.com/photo-1598440947619-2c35fc9aa908?w=500&h=500&fit=crop",
        unit: "Chai 50g",
        batchFar: "LOT-AQU-26D04 (Hạn dùng: 2028 - An toàn)",
        batchNear: "LOT-AQU-24H12 (Cận date 38 ngày - Sale 30%)",
        desc: "Hiệu chỉnh sắc da ánh tím Lavender trong veo, kiềm dầu khô thoáng suốt cả ngày."
    },
    {
        id: 5,
        name: "Son Kem Lì Rom&nd Zero Velvet Tint #02",
        price: 159000,
        originalPrice: 179000,
        category: "makeup",
        brand: "Amorepacific",
        image: "https://images.unsplash.com/photo-1586495777744-4413f21062fa?w=500&h=500&fit=crop",
        unit: "Thỏi 5.5g",
        batchFar: "LOT-ROM-26E05 (Hạn dùng: 2028 - An toàn)",
        batchNear: "LOT-ROM-24I01 (Cận date 38 ngày - Sale 40%)",
        desc: "Chất son velvet xốp mịn làm mờ rãnh môi, sắc đỏ cam gạch tôn da rực rỡ."
    },
    {
        id: 6,
        name: "Phấn Nước Kiềm Dầu Laneige Neo Cushion 15g",
        price: 580000,
        originalPrice: 650000,
        category: "makeup",
        brand: "Amorepacific",
        image: "https://images.unsplash.com/photo-1512496015851-a90fb38ba796?w=500&h=500&fit=crop",
        unit: "Hộp 15g #21N",
        batchFar: "LOT-LAN-26F06 (Hạn dùng: 2028 - An toàn)",
        batchNear: "LOT-LAN-24J02 (Cận date 38 ngày - Sale 25%)",
        desc: "Hạt phấn siêu mịn che phủ khuyết điểm bền màu 24h, mỏng nhẹ không dính khẩu trang."
    },
    {
        id: 7,
        name: "Dầu Dưỡng Tóc L'Oréal Elseve Oil 100ml",
        price: 229000,
        originalPrice: 259000,
        category: "haircare",
        brand: "L'Oréal Paris",
        image: "https://images.unsplash.com/photo-1608248597359-269201991823?w=500&h=500&fit=crop",
        unit: "Chai 100ml",
        batchFar: "LOT-ELS-26G07 (Hạn dùng: 2028 - An toàn)",
        batchNear: "LOT-ELS-24K03 (Cận date 38 ngày - Sale 30%)",
        desc: "Chiết xuất 6 loại hoa quý nuôi dưỡng ngọn tóc suôn mượt không gây bết dính."
    }
];

document.addEventListener('DOMContentLoaded', () => {
    initLiveSearch();
    initMiniCartEvents();
    initScrollAnimations();
    window.addEventListener('scroll', handleHeaderSticky);
    
    // Tích hợp Toast Notification cho các request từ HTMX
    document.body.addEventListener('htmx:afterRequest', function(evt) {
        if (evt.detail.successful) {
            const path = evt.detail.requestConfig.path;
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

    document.querySelectorAll('.animate-on-scroll').forEach(el => observer.observe(el));
}

function initLiveSearch() {
    const searchInput = document.getElementById('storefrontSearchInput');
    const dropdown = document.getElementById('liveSearchResults');
    const resultsList = document.getElementById('searchResultsList');
    const btnClear = document.getElementById('btnClearSearch');

    if (!searchInput || !dropdown || !resultsList) return;

    searchInput.addEventListener('input', (e) => {
        const query = e.target.value.trim().toLowerCase();
        if (query.length > 0) {
            btnClear.classList.remove('hidden');
            const matched = mockProducts.filter(p => p.name.toLowerCase().includes(query) || p.brand.toLowerCase().includes(query));
            renderSearchResults(matched);
            dropdown.classList.add('is-active');
        } else {
            btnClear.classList.add('hidden');
            dropdown.classList.remove('is-active');
        }
    });

    btnClear.addEventListener('click', () => {
        searchInput.value = '';
        btnClear.classList.add('hidden');
        dropdown.classList.remove('is-active');
        searchInput.focus();
    });

    document.addEventListener('click', (e) => {
        if (!searchInput.contains(e.target) && !dropdown.contains(e.target)) {
            dropdown.classList.remove('is-active');
        }
    });
}

function renderSearchResults(items) {
    const resultsList = document.getElementById('searchResultsList');
    if (!resultsList) return;

    if (items.length === 0) {
        resultsList.innerHTML = `<div class="p-4 text-xs text-slate-400 text-center">Không tìm thấy sản phẩm phù hợp.</div>`;
        return;
    }

    resultsList.innerHTML = items.map(item => `
        <div onclick="openQuickView(${item.id})" class="flex items-center gap-3 p-2 rounded-xl hover:bg-pink-50/60 cursor-pointer transition">
            <img src="${item.image}" class="h-10 w-10 rounded-lg object-cover flex-shrink-0" alt="${item.name}" />
            <div class="truncate flex-1">
                <div class="text-xs font-bold text-slate-800 truncate">${item.name}</div>
                <div class="text-[11px] text-pink-600 font-bold font-mono-numeric">${formatCurrency(item.price)}</div>
            </div>
            <span class="text-[10px] text-slate-400 font-semibold px-2 py-0.5 rounded bg-slate-100">${item.brand}</span>
        </div>
    `).join('');
}

function initMiniCartEvents() {
    const btnOpen = document.getElementById('btnOpenMiniCart');
    if (btnOpen) {
        btnOpen.addEventListener('click', openMiniCart);
    }
}

function openMiniCart() {
    document.getElementById('miniCartDrawer')?.classList.add('is-open');
    document.getElementById('miniCartOverlay')?.classList.add('is-active');
    document.body.style.overflow = 'hidden';
    renderMiniCartItems();
}

function closeMiniCart() {
    document.getElementById('miniCartDrawer')?.classList.remove('is-open');
    document.getElementById('miniCartOverlay')?.classList.remove('is-active');
    document.body.style.overflow = '';
}

function addToCart(productId, title, price, imageUrl) {
    const existing = cartItems.find(i => i.id === productId);
    if (existing) {
        existing.quantity += 1;
    } else {
        cartItems.push({
            id: productId,
            title: title,
            price: price,
            imageUrl: imageUrl,
            quantity: 1
        });
    }

    updateCartBadges();
    showStorefrontToast(`Đã thêm "${title}" vào giỏ hàng!`, 'success');
}

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

function openQuickView(productId) {
    const product = mockProducts.find(p => p.id === productId);
    if (!product) return;

    const modal = document.getElementById('quickViewModal');
    const modalBody = document.getElementById('quickViewModalBody');

    if (!modal || !modalBody) return;

    modalBody.innerHTML = `
        <div class="aspect-square rounded-2xl overflow-hidden bg-pink-50 relative">
            <img src="${product.image}" class="w-full h-full object-cover" alt="${product.name}" />
            <span class="badge-fefo-fresh absolute top-3 left-3">Chính Hãng 100%</span>
        </div>
        <div class="flex flex-col justify-between space-y-4">
            <div>
                <span class="text-[11px] font-bold text-pink-600 uppercase tracking-widest">${product.brand}</span>
                <h3 class="font-serif-luxury text-xl font-bold text-slate-900 mt-1">${product.name}</h3>
                <p class="text-xs text-slate-500 mt-2 leading-relaxed">${product.desc}</p>
                <div class="flex items-baseline gap-3 mt-3">
                    <span class="text-2xl font-bold text-slate-900 font-mono-numeric">${formatCurrency(product.price)}</span>
                    <span class="text-sm text-slate-400 line-through font-mono-numeric">${formatCurrency(product.originalPrice)}</span>
                </div>
            </div>

            <!-- FEFO Batch Selection -->
            <div class="space-y-2">
                <label class="text-xs font-bold text-slate-700 block">Chọn Lô Hạn Sử Dụng (Thuật toán FEFO):</label>
                <div class="space-y-2">
                    <div class="fefo-batch-pill is-selected" onclick="selectBatchPill(this, ${product.price})">
                        <div class="flex items-center justify-between text-xs font-bold text-slate-800">
                            <span>Lô Date Xa (Khuyên Dùng)</span>
                            <span class="text-emerald-600 font-mono-numeric">${formatCurrency(product.price)}</span>
                        </div>
                        <div class="text-[11px] text-slate-500 mt-0.5">${product.batchFar}</div>
                    </div>
                    <div class="fefo-batch-pill" onclick="selectBatchPill(this, ${Math.round(product.price * 0.6)})">
                        <div class="flex items-center justify-between text-xs font-bold text-slate-800">
                            <span>Lô Cận Date (Deal Tiết Kiệm -40%)</span>
                            <span class="text-rose-600 font-mono-numeric">${formatCurrency(Math.round(product.price * 0.6))}</span>
                        </div>
                        <div class="text-[11px] text-slate-500 mt-0.5">${product.batchNear}</div>
                    </div>
                </div>
            </div>

            <!-- Actions -->
            <div class="pt-3">
                <button type="button" onclick="addToCart(${product.id}, '${product.name}', ${product.price}, '${product.image}'); closeQuickViewModal();" class="w-full py-3.5 rounded-full bg-pink-600 text-white font-bold text-xs uppercase tracking-wider hover:bg-pink-700 shadow-lg shadow-pink-600/30 transition flex items-center justify-center gap-2">
                    <i class="fa-solid fa-cart-plus"></i> Thêm Vào Giỏ Hàng Ngay
                </button>
            </div>
        </div>
    `;

    modal.classList.remove('hidden');
    modal.classList.add('flex');
    document.body.style.overflow = 'hidden';
}

function selectBatchPill(element, updatedPrice) {
    document.querySelectorAll('.fefo-batch-pill').forEach(el => el.classList.remove('is-selected'));
    element.classList.add('is-selected');
}

function closeQuickViewModal() {
    const modal = document.getElementById('quickViewModal');
    if (modal) {
        modal.classList.add('hidden');
        modal.classList.remove('flex');
        document.body.style.overflow = '';
    }
}

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

function toggleWishlist(productId) {
    const badge = document.getElementById('wishlistCountBadge');
    if (wishlistItems.has(productId)) {
        wishlistItems.delete(productId);
        showStorefrontToast("Đã xóa khỏi danh sách yêu thích.", "info");
    } else {
        wishlistItems.add(productId);
        showStorefrontToast("Đã thêm vào danh sách yêu thích!", "success");
    }

    if (badge) badge.innerText = wishlistItems.size;
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

function initCountdownTimer() {
    let hours = 8, minutes = 45, seconds = 20;
    setInterval(() => {
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

        const hEl = document.getElementById('countdownHours');
        const mEl = document.getElementById('countdownMinutes');
        const sEl = document.getElementById('countdownSeconds');

        if (hEl) hEl.innerText = hours.toString().padStart(2, '0');
        if (mEl) mEl.innerText = minutes.toString().padStart(2, '0');
        if (sEl) sEl.innerText = seconds.toString().padStart(2, '0');
    }, 1000);
}

function formatCurrency(num) {
    return new Intl.NumberFormat('vi-VN').format(num) + ' ₫';
}