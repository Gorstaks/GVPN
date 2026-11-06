using System;
using System.IO;

namespace GVPN
{
    public class Logger
    {
        private string logFilePath;
        private object lockObj = new object();

        public Logger(string filename)
        {
            logFilePath = Path.Combine(Directory.GetCurrentDirectory(), filename);
        }

        public void Log(string message)
        {
            lock (lockObj)
            {
                try
                {
                    string logMessage = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " - " + message;
                    File.AppendAllText(logFilePath, logMessage + Environment.NewLine);
                }
                catch (Exception ex)
                {
                    // Silently fail if logging fails
                    System.Diagnostics.Debug.WriteLine("Logging error: " + ex.Message);
                }
            }
        }
    }
}
