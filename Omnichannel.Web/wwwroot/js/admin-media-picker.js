/**
 * Admin Media Picker Helper
 * Quản lý mở modal, phân trang, upload và chọn ảnh từ CSDL
 */

let activePickerTargetInput = null;
let activePickerTargetPreview = null;
let activePickerCategory = 'PRODUCT';
let currentSelectedMediaId = null;
let currentSelectedMediaUrl = null;

window.initMediaPicker = function({ btnSelector = '.btn-select-media', inputTargetId, previewTargetId, category = 'PRODUCT' } = {}) {
    document.querySelectorAll(btnSelector).forEach(btn => {
        btn.addEventListener('click', function(e) {
            e.preventDefault();
            activePickerTargetInput = this.getAttribute('data-input') || inputTargetId;
            activePickerTargetPreview = this.getAttribute('data-preview') || previewTargetId;
            activePickerCategory = this.getAttribute('data-category') || category;

            openMediaPickerModal();
        });
    });
};

function openMediaPickerModal() {
    const modal = document.getElementById('mediaPickerModal');
    if (!modal) return;

    modal.classList.remove('hidden');
    document.getElementById('pickerCategorySelect').value = activePickerCategory || 'ALL';
    loadMediaPickerItems(1);
}

function closeMediaPickerModal() {
    const modal = document.getElementById('mediaPickerModal');
    if (modal) modal.classList.add('hidden');
    currentSelectedMediaId = null;
    currentSelectedMediaUrl = null;
    const confirmBtn = document.getElementById('btnConfirmPicker');
    if (confirmBtn) confirmBtn.disabled = true;
    const info = document.getElementById('pickerSelectedInfo');
    if (info) info.innerText = 'Chưa chọn ảnh nào';
}

function switchMediaTab(tab) {
    const tabLib = document.getElementById('mediaTabLibrary');
    const tabUpload = document.getElementById('mediaTabUpload');
    const btnLib = document.getElementById('tabLibraryBtn');
    const btnUpload = document.getElementById('tabUploadBtn');

    if (tab === 'library') {
        tabLib.classList.remove('hidden');
        tabUpload.classList.add('hidden');
        btnLib.classList.add('border-rose-600', 'text-rose-600');
        btnLib.classList.remove('border-transparent', 'text-gray-500');
        btnUpload.classList.remove('border-rose-600', 'text-rose-600');
        btnUpload.classList.add('border-transparent', 'text-gray-500');
        loadMediaPickerItems(1);
    } else {
        tabLib.classList.add('hidden');
        tabUpload.classList.remove('hidden');
        btnUpload.classList.add('border-rose-600', 'text-rose-600');
        btnUpload.classList.remove('border-transparent', 'text-gray-500');
        btnLib.classList.remove('border-rose-600', 'text-rose-600');
        btnLib.classList.add('border-transparent', 'text-gray-500');
    }
}

function loadMediaPickerItems(page = 1) {
    const category = document.getElementById('pickerCategorySelect').value;
    const keyword = document.getElementById('pickerSearchInput').value.trim();
    const grid = document.getElementById('pickerGridContainer');
    const loading = document.getElementById('pickerLoading');
    const empty = document.getElementById('pickerEmpty');
    const info = document.getElementById('pickerPaginationInfo');
    const paginationBtns = document.getElementById('pickerPaginationBtns');

    grid.innerHTML = '';
    loading.classList.remove('hidden');
    empty.classList.add('hidden');

    fetch(`/admin/media/api/list?PageIndex=${page}&PageSize=18&Category=${encodeURIComponent(category)}&Keyword=${encodeURIComponent(keyword)}`)
        .then(r => r.json())
        .then(data => {
            loading.classList.add('hidden');
            if (!data.success || !data.items || data.items.length === 0) {
                empty.classList.remove('hidden');
                info.innerText = 'Trang 0 / 0';
                paginationBtns.innerHTML = '';
                return;
            }

            const totalPages = Math.ceil(data.totalCount / 18);
            info.innerText = `Trang ${page} / ${totalPages} (Tổng ${data.totalCount} ảnh)`;

            data.items.forEach(item => {
                const card = document.createElement('div');
                card.className = `group relative aspect-square rounded-xl overflow-hidden border-2 cursor-pointer transition p-1 bg-gray-50 dark:bg-gray-900 ${currentSelectedMediaId === item.id ? 'border-rose-600 ring-2 ring-rose-500' : 'border-gray-200 dark:border-gray-700 hover:border-gray-400'}`;
                card.onclick = () => selectMediaPickerItem(item.id, item.url, item.fileName, card);

                card.innerHTML = `
                    <img src="${item.url}" alt="${item.fileName}" loading="lazy" class="w-full h-full object-cover rounded-lg" />
                    <span class="absolute bottom-1 right-1 text-[9px] bg-black/60 text-white px-1 py-0.5 rounded backdrop-blur-xs">${Math.round(item.fileSize / 1024)}KB</span>
                `;
                grid.appendChild(card);
            });

            // Pagination Buttons
            paginationBtns.innerHTML = '';
            for (let p = 1; p <= totalPages; p++) {
                if (totalPages > 6 && Math.abs(p - page) > 2 && p !== 1 && p !== totalPages) continue;
                const btn = document.createElement('button');
                btn.type = 'button';
                btn.innerText = p;
                btn.className = `px-2 py-1 rounded text-[11px] font-medium transition ${p === page ? 'bg-rose-600 text-white' : 'bg-gray-100 dark:bg-gray-700 text-gray-700 dark:text-gray-300 hover:bg-gray-200'}`;
                btn.onclick = () => loadMediaPickerItems(p);
                paginationBtns.appendChild(btn);
            }
        })
        .catch(() => {
            loading.classList.add('hidden');
            empty.classList.remove('hidden');
        });
}

function selectMediaPickerItem(id, url, fileName, element) {
    currentSelectedMediaId = id;
    currentSelectedMediaUrl = url;

    document.querySelectorAll('#pickerGridContainer > div').forEach(el => {
        el.classList.remove('border-rose-600', 'ring-2', 'ring-rose-500');
        el.classList.add('border-gray-200', 'dark:border-gray-700');
    });

    element.classList.add('border-rose-600', 'ring-2', 'ring-rose-500');
    element.classList.remove('border-gray-200', 'dark:border-gray-700');

    document.getElementById('pickerSelectedInfo').innerHTML = `<strong class="text-rose-600">${fileName}</strong> (ID: ${id})`;
    document.getElementById('btnConfirmPicker').disabled = false;
}

function confirmMediaPickerSelection() {
    if (!currentSelectedMediaId) return;

    if (activePickerTargetInput) {
        const input = document.querySelector(activePickerTargetInput);
        if (input) {
            input.value = currentSelectedMediaId;
            input.dispatchEvent(new Event('change'));
        }
    }

    if (activePickerTargetPreview) {
        const preview = document.querySelector(activePickerTargetPreview);
        if (preview) {
            preview.src = currentSelectedMediaUrl || `/media/${currentSelectedMediaId}`;
        }
    }

    closeMediaPickerModal();
}

function previewUploadFile(files) {
    if (!files || files.length === 0) return;
    const file = files[0];
    const previewContainer = document.getElementById('modalUploadPreviewContainer');
    const previewImg = document.getElementById('modalUploadPreviewImg');
    const fileName = document.getElementById('modalUploadFileName');
    const fileSize = document.getElementById('modalUploadFileSize');

    fileName.innerText = file.name;
    fileSize.innerText = Math.round(file.size / 1024) + ' KB';

    const reader = new FileReader();
    reader.onload = e => {
        previewImg.src = e.target.result;
        previewContainer.classList.remove('hidden');
    };
    reader.readAsDataURL(file);
}

function submitModalUpload() {
    const input = document.getElementById('modalUploadInput');
    if (!input.files || input.files.length === 0) return;

    const formData = new FormData();
    formData.append('files', input.files[0]);
    formData.append('category', activePickerCategory || 'PRODUCT');

    fetch('/admin/media/api/upload', {
        method: 'POST',
        body: formData
    })
    .then(r => r.json())
    .then(data => {
        if (data.success && data.items && data.items.length > 0) {
            const uploaded = data.items[0];
            switchMediaTab('library');
            currentSelectedMediaId = uploaded.id;
            currentSelectedMediaUrl = uploaded.url;
            confirmMediaPickerSelection();
        } else {
            alert('Upload thất bại: ' + (data.message || 'Lỗi không xác định'));
        }
    })
    .catch(() => alert('Lỗi kết nối khi upload ảnh.'));
}

// Tự động khởi tạo khi DOM ready
document.addEventListener('DOMContentLoaded', function() {
    window.initMediaPicker();
});
