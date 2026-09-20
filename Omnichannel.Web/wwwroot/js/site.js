/* ==========================================================================
   OMNICHANNEL BEAUTY - CLIENT SCRIPTS (site.js)
   Tự động chạy ngầm, không bao giờ bị lộ code ra giao diện HTML
   ========================================================================== */

// 1. Đồng hồ đếm lùi Flash Sale từng giây
function startFlashSaleTimer() {
    var hoursEl = document.getElementById('timerHours');
    var minutesEl = document.getElementById('timerMinutes');
    var secondsEl = document.getElementById('timerSeconds');

    var bannerHoursEl = document.getElementById('bannerHours');
    var bannerMinutesEl = document.getElementById('bannerMinutes');
    var bannerSecondsEl = document.getElementById('bannerSeconds');

    // Nếu trang hiện tại không có đồng hồ Flash Sale thì dừng
    if (!hoursEl && !bannerHoursEl) return;

    var storageKey = 'Omnichannel_Sale_Target_Time';
    var savedTarget = localStorage.getItem(storageKey);
    var targetTime = savedTarget ? parseInt(savedTarget, 10) : 0;

    // Thiết lập mốc đếm ngược 8 giờ 45 phút 12 giây nếu chưa có
    if (!targetTime || targetTime <= Date.now()) {
        targetTime = Date.now() + (8 * 3600 + 45 * 60 + 12) * 1000;
        localStorage.setItem(storageKey, targetTime);
    }

    function update() {
        var remaining = Math.max(0, Math.floor((targetTime - Date.now()) / 1000));
        if (remaining <= 0) {
            targetTime = Date.now() + 8 * 3600 * 1000;
            localStorage.setItem(storageKey, targetTime);
            remaining = 8 * 3600;
        }

        var h = String(Math.floor(remaining / 3600)).padStart(2, '0');
        var m = String(Math.floor((remaining % 3600) / 60)).padStart(2, '0');
        var s = String(remaining % 60).padStart(2, '0');

        if (hoursEl) hoursEl.textContent = h;
        if (minutesEl) minutesEl.textContent = m;
        if (secondsEl) secondsEl.textContent = s;

        if (bannerHoursEl) bannerHoursEl.textContent = h;
        if (bannerMinutesEl) bannerMinutesEl.textContent = m;
        if (bannerSecondsEl) bannerSecondsEl.textContent = s;
    }

    update();
    setInterval(update, 1000);
}

// 2. Thêm nhanh sản phẩm vào giỏ hàng
async function quickAddToCart(productId) {
    try {
        const res = await fetch('/Cart/AddToCart', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ productId: productId, quantity: 1 })
        });

        const data = await res.json();
        if (data.success) {
            const drawerRes = await fetch('/Cart/GetMiniCart');
            const drawerHtml = await drawerRes.text();
            const container = document.getElementById('miniCartContainer');
            if (container) {
                container.innerHTML = drawerHtml;
                const drawerEl = document.getElementById('miniCartDrawer');
                if (drawerEl && window.bootstrap) {
                    const bsDrawer = new bootstrap.Offcanvas(drawerEl);
                    bsDrawer.show();
                }
            }
        } else {
            alert(data.message || 'Lỗi thêm vào giỏ hàng.');
        }
    } catch (err) {
        console.error('Lỗi thêm giỏ hàng:', err);
    }
}

// 3. Xóa sản phẩm khỏi giỏ trượt Mini-Cart Drawer
async function removeDrawerItem(productId) {
    try {
        await fetch('/Cart/RemoveItem', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(productId)
        });

        const drawerRes = await fetch('/Cart/GetMiniCart');
        const container = document.getElementById('miniCartContainer');
        if (container) {
            container.innerHTML = await drawerRes.text();
        }
    } catch (err) {
        console.error('Lỗi xóa sản phẩm:', err);
    }
}

// Tự động kích hoạt khi tài liệu HTML sẵn sàng
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', startFlashSaleTimer);
} else {
    startFlashSaleTimer();
}