using GPT_3_Encoder_Sharp;
using SharpToken;

namespace GPT3Example
{
    public class TokenCounterUtil
    {
        public static void GPT3EncoderSharp(String countText)
        {
            Console.WriteLine();
            Console.WriteLine("GPT3EncoderSharp");
            //Link Repo: https://github.com/Alex1911-Jiang/GPT-3-Encoder-Sharp
            Encoder encoder = Encoder.Get_Encoder();
            var encoded = encoder.Encode(countText);
            Console.WriteLine();
            Console.WriteLine("encoded is: \r\n" + string.Join(',', encoded));
            Console.WriteLine($"Tokens: {encoded.Count}, Characters: {countText.Length}");
            Console.WriteLine("decoded is: \r\n" + encoder.Decode(encoded));
        }

        public static void SharpTokenCounter(String countText)
        {
            Console.WriteLine();
            Console.WriteLine("SharpTopkenCounter");

            //Link Repo: https://github.com/dmitry-brazhenko/SharpToken

            // Get encoding by encoding name
            var encodingClk = GptEncoding.GetEncoding("cl100k_base");

            // Get encoding by model name
            var encodingGpt4 = GptEncoding.GetEncodingForModel("gpt-4");

            // Get encoding by model name
            var encodingGpt3 = GptEncoding.GetEncodingForModel("gpt-3.5-turbo");

            // Get encoding by model name
            var textdavinci = GptEncoding.GetEncodingForModel("text-davinci-003");

            var eclk = encodingClk.Encode(countText);
            var egpt4 = encodingGpt4.Encode(countText);
            var egpt3t = encodingGpt3.Encode(countText);
            var td003 = textdavinci.Encode(countText);

            int countEclk = CountInts(eclk);
            int countEgpt4 = CountInts(egpt4);
            int countEgpt3t = CountInts(egpt3t);
            int counttd003 = CountInts(td003);

            Console.WriteLine($"Number of tokens in clk: {countEclk}");
            Console.WriteLine($"Number of tokens in gpt4: {countEgpt4}");
            Console.WriteLine($"Number of tokens in gpt3t: {countEgpt3t}");
            Console.WriteLine($"Number of tokens in td003: {counttd003}");
        }

        private static int CountInts(List<int> list)
        {
            int count = 0;
            foreach (int i in list)
            {
                count++;
            }
            return count;
        }
    }
}