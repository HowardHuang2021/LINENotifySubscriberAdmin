namespace LINENotifySubscriberAdmin.Models
{
    public class Subscriber
    {
        public int Id { get; set; }
        public string UserId { get; set; } 
        public string Username { get; set; }
        public string Email { get; set; }
        public string LINENotifyAccessToken { get; set; }
        public string LINELoginAccessToken { get; set; }
        public string LINELoginIdToken { get; set; }
    }
}
