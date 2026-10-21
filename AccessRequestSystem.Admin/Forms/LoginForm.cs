using AccessRequestSystem.Admin.Services;

namespace AccessRequestSystem.Admin.Forms;

public class LoginForm : Form
{
    private readonly ApiClient _apiClient;
    private readonly TextBox _txtUsername;
    private readonly TextBox _txtPassword;
    private readonly Button _btnLogin;
    private readonly Label _lblStatus;

    public LoginForm(ApiClient apiClient)
    {
        _apiClient = apiClient;

        Text = "Access Request System - Login";
        Width = 400;
        Height = 280;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        MaximizeBox = false;

        var lblTitle = new Label
        {
            Text = "Privileged Access Management",
            Left = 30,
            Top = 20,
            Width = 320,
            Height = 25,
            Font = new Font(Font.FontFamily, 12, FontStyle.Bold)
        };

        var lblUser = new Label
        {
            Text = "Username:",
            Left = 30,
            Top = 60,
            Width = 100
        };

        _txtUsername = new TextBox
        {
            Left = 30,
            Top = 85,
            Width = 320,
            Text = "approver" // Pre-filled default for convenient dev testing
        };

        var lblPass = new Label
        {
            Text = "Password:",
            Left = 30,
            Top = 120,
            Width = 100
        };

        _txtPassword = new TextBox
        {
            Left = 30,
            Top = 145,
            Width = 320,
            UseSystemPasswordChar = true,
            Text = "Password123!" // Pre-filled default for convenient dev testing
        };

        _btnLogin = new Button
        {
            Text = "Sign In",
            Left = 240,
            Top = 185,
            Width = 110,
            Height = 32
        };
        _btnLogin.Click += async (_, _) => await HandleLoginAsync();

        _lblStatus = new Label
        {
            Left = 30,
            Top = 190,
            Width = 200,
            Height = 30,
            ForeColor = Color.Red
        };

        Controls.Add(lblTitle);
        Controls.Add(lblUser);
        Controls.Add(_txtUsername);
        Controls.Add(lblPass);
        Controls.Add(_txtPassword);
        Controls.Add(_btnLogin);
        Controls.Add(_lblStatus);

        AcceptButton = _btnLogin;
    }

    private async Task HandleLoginAsync()
    {
        var username = _txtUsername.Text.Trim();
        var password = _txtPassword.Text;

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            MessageBox.Show("Please enter both username and password.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _btnLogin.Enabled = false;
        _lblStatus.Text = "Authenticating...";
        _lblStatus.ForeColor = Color.DarkBlue;

        try
        {
            var response = await _apiClient.LoginAsync(username, password);

            // Hide login form and open main form
            Hide();
            var mainForm = new MainForm(_apiClient);
            mainForm.FormClosed += (_, _) => Close();
            mainForm.Show();
        }
        catch (ApiException ex)
        {
            _lblStatus.ForeColor = Color.Red;
            _lblStatus.Text = "Login failed.";
            MessageBox.Show(ex.Message, "Authentication Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        catch (Exception ex)
        {
            _lblStatus.ForeColor = Color.Red;
            _lblStatus.Text = "Connection error.";
            MessageBox.Show($"Unexpected error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _btnLogin.Enabled = true;
        }
    }
}
