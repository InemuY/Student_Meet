// Auto-dismiss alerts after 4 seconds
document.addEventListener('DOMContentLoaded', function () {
    const alerts = document.querySelectorAll('.alert-dismissible');
    alerts.forEach(alert => {
        setTimeout(() => {
            const bsAlert = new bootstrap.Alert(alert);
            bsAlert.close();
        }, 4000);
    });
});

// Предпросмотр фото и отправка формы
function previewPhoto(input, imgId, placeholderId, formId) {
    const file = input.files[0];
    if (!file) return;

    const allowedTypes = ['image/jpeg', 'image/png', 'image/gif', 'image/webp'];
    const maxSize = 5 * 1024 * 1024; // 5 МБ

    if (!allowedTypes.includes(file.type)) {
        alert('Недопустимый формат. Используйте JPG, PNG, GIF или WEBP.');
        input.value = '';
        return;
    }

    if (file.size > maxSize) {
        alert('Файл слишком большой. Максимальный размер — 5 МБ.');
        input.value = '';
        return;
    }

    // Показать предпросмотр
    const reader = new FileReader();
    reader.onload = function (e) {
        const img = document.getElementById(imgId);
        const placeholder = document.getElementById(placeholderId);

        if (img) {
            img.src = e.target.result;
            img.classList.remove('d-none');
        }
        if (placeholder) {
            placeholder.classList.add('d-none');
        }

        // Отправить форму после показа превью
        setTimeout(() => {
            const form = document.getElementById(formId);
            if (form) form.submit();
        }, 300);
    };
    reader.readAsDataURL(file);
}
