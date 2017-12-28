using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace VideoConsultationsService {
	static class VideoConsultationsService {
		public class Service : ServiceBase {
			public Service() {

			}

			protected override void OnStart(string[] args) {
				VideoConsultationsService.Start();
			}

			protected override void OnStop() {
				VideoConsultationsService.Stop();
			}
		}

		static void Main() {
			if (!Environment.UserInteractive)
				using (Service service = new Service())
					ServiceBase.Run(service);
			else {
				Start();

				Console.WriteLine("Press any key to stop...");
				Console.ReadKey(true);

				Stop();
			}
		}


		private static void Start() {
			LoggingSystem.LogMessageToFile("Starting, cycle interval in seconds: " +
				Properties.Settings.Default.UpdatePeriodInSeconds);

			EventSystem eventSystem = new EventSystem();
			Thread thread = new Thread(eventSystem.CheckForNewEvents);
			thread.Start();
		}

		private static void Stop() {
			LoggingSystem.LogMessageToFile("Stopping");
		}
	}
}
