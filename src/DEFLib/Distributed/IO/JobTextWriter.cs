using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Defcore.Distributed.IO
{
    /// <summary>
    /// JobTextWriter is replaced 
    /// </summary>
    internal class JobTextWriter : TextWriter
    {

        private UserOutput _userOutput;

        public JobTextWriter()
        {
            _userOutput = new UserOutput();
        }

        public override void Write(char value)
        {
            _userOutput.ConsoleOutput.Add(value.ToString());
        }

        public override void WriteLine(string value)
        {
            _userOutput.ConsoleOutput.Add(value);
        }

        public override Encoding Encoding {
            get
            {
                return Encoding.ASCII;
            }
        }
    }

    internal sealed class UserOutput
    {
        public List<string> ConsoleOutput { get; set; }
    }
}
