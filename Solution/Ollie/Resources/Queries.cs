using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ollie.Resources
{
    /// <summary>
    /// Class that contains all queries for a specific TI
    /// </summary>
    public static class Queries
    {
        public static List<string> IpQueries = new List<string>()
        {
            "imNetworkSession | where TimeGenerated > ago(180d) | where SrcIpAddr == ",
            "imDns | where TimeGenerated  > ago(180d) | where SrcIpAddr == ",
            "imAuthentication | where TimeGenerated  > ago(180d) | where SrcDvcIpAddr == ",
            "imAuthentication | where TimeGenerated > ago(180d) | where TargetDvcIpAddr == ",
            "imFileEvent | where TimeGenerated  > ago(180d) | where SrcIpAddr == ",
        };

        public static List<string> UrlQueries = new List<string>()
        {
            "imAuthentication | where TimeGenerated  > ago(180d) | where TargetUrl ==",
            "imFileEvent | where TimeGenerated  > ago(180d) | where TargetUrl == "
        };

        public static List<string> DomainQueries = new List<string>()
        {
            "imNetworkSession | where TimeGenerated > ago(180d) | where SrcDomain == ",
            "imDns | where TimeGenerated  > ago(180d) | where DnsQuery == "
        };

        public static List<string> FileHashQueries = new List<string>()
        {
            "imProcess | where TimeGenerated > ago(180d) | where Hash ==",
            "imFileEvent | where TimeGenerated  > ago(180d) | where SrcFileMD5 ==",
            "imFileEvent | where TimeGenerated  > ago(180d) | where SrcFileSHA1 == ",
            "imFileEvent | where TimeGenerated  > ago(180d) | where SrcFileSHA256 == ",
            "imFileEvent | where TimeGenerated  > ago(180d) | where SrcFileSHA512 == ",
            "imFileEvent | where TimeGenerated  > ago(180d) | where Hash == "
        };
    }
}
