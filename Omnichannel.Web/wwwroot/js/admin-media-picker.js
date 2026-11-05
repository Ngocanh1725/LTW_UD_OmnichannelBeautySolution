/**
 * Admin Media Picker Helper
 * Quản lý mở modal, phân trang, upload và chọn ảnh từ CSDL cho Product, ThemeConfig, CMS
 */
(function () {
    let activeInputSelector = null;
    let activePreviewSelector = null;
    let activeUrlInputSelector = '#thumbnailUrlInput';

    window.initMediaPicker = function ({
        btnSelector = '.btn-select-media',
        inputTargetId = '#ThumbnailMediaId',
        previewTargetId = '#imagePreview',
        category = 'PRODUCT'
    } = {}) {
        document.querySelectorAll(btnSelector).forEach(btn => {
            if (btn.dataset.mediaPickerBound === 'true') return;
            btn.dataset.mediaPickerBound = 'true';

            btn.addEventListener('click', function (e) {
                e.preventDefault();
                activeInputSelector = this.getAttribute('data-input') || inputTargetId;
                activePreviewSelector = this.getAttribute('data-preview') || previewTargetId;
                const targetCategory = this.getAttribute('data-category') || category || '';

                const catSelect = document.getElementById('mediaCategorySelect');
                if (catSelect) {
                    const normalizedCat = (targetCategory === 'LOGO' || targetCategory === 'FAVICON' || targetCategory === 'BRAND')
                        ? 'THEME'
                        : targetCategory;
                    const hasOpt = Array.from(catSelect.options).some(o => o.value === normalizedCat);
                    catSelect.value = hasOpt ? normalizedCat : '';
                }

                if (typeof window.openMediaPicker === 'function') {
                    window.openMediaPicker(function (selectedItem) {
                        if (!selectedItem) return;
                        const resolvedUrl = selectedItem.publicUrl || ('/media/' + selectedItem.id);

                        if (activeInputSelector) {
                            const hiddenEl = document.querySelector(activeInputSelector);
                            if (hiddenEl) {
                                hiddenEl.value = selectedItem.id || '';
                                hiddenEl.dispatchEvent(new Event('change'));
                            }
                        }

                        const urlInputEl = document.querySelector(activeUrlInputSelector);
                        if (urlInputEl) {
                            urlInputEl.value = resolvedUrl;
                            urlInputEl.dispatchEvent(new Event('input'));
                        }

                        if (activePreviewSelector) {
                            const previewImg = document.querySelector(activePreviewSelector);
                            if (previewImg) {
                                previewImg.src = resolvedUrl;
                            }
                        }
                    });
                }
            });
        });
    };

    document.addEventListener('DOMContentLoaded', function () {
        window.initMediaPicker();
    });
})();
