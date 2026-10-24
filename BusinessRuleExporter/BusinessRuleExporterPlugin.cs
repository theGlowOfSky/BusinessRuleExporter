using System.ComponentModel.Composition;
using XrmToolBox.Extensibility;
using XrmToolBox.Extensibility.Interfaces;

namespace BusinessRuleExporter
{
    [Export(typeof(IXrmToolBoxPlugin))]
    [ExportMetadata("Name", "Business Rule Exporter")]
    [ExportMetadata("Description", "A custom XrmToolBox utility for retrieving Microsoft Dataverse business rules, interpreting available client-side logic, and exporting rule details to Excel.")]
    [ExportMetadata("SmallImageBase64", PluginIcons.Small)]
    [ExportMetadata("BigImageBase64", PluginIcons.Large)]
    [ExportMetadata("BackgroundColor", "White")]
    [ExportMetadata("PrimaryFontColor", "Black")]
    [ExportMetadata("SecondaryFontColor", "DimGray")]
    public class BusinessRuleExporterPlugin : PluginBase
    {
        public override IXrmToolBoxPluginControl GetControl()
        {
            return new BusinessRuleExporterControl();
        }
    }
}
