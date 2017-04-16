using Newtonsoft.Json;

namespace Defcore.Distributed.Jobs
{
    /// <summary>
    /// Job Result
    /// </summary>
    public class JobResult
    {
        public int JobId { get; private set; }

        // Custom json handle settings
        private static readonly JsonSerializerSettings Settings 
            = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };

        /// <summary>
        /// A job result object associated with the
        /// job id it came from.
        /// </summary>
        /// <param name="id"></param>
        public JobResult(int id)
        {
            JobId = id;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public string SerializeResult()
        {
            return JsonConvert.SerializeObject(this, Settings);
        }

        /// <summary>
        /// Deserialize the output string into a JobResult object
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static JobResult DeserializeString(string input)
        {
            return JsonConvert.DeserializeObject<JobResult>(input,Settings);
        }
    }

}
