using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using XrmToolBox.Extensibility;

namespace BusinessRuleExporter
{
    public class BusinessRuleExporterControl : PluginControlBase
    {
        private readonly DataGridView _grid;
        private readonly ToolStrip _tools;

        // FIX: Explicitly specify the Windows Forms Label type.
        private readonly System.Windows.Forms.Label _status;

        private List<BusinessRuleRow> _rows = new List<BusinessRuleRow>();

        public BusinessRuleExporterControl()
        {
            _tools = new ToolStrip();
            var load = new ToolStripButton("Load business rules");
            load.Click += (s, e) => ExecuteMethod(LoadRules);
            var export = new ToolStripButton("Export to Excel");
            export.Click += (s, e) => Export();
            _tools.Items.Add(load);
            _tools.Items.Add(export);

            // FIX: Explicitly specify the Windows Forms Label type.
            _status = new System.Windows.Forms.Label
            {
                Dock = DockStyle.Bottom,
                Height = 28,
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
                Padding = new Padding(8, 0, 0, 0),
                Text = "Connect to an environment, then click Load business rules."
            };

            _grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.DisplayedCells,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoGenerateColumns = false
            };
            _grid.Columns.Add(Col("Name", "Name", 220));
            _grid.Columns.Add(Col("Id", "Id", 220));
            _grid.Columns.Add(Col("Table", "Table", 140));
            _grid.Columns.Add(Col("State", "State", 80));
            _grid.Columns.Add(Col("Logic", "Logic", 480));
            _grid.DefaultCellStyle.WrapMode = DataGridViewTriState.True;

            Controls.Add(_grid);
            Controls.Add(_status);
            Controls.Add(_tools);
        }

        private static DataGridViewTextBoxColumn Col(string prop, string header, int width)
        {
            return new DataGridViewTextBoxColumn
            {
                DataPropertyName = prop,
                HeaderText = header,
                Name = prop,
                Width = width
            };
        }

        private void LoadRules()
        {
            WorkAsync(new WorkAsyncInfo
            {
                Message = "Loading business rules...",
                Work = (worker, args) =>
                {
                    args.Result = RetrieveRules(Service);
                },
                PostWorkCallBack = args =>
                {
                    if (args.Error != null)
                    {
                        _status.Text = "Load failed.";
                        ShowErrorDialog(args.Error);
                        return;
                    }

                    _rows = (List<BusinessRuleRow>)args.Result;
                    _grid.DataSource = null;
                    _grid.DataSource = _rows;
                    _status.Text = _rows.Count + " business rules loaded from " + ConnectionDetail.WebApplicationUrl;
                }
            });
        }

        private static List<BusinessRuleRow> RetrieveRules(IOrganizationService service)
        {
            var entities = new List<Entity>();
            var query = new QueryExpression("workflow")
            {
                ColumnSet = new ColumnSet(
                    "name", "workflowid", "primaryentity", "statecode",
                    "statuscode", "scope", "clientdata", "category", "type"),
                Criteria = new FilterExpression(LogicalOperator.And)
                {
                    Conditions =
                    {
                        new ConditionExpression("category", ConditionOperator.Equal, 2),
                        new ConditionExpression("type", ConditionOperator.Equal, 1)
                    }
                },
                PageInfo = new PagingInfo { Count = 50, PageNumber = 1 }
            };
            query.AddOrder("name", OrderType.Ascending);

            while (true)
            {
                var page = service.RetrieveMultiple(query);
                entities.AddRange(page.Entities);
                if (!page.MoreRecords)
                {
                    break;
                }

                query.PageInfo.PageNumber++;
                query.PageInfo.PagingCookie = page.PagingCookie;
            }

            return entities.Select(ToRow).ToList();
        }

        private static BusinessRuleRow ToRow(Entity rule)
        {
            var clientData = rule.GetAttributeValue<string>("clientdata") ?? string.Empty;
            var javascript = BusinessRuleInterpreter.ExtractJavaScript(clientData);
            var fields = BusinessRuleInterpreter.MapVariables(javascript);
            var mapping = new StringBuilder();
            mapping.Append("{");
            var first = true;
            foreach (var pair in fields.OrderBy(p => p.Key, StringComparer.OrdinalIgnoreCase))
            {
                if (!first) mapping.Append(", ");
                first = false;
                mapping.Append("\"").Append(pair.Key).Append("\": \"").Append(pair.Value).Append("\"");
            }

            mapping.Append("}");

            var state = rule.GetAttributeValue<OptionSetValue>("statecode");
            return new BusinessRuleRow
            {
                Name = rule.GetAttributeValue<string>("name") ?? string.Empty,
                Id = rule.Id.ToString(),
                Table = rule.GetAttributeValue<string>("primaryentity") ?? string.Empty,
                State = state == null ? string.Empty : state.Value == 1 ? "Activated" : state.Value == 0 ? "Draft" : state.Value.ToString(),
                StatusCode = Option(rule, "statuscode"),
                Scope = Option(rule, "scope"),
                ReferencedFields = string.Join(", ", fields.Values.Distinct(StringComparer.OrdinalIgnoreCase)),
                VariableMapping = mapping.ToString(),
                Logic = BusinessRuleInterpreter.ToLogic(javascript)
            };
        }

        private static string Option(Entity entity, string attribute)
        {
            var value = entity.GetAttributeValue<OptionSetValue>(attribute);
            return value == null ? string.Empty : value.Value.ToString();
        }

        // Suggested name only: the user can still change the folder and the name in the dialog.
        // ConnectionDetail already carries the environment names, so no extra Dataverse request is made.
        private string GetSuggestedFileName()
        {
            try
            {
                var detail = ConnectionDetail;
                if (detail == null)
                {
                    return ExportFileNameBuilder.Build();
                }

                return ExportFileNameBuilder.Build(
                    detail.OrganizationFriendlyName,
                    detail.Organization,
                    detail.ConnectionName,
                    detail.OrganizationUrlName);
            }
            catch (Exception)
            {
                return ExportFileNameBuilder.Build();
            }
        }

        private void Export()
        {
            if (_rows.Count == 0)
            {
                MessageBox.Show(this, "Load business rules before exporting.", "Business Rule Exporter");
                return;
            }

            using (var dialog = new SaveFileDialog
            {
                Filter = "Excel workbook (*.xlsx)|*.xlsx",
                FileName = GetSuggestedFileName(),
                OverwritePrompt = true
            })
            {
                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                try
                {
                    XlsxWriter.Write(dialog.FileName, _rows);
                    _status.Text = "Exported " + _rows.Count + " rules to " + dialog.FileName;
                }
                catch (Exception ex)
                {
                    ShowErrorDialog(ex);
                }
            }
        }
    }
}