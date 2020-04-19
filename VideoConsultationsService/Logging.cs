using System;
using System.IO;
using System.Linq;

namespace VideoConsultationsService {
	public class Logging {
		private const string LOG_FILE_NAME = "VideoConsultationsService_*.log";
		private const int MAX_LOGFILES_QUANTITY = 7;

		public static void ToLog(string msg, bool writeToConsole = true) {
			string today = DateTime.Now.ToString("yyyyMMdd");
			string logFileName = AppDomain.CurrentDomain.BaseDirectory + "\\" + LOG_FILE_NAME.Replace("*", today);

			try {
				using (System.IO.StreamWriter sw = System.IO.File.AppendText(logFileName)) 
					sw.WriteLine(ToLogFormat(msg));

			} catch (Exception e) {
				Console.WriteLine("Cannot write to log file: " + logFileName + " | " + e.Message + " | " + e.StackTrace);
			}

			if (writeToConsole)
				Console.WriteLine(msg);

			CheckAndCleanOldFiles();
		}

		private static void CheckAndCleanOldFiles() {
			try {
				DirectoryInfo dirInfo = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
				FileInfo[] files = dirInfo.GetFiles(LOG_FILE_NAME).OrderBy(p => p.CreationTime).ToArray();

				if (files.Length <= MAX_LOGFILES_QUANTITY)
					return;

				for (int i = 0; i < files.Length - MAX_LOGFILES_QUANTITY; i++)
					files[i].Delete();
			} catch (Exception e) {
				Console.WriteLine("Cannot delete old lig files: " + e.Message + " | " + e.StackTrace);
			}
		}

		public static string ToLogFormat(string text, bool addNewLine = false) {
			return System.String.Format("{0:G}: {1}", System.DateTime.Now, text) + (addNewLine ? Environment.NewLine : "");
		}
	}
}
