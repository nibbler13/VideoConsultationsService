using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace SmsNotificationService {
	static class SmsNiotificationService {
		public class Service : ServiceBase {
			public Service() {

			}

			protected override void OnStart(string[] args) {
				SmsNiotificationService.Start();
			}

			protected override void OnStop() {
				SmsNiotificationService.Stop();
			}
		}

		static int Main(string[] args) {
			if (args.Length == 1) {
				string arg0 = args[0].ToLower();

				if (arg0.Equals("CheckTrueConfServer".ToLower())) {
					EventSystem eventSystem = new EventSystem();
					eventSystem.CheckTrueConfServer(true);
				} else if (arg0.Equals("zabbix")) {
					EventSystem eventSystem = new EventSystem(true);
					return eventSystem.CheckTrueConfServer(false);
				}
			} else if (!Environment.UserInteractive) {
				using (Service service = new Service())
					ServiceBase.Run(service);
			} else {
				Start();

				Console.WriteLine("Press any key to stop...");
				Console.ReadKey(true);

				Stop();
			}

			return 0;
		}


		private static void Start() {
			Logging.ToLog("Запуск");
			Logging.ToLog("Starting, cycle interval in seconds: " +
				Properties.Settings.Default.UpdatePeriodInSeconds);

			EventSystem eventSystem = new EventSystem();

			Thread threadNewEvents = new Thread(eventSystem.CheckForNewEvents) {
				IsBackground = true
			};

			threadNewEvents.Start();

			//Thread threadCheckState = new Thread(eventSystem.CheckTrueconfServerStateByTimer);
			//threadCheckState.IsBackground = true;
			//threadCheckState.Start();
		}

		private static void Stop() {
			Logging.ToLog("Stopping");
		}
	}
}
