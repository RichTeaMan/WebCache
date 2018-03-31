using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RichTea.WebCache
{
    public class ExceptionMessage : Message
    {
        public Exception Exception { get; protected set; }

        public ExceptionMessage(Severity severity, Exception exception, string text, params object[] args)
            : base(severity, text, args)
        {
            Exception = exception;
        }

        public ExceptionMessage(Exception exception, string text, params object[] args)
            : this(Severity.Error, exception, text, args)
        { }

        public override string ToString()
        {
            var str = string.Format("{0} - {1} - {2}", Severity, Text, Exception.Message);
            return str;
        }

    }
}
