namespace MRIV.ViewModels
{
    public class TicketViewModel
    {
        public int RequestId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
    }

    public class TicketSearchResult
    {
        public List<Ticket> Tickets { get; set; }
        public int TotalCount { get; set; }
    }
}
