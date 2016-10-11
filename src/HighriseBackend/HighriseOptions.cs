using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HighriseBackend
{
    public class HighriseOptions
    {
        public string Credential { get; set; }
        public string Url { get; set; }
        public string UserAgent { get; set; }
        public bool UseMock { get; set; }
        public bool SetupMock { get; set; }
    }
}
