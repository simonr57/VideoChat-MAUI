namespace ChatApp.Models
{
    public class OnFriendSuggestAdd
    {
        public string FriendName { get; set; }

        public OnFriendSuggestAdd(string friendName)
        {
            FriendName = friendName;
        }
    }

    public class OnFriendrequestAdd
    {
        public string FriendName { get; set; }

        public OnFriendrequestAdd(string friendName)
        {
            FriendName = friendName;
        }
    }

    public class OnChatOpen
    {
        public string FriendName { get; set; }

        public OnChatOpen(string friendName)
        {
            FriendName = friendName;
        }
    }

    public class PullMainListEvent { }

    public class SaveChatEvent { }

    public class StartCallEvent { }

    public class OnScreenIsOff { }

    public class OnUpdateDatabase { }

    public class OnChangeInCallVariable { }

    public class OnChangeInCallVariableFalse { }
}
