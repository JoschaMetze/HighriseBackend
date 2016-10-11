using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HighriseBackend.Model
{
    public class Party
    {
        public string Name { get; set; }
        public string FirmenName { get; set; }
        public string Id { get; set; }
        public DateTime Aktualisiert { get; set; }
        public int Haeufigkeit { get; set; }
        public string Ansprechpartner { get; set; }
        public DateTime LetzterKommentarAm { get; set; }
        public string LetzterKommentar { get; set; }
        public string Type { get; set; }
        public string Status { get; set; }
    }
}
