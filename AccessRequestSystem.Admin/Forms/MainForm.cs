using AccessRequestSystem.Admin.Models;
using AccessRequestSystem.Admin.Services;

namespace AccessRequestSystem.Admin.Forms;

public class MainForm : Form
{
    private readonly ApiClient _apiClient;

    private readonly TabControl _tabControl;
    private readonly TabPage _tabPending;
    private readonly TabPage _tabAudit;

    private readonly DataGridView _dgvPending;
    private readonly Button _btnRefreshPending;
    private readonly Button _btnApprove;
    private readonly Button _btnDeny;

    private readonly DataGridView _dgvAudit;
    private readonly Button _btnRefreshAudit;

    private readonly StatusStrip _statusStrip;
    private readonly ToolStripStatusLabel _statusLabel;

    public MainForm(ApiClient apiClient)
    {
        _apiClient = apiClient;

        Text = "Access Request & Approval Management Console";
        Width = 980;
        Height = 650;
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(800, 500);

        // 1. Status Bar
        _statusStrip = new StatusStrip();
        _statusLabel = new ToolStripStatusLabel
        {
            Text = $"Logged in as: {_apiClient.CurrentUsername} | Role: {_apiClient.CurrentRole}"
        };
        _statusStrip.Items.Add(_statusLabel);

        // 2. Tab Control
        _tabControl = new TabControl { Dock = DockStyle.Fill };
        _tabPending = new TabPage("Pending Requests");
        _tabAudit = new TabPage("Audit Logs");

        // 3. Tab 1: Pending Requests UI
        var pendingTopPanel = new Panel { Dock = DockStyle.Top, Height = 45 };
        _btnRefreshPending = new Button { Text = "Refresh", Left = 12, Top = 8, Width = 90, Height = 30 };
        _btnApprove = new Button { Text = "Approve", Left = 110, Top = 8, Width = 100, Height = 30, BackColor = Color.LightGreen };
        _btnDeny = new Button { Text = "Deny", Left = 220, Top = 8, Width = 100, Height = 30, BackColor = Color.MistyRose };

        _btnRefreshPending.Click += async (_, _) => await LoadPendingRequestsAsync();
        _btnApprove.Click += async (_, _) => await HandleApproveAsync();
        _btnDeny.Click += async (_, _) => await HandleDenyAsync();

        pendingTopPanel.Controls.Add(_btnRefreshPending);
        pendingTopPanel.Controls.Add(_btnApprove);
        pendingTopPanel.Controls.Add(_btnDeny);

        _dgvPending = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            MultiSelect = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        };
        ConfigurePendingGridColumns();

        _tabPending.Controls.Add(_dgvPending);
        _tabPending.Controls.Add(pendingTopPanel);
        _tabControl.TabPages.Add(_tabPending);

        // 4. Tab 2: Audit Logs UI (Only available to Admin)
        var isSystemAdmin = string.Equals(_apiClient.CurrentRole, "Admin", StringComparison.OrdinalIgnoreCase);

        var auditTopPanel = new Panel { Dock = DockStyle.Top, Height = 45 };
        _btnRefreshAudit = new Button { Text = "Refresh Audit Log", Left = 12, Top = 8, Width = 140, Height = 30 };
        _btnRefreshAudit.Click += async (_, _) => await LoadAuditLogsAsync();
        auditTopPanel.Controls.Add(_btnRefreshAudit);

        _dgvAudit = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            MultiSelect = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        };
        ConfigureAuditGridColumns();

        _tabAudit.Controls.Add(_dgvAudit);
        _tabAudit.Controls.Add(auditTopPanel);

        if (isSystemAdmin)
        {
            _tabControl.TabPages.Add(_tabAudit);
            _tabControl.SelectedIndexChanged += async (_, _) =>
            {
                if (_tabControl.SelectedTab == _tabAudit && _dgvAudit.Rows.Count == 0)
                {
                    await LoadAuditLogsAsync();
                }
            };
        }

        Controls.Add(_tabControl);
        Controls.Add(_statusStrip);

        // Load data on form initial show
        Shown += async (_, _) => await LoadPendingRequestsAsync();
    }

    private void ConfigurePendingGridColumns()
    {
        _dgvPending.Columns.Clear();
        _dgvPending.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "ID", DataPropertyName = "Id", Width = 60, FillWeight = 30 });
        _dgvPending.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Requester", DataPropertyName = "RequesterUsername", Width = 110, FillWeight = 60 });
        _dgvPending.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Resource", DataPropertyName = "ResourceName", Width = 160, FillWeight = 90 });
        _dgvPending.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Justification", DataPropertyName = "Justification", Width = 260, FillWeight = 160 });
        _dgvPending.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Duration (m)", DataPropertyName = "DurationMinutes", Width = 90, FillWeight = 40 });
        _dgvPending.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Created At (UTC)", DataPropertyName = "CreatedAt", Width = 140, FillWeight = 70 });
    }

    private void ConfigureAuditGridColumns()
    {
        _dgvAudit.Columns.Clear();
        _dgvAudit.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "ID", DataPropertyName = "Id", Width = 60, FillWeight = 25 });
        _dgvAudit.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Timestamp (UTC)", DataPropertyName = "Timestamp", Width = 140, FillWeight = 65 });
        _dgvAudit.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Actor", DataPropertyName = "ActorUsername", Width = 100, FillWeight = 50 });
        _dgvAudit.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Action", DataPropertyName = "Action", Width = 120, FillWeight = 60 });
        _dgvAudit.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Entity", DataPropertyName = "EntityType", Width = 100, FillWeight = 50 });
        _dgvAudit.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Details", DataPropertyName = "Details", Width = 300, FillWeight = 180 });
    }

    private async Task LoadPendingRequestsAsync()
    {
        SetBusy(true, "Loading pending requests...");
        try
        {
            var requests = await _apiClient.GetPendingRequestsAsync();
            _dgvPending.DataSource = requests;
            _statusLabel.Text = $"Logged in as: {_apiClient.CurrentUsername} ({_apiClient.CurrentRole}) | Loaded {requests.Count} pending request(s).";
        }
        catch (ApiException ex)
        {
            MessageBox.Show(ex.Message, "Error Loading Requests", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to load requests: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task HandleApproveAsync()
    {
        var selectedItem = GetSelectedPendingRequest();
        if (selectedItem == null)
        {
            MessageBox.Show("Please select a pending request to approve.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var confirm = MessageBox.Show(
            $"Are you sure you want to approve access for '{selectedItem.RequesterUsername}' to '{selectedItem.ResourceName}' ({selectedItem.DurationMinutes} minutes)?",
            "Confirm Approval",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (confirm != DialogResult.Yes) return;

        SetBusy(true, $"Approving request #{selectedItem.Id}...");
        try
        {
            var response = await _apiClient.ApproveRequestAsync(selectedItem.Id);

            // Display the one-time token dialog
            using var tokenDialog = new TokenDisplayDialog(response.RequestId, response.GrantToken, response.ExpiresAt);
            tokenDialog.ShowDialog(this);

            await LoadPendingRequestsAsync();
        }
        catch (ApiException ex)
        {
            MessageBox.Show(ex.Message, "Approval Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Unexpected error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task HandleDenyAsync()
    {
        var selectedItem = GetSelectedPendingRequest();
        if (selectedItem == null)
        {
            MessageBox.Show("Please select a pending request to deny.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        // Ask for optional denial reason
        using var prompt = new PromptDialog("Deny Access Request", $"Provide an optional reason for denying Request #{selectedItem.Id}:");
        if (prompt.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var reason = prompt.UserInput;

        SetBusy(true, $"Denying request #{selectedItem.Id}...");
        try
        {
            await _apiClient.DenyRequestAsync(selectedItem.Id, reason);
            MessageBox.Show($"Request #{selectedItem.Id} has been denied.", "Request Denied", MessageBoxButtons.OK, MessageBoxIcon.Information);
            await LoadPendingRequestsAsync();
        }
        catch (ApiException ex)
        {
            MessageBox.Show(ex.Message, "Denial Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Unexpected error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task LoadAuditLogsAsync()
    {
        SetBusy(true, "Loading audit logs...");
        try
        {
            var logs = await _apiClient.GetAuditLogsAsync(page: 1, pageSize: 100);
            _dgvAudit.DataSource = logs;
            _statusLabel.Text = $"Logged in as: {_apiClient.CurrentUsername} ({_apiClient.CurrentRole}) | Loaded {logs.Count} audit records.";
        }
        catch (ApiException ex)
        {
            MessageBox.Show(ex.Message, "Error Loading Audit Logs", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to load audit logs: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private PendingRequestItem? GetSelectedPendingRequest()
    {
        if (_dgvPending.SelectedRows.Count == 0) return null;
        return _dgvPending.SelectedRows[0].DataBoundItem as PendingRequestItem;
    }

    private void SetBusy(bool isBusy, string? message = null)
    {
        _btnRefreshPending.Enabled = !isBusy;
        _btnApprove.Enabled = !isBusy;
        _btnDeny.Enabled = !isBusy;
        _btnRefreshAudit.Enabled = !isBusy;

        if (isBusy && message != null)
        {
            _statusLabel.Text = message;
        }
        UseWaitCursor = isBusy;
    }
}
