using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System;
using System.Text.RegularExpressions;

namespace TLH.Data
{
    public static class Prooompting
    {
        public static string TeacherFörhållningsätt;
        public static string Uppgiften;
        public static string ElevensInlämnadeUppgift;

        public static void CalculateTokens()
        {
            string text = TeacherFörhållningsätt + Uppgiften + ElevensInlämnadeUppgift;
            //int tokenCount = CountTokens("");
            //Console.WriteLine($"The number of tokens in the text is: {tokenCount}");
        }
    }
}
