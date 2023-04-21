namespace TLH.Data
{
    public static class Prooompting
    {
        public static string TeacherFörhållningsätt = "Hej";
        public static string Uppgiften = "På";
        public static string ElevensInlämnadeUppgift = "Dig";

        public static void CalculateTokens()
        {
            string text = TeacherFörhållningsätt + Uppgiften + ElevensInlämnadeUppgift;
            //int tokenCount = CountTokens("");
            //Console.WriteLine($"The number of tokens in the text is: {tokenCount}");
        }
    }
}