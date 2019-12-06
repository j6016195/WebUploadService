using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Web;

namespace WebUploadService.Common
{
    /// <summary>
    /// 日志操作类
    /// </summary>
    public class Logger
    {
        public static string LOGPATH = ConfigurationManager.AppSettings["LogPath"];
        public static object lockObj = new object();
        static Logger()
        {
            if (!Directory.Exists(LOGPATH))
            {
                Directory.CreateDirectory(LOGPATH);
            }
        }
        public static void SaveLog(string message)
        {

            string logFileName = Path.Combine(LOGPATH, string.Format("{0}.log", DateTime.Now.ToString("yyyyMMdd")));
            string logMsg = string.Format("{0}:{1}\r\n", DateTime.Now.ToString(), message);
            lock (lockObj)
            {
                File.AppendAllText(logFileName, logMsg);
            }
        }

    }
}