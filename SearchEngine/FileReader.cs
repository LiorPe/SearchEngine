using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SearchEngine
{
    public static class FileReader
    {
        public static readonly char[] Splitters = {' ','\n'};
        public static string[] ReadTextFile(string path)
        {
            if (File.Exists(path))
            {
                return File.ReadAllText(path).Split(Splitters);
            }
            else
            {
                return null;
            }
        }
        public static string[] ReadUtfFile(string path)
        {
            if (File.Exists(path))
            {
                return File.ReadAllLines(path);
            }
            else
            {
                return null;
            }
        }
    }
}
