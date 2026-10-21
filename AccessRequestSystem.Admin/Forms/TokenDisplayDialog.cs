namespace AccessRequestSystem.Admin.Forms;

public class TokenDisplayDialog : Form
{
    public TokenDisplayDialog(int requestId, string token, DateTime expiresAt)
    {
        Text = $"Access Approved - Request #{requestId}";
        Width = 520;
        Height = 270;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;

        var lblHeader = new Label
        {
            Text = $"Request #{requestId} was successfully approved!\nExpires at: {expiresAt.ToLocalTime():yyyy-MM-dd HH:mm:ss} (Local)",
            Left = 20,
            Top = 15,
            Width = 460,
            Height = 35,
            Font = new Font(Font, FontStyle.Bold)
        };

        var lblToken = new Label
        {
            Text = "One-Time Grant Token:",
            Left = 20,
            Top = 60,
            Width = 200,
            Height = 20
        };

        var txtToken = new TextBox
        {
            Text = token,
            Left = 20,
            Top = 85,
            Width = 460,
            ReadOnly = true
        };

        var lblWarning = new Label
        {
            Text = "IMPORTANT: Store this token now. It will never be displayed or recovered again.",
            Left = 20,
            Top = 125,
            Width = 460,
            Height = 35,
            ForeColor = Color.DarkRed
        };

        var btnCopy = new Button
        {
            Text = "Copy to Clipboard",
            Left = 240,
            Top = 175,
            Width = 140,
            Height = 32
        };
        btnCopy.Click += (_, _) =>
        {
            Clipboard.SetText(token);
            btnCopy.Text = "Copied!";
        };

        var btnClose = new Button
        {
            Text = "Close",
            Left = 390,
            Top = 175,
            Width = 90,
            Height = 32,
            DialogResult = DialogResult.OK
        };

        Controls.Add(lblHeader);
        Controls.Add(lblToken);
        Controls.Add(txtToken);
        Controls.Add(lblWarning);
        Controls.Add(btnCopy);
        Controls.Add(btnClose);

        AcceptButton = btnClose;
    }
}
