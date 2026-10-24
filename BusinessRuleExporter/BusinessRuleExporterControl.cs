using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Metadata.Query;
using Microsoft.Xrm.Sdk.Query;
using McTools.Xrm.Connection;
using XrmToolBox.Extensibility;

namespace BusinessRuleExporter
{
    public class BusinessRuleExporterControl : PluginControlBase
    {
        private const int TableQueryBatchSize = 100;
        private const int TablePaneMinimumWidth = 220;
        private const int PreviewPaneMinimumWidth = 400;

        private readonly DataGridView _grid;
        private readonly ToolStrip _tools;
        private readonly Button _loadTablesButton;
        private readonly ToolStripButton _loadRulesButton;
        private readonly ToolStripButton _exportButton;
        private readonly Button _selectAllButton;
        private readonly Button _clearSelectionButton;
        private readonly TextBox _tableSearch;
        private readonly CheckedListBox _tableList;
        private readonly System.Windows.Forms.Label _connectionStatusLabel;
        private readonly System.Windows.Forms.Label _selectedCount;
        private readonly System.Windows.Forms.Label _tablesIntroLabel;
        private readonly System.Windows.Forms.Label _rulesIntroLabel;
        private readonly SplitContainer _mainSplitContainer;
        private bool _initialSplitterRatioSet;

        // FIX: Explicitly specify the Windows Forms Label type.
        private readonly System.Windows.Forms.Label _status;

        private List<BusinessRuleRow> _rows = new List<BusinessRuleRow>();
        private List<TableChoice> _tables = new List<TableChoice>();
        private string _tablesConnectionUrl;
        private string _auditConnectionUrl;
        private string _operationConnectionUrl;
        private int _connectionVersion;
        private bool _updatingTableList;
        private bool _isBusy;

        public BusinessRuleExporterControl()
        {
            BackColor = Color.White;

            // The left pane contains connection/table controls; the right pane contains audit actions and preview.
            _tools = new ToolStrip
            {
                Dock = DockStyle.Top,
                AutoSize = false,
                Height = 31,
                GripStyle = ToolStripGripStyle.Hidden,
                BackColor = Color.White,
                RenderMode = ToolStripRenderMode.System,
                Padding = new Padding(3, 0, 3, 0)
            };
            _loadTablesButton = new Button
            {
                Text = "Load Environment Tables",
                Dock = DockStyle.Top,
                Height = 31,
                FlatStyle = FlatStyle.System,
                BackColor = Color.White,
                Margin = new Padding(2)
            };
            _loadTablesButton.Click += (s, e) => ExecuteMethod(LoadTables);
            _loadRulesButton = new ToolStripButton("Load Business Rules") { DisplayStyle = ToolStripItemDisplayStyle.Text };
            _loadRulesButton.Click += (s, e) => ExecuteMethod(LoadRules);
            _exportButton = new ToolStripButton("Export Excel") { DisplayStyle = ToolStripItemDisplayStyle.Text };
            _exportButton.Click += (s, e) => Export();
            _tools.Items.Add(_loadRulesButton);
            _tools.Items.Add(new ToolStripSeparator());
            _tools.Items.Add(_exportButton);

            var tablePanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(5, 4, 5, 4)
            };
            _connectionStatusLabel = new System.Windows.Forms.Label
            {
                Dock = DockStyle.Top,
                Height = 22,
                Text = "Not connected.",
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
                ForeColor = Color.DimGray,
                Padding = new Padding(2, 0, 0, 0),
                BackColor = Color.White
            };

            var searchTitle = new System.Windows.Forms.Label
            {
                Dock = DockStyle.Top,
                Height = 21,
                Text = "Search tables:",
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
                BackColor = Color.White
            };
            _tableSearch = new TextBox { Dock = DockStyle.Top, BorderStyle = BorderStyle.FixedSingle };
            _tableSearch.TextChanged += (s, e) => RefreshTableList();

            var selectionActions = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 31,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(0, 2, 0, 1),
                BackColor = Color.White
            };
            selectionActions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            selectionActions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            _selectAllButton = new Button
            {
                Text = "Select All",
                Dock = DockStyle.Fill,
                FlatStyle = FlatStyle.System,
                Margin = new Padding(1, 0, 2, 0)
            };
            _selectAllButton.Click += (s, e) => SetAllTablesSelected(true);
            _clearSelectionButton = new Button
            {
                Text = "Clear Selection",
                Dock = DockStyle.Fill,
                FlatStyle = FlatStyle.System,
                Margin = new Padding(2, 0, 1, 0)
            };
            _clearSelectionButton.Click += (s, e) => SetAllTablesSelected(false);
            selectionActions.Controls.Add(_selectAllButton, 0, 0);
            selectionActions.Controls.Add(_clearSelectionButton, 1, 0);

            _selectedCount = new System.Windows.Forms.Label
            {
                Dock = DockStyle.Top,
                Height = 23,
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
                Padding = new Padding(2, 0, 0, 0),
                Text = "0 of 0 tables selected",
                ForeColor = Color.Black,
                BackColor = Color.White
            };

            _tableList = new CheckedListBox
            {
                Dock = DockStyle.Fill,
                CheckOnClick = true,
                IntegralHeight = false,
                HorizontalScrollbar = true,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White,
                DrawMode = DrawMode.OwnerDrawVariable
            };
            _tableList.ItemCheck += TableList_ItemCheck;
            _tableList.MeasureItem += TableList_MeasureItem;
            _tableList.DrawItem += TableList_DrawItem;
            _tableList.Visible = false;

            var tableListHost = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            _tablesIntroLabel = new System.Windows.Forms.Label
            {
                Dock = DockStyle.Fill,
                Text = "Load environment tables to display the available tables.",
                TextAlign = System.Drawing.ContentAlignment.TopLeft,
                Padding = new Padding(4, 8, 4, 4),
                ForeColor = Color.DimGray,
                BackColor = Color.White
            };
            tableListHost.Controls.Add(_tableList);
            tableListHost.Controls.Add(_tablesIntroLabel);

            // Add the fill area first, then top-docked controls in reverse visual order.
            tablePanel.Controls.Add(tableListHost);
            tablePanel.Controls.Add(_selectedCount);
            tablePanel.Controls.Add(selectionActions);
            tablePanel.Controls.Add(_tableSearch);
            tablePanel.Controls.Add(searchTitle);
            tablePanel.Controls.Add(_loadTablesButton);
            tablePanel.Controls.Add(_connectionStatusLabel);

            _grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.DisplayedCells,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoGenerateColumns = false,
                BorderStyle = BorderStyle.FixedSingle,
                GridColor = Color.Gainsboro,
                CellBorderStyle = DataGridViewCellBorderStyle.Single,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single,
                EnableHeadersVisualStyles = false
            };
            _grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(235, 231, 215);
            _grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.Black;
            _grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(235, 231, 215);
            _grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.Black;
            _grid.DefaultCellStyle.BackColor = Color.White;
            _grid.DefaultCellStyle.ForeColor = Color.Black;
            _grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(49, 103, 175);
            _grid.DefaultCellStyle.SelectionForeColor = Color.White;
            _grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(250, 250, 248);
            _grid.Columns.Add(Col("Table", "Table Logical Name", 165));
            _grid.Columns.Add(Col("Name", "Business Rule Name", 230));
            _grid.Columns.Add(Col("Id", "Business Rule ID", 245));
            _grid.Columns.Add(Col("State", "State", 100));
            _grid.Columns.Add(Col("ReferencedFields", "Referenced Fields", 260));
            _grid.Columns.Add(Col("Logic", "Business Rule Logic", 480));
            _grid.DefaultCellStyle.WrapMode = DataGridViewTriState.True;

            _rulesIntroLabel = new System.Windows.Forms.Label
            {
                Dock = DockStyle.Top,
                Height = 25,
                Text = "Select tables on the left, then click Load Business Rules to display the preview.",
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
                Padding = new Padding(6, 0, 0, 0),
                ForeColor = Color.DimGray,
                BackColor = Color.White
            };
            var rulesPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(4, 0, 4, 0) };
            // Keep the grid visible beneath the status/instruction line, as in the reference interface.
            rulesPanel.Controls.Add(_grid);
            rulesPanel.Controls.Add(_rulesIntroLabel);
            rulesPanel.Controls.Add(_tools);

            _mainSplitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                BorderStyle = BorderStyle.FixedSingle,
                SplitterWidth = 5,
                BackColor = Color.White,
                Panel1MinSize = 0,
                Panel2MinSize = 0
            };
            _mainSplitContainer.Panel1.Controls.Add(tablePanel);
            _mainSplitContainer.Panel2.Controls.Add(rulesPanel);
            _mainSplitContainer.SizeChanged += InitializeInitialSplitterRatio;

            var contentPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            contentPanel.Controls.Add(_mainSplitContainer);

            _status = new System.Windows.Forms.Label
            {
                Dock = DockStyle.Bottom,
                Height = 24,
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
                Padding = new Padding(8, 0, 0, 0),
                BackColor = Color.FromArgb(248, 248, 246),
                ForeColor = Color.DimGray,
                Text = "Connect to an environment, then click Load Environment Tables."
            };

            Controls.Add(contentPanel);
            Controls.Add(_status);
            InitializeInitialSplitterRatio(this, EventArgs.Empty);
            UpdateConnectionStatusLabel();
            UpdateUiState();
        }

        private void InitializeInitialSplitterRatio(object sender, EventArgs e)
        {
            if (_initialSplitterRatioSet || _mainSplitContainer == null)
            {
                return;
            }

            var availableWidth = _mainSplitContainer.ClientSize.Width;
            var minimumWidth = TablePaneMinimumWidth + PreviewPaneMinimumWidth + _mainSplitContainer.SplitterWidth;
            if (availableWidth < minimumWidth)
            {
                return;
            }

            var maximumDistance = availableWidth - PreviewPaneMinimumWidth - _mainSplitContainer.SplitterWidth;
            var desiredDistance = (int)(availableWidth * 0.25);
            var distance = Math.Max(TablePaneMinimumWidth, Math.Min(desiredDistance, maximumDistance));
            _initialSplitterRatioSet = true;
            _mainSplitContainer.SplitterDistance = distance;
            _mainSplitContainer.Panel1MinSize = TablePaneMinimumWidth;
            _mainSplitContainer.Panel2MinSize = PreviewPaneMinimumWidth;
        }

        public override void UpdateConnection(IOrganizationService newService, ConnectionDetail detail, string actionName, object parameter)
        {
            var newConnectionUrl = detail == null ? null : detail.WebApplicationUrl;
            var previousConnectionUrl = _tablesConnectionUrl ?? _auditConnectionUrl ?? _operationConnectionUrl;
            var connectionChanged = !string.IsNullOrWhiteSpace(previousConnectionUrl)
                && !SameConnection(previousConnectionUrl, newConnectionUrl);

            base.UpdateConnection(newService, detail, actionName, parameter);
            UpdateConnectionStatusLabel();
            if (string.IsNullOrWhiteSpace(newConnectionUrl))
            {
                _status.Text = "Not connected. Connect to a Dataverse environment.";
            }
            else if (!connectionChanged)
            {
                _status.Text = "Connected. Click Load Environment Tables to begin.";
            }

            if (connectionChanged)
            {
                _connectionVersion++;
                ResetAllData();
                _status.Text = _isBusy
                    ? "Environment changed. Results from the previous connection will be discarded."
                    : "Environment changed. Load environment tables again.";
            }

            UpdateUiState();
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

        private void LoadTables()
        {
            var connectionUrl = GetCurrentConnectionUrl();
            var connectionVersion = _connectionVersion;
            var service = Service;
            if (string.IsNullOrWhiteSpace(connectionUrl) || service == null)
            {
                ResetAllData();
                _status.Text = "Connect to a Dataverse environment before loading tables.";
                UpdateUiState();
                return;
            }

            // Starting a new environment load invalidates any previous preview/export.
            ResetAllData();
            _tablesIntroLabel.Text = "Loading environment table metadata...";
            _rulesIntroLabel.Text = "Load business rules after the table list has loaded.";
            _operationConnectionUrl = connectionUrl;
            _isBusy = true;
            _status.Text = "Loading environment table metadata...";
            UpdateUiState();

            WorkAsync(new WorkAsyncInfo
            {
                Message = "Loading environment tables...",
                Work = (worker, args) =>
                {
                    args.Result = RetrieveTables(service);
                },
                PostWorkCallBack = args =>
                {
                    _isBusy = false;
                    _operationConnectionUrl = null;
                    if (connectionVersion != _connectionVersion
                        || !SameConnection(connectionUrl, GetCurrentConnectionUrl()))
                    {
                        ResetAllData();
                        _status.Text = "The environment changed while tables were loading. Load environment tables again.";
                        UpdateUiState();
                        return;
                    }

                    if (args.Error != null)
                    {
                        _tablesConnectionUrl = null;
                        _tablesIntroLabel.Text = "Table loading failed. Check the error and try again.";
                        _status.Text = "Table metadata load failed.";
                        UpdateUiState();
                        ShowErrorDialog(args.Error);
                        return;
                    }

                    _tables = (List<TableChoice>)args.Result;
                    _tablesConnectionUrl = connectionUrl;
                    _rulesIntroLabel.Text = _tables.Count > 0
                        ? "Select tables on the left, then click Load Business Rules to display the preview."
                        : "No selectable tables were returned. Check the environment connection and load tables again if needed.";
                    RefreshTableList();
                    _status.Text = _tables.Count + " tables loaded. Select tables, then load business rules.";
                    UpdateUiState();
                }
            });
        }

        private static List<TableChoice> RetrieveTables(IOrganizationService service)
        {
            // Request only the entity metadata properties needed by the selector.
            var query = new EntityQueryExpression
            {
                Properties = new MetadataPropertiesExpression { AllProperties = false }
            };
            query.Properties.PropertyNames.AddRange(new[]
            {
                "LogicalName", "DisplayName", "IsIntersect", "IsPrivate", "IsValidForAdvancedFind"
            });

            var request = new RetrieveMetadataChangesRequest { Query = query };
            var response = (RetrieveMetadataChangesResponse)service.Execute(request);

            // Keep user-facing tables; private, intersect, and non-Advanced-Find backend tables are excluded.
            return response.EntityMetadata
                .Where(metadata => metadata != null
                    && !string.IsNullOrWhiteSpace(metadata.LogicalName)
                    && metadata.IsPrivate != true
                    && metadata.IsIntersect != true
                    && metadata.IsValidForAdvancedFind == true)
                .Select(metadata => new TableChoice
                {
                    LogicalName = metadata.LogicalName,
                    DisplayName = GetDisplayName(metadata),
                    Selected = false
                })
                .OrderBy(table => table.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(table => table.LogicalName, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static string GetDisplayName(EntityMetadata metadata)
        {
            var label = metadata.DisplayName == null ? null : metadata.DisplayName.UserLocalizedLabel;
            if (label != null && !string.IsNullOrWhiteSpace(label.Label))
            {
                return label.Label;
            }

            if (metadata.DisplayName != null && metadata.DisplayName.LocalizedLabels != null)
            {
                var fallback = metadata.DisplayName.LocalizedLabels.FirstOrDefault(item =>
                    item != null && !string.IsNullOrWhiteSpace(item.Label));
                if (fallback != null)
                {
                    return fallback.Label;
                }
            }

            return metadata.LogicalName;
        }

        private void RefreshTableList()
        {
            if (_tableList == null)
            {
                return;
            }

            var connectionHasTables = !string.IsNullOrWhiteSpace(_tablesConnectionUrl)
                && SameConnection(_tablesConnectionUrl, GetCurrentConnectionUrl());
            var hasLoadedTables = connectionHasTables && _tables.Count > 0;
            _tableList.Visible = hasLoadedTables;
            _tablesIntroLabel.Visible = !hasLoadedTables;
            if (connectionHasTables && _tables.Count == 0)
            {
                _tablesIntroLabel.Text = "No selectable tables were returned for this environment. You can click Load Environment Tables to try again.";
            }

            var filter = (_tableSearch.Text ?? string.Empty).Trim();
            _updatingTableList = true;
            _tableList.BeginUpdate();
            try
            {
                _tableList.Items.Clear();
                foreach (var table in _tables)
                {
                    if (filter.Length > 0
                        && table.DisplayName.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0
                        && table.LogicalName.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        continue;
                    }

                    var index = _tableList.Items.Add(table);
                    if (table.Selected)
                    {
                        _tableList.SetItemChecked(index, true);
                    }
                }
            }
            finally
            {
                _tableList.EndUpdate();
                _updatingTableList = false;
            }

            UpdateSelectedCount();
            UpdateUiState();
        }

        private void TableList_MeasureItem(object sender, MeasureItemEventArgs e)
        {
            if (e.Index >= 0)
            {
                e.ItemHeight = 44;
            }
        }

        private void TableList_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= _tableList.Items.Count)
            {
                return;
            }

            var table = _tableList.Items[e.Index] as TableChoice;
            if (table == null)
            {
                return;
            }

            var isChecked = _tableList.GetItemChecked(e.Index);
            var rowBackColor = isChecked ? Color.FromArgb(233, 240, 249) : Color.White;
            using (var background = new SolidBrush(rowBackColor))
            {
                e.Graphics.FillRectangle(background, e.Bounds);
            }

            var checkState = isChecked ? CheckBoxState.CheckedNormal : CheckBoxState.UncheckedNormal;
            if (!_tableList.Enabled)
            {
                checkState = isChecked ? CheckBoxState.CheckedDisabled : CheckBoxState.UncheckedDisabled;
            }
            var checkSize = CheckBoxRenderer.GetGlyphSize(e.Graphics, checkState);
            var checkX = e.Bounds.Left + 7;
            var checkY = e.Bounds.Top + (e.Bounds.Height - checkSize.Height) / 2;
            CheckBoxRenderer.DrawCheckBox(e.Graphics, new Point(checkX, checkY), checkState);

            var textX = checkX + checkSize.Width + 8;
            var titleBounds = new Rectangle(textX, e.Bounds.Top + 5, Math.Max(1, e.Bounds.Right - textX - 5), 17);
            var detailBounds = new Rectangle(textX, e.Bounds.Top + 23, Math.Max(1, e.Bounds.Right - textX - 5), 16);
            using (var titleFont = new Font(Font, FontStyle.Bold))
            using (var detailFont = new Font(Font.FontFamily, Math.Max(7.5F, Font.Size - 1F), FontStyle.Italic))
            using (var titleBrush = new SolidBrush(Color.FromArgb(35, 35, 35)))
            using (var detailBrush = new SolidBrush(Color.DimGray))
            {
                e.Graphics.DrawString(table.ToString(), titleFont, titleBrush, titleBounds);
                e.Graphics.DrawString("Logical name: " + table.LogicalName, detailFont, detailBrush, detailBounds);
            }

            using (var dividerPen = new Pen(Color.FromArgb(218, 218, 218)))
            {
                e.Graphics.DrawLine(dividerPen, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
            }

            if ((e.State & DrawItemState.Focus) == DrawItemState.Focus)
            {
                e.DrawFocusRectangle();
            }
        }

        private void TableList_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (_updatingTableList || e.Index < 0 || e.Index >= _tableList.Items.Count)
            {
                return;
            }

            var table = _tableList.Items[e.Index] as TableChoice;
            if (table == null)
            {
                return;
            }

            var selected = e.NewValue == CheckState.Checked;
            if (table.Selected == selected)
            {
                return;
            }

            table.Selected = selected;
            InvalidateAuditAfterSelectionChange();
            UpdateSelectedCount();
            _tableList.Invalidate();
            UpdateUiState();
        }

        private void SetAllTablesSelected(bool selected)
        {
            if (_isBusy || _tables.Count == 0)
            {
                return;
            }

            var changed = _tables.Any(table => table.Selected != selected);
            foreach (var table in _tables)
            {
                table.Selected = selected;
            }

            if (changed)
            {
                InvalidateAuditAfterSelectionChange();
            }

            RefreshTableList();
        }

        private void InvalidateAuditAfterSelectionChange()
        {
            // A changed selection always invalidates the preview so only a fresh audit can be exported.
            if (_auditConnectionUrl != null || _rows.Count > 0)
            {
                ClearAuditResults();
            }

            _rulesIntroLabel.Text = "Table selection changed. Click Load Business Rules to load or refresh the preview.";
            _status.Text = "Table selection changed. Load business rules again to refresh the preview.";
            UpdateUiState();
        }

        private void UpdateSelectedCount()
        {
            if (_selectedCount != null)
            {
                _selectedCount.Text = _tables.Count(table => table.Selected) + " of " + _tables.Count + " tables selected";
            }
        }

        private void LoadRules()
        {
            var connectionUrl = GetCurrentConnectionUrl();
            var connectionVersion = _connectionVersion;
            if (!EnsureTablesBelongToCurrentEnvironment(connectionUrl))
            {
                return;
            }

            var selectedTables = _tables
                .Where(table => table.Selected)
                .Select(table => table.LogicalName)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (selectedTables.Count == 0)
            {
                MessageBox.Show(this, "Select at least one table before loading business rules.", "Business Rule Exporter");
                return;
            }

            var service = Service;
            if (service == null)
            {
                _status.Text = "Connect to a Dataverse environment before loading business rules.";
                UpdateUiState();
                return;
            }

            // Clear first so a failed/repeated audit cannot leave older results available to export.
            ClearAuditResults();
            _operationConnectionUrl = connectionUrl;
            _isBusy = true;
            _rulesIntroLabel.Text = "Loading business rules for the selected tables...";
            _status.Text = "Loading business rules for " + selectedTables.Count + " selected tables...";
            UpdateUiState();

            WorkAsync(new WorkAsyncInfo
            {
                Message = "Loading business rules for selected tables...",
                Work = (worker, args) =>
                {
                    args.Result = RetrieveRules(service, selectedTables);
                },
                PostWorkCallBack = args =>
                {
                    _isBusy = false;
                    _operationConnectionUrl = null;
                    if (connectionVersion != _connectionVersion
                        || !SameConnection(connectionUrl, GetCurrentConnectionUrl()))
                    {
                        ResetAllData();
                        _status.Text = "The environment changed while business rules were loading. Load environment tables again.";
                        UpdateUiState();
                        return;
                    }

                    if (args.Error != null)
                    {
                        _auditConnectionUrl = null;
                        _rows = new List<BusinessRuleRow>();
                        _grid.DataSource = null;
                        _rulesIntroLabel.Text = "Business rule loading failed. Review the error and try loading the selected tables again.";
                        _status.Text = "Business rule load failed.";
                        UpdateUiState();
                        ShowErrorDialog(args.Error);
                        return;
                    }

                    _rows = (List<BusinessRuleRow>)args.Result;
                    _grid.DataSource = null;
                    _grid.DataSource = _rows;
                    _auditConnectionUrl = connectionUrl;
                    if (_rows.Count == 0)
                    {
                        _rulesIntroLabel.Text = "Audit completed. No business rules were found for the selected tables.";
                        _status.Text = "Audit completed. No business rules found for the selected tables.";
                    }
                    else
                    {
                        _rulesIntroLabel.Text = "Showing " + _rows.Count + " business rules for "
                            + selectedTables.Count + " selected table(s).";
                        _status.Text = _rows.Count + " business rules loaded from " + connectionUrl;
                    }
                    UpdateUiState();
                }
            });
        }

        private bool EnsureTablesBelongToCurrentEnvironment(string connectionUrl)
        {
            if (string.IsNullOrWhiteSpace(connectionUrl))
            {
                ResetAllData();
                _status.Text = "Connect to a Dataverse environment before loading business rules.";
                UpdateUiState();
                return false;
            }

            if (string.IsNullOrWhiteSpace(_tablesConnectionUrl)
                || !SameConnection(_tablesConnectionUrl, connectionUrl))
            {
                if (_tablesConnectionUrl != null || _tables.Count > 0 || _rows.Count > 0)
                {
                    ResetAllData();
                }

                _status.Text = "Load environment tables for the current connection before loading business rules.";
                UpdateUiState();
                return false;
            }

            return true;
        }

        private static List<BusinessRuleRow> RetrieveRules(IOrganizationService service, IList<string> selectedTables)
        {
            var entities = new List<Entity>();
            var selectedTableSet = new HashSet<string>(selectedTables, StringComparer.OrdinalIgnoreCase);

            // workflow.primaryentity is the Dataverse logical name of the table a business rule belongs to.
            // Use an IN filter in bounded batches so unrelated business rules are not downloaded.
            for (var start = 0; start < selectedTables.Count; start += TableQueryBatchSize)
            {
                var tableBatch = selectedTables.Skip(start).Take(TableQueryBatchSize).ToArray();
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
                            new ConditionExpression("type", ConditionOperator.Equal, 1),
                            new ConditionExpression("primaryentity", ConditionOperator.In, tableBatch.Cast<object>().ToArray())
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
            }

            // Retain only selected primary entities defensively and suppress duplicates across batches.
            return entities
                .Where(rule => selectedTableSet.Contains(rule.GetAttributeValue<string>("primaryentity") ?? string.Empty))
                .GroupBy(rule => rule.Id)
                .Select(group => group.First())
                .Select(ToRow)
                .OrderBy(row => row.Table, StringComparer.OrdinalIgnoreCase)
                .ThenBy(row => row.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(row => row.State, StringComparer.OrdinalIgnoreCase)
                .ToList();
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

        private void ResetAllData()
        {
            _tablesConnectionUrl = null;
            _tables = new List<TableChoice>();
            _tableList.Visible = false;
            _tablesIntroLabel.Visible = true;
            _tablesIntroLabel.Text = "Load environment tables to display the available tables.";
            _rulesIntroLabel.Text = "Select tables on the left, then click Load Business Rules to display the preview.";
            _tableSearch.Text = string.Empty;
            _updatingTableList = true;
            _tableList.BeginUpdate();
            try
            {
                _tableList.Items.Clear();
            }
            finally
            {
                _tableList.EndUpdate();
                _updatingTableList = false;
            }

            ClearAuditResults();
            UpdateSelectedCount();
            UpdateUiState();
        }

        private void ClearAuditResults()
        {
            _rows = new List<BusinessRuleRow>();
            _auditConnectionUrl = null;
            _grid.DataSource = null;
            UpdateUiState();
        }

        private void UpdateConnectionStatusLabel()
        {
            if (_connectionStatusLabel == null)
            {
                return;
            }

            var connectionUrl = GetCurrentConnectionUrl();
            if (string.IsNullOrWhiteSpace(connectionUrl))
            {
                _connectionStatusLabel.Text = "Not connected.";
                return;
            }

            var detail = ConnectionDetail;
            var environmentName = detail == null
                ? null
                : detail.OrganizationFriendlyName ?? detail.Organization ?? detail.ConnectionName;
            if (string.IsNullOrWhiteSpace(environmentName))
            {
                Uri uri;
                environmentName = Uri.TryCreate(connectionUrl, UriKind.Absolute, out uri)
                    ? uri.Host
                    : connectionUrl;
            }

            _connectionStatusLabel.Text = "Connected: " + environmentName;
        }

        private string GetCurrentConnectionUrl()
        {
            try
            {
                return ConnectionDetail == null ? null : ConnectionDetail.WebApplicationUrl;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static bool SameConnection(string first, string second)
        {
            if (string.IsNullOrWhiteSpace(first) || string.IsNullOrWhiteSpace(second))
            {
                return false;
            }

            return string.Equals(first.Trim().TrimEnd('/'), second.Trim().TrimEnd('/'), StringComparison.OrdinalIgnoreCase);
        }

        private void UpdateUiState()
        {
            var currentUrl = GetCurrentConnectionUrl();
            var tablesLoadedForCurrentConnection = !string.IsNullOrWhiteSpace(_tablesConnectionUrl)
                && SameConnection(_tablesConnectionUrl, currentUrl);
            var tablesCurrent = !_isBusy && tablesLoadedForCurrentConnection;
            var auditLoaded = !_isBusy && !string.IsNullOrWhiteSpace(_auditConnectionUrl)
                && SameConnection(_auditConnectionUrl, currentUrl);
            var auditCurrent = auditLoaded && _rows.Count > 0;
            var hasSelectedTables = _tables.Any(table => table.Selected);

            var hasAvailableTables = tablesLoadedForCurrentConnection && _tables.Count > 0;
            _tableList.Visible = hasAvailableTables;
            _tablesIntroLabel.Visible = !hasAvailableTables;
            if (tablesLoadedForCurrentConnection && _tables.Count == 0)
            {
                _tablesIntroLabel.Text = "No selectable tables were returned for this environment. You can click Load Environment Tables to try again.";
            }
            _grid.Visible = true;
            _rulesIntroLabel.Visible = true;

            _loadTablesButton.Enabled = !_isBusy;
            _loadRulesButton.Enabled = tablesCurrent && hasSelectedTables;
            _exportButton.Enabled = auditCurrent;
            _tableSearch.Enabled = tablesCurrent;
            _tableList.Enabled = tablesCurrent;
            _selectedCount.Enabled = !_isBusy;

            _selectAllButton.Enabled = tablesCurrent;
            _clearSelectionButton.Enabled = tablesCurrent;

            _tools.PerformLayout();
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
            var currentUrl = GetCurrentConnectionUrl();
            if (!SameConnection(_auditConnectionUrl, currentUrl))
            {
                if (_auditConnectionUrl != null || _rows.Count > 0)
                {
                    ResetAllData();
                    _status.Text = "The environment changed. Load environment tables and run a new audit before exporting.";
                    UpdateUiState();
                    return;
                }
            }

            if (_rows.Count == 0)
            {
                MessageBox.Show(this, "Load business rules for the selected tables before exporting.", "Business Rule Exporter");
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

        private sealed class TableChoice
        {
            public string LogicalName { get; set; }
            public string DisplayName { get; set; }
            public bool Selected { get; set; }

            public override string ToString()
            {
                if (string.IsNullOrWhiteSpace(DisplayName) || string.Equals(DisplayName, LogicalName, StringComparison.OrdinalIgnoreCase))
                {
                    return LogicalName;
                }

                return DisplayName + " (" + LogicalName + ")";
            }
        }
    }
}
