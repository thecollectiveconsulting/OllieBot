using System;
using System.Collections.Generic;

namespace Ollie.Models
{
    public class ThreatIndicator
    {
        public string Action { get; set; }
        public string Description { get; set; }
        public string ExpirationDateTime { get; set; }
        public string TargetProduct { get; set; }
        public string ThreatType { get; set; }
        public string TlpLevel { get; set; }
        public string NetworkDestinationIPv4 { get; set; }
        public string FileHashType { get; set; }
        public string FileHashValue { get; set; }
        public string DomainName { get; set; }
        public string Url { get; set; }
    }
}
