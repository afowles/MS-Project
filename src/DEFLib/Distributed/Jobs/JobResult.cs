using Newtonsoft.Json;

namespace Defcore.Distributed.Jobs
{
    /// <summary>
    /// A class to represent a result from a job
    /// </summary>
    public class JobResult
    {
        //public int JobId { get; set; }

        // Custom json handle settings
        private static readonly JsonSerializerSettings Settings 
            = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };


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
