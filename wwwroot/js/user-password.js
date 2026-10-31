(() => {
    function setVisibility(button, visible) {
        const input = document.getElementById(button.dataset.passwordToggle);
        input.type = visible ? 'text' : 'password';
        button.textContent = visible ? 'ซ่อนรหัสผ่าน' : 'แสดงรหัสผ่าน';
        button.setAttribute('aria-pressed', String(visible));
    }

    document.querySelectorAll('[data-password-toggle]').forEach(button => {
        button.addEventListener('click', () => {
            setVisibility(button, button.getAttribute('aria-pressed') !== 'true');
        });
        button.closest('form').addEventListener('reset', () => setVisibility(button, false));
    });

    $('#successModal').on('show.bs.modal', () => {
        document.querySelectorAll('[data-password-toggle]').forEach(button => setVisibility(button, false));
    });
})();
