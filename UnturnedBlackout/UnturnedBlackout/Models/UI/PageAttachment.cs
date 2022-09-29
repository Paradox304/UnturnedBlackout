using System.Collections.Generic;
using UnturnedBlackout.Database.Data;

namespace UnturnedBlackout.Models.UI;

public class PageAttachment
{
    public int PageID { get; set; }
    public Dictionary<int, LoadoutAttachment> Attachments { get; set; }

    public PageAttachment(int pageID, Dictionary<int, LoadoutAttachment> attachments)
    {
        PageID = pageID;
        Attachments = attachments;
    }
}