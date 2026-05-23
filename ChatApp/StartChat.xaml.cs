using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;
using ChatApp.Database;
using ChatApp.Encryption;
using ChatApp.External;
using ChatApp.Models;
using ChatApp.Utilities;
using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
#if ANDROID
using CommunityToolkit.Maui.Alerts;
#endif

namespace ChatApp
{
    public partial class StartChat : ContentPage
    {
        private const string reportPath = "report.svg";
        private const string blockPath = "block.svg";
        private const string startCallPath = "startcall.svg";
        private const string toggleOnPath = "toggleon.svg";
        private const string toggleOffPath = "toggleoff.svg";
        private const string fail = "Fail";
        private const string call = "Call";
        private const string encrypted = "Encrypted";
        private const string noID = "noId";
        private const string Test = "Testing";
        private const string OK = "Ok";
        private const string notOK = "not ok";
        private const string sharedImage = "Shared Image";
        private const string getPendingMessageEndpoint = "api/Auth/GetPendingMessageIdsAsync";
        private const string reportAbuseEndpoint = "api/Auth/ReportAbuse";
        private const string sendNotificationEndpoint = "api/Auth/SendNotification";
        private const string deleteFromListEndpoint = "api/Auth/DeleteFriendFromList";
        private const string addPendingEndpoint = "api/Sync/AddPendingMessage/";
        private const string refreshTokenEndpoint = "api/Auth/RefreshToken2";
        private const string addPendingBlobEndpoint = "api/Sync/AddPendingBlob/";
        private string _friendUsername;
        private string _friendDeviceId;
        private HubConnection _connection;
        private string _groupId = string.Empty;
        private string _privateKey;
        private string _publicKey;
        string _user = string.Empty;
        private AppDbContext _context;
        private Message? selectedMessage;
        private bool _isFriendStillOnline = false;
        private bool _isFriendStillOffline = false;
        private bool _onlineFriend = false;
        private bool _InCall = false;
        private bool _InImageShare = false;
        private int pagenSize = 10;
        SyncClientService syncService;
        private bool _connectionDisposed = false;
        public ObservableCollection<Message> Messages { get; set; } =
            new ObservableCollection<Message>();
        private readonly SemaphoreSlim _messageLock = new SemaphoreSlim(1, 1);
        private FirebaseService firebaseService = new FirebaseService(new HttpClient());

        public StartChat(string friendUsername, string deviceId)
        {
            syncService = new SyncClientService(new HttpClient());
            _context = App.ServiceProvider!.GetRequiredService<AppDbContext>();
            _friendUsername = friendUsername;
            _InCall = false;
            _friendDeviceId = deviceId;
            _privateKey = LocalDbExtensions.RetrieveSecureString("_privateKey");
            _publicKey = LocalDbExtensions.RetrieveSecureString("_publicKey");
            _user = LocalDbExtensions.RetrievePreferences("_user");
            InitializeComponent();
            BindingContext = this;
            NavigationPage.SetHasNavigationBar(this, true);
            Shell.SetBackgroundColor(this, Color.FromArgb("#000000"));
            Shell.SetForegroundColor(this, Color.FromArgb("#FFFFFF"));

            var toolbarItem1 = new ToolbarItem
            {
                IconImageSource = reportPath,
                Command = new Command(OnReportTapped),
            };
            var toolbarItem3 = new ToolbarItem
            {
                IconImageSource = blockPath,
                Command = new Command(OnBlockTapped),
            };

            var toolbarItem = new ToolbarItem
            {
                IconImageSource = startCallPath,

                Command = new Command(OnCallTapped),
            };

            this.ToolbarItems.Add(toolbarItem);
            this.ToolbarItems.Add(toolbarItem1);
            this.ToolbarItems.Add(toolbarItem3);

            WeakReferenceMessenger.Default.Register<OnUpdateDatabase>(
                this,
                (r, message) =>
                {
                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        await ResendUndeliveredMessages();
                    });
                }
            );
            WeakReferenceMessenger.Default.Register<OnChangeInCallVariable>(
                this,
                (r, message) =>
                {
                    _InCall = true;
                }
            );
            WeakReferenceMessenger.Default.Register<OnChangeInCallVariableFalse>(
                this,
                (r, message) =>
                {
                    _InCall = false;
                }
            );
        }

        private async Task GetLastMessageIfSeen()
        {
            try
            {
                var lastMessage = await _context
                    .Messages.Where(tr => tr.SelectedReceiverId == _friendUsername)
                    .OrderByDescending(m => m.Timestamp)
                    .FirstOrDefaultAsync();

                if (lastMessage != null)
                {
                    var POSTresult = await firebaseService.PostDataAsync(
                        _friendUsername,
                        getPendingMessageEndpoint
                    );
                    if (!string.IsNullOrEmpty(POSTresult))
                    {
                        if (POSTresult == "false")
                        {
                            var messageView = Messages.FirstOrDefault(m =>
                                m.Id == lastMessage.MessageId
                            );

                            messageView!.IsDelivered = true;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        public async Task InitializeDataAsync()
        {
            await SyncService._syncSemaphore.WaitAsync();
            try
            {
                var getUnreadMSgs = (
                    from tr in _context.Messages
                    where tr.IsRead == false && tr.SelectedReceiverId == _friendUsername
                    select tr
                ).ToList();

                if (getUnreadMSgs.Count > 0)
                {
                    foreach (var item in getUnreadMSgs)
                    {
                        item.IsRead = true;
                    }
                    await _context.SaveChangesAsync();
                }

                await InitListWithMessages(1, 10);
            }
            finally
            {
                SyncService._syncSemaphore.Release();
            }
        }

        private async Task InitListWithMessages(int pageNumber, int pageSize)
        {
            try
            {
                var _dbList = await Task.Run(() =>
                {
                    return _context
                        .Messages.Where(t =>
                            t.SelectedReceiverId == _friendUsername && t.IsHidden == false
                        )
                        .OrderByDescending(t => t.Timestamp)
                        .Skip((pageNumber - 1) * pageSize)
                        .Take(pageSize)
                        .ToList();
                });
                MessagesCollectionView.ItemsSource = Messages;
                if (_dbList.Count > 0)
                {
                    for (int i = _dbList.Count - 1; i >= 0; i--)
                    {
                        var item = _dbList[i];
                        if (item.Content != encrypted)
                        {
                            var danceDecrypt = item.Content.Split(":").ToArray();
                            Messages.Add(
                                new Message
                                {
                                    Text = item.IsView ? "" : item.Content,
                                    Sender = item.SenderId,
                                    IsClient = item.IsClient,
                                    SentDate = item.Timestamp.ToString(),
                                    ShowWebView = item.IsView,
                                    Url = item.IsView
                                        ? Utilities.Configuration.FrontendURL
                                            + "view/index.html?&danceType="
                                            + danceDecrypt[0]
                                            + "&envId="
                                            + danceDecrypt[1]
                                        : null,
                                    Id = item.MessageId,
                                    IsRead = item.IsRead,
                                }
                            );
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private async Task InitializeSignalR()
        {
            if (_connection != null && _connection.State != HubConnectionState.Disconnected)
            {
                return;
            }
            if (_connection != null)
            {
                await _connection.DisposeAsync();
                _connection = null;
            }

            _connection = new HubConnectionBuilder()
                .WithUrl(
                    Utilities.Configuration.BackendURL + "chatHub",
                    options =>
                    {
                        options.Headers["Authorization"] =
                            $"Bearer {LocalDbExtensions.RetrieveSecureString("_jwttoken")}";
                    }
                )
                .Build();
            _connection.On<string>(
                "MessageDeliveryAcknowledged",
                (messageId) =>
                {
                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        var message = _context.Messages.FirstOrDefault(m =>
                            m.MessageId == messageId
                        );
                        if (message != null)
                        {
                            message.IsDelivered = true;
                            _context.Update(message);
                            await _context.SaveChangesAsync();
                        }
                    });

                    var messageView = Messages.FirstOrDefault(m => m.Id == messageId);
                    if (messageView != null)
                    {
                        messageView.IsDelivered = true;
                    }
                }
            );

            _connection.On<bool>(
                "ReceiveOnlineUser",
                (onlineUser) =>
                {
                    if (onlineUser)
                    {
                        if (_isFriendStillOnline == false)
                        {
                            _isFriendStillOffline = false;
                            SetCustomTitle(true);
                            _isFriendStillOnline = true;
                            _onlineFriend = true;
                        }
                    }
                    else
                    {
                        if (_isFriendStillOffline == false)
                        {
                            _isFriendStillOnline = false;
                            SetCustomTitle(false);
                            _isFriendStillOffline = true;
                            _onlineFriend = false;
                        }
                    }
                }
            );
            _connection.On<string, string, string>(
                "ReceiveMessage",
                async (sender, messageRes, messgeSender) =>
                {
                    try
                    {
                        var isClient = sender == _user;
                        var decryptedMessage =
                            sender == _user
                                ? EncryptionRSA.DecryptMessage(messgeSender, _privateKey)
                                : EncryptionRSA.DecryptMessage(messageRes, _privateKey);
                        var decryptedMessageArray = decryptedMessage.Split(":guid");

                        if (!isClient)
                        {
                            await _connection.InvokeAsync(
                                "MessageDelivered",
                                decryptedMessageArray[0],
                                sender
                            );
                        }
                        var containsDance =
                            decryptedMessageArray.Length == 2
                            && decryptedMessageArray[1].Contains("Charc");
                        var containDanceDecrypt = containsDance
                            ? decryptedMessageArray[1].Split(":").ToArray()
                            : null;

                        var newMessage = new Message
                        {
                            Sender = sender,
                            IsClient = isClient,
                            SentDate = DateTime.Now.ToString("yy-MM-dd  HH:mm"),
                            Id = decryptedMessageArray[0],
                            Text = containsDance ? "" : decryptedMessageArray[1],
                            ShowWebView = containsDance ? true : false,
                            Url =
                                containsDance && containDanceDecrypt != null
                                    ? Utilities.Configuration.FrontendURL
                                        + "view/index.html?&danceType="
                                        + containDanceDecrypt[0]
                                        + "&envId="
                                        + containDanceDecrypt[1]
                                    : "",
                        };

                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            var tvar = (
                                from b in Messages
                                where b.Id == noID
                                select b
                            ).FirstOrDefault();
                            if (tvar != null && tvar.IsClient == isClient)
                            {
                                tvar.Id = newMessage.Id;
                            }
                            else
                            {
                                Messages.Add(newMessage);
                                MessagesCollectionView.ScrollTo(
                                    Messages.Last(),
                                    position: ScrollToPosition.End,
                                    animate: false
                                );
                            }
                        });

                        if (newMessage.Text != encrypted)
                        {
                            await _context.Messages.AddAsync(
                                new dBMessage
                                {
                                    SenderId = sender,
                                    Content = decryptedMessageArray[1],
                                    IsClient = isClient,
                                    ReceiverId = isClient ? _friendUsername : _user,
                                    SelectedReceiverId = _friendUsername,
                                    IsRead = true,
                                    IsDelivered = isClient ? false : true,
                                    IsView = containsDance ? true : false,
                                    Timestamp = DateTime.Now,
                                    MessageId = decryptedMessageArray[0],
                                    IsHidden = false,
                                }
                            );

                            await _context.SaveChangesAsync();
                        }

                        if (isClient && !_onlineFriend)
                        {
                            MainThread.BeginInvokeOnMainThread(async () =>
                            {
                                await SendSingleMessage();
                            });
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }
            );

            try
            {
                if (_connection != null)
                {
                    await _connection.StartAsync();
                    if (_connection.State == HubConnectionState.Connected)
                    {
                        await _connection.InvokeAsync("SharePublicKey", _publicKey);
                    }

                    _groupId = ChatExtensions.GenerateGroupId(_user, _friendUsername);
                    if (!string.IsNullOrEmpty(_groupId))
                    {
                        if (_connection.State == HubConnectionState.Connected)
                        {
                            await _connection.InvokeAsync("JoinPrivateChat", _friendUsername);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SignalR Connection Error: {ex.Message}");
            }
        }

        private void SetCustomTitle(bool status)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (status)
                {
                    var exist = this.ToolbarItems.LastOrDefault();
                    if (this.ToolbarItems.Count == 4 && exist != null)
                    {
                        this.ToolbarItems.Remove(exist);
                    }

                    var toolbarItem = new ToolbarItem { IconImageSource = toggleOnPath };

                    this.ToolbarItems.Add(toolbarItem);
                }
                else
                {
                    var exist = this.ToolbarItems.LastOrDefault();
                    if (this.ToolbarItems.Count == 4 && exist != null)
                    {
                        this.ToolbarItems.Remove(exist);
                    }

                    var toolbarItem = new ToolbarItem { IconImageSource = toggleOffPath };

                    this.ToolbarItems.Add(toolbarItem);
                }
            });
        }

        private async void OnReportTapped()
        {
            if (_friendUsername == Test)
            {
                return;
            }

            string result = await DisplayPromptAsync(
                "Report a child abuse!",
                "Describe the abuse issue!"
            );

            if (!string.IsNullOrWhiteSpace(result))
            {
                bool isValid = Regex.IsMatch(result, @"^[a-zA-Z0-9 ]+$");

                if (isValid)
                {
                    var POSTresult = await firebaseService.PostDataTwoAsync(
                        _friendUsername,
                        result,
                        reportAbuseEndpoint
                    );
                    await DisplayAlert("Reported", "Reported!", OK);
                }
                else
                {
                    await DisplayAlert(fail, "Only Chars and numbers are allowed!", OK);
                }
            }
        }

        private async void OnCallTapped()
        {
            if (_friendUsername != Test)
            {
                ConnectionHubService connectionHubService = new ConnectionHubService();
                var POSTresult = await firebaseService.PostDataTwoAsync(
                    _friendDeviceId,
                    call,
                    sendNotificationEndpoint
                );
                _InCall = true;
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    var t = new Calling(_friendUsername);
                    Navigation.PushAsync(t, false);
                });
            }
        }

        private void OnBlockTapped()
        {
            if (_friendUsername == Test)
            {
                return;
            }

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                bool answer = await DisplayAlert(
                    "Confirmation",
                    "Are you sure you want to block this user?",
                    "Yes",
                    "No"
                );

                if (answer)
                {
                    var POSTresult = await firebaseService.PostDataAsync(
                        _friendUsername,
                        deleteFromListEndpoint
                    );
                    WeakReferenceMessenger.Default.Send(new PullMainListEvent());
                    var tpage = new MainPage();
                    await Navigation.PushAsync(tpage, false);
                }
            });
        }

        protected override async void OnDisappearing()
        {
            if (_InImageShare)
            {
                return;
            }
            base.OnDisappearing();

            if (_InCall == false)
            {
                pagenSize = 10;

                try
                {
                    if (_connection != null && !_connectionDisposed)
                    {
                        await _connection.StopAsync();
                        await _connection.DisposeAsync();
                        _connectionDisposed = true;
                        _connection = null;
                        Console.WriteLine("SignalR connection disposed.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                finally
                {
                    _connectionDisposed = true;
                    _connection = null;
                }
                await Shell.Current.Navigation.PopToRootAsync();
            }
        }

        private async Task EncryptMessage(string text)
        {
            try
            {
                if (_connection != null)
                {
                    var otherUserPublicKey = await _connection.InvokeAsync<string?>(
                        "GetPublicKey",
                        _friendUsername
                    );
                    if (otherUserPublicKey == "null")
                    {
                        var encryptSender = EncryptionRSA.EncryptMessage(text, _publicKey);
                        await _connection.InvokeAsync(
                            "SendMessageToGroup",
                            _groupId,
                            null,
                            encryptSender
                        );
                    }
                    else
                    {
                        var encryptRes = EncryptionRSA.EncryptMessage(text, otherUserPublicKey!);
                        var encryptSender = EncryptionRSA.EncryptMessage(text, _publicKey);
                        await _connection.InvokeAsync(
                            "SendMessageToGroup",
                            _groupId,
                            encryptRes,
                            encryptSender
                        );
                    }
                }
            }
            catch (Exception w)
            {
                Console.WriteLine(w.Message);
            }
        }

        private string EncryptMessageFirebase(string text)
        {
            var encryptRes = EncryptionRSA.EncryptString(text, _friendDeviceId, _friendDeviceId);
            return encryptRes;
        }

        private async Task ResendUndeliveredMessages()
        {
            await _messageLock.WaitAsync();

            try
            {
                var messagesToResend = await _context
                    .Messages.Where(msg =>
                        !msg.IsDelivered
                        && msg.SenderId == _user
                        && msg.SelectedReceiverId == _friendUsername
                    )
                    .ToListAsync();

                if (
                    string.IsNullOrEmpty(_user)
                    || string.IsNullOrEmpty(_friendUsername)
                    || string.IsNullOrEmpty(_friendDeviceId)
                    || messagesToResend == null
                    || messagesToResend.Count == 0
                )
                {
                    return;
                }

                bool exist = false;
                foreach (var msg in messagesToResend)
                {
                    var messageView = Messages.FirstOrDefault(m => m.Id == msg.MessageId);
                    if (messageView != null)
                    {
                        messageView.IsSending = true;
                    }

                    if (
                        !msg.IsDelivered
                        && (DateTime.Now - msg.Timestamp) > TimeSpan.FromSeconds(6)
                    )
                    {
                        exist = true;

                        if (msg.IsView)
                        {
                            if (_friendUsername != Test)
                            {
                                var request = await syncService.PostPendingMessage(
                                    new FireMessage
                                    {
                                        Id = Guid.NewGuid().ToString(),
                                        Sender = _user,
                                        SentDate = DateTime.Now,
                                        Text = EncryptMessageFirebase(ChatExtensions.charId),
                                        ShowImageView = false,
                                        ShowWebView = true,
                                        IsClient = false,
                                    },
                                    addPendingEndpoint + msg.SelectedReceiverId
                                );
                            }

                            msg.IsDelivered = true;
                            _context.Update(msg);
                            await _context.SaveChangesAsync();
                        }
                        else
                        {
                            if (_friendUsername != Test)
                            {
                                var request = await syncService.PostPendingMessage(
                                    new FireMessage
                                    {
                                        Id = Guid.NewGuid().ToString(),
                                        Sender = _user,
                                        SentDate = DateTime.Now,
                                        Text = EncryptMessageFirebase(msg.Content),
                                        ShowImageView = false,
                                        ShowWebView = false,
                                        IsClient = false,
                                    },
                                    addPendingEndpoint + msg.SelectedReceiverId
                                );
                            }
                            msg.IsDelivered = true;
                            _context.Update(msg);
                            await _context.SaveChangesAsync();
                        }
                        if (messageView != null)
                        {
                            messageView.IsSending = false;
                        }
                    }
                }

                if (exist)
                {
                    if (_friendUsername != Test)
                    {
                        var getLastMessage = (from t in messagesToResend select t).LastOrDefault();
                        var POSTresult = await firebaseService.PostDataTwoAsync(
                            _friendDeviceId,
                            EncryptMessageFirebase(getLastMessage!.Content),
                            sendNotificationEndpoint
                        );
                        exist = false;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            finally
            {
                _messageLock.Release();
            }
        }

        private async Task SendSingleMessage()
        {
            await _messageLock.WaitAsync();

            try
            {
                var messagesToResend = await _context
                    .Messages.Where(msg =>
                        !msg.IsDelivered
                        && msg.SenderId == _user
                        && msg.SelectedReceiverId == _friendUsername
                    )
                    .ToListAsync();

                if (
                    string.IsNullOrEmpty(_user)
                    || string.IsNullOrEmpty(_friendUsername)
                    || string.IsNullOrEmpty(_friendDeviceId)
                    || messagesToResend == null
                    || messagesToResend.Count == 0
                )
                {
                    return;
                }

                foreach (var msg in messagesToResend)
                {
                    var messageView = Messages.FirstOrDefault(m => m.Id == msg.MessageId);

                    if (!msg.IsDelivered)
                    {
                        if (msg.IsView)
                        {
                            if (_friendUsername != Test)
                            {
                                var r = await syncService.PostPendingMessage(
                                    new FireMessage
                                    {
                                        Id = Guid.NewGuid().ToString(),
                                        Sender = _user,
                                        SentDate = DateTime.Now,
                                        Text = EncryptMessageFirebase(ChatExtensions.charId),
                                        ShowImageView = false,
                                        ShowWebView = true,
                                        IsClient = false,
                                    },
                                    addPendingEndpoint + msg.SelectedReceiverId
                                );

                                if (r)
                                {
                                    messageView!.IsOkFromHttpFirebase = true;
                                }
                            }

                            msg.IsDelivered = true;
                            _context.Update(msg);
                            await _context.SaveChangesAsync();
                        }
                        else
                        {
                            if (_friendUsername != Test)
                            {
                                var endpoint = await syncService.PostPendingMessage(
                                    new FireMessage
                                    {
                                        Id = Guid.NewGuid().ToString(),
                                        Sender = _user,
                                        SentDate = DateTime.Now,
                                        Text = EncryptMessageFirebase(msg.Content),
                                        ShowImageView = false,
                                        ShowWebView = false,
                                        IsClient = false,
                                    },
                                    addPendingEndpoint + msg.SelectedReceiverId
                                );

                                if (endpoint)
                                {
                                    messageView!.IsOkFromHttpFirebase = true;
                                }
                            }

                            msg.IsDelivered = true;
                            _context.Update(msg);
                            await _context.SaveChangesAsync();
                        }
                    }
                    var POSTresult = await firebaseService.PostDataTwoAsync(
                        _friendDeviceId,
                        EncryptMessageFirebase(msg.Content),
                        sendNotificationEndpoint
                    );

                    if (POSTresult != null)
                    {
                        if (POSTresult == OK && messageView != null)
                        {
                            messageView.IsOkFromHttpFirebase = false;
                            messageView.IsOkFromNotification = true;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            finally
            {
                _messageLock.Release();
            }
        }

#if ANDROID
        private async void alerts(string val)
        {
            await Task.Run(async () =>
            {
                await Snackbar.Make(val, duration: TimeSpan.FromMilliseconds(900)).Show();
            });
        }
#endif

        private async Task DanceFromInput(string input, char splitBy)
        {
            var trytosplit = input[..^1].Split(splitBy);

            int ctoInt = 0;
            bool isNumeric = int.TryParse(trytosplit[1], out ctoInt);

            if (isNumeric)
            {
                if (ctoInt < 107)
                {
                    var clist = ChatExtensions.CharsList;
                    string search = $"Charc{ctoInt}__";
                    var rWord = clist.FirstOrDefault(x => x.StartsWith(search));

                    var createdGuid = Guid.NewGuid().ToString();

                    var env =
                        splitBy == 'a' ? "default0"
                        : splitBy == 'b' ? "default"
                        : splitBy == 'c' ? "forest"
                        : splitBy == 'd' ? "desert"
                        : splitBy == 'e' ? "snow"
                        : splitBy == 'f' ? "beach"
                        : splitBy == 'g' ? "space"
                        : splitBy == 'h' ? "city"
                        : splitBy == 'i' ? "mountain"
                        : splitBy == 'j' ? "underwater"
                        : "default0";

                    var finalres = rWord + ":" + env;
                    ChatExtensions.charId = finalres;

                    await Task.Run(async () =>
                    {
                        await EncryptMessage(createdGuid + ":guid" + finalres);
                    });
                }
            }
        }

        private async void SendMessageButton_Clicked(object sender, EventArgs e)
        {
            var getTextInput = MessageEntry.Text;

            if (string.IsNullOrEmpty(getTextInput))
            {
                return;
            }

            if (_friendUsername != Test)
            {
                if (
                    getTextInput.Length is > 1 and < 5
                    && getTextInput[^1] == '#'
                    && getTextInput[0] is >= 'a' and <= 'j'
                )
                {
                    await DanceFromInput(getTextInput, getTextInput[0]);
                    MessageEntry.Text = string.Empty;
                }
                else
                {
                    Messages.Add(
                        new Message
                        {
                            Id = noID,
                            Sender = _user,
                            Text = MessageEntry.Text,
                            IsClient = true,
                            SentDate = DateTime.Now.ToString("yy-MM-dd  HH:mm"),
                            IsRead = false,
                            Url = null,
                            ShowWebView = false,
                        }
                    );
                    MessageEntry.Text = string.Empty;
                    MessagesCollectionView.ScrollTo(
                        Messages.Last(),
                        position: ScrollToPosition.End,
                        animate: false
                    );
                    var createGuid = Guid.NewGuid().ToString();

                    if (_connection != null)
                    {
                        try
                        {
                            await _connection.InvokeAsync("GetOnlineUser", _friendUsername);
                        }
                        catch (Exception ex)
                        {
                            var _jwt = LocalDbExtensions.RetrieveSecureString("_jwttoken");
                            var client = new HttpClient();
                            client.DefaultRequestHeaders.Authorization =
                                new AuthenticationHeaderValue("Bearer", _jwt);
                            var firebaseService2 = new FirebaseService(client);
                            var bearer = await firebaseService2.PostDataAsync(
                                "",
                                refreshTokenEndpoint
                            );

                            if (bearer == notOK) { }
                            else
                            {
                                LocalDbExtensions.SaveSecureString("_jwttoken", bearer);
                            }
#if ANDROID
                            alerts("Message not sent try again!");
#endif
                        }
                    }

                    try
                    {
                        await Task.Run(async () =>
                        {
                            await EncryptMessage(createGuid + ":guid" + getTextInput);
                        });
                    }
                    catch (Exception ee)
                    {
                        Console.WriteLine($"An error occurred: {ee.Message}");
                    }
                }
            }
            else if (_friendUsername == Test)
            {
                if (
                    getTextInput.Length is > 1 and < 5
                    && getTextInput[^1] == '#'
                    && getTextInput[0] is >= 'a' and <= 'j'
                )
                {
                    var trytosplit = getTextInput[..^1].Split(getTextInput[0]);

                    int ctoInt = 0;
                    bool isNumeric = int.TryParse(trytosplit[1], out ctoInt);

                    if (isNumeric)
                    {
                        if (ctoInt < 107)
                        {
                            var clist = ChatExtensions.CharsList;
                            string search = $"Charc{ctoInt}__";
                            var rWord = clist.FirstOrDefault(x => x.StartsWith(search));

                            var createdGuid = Guid.NewGuid().ToString();

                            var env =
                                getTextInput[0] == 'a' ? "default0"
                                : getTextInput[0] == 'b' ? "default"
                                : getTextInput[0] == 'c' ? "forest"
                                : getTextInput[0] == 'd' ? "desert"
                                : getTextInput[0] == 'e' ? "snow"
                                : getTextInput[0] == 'f' ? "beach"
                                : getTextInput[0] == 'g' ? "space"
                                : getTextInput[0] == 'h' ? "city"
                                : getTextInput[0] == 'i' ? "mountain"
                                : getTextInput[0] == 'j' ? "underwater"
                                : "default0";

                            var newMessage = new Message
                            {
                                Sender = _user,
                                IsClient = true,
                                SentDate = DateTime.Now.ToString("yy-MM-dd  HH:mm"),
                                Id = noID,
                                Text = "",
                                ShowWebView = true,
                                Url =
                                    Utilities.Configuration.FrontendURL
                                    + "view/index.html?&danceType="
                                    + rWord
                                    + "&envId="
                                    + env,
                            };
                            Messages.Add(newMessage);
                            MessagesCollectionView.ScrollTo(
                                Messages.Last(),
                                position: ScrollToPosition.End,
                                animate: false
                            );
                            MessageEntry.Text = string.Empty;
                        }
                    }
                }
                else
                {
                    Messages.Add(
                        new Message
                        {
                            Id = noID,
                            Sender = _user,
                            Text = MessageEntry.Text,
                            IsClient = true,
                            SentDate = DateTime.Now.ToString("yy-MM-dd  HH:mm"),
                            IsRead = false,
                            Url = null,
                            ShowWebView = false,
                        }
                    );
                    MessageEntry.Text = string.Empty;
                    MessagesCollectionView.ScrollTo(
                        Messages.Last(),
                        position: ScrollToPosition.End,
                        animate: false
                    );
                }
            }
        }

        private void OnStackLayoutTapped(object sender, TappedEventArgs e)
        {
            if (sender is StackLayout stackLayout)
            {
                var pointY = e.GetPosition(this)!.Value.Y;

                var tappedItem = (Message)stackLayout.BindingContext;

                if (tappedItem != null)
                {
                    selectedMessage = tappedItem;

                    if (selectedMessage!.IsClient == true)
                    {
                        if (selectedMessage.ShowWebView)
                        {
                            ContextMenu.Children.Clear();
                            AddMenuItem("Delete", "deleteStickerMe");
                            AddMenuItem("Close", "Close");
                            AddMenuItem(selectedMessage.SentDate, "None");
                        }
                        else
                        {
                            ContextMenu.Children.Clear();
                            AddMenuItem("Copy", "copyText");
                            AddMenuItem("Delete", "deleteTextMe");
                            AddMenuItem("More Messages", "loadMore");
                            AddMenuItem("Close", "Close");
                            AddMenuItem(selectedMessage.SentDate, "None");
                        }
                    }
                    else
                    {
                        if (selectedMessage.ShowWebView)
                        {
                            ContextMenu.Children.Clear();
                            AddMenuItem("Close", "Close");
                            AddMenuItem(selectedMessage.SentDate, "None");
                        }
                        else
                        {
                            ContextMenu.Children.Clear();
                            AddMenuItem("Copy", "copyText");
                            AddMenuItem("More Messages", "loadMore");
                            AddMenuItem("Close", "Close");
                            AddMenuItem(selectedMessage.SentDate, "None");
                        }
                    }
                    ContextMenu.TranslationY = pointY - 600;
                    ContextMenu.IsVisible = true;
                }
                else
                {
                    Console.WriteLine("BindingContext is null.");
                }
            }
            else
            {
                Console.WriteLine("Sender is not a StackLayout.");
            }
        }

        private void AddMenuItem(string labelText, string commandParameter)
        {
            var label = new Label
            {
                Text = labelText,
                TextColor = Colors.Black,
                Padding = new Thickness(10),
            };

            // Add TapGestureRecognizer to the label
            var tapGestureRecognizer = new TapGestureRecognizer
            {
                CommandParameter = commandParameter,
            };
            tapGestureRecognizer.Tapped += OnMenuItemTapped!; // Existing event handler
            label.GestureRecognizers.Add(tapGestureRecognizer);

            // Add the label to the StackLayout
            ContextMenu.Children.Add(label);
        }

        private async void OnMenuItemTapped(object sender, EventArgs e)
        {
            var tappedLabel = sender as Label;

            if (tappedLabel != null)
            {
                var option =
                    tappedLabel.GestureRecognizers.FirstOrDefault() as TapGestureRecognizer;

                if (option != null && option.CommandParameter != null)
                {
                    var commandParameter = option.CommandParameter.ToString();
                    if (commandParameter == "copyText")
                    {
                        await Clipboard.SetTextAsync(selectedMessage!.Text);
                    }
                    else if (commandParameter == "loadMore")
                    {
                        Messages.Clear();
                        pagenSize = pagenSize + 10;
                        await InitListWithMessages(1, pagenSize);
                    }
                    else if (
                        commandParameter == "deleteTextMe"
                        || commandParameter == "deleteStickerMe"
                        || commandParameter == "deleteImageMe"
                    )
                    {
                        var toDelete = (
                            from b in Messages
                            where b.Id == selectedMessage!.Id
                            select b
                        ).FirstOrDefault();
                        if (toDelete != null)
                        {
                            Messages.Remove(toDelete);
                            var deletefromactualDb = (
                                from b in _context.Messages
                                where b.MessageId == selectedMessage!.Id
                                select b
                            ).FirstOrDefault();

                            if (deletefromactualDb != null)
                            {
                                _context.Messages.Remove(deletefromactualDb);
                                await _context.SaveChangesAsync();
                            }
                        }
                    }
                    else
                    {
                        ContextMenu.IsVisible = false;
                    }
                    ContextMenu.IsVisible = false;
                }
            }
        }

        private async void OnPageLoaded(object sender, EventArgs e)
        {
            if (_friendUsername != Test)
            {
                var tasks = new List<Task>
                {
                    InitializeDataAsync(),
                    InitializeSignalR(),
                    GetLastMessageIfSeen(),
                };

                await Task.WhenAll(tasks);
            }

            if (Messages.Count > 0)
            {
                MessagesCollectionView.ScrollTo(
                    Messages.Last(),
                    position: ScrollToPosition.End,
                    animate: false
                );
            }
        }

        private async void ShowDanceBtn(object sender, EventArgs e)
        {
            var popup = new CharsPopup();
            var r = await this.ShowPopupAsync(popup);
            var createdGuid = Guid.NewGuid().ToString();

            var getCharId = ChatExtensions.charId;

            if (string.IsNullOrEmpty(getCharId))
            {
                return;
            }
            if (_friendUsername != Test)
            {
                await Task.Run(async () =>
                {
                    await EncryptMessage(createdGuid + ":guid" + getCharId);
                });
            }
            else if (_friendUsername == Test)
            {
                string rWord = "";
                string env = "";

                if (!string.IsNullOrEmpty(getCharId))
                {
                    var getfromstudio = getCharId.Split(":");
                    rWord = getfromstudio[0];
                    env = getfromstudio[1];
                }

                var newMessage = new Message
                {
                    Sender = _user,
                    IsClient = true,
                    SentDate = DateTime.Now.ToString("yy-MM-dd  HH:mm"),
                    Id = noID,
                    Text = "",
                    ShowWebView = true,
                    Url =
                        Utilities.Configuration.FrontendURL
                        + "view/index.html?&danceType="
                        + rWord
                        + "&envId="
                        + env,
                };
                Messages.Add(newMessage);
                MessagesCollectionView.ScrollTo(
                    Messages.Last(),
                    position: ScrollToPosition.End,
                    animate: false
                );
            }
        }

        private async Task<byte[]> GetImageBytesFromStream(Stream stream)
        {
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            return memoryStream.ToArray();
        }

        private async void OnPickImageClicked(object sender, EventArgs e)
        {
            _InImageShare = true;
            ChatExtensions.screenIsOff = true;
            try
            {
                var result = await MediaPicker.PickPhotoAsync(
                    new MediaPickerOptions { Title = "Select a photo" }
                );

                if (result != null)
                {
                    var stream = await result.OpenReadAsync();
                    var barray = await GetImageBytesFromStream(stream);
                    string base645 = Convert.ToBase64String(barray);

                    byte[] compressed = await CompressExtensions.CompressImageAsync(
                        barray,
                        maxWidth: 1024,
                        maxHeight: 1024,
                        jpegQuality: 30
                    );

                    string base64 = Convert.ToBase64String(compressed);

                    var encryptedBytes = EncryptionRSA.EncryptNew(
                        compressed,
                        _friendDeviceId,
                        _friendDeviceId
                    );
                    string base604 = Convert.ToBase64String(encryptedBytes);

                    var r = await syncService.PostPendingMessage(
                        new FireMessage
                        {
                            Id = Guid.NewGuid().ToString(),
                            Sender = _user,
                            SentDate = DateTime.Now,
                            Text = base604,
                            ShowImageView = false,
                            ShowWebView = true,
                            IsClient = false,
                        },
                        addPendingBlobEndpoint + _friendUsername
                    );

                    if (r)
                    {
                        await EncryptMessage(Guid.NewGuid() + ":guid" + sharedImage);
                    }
                    _InImageShare = false;
                    ChatExtensions.screenIsOff = false;
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"An error occurred: {ex.Message}", OK);
            }
        }
    }
}
