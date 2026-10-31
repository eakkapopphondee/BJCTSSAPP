// Adapt the common API contract to the existing Web table renderers.
window.SetupApi = {
    saveError(xhr) {
        const problem = xhr.responseJSON;
        const errors = Object.values(problem?.errors || {}).flat().join(' ');
        const message = problem?.Message || problem?.message || errors || problem?.detail || problem?.title || 'ไม่สามารถบันทึกข้อมูลได้ กรุณาลองใหม่';
        const fields = {
            FactoryCode: 'รหัสบริษัท', FactoryDesc: 'ชื่อบริษัท',
            ConstantCode: 'รหัสค่าคงที่', CustomerCode: 'รหัสลูกค้า', CustomerName: 'ชื่อลูกค้า',
            ProductCode: 'รหัสสินค้า', ProductName: 'ชื่อสินค้า',
            ProductTypeCode: 'รหัสกลุ่มสินค้า', ProductTypeName: 'ชื่อกลุ่มสินค้า',
            ProgramTypeCode: 'รหัสประเภทโปรแกรม', ProgramTypeName: 'ชื่อประเภทโปรแกรม',
            ProvinceCode: 'รหัสจังหวัด', ProvinceName: 'ชื่อจังหวัด',
            RoleCode: 'รหัสกลุ่มผู้ใช้งาน', RoleDescription: 'ชื่อกลุ่มผู้ใช้งาน',
            TransportCompanyCode: 'รหัสบริษัทขนส่ง', TransportCompanyName: 'ชื่อบริษัทขนส่ง',
            TruckTypeCode: 'รหัสประเภทรถ', TruckTypeName: 'ชื่อประเภทรถ',
            WeightTypeCode: 'รหัสประเภทการชั่ง', WeightTypeName: 'ชื่อประเภทการชั่ง',
            UserName: 'รหัสผู้ใช้งาน'
        };
        const duplicate = message.match(/duplicate (active )?(\w+)/i);
        if (duplicate && fields[duplicate[2]]) {
            return `บันทึกไม่ได้: ${fields[duplicate[2]]}ซ้ำ`;
        }
        if (/Duplicate license plate, product and effective period/i.test(message))
            return 'บันทึกไม่ได้: ทะเบียนรถ สินค้า และช่วงวันที่มีผลซ้ำ';
        if (/Duplicate MenuID/i.test(message))
            return 'บันทึกไม่ได้: รายการเมนูซ้ำ';
        if (/A record with these values already exists\./i.test(message))
            return 'บันทึกไม่ได้: ข้อมูลซ้ำ';
        if (message.includes('ซ้ำ') && !message.includes('ไม่สามารถบันทึก') && !message.startsWith('บันทึกไม่ได้:'))
            return `บันทึกไม่ได้: ${message}`;
        return message;
    },
    showSaveError(xhr) {
        $('#errorMessage').removeClass('d-none').text(this.saveError(xhr));
        document.getElementById('errorMessage')?.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
    },
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
                .fail(xhr => SetupApi.showSaveError(xhr))
                .always(() => button.prop('disabled', saved));
        });
    }
};
