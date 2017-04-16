using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace Defcore.Distributed.IO
{
    /// <summary>
    /// JobTextWriter writes output to a custom class
    /// and provides the ability to
    /// serialzes that object afterwards. This class is
    /// used in conjunction with JobExecuter to save all user
    /// output from a job before converting it to json and writing
    /// it back out to the process executing JobExecuters main.
    /// </summary>
    public class JobTextWriter : TextWriter
    {
        // capture output in a UserOutput object
        private readonly UserOutput _userOutput;

        /// <summary>
        /// Initializes the UserOutput object
        /// </summary>
        public JobTextWriter()
        {
            _userOutput = new UserOutput();
        }

        /// <summary>
        /// Write a char value to the UserOutput object.
        /// All methods in a textwriter use at the
        /// basic level Write(char value). This is the only
        /// method that needs to be overridden.
        /// </summary>
        /// <param name="value">the character value to be written</param>
        public override void Write(char value)
        {
            _userOutput.ConsoleOutput += value;
        }

        /// <summary>
        /// Encode with ASCII
        /// </summary>
        public override Encoding Encoding => Encoding.ASCII;

        /// <summary>
        /// Get the results as a json string.
        /// </summary>
        /// <returns></returns>
        public string GetJsonResult()
        {
            return JsonConvert.SerializeObject(_userOutput);
        }

        /// <summary>
        /// Get the results as a simple string
        /// </summary>
        /// <returns></returns>
        public string GetSimpleString()
        {
            return _userOutput.ConsoleOutput;
        }
    }

    /// <summary>
    /// Small class wrapped around user output
    /// </summary>
    internal sealed class UserOutput
    {
        public string ConsoleOutput { get; set; } = "";
    }
}
