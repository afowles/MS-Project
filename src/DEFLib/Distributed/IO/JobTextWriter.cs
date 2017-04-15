using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace Defcore.Distributed.IO
{
    /// <summary>
    /// JobTextWriter is replaced 
    /// </summary>
    internal class JobTextWriter : TextWriter
    {

        private readonly UserOutput _userOutput;

        public JobTextWriter()
        {
            _userOutput = new UserOutput();
        }

        public override void Write(char value)
        {
            _userOutput.ConsoleOutput += value;
        }


        public override Encoding Encoding => Encoding.ASCII;

        public string GetJsonResult()
        {
            return JsonConvert.SerializeObject(_userOutput);
        }

        public string GetString()
        {
            return _userOutput.ConsoleOutput;
        }
    }

    internal sealed class UserOutput
    {
        public string ConsoleOutput { get; set; } = "";
    }
}
