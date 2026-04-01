namespace HochuProectWebApp.Models
{
    public class Category
    {
        public int Id { get; set; }

        public string Name { get; set; }



        //навигационные свойства
        public List<Advertisement> Advertisements { get; set; }

    }
}
