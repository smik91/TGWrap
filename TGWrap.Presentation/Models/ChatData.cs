namespace TGWrap.Presentation.Models;

public class ChatData
{
    public string type { get; set; }
    public long id { get; set; }
    public Message[] messages { get; set; }
}
