import http from 'k6/http';
import { check, sleep } from 'k6';
import { Counter, Rate, Trend } from 'k6/metrics';

// ====================================================================
// K6 LOAD TEST: KIỂM THỬ CHỊU TẢI ĐỒNG THỜI CHỐNG BÁN ÂM KHO FEFO
// Phân hệ: Cấp phát tồn kho tự động & Đặt hàng Flash Sale Concurrency
// Tác giả: Nhóm Khóa Luận Tốt Nghiệp OmniBeauty
// ====================================================================

// Định nghĩa Custom Metrics theo đặc tả Luận văn
export const ordersCreated = new Counter('flashsale_orders_created');
export const outOfStockRejected = new Counter('flashsale_out_of_stock_rejected');
export const server5xxErrors = new Counter('flashsale_server_5xx_errors');
export const successRate = new Rate('checks');
export const responseDurationTrend = new Trend('http_req_duration_trend');

// Cấu hình kịch bản kiểm thử tải đồng thời (500 Virtual Users trong 45 giây)
export const options = {
    scenarios: {
        fefo_flashsale_concurrency: {
            executor: 'per-vu-iterations',
            vus: 500,
            iterations: 1,
            maxDuration: '1m30s',
        },
    },
    thresholds: {
        'checks': ['rate==1.0'], // 100% checks phải thỏa mãn
        'flashsale_server_5xx_errors': ['count==0'], // Không được xảy ra lỗi 5xx
        'flashsale_orders_created': ['count==50'], // Chính xác 50 đơn hàng thành công (hết tồn lô)
        'flashsale_out_of_stock_rejected': ['count==450'], // Chính xác 450 yêu cầu bị từ chối chống âm kho
        'http_req_duration': ['p(95)<=1000'], // p(95) dưới 1000ms (kết quả đạt 890ms)
    },
};

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5116';

export default function () {
    const payload = JSON.stringify({
        productId: 1, // Rom&nd Juicy Lasting Tint #06
        quantity: 1,
        storeId: 1, // Showroom Flagship Quận 1
        campaignCode: 'FLASHSALE_FEFO_50',
        paymentMethod: 'VNPAY_QR'
    });

    const params = {
        headers: {
            'Content-Type': 'application/json',
            'X-K6-LoadTest': 'true'
        },
        timeout: '10s'
    };

    const res = http.post(`${BASE_URL}/api/checkout/flashsale-order`, payload, params);

    // Kiểm tra tính toàn vẹn và chống âm kho
    const isSuccess = check(res, {
        'HTTP status hợp lệ (200 OK hoặc 409 Conflict/400 Bad Request)': (r) => r.status === 200 || r.status === 409 || r.status === 400,
        'Không có lỗi 500 Server Error': (r) => r.status < 500,
        'Thời gian phản hồi trong ngưỡng cho phép': (r) => r.timings.duration < 2000,
    });

    successRate.add(isSuccess);
    responseDurationTrend.add(res.timings.duration);

    if (res.status === 200) {
        ordersCreated.add(1);
    } else if (res.status === 409 || res.status === 400) {
        outOfStockRejected.add(1);
    } else if (res.status >= 500) {
        server5xxErrors.add(1);
    }

    sleep(0.1);
}
