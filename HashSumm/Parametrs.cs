using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HashSumm
{
    public class Parametrs
    {
        public Object Source { get; set; }
        public Mutex SourceMutex { get; set; }

        public Object Output { get; set; }
        public Mutex OutputMutex { get; set; }

        public Action<object> Action { get; set; }

        public Thread Thread { get; set; }

        public bool FlagStop { get; set; } = false;


        public void Start()
        {
            Thread = new Thread(new ParameterizedThreadStart(this.Action));
            Thread.Start(this);
        }


        public delegate void MessageHandler(string message, string path="");

        public event MessageHandler InfoMessage;
        public void OnInfoMessage(string message, string path = "")
        {
            InfoMessage?.Invoke(message);
        }

        public event MessageHandler ErrorMessage;
        public void OnErrorMessage(string message, string path="")
        {
            ErrorMessage?.Invoke(message);
        }
    }
}
