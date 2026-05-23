using System.ComponentModel;

namespace ChatApp.Models
{
    public class Message : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private bool IsValidGlbUrl(string? url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return false;

            try
            {
                var uri = new Uri(url);
                return uri.Host == Utilities.Configuration.FrontendNoHTTP
                    && uri.AbsolutePath.Contains("/view/index.html")
                    && uri.Query.Contains("danceType=")
                    && uri.Query.Contains("envId=");
            }
            catch (UriFormatException)
            {
                return false;
            }
        }

        public required string Id { get; set; }
        public required string Sender { get; set; }
        public required string Text { get; set; }
        public required bool IsClient { get; set; }

        private string? _url;
        public string? Url
        {
            get => _url;
            set
            {
                if (_url != value)
                {
                    _url = IsValidGlbUrl(value) ? value : "about:blank";
                    OnPropertyChanged(nameof(Url));
                }
            }
        }
        public bool ShowWebView { get; set; }
        public required string SentDate { get; set; }
        public bool? IsRead { get; set; }

        private bool _isDelivered;
        public bool IsDelivered
        {
            get => _isDelivered;
            set
            {
                if (_isDelivered != value)
                {
                    _isDelivered = value;
                    OnPropertyChanged(nameof(IsDelivered));
                }
            }
        }

        private bool _isSending;
        public bool IsSending
        {
            get => _isSending;
            set
            {
                if (_isSending != value)
                {
                    _isSending = value;
                    OnPropertyChanged(nameof(IsSending));
                }
            }
        }

        public bool ShowTextOnly => !ShowWebView;

        private bool _isOkFromHttpFirebase;
        public bool IsOkFromHttpFirebase
        {
            get => _isOkFromHttpFirebase;
            set
            {
                if (_isOkFromHttpFirebase != value)
                {
                    _isOkFromHttpFirebase = value;
                    OnPropertyChanged(nameof(IsOkFromHttpFirebase));
                }
            }
        }

        private bool _isOkFromNotification;
        public bool IsOkFromNotification
        {
            get => _isOkFromNotification;
            set
            {
                if (_isOkFromNotification != value)
                {
                    _isOkFromNotification = value;
                    OnPropertyChanged(nameof(IsOkFromNotification));
                }
            }
        }
    }
}
