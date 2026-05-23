namespace ChatApp;

public partial class ReportAbuse : ContentPage
{
	public ReportAbuse()
	{
		InitializeComponent();
	}
    private async void OnReportAbuseClicked(object sender, EventArgs e)
    {
        var message = new EmailMessage
        {
            Subject = "Report Abuse",
            Body = "Please describe the issue you encountered.",
            To = new List<string> { "chit.cg@outlook.com" }
        };

        await Email.ComposeAsync(message);
    }
}