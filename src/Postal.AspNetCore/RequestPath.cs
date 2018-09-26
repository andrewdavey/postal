using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Postal
{
    public class RequestPath
    {
        public string PathBase { get; set; }
        public string Host { get; set; }
        public bool IsHttps { get; set; }
        public string Scheme { get; set; }
        public string Method { get; set; }
    }
}
