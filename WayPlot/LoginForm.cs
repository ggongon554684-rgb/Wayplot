using WayPlot.domain.Contracts;
using WayPlot.infrastructure.Data.Seed;
using WayPlot.infrastructure.Tenancy;
using WayPlot.UI;
using WayPlot.Views;

namespace WayPlot;

/// <summary>Sign-in screen. The layout is in LoginForm.Designer.cs; this file only holds behavior.</summary>
public partial class LoginForm : Form
{
    private readonly AuthClient _auth;
    private readonly SessionStarter _sessionStarter;
    private readonly TenantSeeder _seeder;
    private readonly Func<LoginResult, MainShell> _shellFactory;

    public LoginForm(AuthClient auth, SessionStarter sessionStarter, TenantSeeder seeder, Func<LoginResult, MainShell> shellFactory)
    {
        InitializeComponent();

        _auth = auth;
        _sessionStarter = sessionStarter;
        _seeder = seeder;
        _shellFactory = shellFactory;

        ApplyTheme();
    }

    /// <summary>Colors and fonts come from the shared Theme class, so they change in one place.</summary>
    private void ApplyTheme()
    {
        BackColor = Theme.Page;
        lblTitle.Font = Theme.Heading;
        lblTitle.ForeColor = Theme.Navy;
        lblMessage.ForeColor = Theme.Muted;

        btnSignIn.FlatStyle = FlatStyle.Flat;
        btnSignIn.FlatAppearance.BorderColor = Theme.Navy;
        btnSignIn.BackColor = Theme.Navy;
        btnSignIn.ForeColor = Color.White;
        btnSignIn.Font = Theme.BodyBold;
        btnSignIn.Cursor = Cursors.Hand;
        btnSignIn.Height = Math.Max(btnSignIn.Height, Theme.ButtonHeight);
    }

    private async void btnSignIn_Click(object? sender, EventArgs e)
    {
        btnSignIn.Enabled = false;
        try
        {
            var login = await _auth.LoginAsync(txtUser.Text.Trim(), txtPass.Text);
            if (login is null)
            {
                lblMessage.Text = "Invalid username or password.";
                return;
            }

            // Point the app at this user's tenant database and make sure the user has a Staff row there.
            var started = await _sessionStarter.StartAsync(login);
            if (!started.Ok)
            {
                lblMessage.Text = started.Message;
                return;
            }

            // Demo data: runs only when the tenant has no parcels yet, and writes through the services.
            lblMessage.Text = "Preparing your workspace...";
            var seeded = await _seeder.SeedAsync(login.TenantName);
            if (!seeded.Ok)
                MessageBox.Show(this, seeded.Message, "WayPlot CRM", MessageBoxButtons.OK, MessageBoxIcon.Warning);

            txtPass.Clear();
            OpenShell(login);
        }
        catch (HttpRequestException ex)
        {
            lblMessage.Text = "Cannot reach the server: " + ex.Message;
        }
        catch (Exception ex)
        {
            lblMessage.Text = "Could not start the session: " + ex.Message;
        }
        finally
        {
            btnSignIn.Enabled = true;
        }
    }

    private void OpenShell(LoginResult login)
    {
        var shell = _shellFactory(login);
        shell.FormClosed += (_, _) => Close();   // closing the main window ends the app
        Hide();
        shell.Show();
    }
}