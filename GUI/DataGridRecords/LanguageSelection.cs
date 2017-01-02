using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GUI.DataGridRecords
{
    public class LanguageSelection
    {
        public string Language { get; set; }
        public bool Selected { get; set; }

        public LanguageSelection(string language)
        {
            Language = language;
            Selected = false;
        }
    }
}
