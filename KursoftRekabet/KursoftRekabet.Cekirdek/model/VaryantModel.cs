namespace KursoftRekabet.model
{
    public class VaryantModel
    {
        /// <summary>Renk, Beden vb.</summary>
        public string Ozellik { get; set; }
        public string Deger { get; set; }
        public string VaryantID { get; set; }
        public string Barkod { get; set; }
        public string FiyatMetni { get; set; }
        public decimal? Fiyat { get; set; }
        public bool? StokVar { get; set; }
        public bool? SeciliMi { get; set; }
        public string Link { get; set; }
    }
}
