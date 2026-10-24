using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using BusinessRuleExporter;
using Xunit;

namespace BusinessRuleExporter.Tests
{
    /// <summary>
    /// Characterization tests using sanitized, synthetic business-rule JavaScript.
    /// No real tenant or client data is used.
    /// </summary>
    public class BusinessRuleInterpreterTests
    {
        private const string SampleJs =
@"function SetRequired(executionContext) {
  try {
    var formContext = executionContext.getFormContext();
    var v1 = formContext.data.entity.attributes.get('new_status');
    var v2 = (v1) ? v1.getValue() : null;
    var v3 = formContext.data.entity.attributes.get('new_approvaldate');
    var v4 = formContext.data.entity.attributes.get('new_notes');
    if (v2 == 100000001)
    {
      v3.setRequiredLevel('required');
      v4.setValue(null);
    }
    else
    {
      v3.setRequiredLevel('none');
    }
  } catch (e) { }
}";

        private static string WrapClientData(string js)
        {
            return "<clientdata><clientcode><![CDATA[" + js + "]]></clientcode></clientdata>";
        }

        [Fact]
        public void ExtractJavaScript_ReturnsCodeFromClientCodeCdata()
        {
            var js = BusinessRuleInterpreter.ExtractJavaScript(WrapClientData(SampleJs));
            Assert.Contains("function SetRequired", js);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("<<not xml")]
        [InlineData("<clientdata></clientdata>")]
        public void ExtractJavaScript_EmptyOrInvalidInput_ReturnsEmptyString(string input)
        {
            Assert.Equal(string.Empty, BusinessRuleInterpreter.ExtractJavaScript(input));
        }

        [Fact]
        public void MapVariables_MapsAttributesGetAndValueAliases()
        {
            var map = BusinessRuleInterpreter.MapVariables(SampleJs);
            Assert.Equal("new_status", map["v1"]);
            Assert.Equal("new_status", map["v2"]);
            Assert.Equal("new_approvaldate", map["v3"]);
            Assert.Equal("new_notes", map["v4"]);
        }

        [Fact]
        public void MapVariables_IsCaseInsensitiveOnVariableName()
        {
            var map = BusinessRuleInterpreter.MapVariables(SampleJs);
            Assert.Equal("new_status", map["V1"]);
        }

        [Fact]
        public void MapVariables_ComparisonOnSameLineIsNotTreatedAsAssignment()
        {
            const string js = "if (v2 == 1) { var v3 = formContext.data.entity.attributes.get('new_x'); }";
            var map = BusinessRuleInterpreter.MapVariables(js);
            Assert.False(map.ContainsKey("v2"));
            Assert.Equal("new_x", map["v3"]);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void MapVariables_EmptyInput_ReturnsEmptyMap(string input)
        {
            Assert.Empty(BusinessRuleInterpreter.MapVariables(input));
        }

        [Fact]
        public void ToLogic_EmptyInput_ReturnsNoJavaScriptMessage()
        {
            Assert.Equal("No client JavaScript found.", BusinessRuleInterpreter.ToLogic(string.Empty));
        }

        [Fact]
        public void ToLogic_RendersIfThenElseWithResolvedFieldNames()
        {
            var logic = BusinessRuleInterpreter.ToLogic(SampleJs);
            Assert.StartsWith("IF", logic);
            // Field references use the exact logical names from the variable mapping.
            Assert.Contains("new_status equals 100000001", logic);
            Assert.Contains("Set new_approvaldate as Business Required", logic);
            Assert.Contains("Clear new_notes", logic);
            Assert.Contains("ELSE", logic);
            Assert.Contains("Set new_approvaldate as Not Business Required", logic);
        }

        [Fact]
        public void ToLogic_UnrecognizedJavaScript_ReturnsReviewRequiredMessage()
        {
            var logic = BusinessRuleInterpreter.ToLogic("var x = 1;");
            Assert.StartsWith("Review required", logic);
        }

        [Theory]
        [InlineData("new_approvaldate", "Approval Date")]
        [InlineData("cr123_projectName", "Project Name")]
        [InlineData(null, "Unknown Field")]
        [InlineData("", "Unknown Field")]
        public void Friendly_FormatsLogicalNames(string input, string expected)
        {
            Assert.Equal(expected, BusinessRuleInterpreter.Friendly(input));
        }

        [Fact]
        public void XlsxWriter_WritesValidPackageAndStripsIllegalXmlCharacters()
        {
            var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".xlsx");
            try
            {
                var rows = new[]
                {
                    new BusinessRuleRow
                    {
                        Table = "new_sample",
                        Name = "Rule \u0001 <A> & B",
                        Id = "00000000-0000-0000-0000-000000000001",
                        State = "Activated",
                        ReferencedFields = "new_field1, new_field2",
                        Logic = "IF\nTHEN",
                        StatusCode = "1",
                        Scope = "2",
                        VariableMapping = "{\"v1\": \"new_field1\"}"
                    }
                };
                XlsxWriter.Write(path, rows);

                using (var zip = ZipFile.OpenRead(path))
                {
                    Assert.NotNull(zip.GetEntry("[Content_Types].xml"));
                    Assert.NotNull(zip.GetEntry("xl/workbook.xml"));
                    var sharedEntry = zip.GetEntry("xl/sharedStrings.xml");
                    Assert.NotNull(sharedEntry);
                    string shared;
                    using (var reader = new StreamReader(sharedEntry.Open())) shared = reader.ReadToEnd();
                    Assert.DoesNotContain("\u0001", shared);
                    var ns = (System.Xml.Linq.XNamespace)"http://schemas.openxmlformats.org/spreadsheetml/2006/main";
                    var sharedValues = System.Xml.Linq.XDocument.Parse(shared).Root
                        .Elements(ns + "si")
                        .Select(item => (string)item.Element(ns + "t"))
                        .ToList();
                    Assert.Equal(new[]
                    {
                        "Table Logical Name", "Business Rule Name", "Business Rule ID",
                        "State", "Referenced Fields", "Business Rule Logic"
                    }, sharedValues.Take(6));
                    Assert.DoesNotContain("Status Code", sharedValues);
                    Assert.DoesNotContain("Scope", sharedValues);
                    Assert.DoesNotContain("Variable Mapping", sharedValues);

                    var sheetEntry = zip.GetEntry("xl/worksheets/sheet1.xml");
                    Assert.NotNull(sheetEntry);
                    using (var sheetStream = sheetEntry.Open())
                    {
                        var sheet = System.Xml.Linq.XDocument.Load(sheetStream);
                        Assert.Equal("A1:F2", (string)sheet.Root.Element(ns + "autoFilter").Attribute("ref"));
                        var dataRow = sheet.Root.Element(ns + "sheetData").Elements(ns + "row").Skip(1).First();
                        var rowValues = dataRow.Elements(ns + "c")
                            .Select(cell => sharedValues[int.Parse((string)cell.Element(ns + "v"))])
                            .ToList();
                        Assert.Equal(new[]
                        {
                            "new_sample", "Rule  <A> & B", "00000000-0000-0000-0000-000000000001",
                            "Activated", "new_field1, new_field2", "IF\nTHEN"
                        }, rowValues);
                    }

                    // Every part must be well-formed XML.
                    foreach (var entry in zip.Entries)
                    {
                        using (var stream = entry.Open())
                        {
                            System.Xml.Linq.XDocument.Load(stream);
                        }
                    }
                }
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }
    }
}
