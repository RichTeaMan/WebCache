using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RichTea.WebCache
{
    public class Message
    {
        public DateTime DateTime { get; protected set; }
        public Severity Severity { get; protected set; }
        public string Text { get; protected set; }

        protected Message()
        {
            DateTime = DateTime.Now;
        }

        public Message(Severity severity, string text, params object[] args) : this()
        {
            Severity = severity;
            Text = string.Format(text, args);
        }

        public override string ToString()
        {
            var str = string.Format("{0} - {1}", Severity, Text);
            return str;
        }
    }
}
