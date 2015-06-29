using System.Collections.Generic;
using System.Web.Http;

namespace TPLHLRC
{
    public class RateLimitController : ApiController
    {
        // GET api/values 
        public IEnumerable<string> Get()
        {
            return Program.LookupService.Blocks.Keys;
        }

        // GET api/values/5 
        public string Get(int id)
        {
            return "value";
        }

        // POST api/values 
        public void Post([FromBody]int value)
        {
            Program.LookupService.SetDnsRateLimit(value);
        }

        // PUT api/values/5 
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/values/5 
        public void Delete(int id)
        {
        }
    } 
}