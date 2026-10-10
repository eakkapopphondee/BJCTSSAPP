// Adapt the common API contract to the existing Web table renderers.
window.SetupApi = {
    rows(response) {
        const lookups = new Map((response.Lookups || []).map(row => [row.ID, row]));
        return response.Data.map(row => Object.fromEntries(
            Object.entries({ ...row, ...lookups.get(row.ID) }).map(([key, value]) => [
                key.replace(/^[A-Z]+(?=[A-Z][a-z]|$)|^[A-Z]/, prefix => prefix.toLowerCase()), value
            ])
        ));
    },
    page(response) {
        return {
            rows: this.rows(response), page: response.Pagination.Page, pageSize: response.Pagination.PerPage,
            totalCount: response.Pagination.TotalCount, totalPages: response.Pagination.TotalPages
        };
    },
    bindForm({ form, url, numbers = ['Id'], flags = [] }) {
        const element = $(form);
        const token = element.find('input[name="__RequestVerificationToken"]').val();
        const success = new bootstrap.Modal(document.getElementById('successModal'));
        let saved = false;
        element.on('submit', function (event) {
            event.preventDefault();
            if (saved || !this.reportValidity()) return;
            $('#errorMessage').addClass('d-none');
            const button = $('#btnSave').prop('disabled', true);
            const payload = {};
            $(this).serializeArray().forEach(({ name, value }) => {
                if (name !== '__RequestVerificationToken')
                    payload[name] = value === '' ? null : numbers.includes(name) ? Number(value) : value;
            });
            flags.forEach(name => { payload[name] = element.find(`[name="${name}"]`).prop('checked') ? 'Y' : 'N'; });
            $.ajax({ url, method: 'POST', contentType: 'application/json',
                headers: { RequestVerificationToken: token }, data: JSON.stringify(payload)
            }).done(() => { saved = true; success.show(); })
                .fail(xhr => $('#errorMessage').removeClass('d-none').text(xhr.responseJSON?.Message || 'ไม่สามารถบันทึกข้อมูลได้ กรุณาลองใหม่'))
                .always(() => button.prop('disabled', saved));
        });
    }
};
