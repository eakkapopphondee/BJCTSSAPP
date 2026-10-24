// Minimal Open XML workbook writer. ZIP entries use the STORE method, so no external library is needed.
window.ExcelExport = (() => {
    const encoder = new TextEncoder();
    const mime = 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet';
    const escapeXml = value => String(value).replace(/[&<>"']/g, character =>
        ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&apos;' })[character]);
    const column = index => {
        let name = '';
        for (index++; index; index = Math.floor((index - 1) / 26))
            name = String.fromCharCode(65 + (index - 1) % 26) + name;
        return name;
    };
    const cell = (value, address, style = '') => {
        if (typeof value === 'number' && Number.isFinite(value))
            return `<c r="${address}"${style}><v>${value}</v></c>`;
        if (typeof value === 'boolean')
            return `<c r="${address}"${style} t="b"><v>${value ? 1 : 0}</v></c>`;
        // All strings are inline text. Spreadsheet formulas are never evaluated from user data.
        const safe = String(value ?? '').replace(/[\u0000-\u0008\u000B\u000C\u000E-\u001F]/g, '');
        return `<c r="${address}"${style} t="inlineStr"><is><t xml:space="preserve">${escapeXml(safe)}</t></is></c>`;
    };
    const crcTable = Array.from({ length: 256 }, (_, index) => {
        let crc = index;
        for (let bit = 0; bit < 8; bit++) crc = (crc >>> 1) ^ (crc & 1 ? 0xedb88320 : 0);
        return crc >>> 0;
    });
    const crc32 = bytes => {
        let crc = 0xffffffff;
        for (const byte of bytes) crc = (crc >>> 8) ^ crcTable[(crc ^ byte) & 255];
        return (crc ^ 0xffffffff) >>> 0;
    };
    function zip(entries) {
        const chunks = [], directory = [];
        let offset = 0;
        for (const [path, content] of entries) {
            const name = encoder.encode(path), data = encoder.encode(content), crc = crc32(data);
            const local = new Uint8Array(30 + name.length);
            const header = new DataView(local.buffer);
            header.setUint32(0, 0x04034b50, true);
            header.setUint16(4, 20, true);
            header.setUint32(14, crc, true);
            header.setUint32(18, data.length, true);
            header.setUint32(22, data.length, true);
            header.setUint16(26, name.length, true);
            local.set(name, 30);
            chunks.push(local, data);
            const central = new Uint8Array(46 + name.length);
            const record = new DataView(central.buffer);
            record.setUint32(0, 0x02014b50, true);
            record.setUint16(4, 20, true);
            record.setUint16(6, 20, true);
            record.setUint32(16, crc, true);
            record.setUint32(20, data.length, true);
            record.setUint32(24, data.length, true);
            record.setUint16(28, name.length, true);
            record.setUint32(42, offset, true);
            central.set(name, 46);
            directory.push(central);
            offset += local.length + data.length;
        }
        const directorySize = directory.reduce((total, entry) => total + entry.length, 0);
        const end = new Uint8Array(22), footer = new DataView(end.buffer);
        footer.setUint32(0, 0x06054b50, true);
        footer.setUint16(8, entries.length, true);
        footer.setUint16(10, entries.length, true);
        footer.setUint32(12, directorySize, true);
        footer.setUint32(16, offset, true);
        return new Blob([...chunks, ...directory, end], { type: mime });
    }
    function worksheet(fields, rows, options = {}) {
        const requiredHeaders = new Set(options.requiredHeaders ?? []);
        const allRows = [fields, ...rows.map(row => fields.map(field => row[field]))];
        const sheetRows = allRows.map((values, index) => `<row r="${index + 1}">${values.map((value, position) =>
            cell(value, `${column(position)}${index + 1}`, index === 0 && requiredHeaders.has(fields[position]) ? ' s="2"' : '')).join('')}</row>`).join('');
        const columns = options.textColumns ? `<cols><col min="1" max="${fields.length}" width="24" customWidth="1" style="1"/></cols>` : '';
        return `<?xml version="1.0" encoding="UTF-8" standalone="yes"?><worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">${columns}<sheetData>${sheetRows}</sheetData></worksheet>`;
    }
    function workbookSheets(sheets) {
        const hasStyles = sheets.some(sheet => sheet.options?.textColumns || sheet.options?.requiredHeaders?.length);
        return zip([
            ['[Content_Types].xml', `<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types"><Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/><Default Extension="xml" ContentType="application/xml"/><Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/>${sheets.map((_, index) => `<Override PartName="/xl/worksheets/sheet${index + 1}.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>`).join('')}${hasStyles ? '<Override PartName="/xl/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml"/>' : ''}</Types>`],
            ['_rels/.rels', '<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/></Relationships>'],
            ['xl/workbook.xml', `<?xml version="1.0" encoding="UTF-8" standalone="yes"?><workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships"><sheets>${sheets.map((sheet, index) => `<sheet name="${escapeXml(sheet.name)}" sheetId="${index + 1}" r:id="rId${index + 1}"/>`).join('')}</sheets></workbook>`],
            ['xl/_rels/workbook.xml.rels', `<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">${sheets.map((_, index) => `<Relationship Id="rId${index + 1}" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet${index + 1}.xml"/>`).join('')}${hasStyles ? `<Relationship Id="rId${sheets.length + 1}" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles" Target="styles.xml"/>` : ''}</Relationships>`],
            ...(hasStyles ? [['xl/styles.xml', '<?xml version="1.0" encoding="UTF-8" standalone="yes"?><styleSheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><fonts count="2"><font><sz val="11"/><name val="Calibri"/></font><font><b/><color rgb="FF1565C0"/><sz val="11"/><name val="Calibri"/></font></fonts><fills count="2"><fill><patternFill patternType="none"/></fill><fill><patternFill patternType="gray125"/></fill></fills><borders count="1"><border/></borders><cellStyleXfs count="1"><xf numFmtId="0" fontId="0" fillId="0" borderId="0"/></cellStyleXfs><cellXfs count="3"><xf numFmtId="0" fontId="0" fillId="0" borderId="0"/><xf numFmtId="49" fontId="0" fillId="0" borderId="0" applyNumberFormat="1"/><xf numFmtId="0" fontId="1" fillId="0" borderId="0" applyFont="1"/></cellXfs></styleSheet>']] : []),
            ...sheets.map((sheet, index) => [`xl/worksheets/sheet${index + 1}.xml`, worksheet(sheet.fields, sheet.rows, sheet.options)])
        ]);
    }
    function workbook(fields, rows, options = {}) {
        return workbookSheets([{ name: 'Data', fields, rows, options }]);
    }
    function save(blob, filename) {
        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = filename;
        document.body.appendChild(link);
        link.click();
        link.remove();
        setTimeout(() => URL.revokeObjectURL(url), 1000);
    }
    function download(fields, rows, filename, options) {
        save(workbook(fields, rows, options), filename);
    }
    async function downloadSearch(fields, url, filters, filename, options = {}) {
        const { notification = false } = options;
        const rows = [];
        const pageSize = 1000;
        for (let page = 1; ; page++) {
            const response = await $.getJSON(url, notification
                ? { ...filters, page, pageSize }
                : { ...filters, Page: page, PerPage: pageSize });
            const batch = notification ? response.rows : window.SetupApi.rows(response);
            rows.push(...batch);
            const totalPages = notification ? response.totalPages : response.Pagination.TotalPages;
            if (page >= totalPages) break;
        }
        download(fields, rows, filename);
    }
    return { download, downloadSearch, workbook, workbookSheets, downloadSheets: (sheets, filename) => save(workbookSheets(sheets), filename) };
})();
