using System.ComponentModel;
using ChatApp.Utilities;

namespace ChatApp.Models
{
    public class ProfileUser : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public string? Username { get; set; }
        private Message? _latestMessage;
        public Message? LatestMessage
        {
            get => _latestMessage;
            set
            {
                if (_latestMessage != value)
                {
                    _latestMessage = value;
                    OnPropertyChanged(nameof(LatestMessage));
                }
            }
        }
        public required string Base64Image { get; set; }
        public ImageSource ImageSource =>
            ImageSource.FromStream(() =>
            {
                var bytes = Convert.FromBase64String(Base64Image);

                return new MemoryStream(bytes);
            });

        public string? DeviceId { get; set; }
        public bool IsStory => Username == ChatExtensions.StoryUserName;
    }
}
