using Distributed.Node;

namespace StartNode
{
    public class StartNode
    {
        public static void Main(string[] args)
        {
            Node.Main(new string[] { "129.21.89.207", "12345" });
        }
    }
}
