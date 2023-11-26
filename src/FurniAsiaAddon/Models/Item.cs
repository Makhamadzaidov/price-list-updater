namespace FurniAsiaAddon.Models
{
    public class Item
    {
        public string ItemCode { get; set; }
        public int PriceList { get; set; } = 1;
        public decimal Price { get; set; }
        public decimal PackagePrice { get; set; }
        public string Currency { get; set; }
    }
}
