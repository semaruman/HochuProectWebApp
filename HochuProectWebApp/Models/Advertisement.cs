namespace HochuProectWebApp.Models
{
    public class Advertisement
    {
        public int Id { get; set; }

        public string Title { get; set; }

        public List<string> Photos {  get; set; } = new List<string>();

        public string Description { get; set; } = "Нет описания";
    }
}
