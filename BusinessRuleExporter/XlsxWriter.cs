using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace BusinessRuleExporter
{
    /// <summary>
    /// Minimal xlsx writer. No third-party Excel library, so the plugin is a single DLL.
    /// </summary>
    public static class XlsxWriter
    {
        private static readonly string[] Headers =
        {
            "Business Rule Name",
            "Business Rule ID",
            "Table Logical Name",
            "State",
            "Status Code",
            "Scope",
            "Referenced Fields",
            "Variable Mapping",
            "Business Rule Logic"
        };

        public static void Write(string path, IList<BusinessRuleRow> rows)
        {
            var shared = new List<string>();
            var index = new Dictionary<string, int>(StringComparer.Ordinal);
            Func<string, int> add = value =>
            {
                value = value ?? string.Empty;
                if (value.Length > 32767)
                {
                    value = value.Substring(0, 32767 - 16) + "... [TRUNCATED]";
                }

                int existing;
                if (index.TryGetValue(value, out existing))
                {
                    return existing;
                }

                existing = shared.Count;
                shared.Add(value);
                index[value] = existing;
                return existing;
            };

            var sheet = new StringBuilder();
            sheet.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
            sheet.Append("<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">");
            sheet.Append("<sheetViews><sheetView tabSelected=\"1\" workbookViewId=\"0\"><pane ySplit=\"1\" topLeftCell=\"A2\" activePane=\"bottomLeft\" state=\"frozen\"/></sheetView></sheetViews>");
            sheet.Append("<cols>");
            for (var c = 1; c <= Headers.Length; c++)
            {
                var width = c == Headers.Length ? 90 : 28;
                sheet.Append("<col min=\"").Append(c).Append("\" max=\"").Append(c).Append("\" width=\"").Append(width).Append("\" customWidth=\"1\"/>");
            }

            sheet.Append("</cols><sheetData>");

            WriteRow(sheet, 1, Headers, add, true);
            for (var r = 0; r < rows.Count; r++)
            {
                var row = rows[r];
                WriteRow(sheet, r + 2, new[]
                {
                    row.Name, row.Id, row.Table, row.State, row.StatusCode, row.Scope,
                    row.ReferencedFields, row.VariableMapping, row.Logic
                }, add, false);
            }

            sheet.Append("</sheetData>");
            sheet.Append("<autoFilter ref=\"A1:I").Append(Math.Max(1, rows.Count + 1)).Append("\"/>");
            sheet.Append("</worksheet>");

            var sharedXml = new StringBuilder();
            sharedXml.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
            sharedXml.Append("<sst xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" count=\"")
                .Append(shared.Count).Append("\" uniqueCount=\"").Append(shared.Count).Append("\">");
            foreach (var text in shared)
            {
                sharedXml.Append("<si><t xml:space=\"preserve\">").Append(XmlEscape(text)).Append("</t></si>");
            }

            sharedXml.Append("</sst>");

            if (File.Exists(path))
            {
                File.Delete(path);
            }

            using (var zip = ZipFile.Open(path, ZipArchiveMode.Create))
            {
                Add(zip, "[Content_Types].xml", ContentTypes);
                Add(zip, "_rels/.rels", RootRels);
                Add(zip, "xl/workbook.xml", Workbook);
                Add(zip, "xl/_rels/workbook.xml.rels", WorkbookRels);
                Add(zip, "xl/styles.xml", Styles);
                Add(zip, "xl/sharedStrings.xml", sharedXml.ToString());
                Add(zip, "xl/worksheets/sheet1.xml", sheet.ToString());
            }
        }

        private static void WriteRow(StringBuilder sheet, int rowNumber, string[] values, Func<string, int> add, bool header)
        {
            sheet.Append("<row r=\"").Append(rowNumber).Append("\"");
            if (header) sheet.Append(" ht=\"30\" customHeight=\"1\"");
            sheet.Append(">");
            for (var c = 0; c < values.Length; c++)
            {
                var cell = ColumnName(c) + rowNumber;
                var style = header ? 1 : 2;
                sheet.Append("<c r=\"").Append(cell).Append("\" t=\"s\" s=\"").Append(style).Append("\"><v>")
                    .Append(add(values[c])).Append("</v></c>");
            }

            sheet.Append("</row>");
        }

        private static string ColumnName(int index)
        {
            return ((char)('A' + index)).ToString();
        }

        private static string XmlEscape(string value)
        {
            return System.Security.SecurityElement.Escape(StripInvalidXmlChars(value ?? string.Empty)) ?? string.Empty;
        }

        // Control characters such as U+0001 are illegal in XML 1.0 and make Excel report the workbook as corrupt.
        private static string StripInvalidXmlChars(string value)
        {
            var sb = new StringBuilder(value.Length);
            for (var i = 0; i < value.Length; i++)
            {
                var ch = value[i];
                if (ch == '\t' || ch == '\n' || ch == '\r' || (ch >= 0x20 && ch <= 0xD7FF) || (ch >= 0xE000 && ch <= 0xFFFD))
                {
                    sb.Append(ch);
                }
                else if (char.IsHighSurrogate(ch) && i + 1 < value.Length && char.IsLowSurrogate(value[i + 1]))
                {
                    sb.Append(ch).Append(value[i + 1]);
                    i++;
                }
            }

            return sb.ToString();
        }

        private static void Add(ZipArchive zip, string name, string content)
        {
            var entry = zip.CreateEntry(name, CompressionLevel.Optimal);
            using (var stream = entry.Open())
            using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
            {
                writer.Write(content);
            }
        }

        private const string ContentTypes =
            "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
            "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">" +
            "<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>" +
            "<Default Extension=\"xml\" ContentType=\"application/xml\"/>" +
            "<Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>" +
            "<Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>" +
            "<Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/>" +
            "<Override PartName=\"/xl/sharedStrings.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sharedStrings+xml\"/>" +
            "</Types>";

        private const string RootRels =
            "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
            "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
            "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/>" +
            "</Relationships>";

        private const string Workbook =
            "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
            "<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">" +
            "<sheets><sheet name=\"Business Rules\" sheetId=\"1\" r:id=\"rId1\"/></sheets></workbook>";

        private const string WorkbookRels =
            "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
            "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
            "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/>" +
            "<Relationship Id=\"rId2\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/>" +
            "<Relationship Id=\"rId3\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/sharedStrings\" Target=\"sharedStrings.xml\"/>" +
            "</Relationships>";

        private const string Styles =
            "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
            "<styleSheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">" +
            "<fonts count=\"2\"><font><sz val=\"11\"/><name val=\"Calibri\"/></font><font><b/><sz val=\"11\"/><name val=\"Calibri\"/></font></fonts>" +
            "<fills count=\"2\"><fill><patternFill patternType=\"none\"/></fill><fill><patternFill patternType=\"gray125\"/></fill></fills>" +
            "<borders count=\"1\"><border><left/><right/><top/><bottom/><diagonal/></border></borders>" +
            "<cellStyleXfs count=\"1\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/></cellStyleXfs>" +
            "<cellXfs count=\"3\">" +
            "<xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\"/>" +
            "<xf numFmtId=\"0\" fontId=\"1\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyFont=\"1\" applyAlignment=\"1\"><alignment wrapText=\"1\" vertical=\"top\"/></xf>" +
            "<xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyAlignment=\"1\"><alignment wrapText=\"1\" vertical=\"top\"/></xf>" +
            "</cellXfs></styleSheet>";
    }
}
