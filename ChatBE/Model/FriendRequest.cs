namespace ChatBE.Model
{
    public class FriendRequest
    {
        public required string RequestId { get; set; }  // Unique ID combining user1Id and user2Id
        public required string SenderUserId { get; set; }  // The user who sent the request
        public required string ReceiverUserId { get; set; }  // The user who received the request
        public required string Status { get; set; }    // Status of the request ("pending", "accepted", "rejected")
        public DateTime SentAt { get; set; }  // Timestamp when the request was sent
    }

    public class FriendRequestDto
    {

        public required string SenderUserId { get; set; }  // The user who sent the request
        public required string ReceiverUserId { get; set; }  // The user who received the request
        public required string Status { get; set; }    // Status of the request ("pending", "accepted", "rejected")
        public DateTime SentAt { get; set; }  // Timestamp when the request was sent

    }
}
