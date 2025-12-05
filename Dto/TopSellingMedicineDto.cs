namespace BE_QLTiemThuoc.Dto
{
    public class TopSellingMedicineDto
    {
        public string MaThuoc { get; set; }
        public string TenThuoc { get; set; }
        public int TotalQuantity { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal AveragePrice { get; set; }
        public string CategoryName { get; set; }
    }
}