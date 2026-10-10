using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO.Compression;
using System.Xml;
using System.Xml.Linq;

namespace thaibev.Data;

public static class CustomerExcelParser
{
    public static readonly string[] Columns =
    [
        "CustomerCode", "CustomerName", "Address1", "PhoneNumber", "SAPCode", "SAPDesc",
        "Remark", "MolasFlag", "TBRFlag", "DefaultFlag", "DataStatus"
    ];

    public static List<Dictionary<string, object?>> Parse(Stream file)
    {
        try { return ParseWorkbook(file); }
        catch (InvalidDataException error) { throw new ValidationException("Invalid .xlsx file.", error); }
        catch (XmlException error) { throw new ValidationException("Invalid Excel worksheet XML.", error); }
        catch (OverflowException error) { throw new ValidationException("Excel file is too large or malformed.", error); }
    }

    private static List<Dictionary<string, object?>> ParseWorkbook(Stream file)
    {
        using var archive = new ZipArchive(file, ZipArchiveMode.Read, leaveOpen: true);
        if (archive.Entries.Sum(entry => entry.Length) > 25_000_000)
            throw new ValidationException("Excel file is too large.");
        var sheet = archive.GetEntry("xl/worksheets/sheet1.xml") ??
            archive.Entries.FirstOrDefault(entry => entry.FullName.StartsWith("xl/worksheets/sheet", StringComparison.OrdinalIgnoreCase)
                && entry.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase));
        if (sheet is null) throw new ValidationException("Excel worksheet was not found.");
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        var strings = archive.GetEntry("xl/sharedStrings.xml") is { } stringEntry
            ? ReadXml(stringEntry).Descendants(ns + "si").Select(item => string.Concat(item.Descendants(ns + "t").Select(text => text.Value))).ToList()
            : [];
        var document = ReadXml(sheet);
        var rows = document.Descendants(ns + "sheetData").Elements(ns + "row").ToList();
        if (rows.Count == 0) throw new ValidationException("Excel template has no header row.");
        var headers = ReadCells(rows[0], ns, strings);
        var mapping = new Dictionary<int, string>();
        foreach (var pair in headers)
        {
            var name = pair.Value.Trim();
            var column = Columns.FirstOrDefault(value => value.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (column is null) continue;
            if (mapping.ContainsValue(column)) throw new ValidationException($"Duplicate Excel column: {column}.");
            mapping[pair.Key] = column;
        }
        if (mapping.Count != Columns.Length)
            throw new ValidationException("Excel columns do not match the downloaded customer template.");

        var imported = new List<Dictionary<string, object?>>();
        foreach (var row in rows.Skip(1))
        {
            var cells = ReadCells(row, ns, strings);
            if (!cells.Any(cell => mapping.ContainsKey(cell.Key) && !string.IsNullOrWhiteSpace(cell.Value))) continue;
            if (imported.Count >= 5000) throw new ValidationException("Excel file has more than 5000 data rows.");
            if (!int.TryParse(row.Attribute("r")?.Value, NumberStyles.None, CultureInfo.InvariantCulture, out var rowNumber)
                || rowNumber < 2) throw new ValidationException("Invalid Excel row number.");
            var item = new Dictionary<string, object?> { ["ExcelRow"] = rowNumber };
            foreach (var (columnIndex, field) in mapping)
            {
                cells.TryGetValue(columnIndex, out var value);
                item[field] = string.IsNullOrWhiteSpace(value)
                    ? field switch { "MolasFlag" or "TBRFlag" or "DefaultFlag" => "N", "DataStatus" => "A", _ => null }
                    : value.Trim();
            }
            imported.Add(item);
        }
        if (imported.Count == 0) throw new ValidationException("Excel file has no customer rows.");
        return imported;
    }

    private static XDocument ReadXml(ZipArchiveEntry entry)
    {
        using var stream = entry.Open();
        using var reader = XmlReader.Create(stream, new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 25_000_000
        });
        return XDocument.Load(reader);
    }

    private static Dictionary<int, string> ReadCells(XElement row, XNamespace ns, List<string> strings)
    {
        var cells = new Dictionary<int, string>();
        foreach (var cell in row.Elements(ns + "c"))
        {
            if (cell.Element(ns + "f") is not null)
                throw new ValidationException("Excel formulas are not supported in the import template.");
            var reference = cell.Attribute("r")?.Value ?? "";
            var index = 0;
            foreach (var letter in reference.TakeWhile(char.IsLetter))
                index = checked(index * 26 + char.ToUpperInvariant(letter) - 'A' + 1);
            if (index == 0) throw new ValidationException("Invalid Excel cell address.");
            var type = cell.Attribute("t")?.Value;
            var value = type == "inlineStr"
                ? string.Concat(cell.Descendants(ns + "t").Select(text => text.Value))
                : cell.Element(ns + "v")?.Value ?? "";
            if (type == "s")
            {
                if (!int.TryParse(value, out var stringIndex) || stringIndex < 0 || stringIndex >= strings.Count)
                    throw new ValidationException("Invalid shared string in Excel file.");
                value = strings[stringIndex];
            }
            cells[index - 1] = value;
        }
        return cells;
    }
}
