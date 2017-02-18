using Distributed.Node;

namespace StartNode
{
    public class StartNode
    {
        public static void Main(string[] args)
        {
            //TODO: Figure out a better way to get IP
            Node.Main(new string[] { "129.21.89.154", "12345" });
        }
    }
}
