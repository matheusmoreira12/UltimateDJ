using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Reflection;
using System.IO;

namespace UltimateDJ
{
    static class FileSystem
    {
        public static string HomeDirectory = "";
        static FileSystem()
        {
            HomeDirectory = Path.GetDirectoryName(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
        }
    }
}
