using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cztOCR
{
    public class OcrRecord
    {
        public string Hash { get; set; }
        public string Label { get; set; }
        public string Text { get; set; }
        public long Time {  get; set; }
    }
}
