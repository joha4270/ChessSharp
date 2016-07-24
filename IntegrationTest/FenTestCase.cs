using System.Linq;

namespace IntegrationTest
{
    class FenTestCase
    {
        public string Fen { get; }
        public int[] Perft { get; }

        public FenTestCase(string t)
        {
            string[] parts = t.Split(';');
            Fen = parts[0].Trim();

            Perft = parts.Skip(1).Select(x => int.Parse(x.Split(' ')[1])).ToArray();
        }
    }
}