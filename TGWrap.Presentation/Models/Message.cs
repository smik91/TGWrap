namespace TGWrap.Presentation.Models
{
    public class Message
    {
        public long id { get; set; }
        public string type { get; set; }
        public DateTime date { get; set; }
        public string date_unixtime { get; set; }
        public string from { get; set; }
        public string from_id { get; set; }
        public string forwarded_from { get; set; }
        public string saved_from { get; set; }
        public object text { get; set; }
        public TextEntity[] text_entities { get; set; }
        public string photo { get; set; }
        public int width { get; set; }
        public int height { get; set; }
        public string file { get; set; }
        public string thumbnail { get; set; }
        public string mime_type { get; set; }
        public string media_type { get; set; }
        public string sticker_emoji { get; set; }
        public DateTime edited { get; set; }
        public string edited_unixtime { get; set; }
        public int duration_seconds { get; set; }
    }
}
