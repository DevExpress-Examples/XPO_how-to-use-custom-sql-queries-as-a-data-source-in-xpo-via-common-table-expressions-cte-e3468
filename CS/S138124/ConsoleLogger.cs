using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DevExpress.Xpo.Logger;

namespace S138124 {
    public class ConsoleLogger: ILogger {
        #region ILogger Members

        public void ClearLog() { }

        public int Count {
            get { return 0; }
        }

        public bool Enabled {
            get {
                return true;
            }
            set {
            }
        }

        public int LostMessageCount {
            get { return 0; }
        }

        public bool IsServerActive {
            get { return true; }
        }

        public int Capacity {
            get { return Int32.MaxValue; }
        }

        public void Log(LogMessage message) {
            if (message.MessageType != LogMessageType.DbCommand) return;
            Console.WriteLine(message.MessageText);
        }

        public void Log(LogMessage[] messages) {
            foreach (LogMessage message in messages) {
                Log(messages);
            }
        }

        #endregion
    }
}