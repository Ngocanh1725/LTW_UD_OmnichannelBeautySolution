/* ==========================================================================
   OMNICHANNEL BEAUTY - MATRIX & USER AJAX ENGINE
   ========================================================================== */

function showToast(message, isSuccess = true) {
    let container = document.getElementById('toast-container');
    if (!container) {
        container = document.createElement('div');
        container.id = 'toast-container';
        container.className = 'beauty-toast-container';
        document.body.appendChild(container);
    }

    const toast = document.createElement('div');
    toast.className = `alert ${isSuccess ? 'alert-success border-success' : 'alert-danger border-danger'} alert-dismissible fade show shadow-lg rounded-4 py-3 px-4 d-flex align-items-center gap-3`;
    toast.style.background = isSuccess ? '#FDF2F8' : '#FEF2F2';
    toast.style.borderColor = isSuccess ? '#FBCFE8' : '#FECACA';
    toast.style.color = isSuccess ? '#831843' : '#991B1B';

    toast.innerHTML = `
        <i class="fa-solid ${isSuccess ? 'fa-circle-check text-success' : 'fa-circle-exclamation text-danger'} fs-4"></i>
        <div class="fw-semibold small">${message}</div>
        <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
    `;

    container.appendChild(toast);

    setTimeout(() => {
        toast.classList.remove('show');
        setTimeout(() => toast.remove(), 300);
    }, 4000);
}

// Hàm chọn / bỏ chọn toàn bộ quyền của một Module
function toggleModuleCheckboxes(moduleCode, isChecked) {
    const checkboxes = document.querySelectorAll(`input[data-module='${moduleCode}']`);
    checkboxes.forEach(cb => cb.checked = isChecked);
}

// Lưu ma trận phân quyền cho Role
async function saveRoleMatrix(roleId) {
    const checkboxes = document.querySelectorAll('.perm-checkbox:checked');
    const selectedIds = Array.from(checkboxes).map(cb => parseInt(cb.value));

    const saveBtn = document.getElementById('btnSaveMatrix');
    const originalText = saveBtn.innerHTML;
    saveBtn.disabled = true;
    saveBtn.innerHTML = '<i class="fa-solid fa-spinner fa-spin me-2"></i>Đang lưu & Invalidate Cache...';

    try {
        const response = await fetch('/Admin/PermissionManagement/SaveRoleMatrix', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value || ''
            },
            body: JSON.stringify({ roleId: roleId, permissionIds: selectedIds })
        });

        const data = await response.json();

        if (response.ok && data.success) {
            showToast(data.message, true);
        } else {
            showToast(data.message || 'Lỗi khi cập nhật ma trận phân quyền.', false);
        }
    } catch (err) {
        console.error(err);
        showToast('Lỗi mạng hoặc kết nối máy chủ.', false);
    } finally {
        saveBtn.disabled = false;
        saveBtn.innerHTML = originalText;
    }
}