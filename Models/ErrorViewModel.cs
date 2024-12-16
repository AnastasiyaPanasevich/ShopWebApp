namespace ShopWebApp
{
    public class ErrorViewModel : BaseViewModel
    {
        public int ErrorCode { get; set; }
        public string AboutError { get; set; }

        public ErrorViewModel() : base("An error occurred")
        {
        }
    }
}
