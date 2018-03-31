using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RichTea.WebCache
{
    public class GeneratorException : Exception
    {
        public Uri Url { get; set; }

        public GeneratorException(string message, Exception innerException) : base(message, innerException) { }
    }
}
