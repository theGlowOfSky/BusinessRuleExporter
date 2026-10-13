using BusinessRuleExporter;
using Xunit;

namespace BusinessRuleExporter.Tests
{
    /// <summary>
    /// Regression tests: the Logic text must use the original Dataverse logical names from the
    /// variable mapping. Sample JavaScript is synthetic, modelled on generated business-rule code.
    /// </summary>
    public class LogicalNameTests
    {
        // v4/v5 are aliases of v1/v2 (same logical field reached through two variables).
        private const string ShowHideJs =
@"function BusinessRule(executionContext) {
  try {
    var formContext = executionContext.getFormContext();
    var v1 = formContext.data.entity.attributes.get('int_interrelateprogram');
    var v2 = formContext.data.entity.attributes.get('int_schoolservicetype');
    var v3 = formContext.data.entity.attributes.get('int_acceptonlineregistration');
    var v4 = (v1) ? v1.getValue() : null;
    var v5 = (v2) ? v2.getValue() : null;
    if (v4 == 100000000)
    {
      v3.controls.forEach(function (control, index) { if (control && control.setVisible) { control.setVisible(true); } });
    }
    else
    {
      v3.controls.forEach(function (control, index) { if (control && control.setVisible) { control.setVisible(false); } });
    }
  } catch (e) { }
}";

        [Fact]
        public void VariableMapping_StillContainsOriginalLogicalNames()
        {
            var map = BusinessRuleInterpreter.MapVariables(ShowHideJs);
            Assert.Equal("int_interrelateprogram", map["v1"]);
            Assert.Equal("int_schoolservicetype", map["v2"]);
            Assert.Equal("int_acceptonlineregistration", map["v3"]);
            Assert.Equal("int_interrelateprogram", map["v4"]);
            Assert.Equal("int_schoolservicetype", map["v5"]);
        }

        [Fact]
        public void ToLogic_ConditionAndShowHideActions_UseExactLogicalNames()
        {
            var logic = BusinessRuleInterpreter.ToLogic(ShowHideJs);

            Assert.Contains("int_interrelateprogram equals 100000000", logic);
            Assert.Contains("Show field int_acceptonlineregistration", logic);
            Assert.Contains("Hide field int_acceptonlineregistration", logic);
            // The old shortened/capitalized display names must be gone.
            Assert.DoesNotContain("Interrelateprogram", logic);
            Assert.DoesNotContain("Acceptonlineregistration", logic);
        }

        [Fact]
        public void ToLogic_PreservesBranchStructureAndOperators()
        {
            var logic = BusinessRuleInterpreter.ToLogic(ShowHideJs);
            var expected = string.Join(System.Environment.NewLine, new[]
            {
                "IF",
                "int_interrelateprogram equals 100000000",
                "THEN",
                "* Show field int_acceptonlineregistration",
                "",
                "ELSE",
                "THEN",
                "* Hide field int_acceptonlineregistration"
            });
            Assert.Equal(expected, logic);
        }

        [Fact]
        public void ToLogic_DuplicateVariablesForSameField_ResolveToSameLogicalName()
        {
            const string js =
@"var v1 = formContext.data.entity.attributes.get('int_interrelateprogram');
var v2 = formContext.data.entity.attributes.get('int_interrelateprogram');
var v3 = formContext.data.entity.attributes.get('int_acceptonlineregistration');
if ((v1 == null) || (v2 == null))
{
  v3.setRequiredLevel('required');
}";
            var logic = BusinessRuleInterpreter.ToLogic(js);

            // Two variables, one field: reported once, with the exact logical name.
            Assert.Contains("int_interrelateprogram does not contain data", logic);
            Assert.DoesNotContain("OR", logic);
            Assert.Contains("Set int_acceptonlineregistration as Business Required", logic);
        }

        [Fact]
        public void ToLogic_ComparisonBetweenTwoVariables_UsesLogicalNamesForBothSides()
        {
            const string js =
@"var v1 = formContext.data.entity.attributes.get('int_startvalue');
var v2 = formContext.data.entity.attributes.get('int_endvalue');
var v3 = formContext.data.entity.attributes.get('int_notes');
if (v1 != v2)
{
  v3.setValue(null);
}";
            var logic = BusinessRuleInterpreter.ToLogic(js);
            Assert.Contains("int_startvalue does not equal int_endvalue", logic);
            Assert.Contains("Clear int_notes", logic);
        }

        [Fact]
        public void ToLogic_UnresolvedVariables_AreNotReplacedWithInventedNames()
        {
            // v8 and v9 have no attributes.get(...) mapping anywhere in the script.
            const string js =
@"var v1 = formContext.data.entity.attributes.get('int_known');
if (v9 == 5)
{
  v8.setRequiredLevel('required');
  v1.setValue(null);
}";
            var logic = BusinessRuleInterpreter.ToLogic(js);

            Assert.Contains("v9 equals 5", logic);
            Assert.Contains("Set v8 as Business Required", logic);
            Assert.Contains("Clear int_known", logic);
        }

        [Fact]
        public void ToLogic_LogicalNameCaseIsPreservedExactly()
        {
            const string js =
@"var v1 = formContext.data.entity.attributes.get('cr123_MixedCaseField');
if (v1 == 1)
{
  v1.setRequiredLevel('none');
}";
            var logic = BusinessRuleInterpreter.ToLogic(js);
            Assert.Contains("cr123_MixedCaseField equals 1", logic);
            Assert.Contains("Set cr123_MixedCaseField as Not Business Required", logic);
        }
    }
}
