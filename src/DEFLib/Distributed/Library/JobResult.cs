using Newtonsoft.Json;

namespace Distributed.Library
{
    public class JobResult
    {
        public int JobId { get; private set; }
        private readonly JsonSerializerSettings _settings 
            = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };

        public JobResult(int id)
        {
            JobId = id;
        }

        public string SerializeResult()
        {
            return JsonConvert.SerializeObject(this, _settings);
        }
    }

}
