// Session expiry is supplied by the server; polling never renews the cookie.
$(function () {
    const element = document.getElementById('sessionModal');
    if (!element) return;
    const modal = new bootstrap.Modal(element);
    const form = document.getElementById('sessionLogoutForm');
    const token = $(form).find('input[name="__RequestVerificationToken"]').val();
    const channel = typeof BroadcastChannel !== 'undefined' ? new BroadcastChannel('tbr-session') : null;
    let deadline = null, warningSeconds = 30, checking = false, renewing = false, leaving = false, revision = 0;

    function login() {
        if (leaving) return;
        leaving = true;
        window.location.replace(element.dataset.loginUrl);
    }
    function update(data, started) {
        deadline = performance.now() + Math.max(0, data.remainingSeconds * 1000 - (performance.now() - started));
        warningSeconds = data.warningSeconds;
        $('#sessionError').empty();
        render();
    }
    function render() {
        if (deadline === null || leaving) return;
        const seconds = Math.max(0, Math.ceil((deadline - performance.now()) / 1000));
        $('#sessionCountdown').text(`${Math.floor(seconds / 60).toString().padStart(2, '0')}:${(seconds % 60).toString().padStart(2, '0')}`);
        if (seconds <= warningSeconds) modal.show(); else modal.hide();
        // Recheck first in case another tab has renewed the shared cookie.
        if (seconds === 0 && !renewing) check();
    }
    function check() {
        if (checking || renewing || leaving) return;
        checking = true;
        const requestRevision = revision;
        const started = performance.now();
        $.ajax({ url: element.dataset.statusUrl, cache: false, dataType: 'json', timeout: 10000 })
            .done(data => {
                if (requestRevision !== revision) return;
                if (data.remainingSeconds <= 0) login(); else update(data, started);
            })
            .fail(xhr => {
                if (requestRevision !== revision) return;
                if (xhr.status === 401) login();
                else {
                    $('#sessionError').text('ตรวจสอบอายุการเข้าสู่ระบบไม่ได้ กรุณาตรวจสอบการเชื่อมต่อ');
                    if (deadline !== null && performance.now() >= deadline) login();
                }
            }).always(() => { checking = false; });
    }
    $('#sessionRenew').on('click', function () {
        if (renewing || leaving) return;
        renewing = true;
        revision++;
        const started = performance.now();
        $(this).prop('disabled', true);
        $.ajax({ url: element.dataset.renewUrl, method: 'POST', dataType: 'json', timeout: 10000,
            headers: { RequestVerificationToken: token } })
            .done(data => { update(data, started); channel?.postMessage('renewed'); })
            .fail(xhr => {
                if (xhr.status === 401) login();
                else {
                    $('#sessionError').text('ต่ออายุไม่สำเร็จ กรุณาลองอีกครั้ง');
                    if (deadline !== null && performance.now() >= deadline) login();
                }
            }).always(() => { renewing = false; $('#sessionRenew').prop('disabled', false); });
    });
    $('#sessionDecline').on('click', () => form.requestSubmit());
    $(form).on('submit', () => { leaving = true; });
    if (channel) channel.onmessage = event => {
        if (event.data === 'renewed') check();

    };
    document.addEventListener('visibilitychange', () => { if (!document.hidden) check(); });
    window.addEventListener('focus', check);
    setInterval(render, 1000);
    setInterval(check, 10000);
    check();
});

