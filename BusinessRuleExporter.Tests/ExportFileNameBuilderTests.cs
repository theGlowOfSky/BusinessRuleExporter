using BusinessRuleExporter;
using Xunit;

namespace BusinessRuleExporter.Tests
{
    public class ExportFileNameBuilderTests
    {
        [Fact]
        public void Build_SimpleEnvironmentName_AppendsSuffix()
        {
            Assert.Equal("Development_Business_Rules.xlsx", ExportFileNameBuilder.Build("Development"));
        }

        [Fact]
        public void Build_NameWithSpacesAndHyphens_ProducesValidFileName()
        {
            Assert.Equal("Interrelate_UAT-2_Business_Rules.xlsx", ExportFileNameBuilder.Build("Interrelate UAT-2"));
        }

        [Theory]
        [InlineData("Dev\\Test", "Dev_Test_Business_Rules.xlsx")]
        [InlineData("Dev/Test", "Dev_Test_Business_Rules.xlsx")]
        [InlineData("Dev:Test", "Dev_Test_Business_Rules.xlsx")]
        [InlineData("Dev*Test", "Dev_Test_Business_Rules.xlsx")]
        [InlineData("Dev?Test", "Dev_Test_Business_Rules.xlsx")]
        [InlineData("Dev\"Test", "Dev_Test_Business_Rules.xlsx")]
        [InlineData("Dev<Test>", "Dev_Test_Business_Rules.xlsx")]
        [InlineData("Dev|Test", "Dev_Test_Business_Rules.xlsx")]
        [InlineData("A:/*?B", "A_B_Business_Rules.xlsx")]
        public void Build_InvalidFileNameCharacters_AreSanitized(string input, string expected)
        {
            Assert.Equal(expected, ExportFileNameBuilder.Build(input));
        }

        [Theory]
        [InlineData("Production. ", "Production_Business_Rules.xlsx")]
        [InlineData("  Production  ", "Production_Business_Rules.xlsx")]
        [InlineData("Production...", "Production_Business_Rules.xlsx")]
        public void Build_TrailingSpacesAndPeriods_AreTrimmed(string input, string expected)
        {
            Assert.Equal(expected, ExportFileNameBuilder.Build(input));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("???")]
        [InlineData("...")]
        public void Build_MissingOrUnusableName_UsesFallback(string input)
        {
            Assert.Equal("Dataverse_Business_Rules.xlsx", ExportFileNameBuilder.Build(input));
        }

        [Fact]
        public void Build_NoCandidates_UsesFallback()
        {
            Assert.Equal("Dataverse_Business_Rules.xlsx", ExportFileNameBuilder.Build());
            Assert.Equal("Dataverse_Business_Rules.xlsx", ExportFileNameBuilder.Build((string[])null));
        }

        [Theory]
        [InlineData("Dev_Business_Rules.xlsx")]
        [InlineData("Dev_Business_Rules")]
        [InlineData("dev_business_rules.XLSX")]
        public void Build_SuffixIsNeverAppendedTwice(string input)
        {
            var result = ExportFileNameBuilder.Build(input);
            Assert.True(result.EndsWith("_Business_Rules.xlsx"));
            Assert.False(result.Replace("_Business_Rules.xlsx", "").Contains("Business_Rules"));
        }

        [Fact]
        public void Build_UsesFirstUsableCandidate()
        {
            Assert.Equal("Contoso_Prod_Business_Rules.xlsx", ExportFileNameBuilder.Build(null, "  ", "Contoso Prod", "other"));
        }

        [Fact]
        public void Build_ResultContainsNoInvalidCharacters()
        {
            var result = ExportFileNameBuilder.Build("a\\b/c:d*e?f\"g<h>i|j\tk");
            foreach (var ch in System.IO.Path.GetInvalidFileNameChars())
            {
                Assert.False(result.Contains(ch.ToString()));
            }
        }
    }
}
