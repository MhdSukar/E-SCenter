using System.Collections.Generic;

namespace ESCenter.Models
{
    public class EscCommandRequest
    {
        public string Command { get; set; } = string.Empty;

        public Dictionary<string, string>? Parameters { get; set; }
    }
}

