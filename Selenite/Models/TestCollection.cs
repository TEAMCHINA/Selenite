using System.Collections.Generic;
using System.ComponentModel;
using Newtonsoft.Json;

namespace Selenite.Models
{
    public class TestCollection
    {
        [JsonIgnore]
        public string File { get; set; }

        [DefaultValue(true)]
        public bool Enabled { get; set; }
        
        public string DefaultDomain { get; set; }
        public IList<Test> Tests { get; set; }
        public IDictionary<string, string> Macros { get; set; }
    }
}