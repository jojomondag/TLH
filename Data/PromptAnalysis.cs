namespace TLH.Data
{
    public static class PromptAnalysis
    {
        private static string teacherFörhållningsätt = "Hej";
        private static string uppgiften = "På";
        private static string elevensInlämnadeUppgift = "Dig";
        public static string TeacherFörhållningsätt
        {
            get { return teacherFörhållningsätt; }
            set { teacherFörhållningsätt = value; }
        }
        public static string Uppgiften
        {
            get { return uppgiften; }
            set { uppgiften = value; }
        }
        public static string ElevensInlämnadeUppgift
        {
            get { return elevensInlämnadeUppgift; }
            set { elevensInlämnadeUppgift = value; }
        }
        public static void CalculateTokens()
        {
            //string text = TeacherFörhållningsätt + Uppgiften + ElevensInlämnadeUppgift;
            //int tokenCount = CountTokens("");
            //Console.WriteLine($"The number of tokens in the text is: {tokenCount}");
        }
    }
}