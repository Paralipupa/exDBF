using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace exDBF
{
    public class Log
    {
        private Log() { }
        private static List<string> _messageList = new List<string>();

        private static object sync = new object();

        public bool IsAttention { get; set; }

        public List<string> GetMessages()
        {
            return _messageList;
        }

        public void Clear()
        {
            _messageList.Clear();
            IsAttention = false;
        }

        public void Write(Exception ex, string message = "")
        {
            try
            {
                string pathToLog = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Log");
                if (!Directory.Exists(pathToLog))
                {
                    Directory.CreateDirectory(pathToLog);
                }

                string filename = Path.Combine(pathToLog, string.Format("{0}_{1:dd.MM.yyy}.log",
                AppDomain.CurrentDomain.FriendlyName, DateTime.Now));
                string fullText = string.Format("{0}: [{1:dd.MM.yyy HH:mm:ss.fff}] [{2}.{3}()] {4} {5}\r\n",
                "", DateTime.Now, ex.TargetSite.DeclaringType, ex.TargetSite.Name, ex.Message, ex.StackTrace.Replace("\r", "").Replace("\n", "").Replace(" в ", "\r\n      в "));

                lock (sync)
                {
                    File.AppendAllText(filename, $"{fullText}{(string.IsNullOrWhiteSpace(message) ? "" : $"\n{message}")}", Encoding.GetEncoding("Windows-1251"));
                    _messageList.Add(fullText);
                    IsAttention = true;
                }
            }
            catch
            {
                Log.Instance.Write(ex);
            }
        }

        private static Log _instance;
        public static Log Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new Log();
                }
                return _instance;
            }
        }
    }
}
