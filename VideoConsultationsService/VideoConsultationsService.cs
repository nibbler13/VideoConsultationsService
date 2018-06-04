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

		static void Main(string[] args) {
			LoggingSystem.ToLogFormat("Запуск");
			if (!Environment.UserInteractive)
				using (Service service = new Service())
					ServiceBase.Run(service);
			else {
				if (args.Length == 1 && args[0].ToLower().Equals("CheckTrueConfServer".ToLower())) {
					EventSystem eventSystem = new EventSystem();
					eventSystem.CheckTrueConfServer(true);
				} else {
					Start();

					Console.WriteLine("Press any key to stop...");
					Console.ReadKey(true);

					Stop();
				}
			}
		}


		private static void Start() {
			LoggingSystem.LogMessageToFile("Starting, cycle interval in seconds: " +
				Properties.Settings.Default.UpdatePeriodInSeconds);

			EventSystem eventSystem = new EventSystem();
			Thread threadNewEvents = new Thread(eventSystem.CheckForNewEvents);
			threadNewEvents.IsBackground = true;
			threadNewEvents.Start();

			Thread threadCheckState = new Thread(eventSystem.CheckTrueconfServerStateByTimer);
			threadCheckState.IsBackground = true;
			threadCheckState.Start();
		}

		private static void Stop() {
			LoggingSystem.LogMessageToFile("Stopping");
		}
	}
}
