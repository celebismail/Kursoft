namespace KursoftRekabet.model
{
    public class SaticiModel
    {
        public string SaticiID { get; set; }
        public string SaticiAdi { get; set; }
        public string SaticiPuaniMetni { get; set; }
        public decimal? SaticiPuani { get; set; }

        /// <summary>Puanın ölçeği (10 ya da 5). Platform ayarından gelir.</summary>
        public int? PuanOlcegi { get; set; }
        public bool? ResmiMagazaMi { get; set; }
        public string SaticiSehri { get; set; }
        public string SaticiLinki { get; set; }
        public int? TakipciSayisi { get; set; }
        public bool? BuyboxMi { get; set; }

        // Diğer satıcılar listesinde her satıcının kendi teklifi
        public string ListingID { get; set; }
        public string FiyatMetni { get; set; }
        public decimal? SatisFiyati { get; set; }
        public string ListeFiyatiMetni { get; set; }
        public decimal? ListeFiyati { get; set; }
        public string KargoUcretiMetni { get; set; }
        public decimal? KargoUcreti { get; set; }
        public bool? KargoBedavaMi { get; set; }
        public string TahminiTeslimat { get; set; }
        public bool? StokVar { get; set; }
    }
}
