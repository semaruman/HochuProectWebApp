namespace HochuProectWebApp.Models
{
    public class Advertisement
    {
        public int Id { get; set; }

        public string Title { get; set; }

        public string Photos {  get; set; } 

        public string Description { get; set; } = "Нет описания";


        //навигационные свойства
        public int UserId { get; set; }
        public User User { get; set; }

        public int CategoryId { get; set; }
        public Category Category { get; set; }

    }
}
