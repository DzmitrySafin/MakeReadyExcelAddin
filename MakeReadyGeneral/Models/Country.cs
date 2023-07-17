using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MakeReadyGeneral.Models
{
    public class Country
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string Title { get; set; }

        public Country()
        {
            //
        }

        public Country(int id, string code, string title)
        {
            Id = id;
            Code = code;
            Title = title;
        }

        public static List<Country> CreateDefaultList()
        {
            return new List<Country>
            {
                new Country(1, "AT", "Austria"),
                new Country(2, "AU", "Australia"),
                new Country(3, "BG", "Bulgaria"),
                new Country(4, "BR", "Brazil"),
                new Country(5, "BY", "Belarus"),
                new Country(6, "CA", "Canada"),
                new Country(7, "CZ", "Czechia"),
                new Country(8, "DE", "Germany"),
                new Country(9, "EE", "Estonia"),
                new Country(10, "FI", "Finland"),
                new Country(11, "FR", "France"),
                new Country(12, "GR", "Greece"),
                new Country(13, "HN", "Honduras"),
                new Country(14, "HU", "Hungary"),
                new Country(15, "IL", "Israel"),
                new Country(16, "IT", "Italy"),
                new Country(17, "KG", "Kyrgyzstan"),
                new Country(18, "KZ", "Kazakhstan"),
                new Country(19, "LT", "Lithuania"),
                new Country(20, "LV", "Latvia"),
                new Country(21, "MD", "Moldova"),
                new Country(22, "MN", "Mongolia"),
                new Country(23, "PG", "Papua New Guinea"),
                new Country(24, "PL", "Poland"),
                new Country(25, "PT", "Portugal"),
                new Country(26, "RS", "Serbia"),
                new Country(27, "RU", "Russian Federation"),
                new Country(28, "SK", "Slovakia"),
                new Country(29, "TH", "Thailand"),
                new Country(30, "UA", "Ukraine"),
                new Country(31, "US", "United States of America"),
            };
        }
    }
}
