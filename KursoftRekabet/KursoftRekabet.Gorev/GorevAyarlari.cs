using Newtonsoft.Json.Linq;

namespace KursoftRekabet.Gorev
{
    /// <summary>
    /// appsettings.json (+ varsa appsettings.Local.json) okunur. Local dosya repoya girmez;
    /// gerçek bağlantı cümlesi oraya yazılabilir. Aynı JSON hem bu sınıfa hem çekirdeğin CalismaAyarlari'na dolar.
    /// </summary>
    public class GorevAyarlari
    {
        public string BaglantiCumlesi { get; set; }

        /// <summary>SQL metni ya da (boşluk içermiyorsa) stored procedure adı. En az ID, Link kolonları dönmeli; ModulType opsiyonel.</summary>
        public string LinkSorgusu { get; set; }
        public string SonucTabloAdi { get; set; } = "RekabetSonuc";
        public int DbKomutZamanAsimiSn { get; set; } = 60;
        public bool DbLogAcik { get; set; } = true;
        public int ToplamZamanAsimiDk { get; set; } = 120;

        public static void Oku(string klasor, out GorevAyarlari gorev, out CalismaAyarlari cekirdek)
        {
            string ana = Path.Combine(klasor, "appsettings.json");
            if (!File.Exists(ana)) throw new FileNotFoundException("appsettings.json bulunamadı: " + ana);

            JObject birlesik = JObject.Parse(File.ReadAllText(ana));
            string yerel = Path.Combine(klasor, "appsettings.Local.json");
            if (File.Exists(yerel))
            {
                birlesik.Merge(JObject.Parse(File.ReadAllText(yerel)), new JsonMergeSettings { MergeArrayHandling = MergeArrayHandling.Replace });
            }

            gorev = birlesik.ToObject<GorevAyarlari>();
            cekirdek = birlesik.ToObject<CalismaAyarlari>();
        }

        /// <summary>Log'a yazılacak özet. Bağlantı cümlesi ASLA yazılmaz.</summary>
        public object LogOzeti(CalismaAyarlari c)
        {
            return new
            {
                BaglantiCumlesiVar = !string.IsNullOrWhiteSpace(BaglantiCumlesi),
                LinkSorgusu = LinkSorgusu,
                SonucTabloAdi = SonucTabloAdi,
                DbLogAcik = DbLogAcik,
                ToplamZamanAsimiDk = ToplamZamanAsimiDk,
                c.MaksimumEszamanliSayfa,
                c.TekrarDenemeSayisi,
                c.LinkZamanAsimiSn,
                c.PlatformBeklemeMs,
                c.MedyaEngelle,
                c.HamJsonDondur,
                c.DetayLog,
                c.LogSaklamaGun,
                c.Headless,
                TarayiciYoluVar = !string.IsNullOrWhiteSpace(c.TarayiciYolu)
            };
        }
    }
}
