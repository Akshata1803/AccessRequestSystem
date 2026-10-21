namespace AccessRequestSystem.Admin.Forms;

public class PromptDialog : Form
{
    private readonly TextBox _inputBox;
    private readonly Button _btnOk;
    private readonly Button _btnCancel;

    public string UserInput => _inputBox.Text;

    public PromptDialog(string title, string promptText)
    {
        Text = title;
        Width = 420;
        Height = 200;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;

        var lblPrompt = new Label
        {
            Text = promptText,
            Left = 20,
            Top = 15,
            Width = 360,
            Height = 35
        };

        _inputBox = new TextBox
        {
            Left = 20,
            Top = 55,
            Width = 360
        };

        _btnOk = new Button
        {
            Text = "OK",
            Left = 210,
            Top = 100,
            Width = 80,
            DialogResult = DialogResult.OK
        };

        _btnCancel = new Button
        {
            Text = "Cancel",
            Left = 300,
            Top = 100,
            Width = 80,
            DialogResult = DialogResult.Cancel
        };

        AcceptButton = _btnOk;
        CancelButton = _btnCancel;

        Controls.Add(lblPrompt);
        Controls.Add(_inputBox);
        Controls.Add(_btnOk);
        Controls.Add(_btnCancel);
    }
}
