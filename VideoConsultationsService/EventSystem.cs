using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace VideoConsultationsService {
	class EventSystem {
		private TrueConf trueConf = new TrueConf();
		private FBClient fbClient;
		private bool isZabbixCheck;
		
		private int previousDay;
		private int previousDayTrueConfServer;
		private uint errorTrueConfCount = 0;
		private uint errorTrueConfServerCountByTimer = 0;
		private uint errorMisFbCount = 0;
		private bool mailSystemErrorSendedToStp = false;
		private bool fbClientErrorSendedToStp = false;
		private bool trueConfServerErrorSendedToStp = false;
		private List<string> sendedEarlierWebinars = new List<string>();
		private List<string> sendedEarlierScheduleEvents = new List<string>();
		private List<string> sendedEarlierRemindersEvents = new List<string>();
		private List<string> sendedEarlierRemindersDocsEvents = new List<string>();

		/* новая запись в расписании от сегодня и вперед */
		private string sqlQueryGetNewSchedule =
			"SELECT " +
			"	Cl.phone1, " +
			"	Cl.phone2, " +
			"	Cl.phone3, " +
			"	CAST(LPAD(EXTRACT(DAY FROM Sh.workdate), 2, '0') AS VARCHAR(2)) || '.' || " +
			"	CAST(LPAD(EXTRACT(MONTH FROM Sh.workdate), 2, '0') AS VARCHAR(2)) || '.' || " +
			"	EXTRACT(YEAR FROM Sh.workdate) AS DATA, " +
			"	Sh.bhour, " +
			"	Sh.bMin, " +
			"	Sh.SchedId " +
			"FROM Clients Cl " +
			"	JOIN Schedule Sh ON Cl.pcode = Sh.pcode " +
			"	JOIN Chairs Ch ON Ch.chid = Sh.chid " +
			"	JOIN Rooms R ON R.rid = Ch.rid " +
			"	JOIN DoctShedule Ds ON Ds.DCode = Sh.DCode AND Sh.WorkDate = Ds.WDate AND Sh.ChId = Ds.Chair " +
			"	JOIN Doctor D ON D.DCode = Sh.DCode " +
			"WHERE Sh.createdate >= current_date + DATEADD(minute, -10, current_time) " +
			"	AND (Sh.emid IS NULL OR Sh.emid = 0) " +
			"	AND Ds.DepNum IN (992092102, 992092953, 992092954, 992092955, 992092956, 764) " +
			"	AND Sh.OnlineType = 1";

		/* напоминание за 0-6 минут до начала */
		private string sqlQueryGetNewNotifications = 
			"SELECT " +
			"	Cl.phone1, " +
			"	Cl.phone2, " +
			"	Cl.phone3, " +
			"	CAST(LPAD(EXTRACT(DAY FROM Sh.workdate), 2, '0') AS VARCHAR(2)) || '.' || " +
			"	CAST(LPAD(EXTRACT(MONTH FROM Sh.workdate), 2, '0') AS VARCHAR(2)) || '.' || " +
			"	EXTRACT(YEAR FROM Sh.workdate) AS DATA, " +
			"	Sh.bhour, " +
			"	Sh.bMin, " +
			"	Sh.SchedId " +
			"FROM Clients Cl " +
			"	JOIN Schedule Sh ON Cl.pcode = Sh.pcode " +
			"	JOIN Chairs Ch ON Ch.chid = Sh.chid " +
			"	JOIN Rooms R ON R.rid = Ch.rid " +
			"	JOIN DoctShedule Ds ON Ds.DCode = Sh.DCode AND Sh.WorkDate = Ds.WDate AND Sh.ChId = Ds.Chair " +
			"	JOIN Doctor D ON D.DCode = Sh.DCode " +
			"WHERE Sh.workdate = 'TODAY' " +
			"	AND CAST('NOW' AS TIME) BETWEEN " +
			"   CAST(IIF (Sh.bhour< 10,0 || Sh.bhour, Sh.bhour) || ':' || " +
			"	   IIF (Sh.bmin< 10,0 || Sh.bmin, Sh.bmin) AS TIME) - 360 " +
			"	AND CAST(IIF (Sh.bhour< 10,0 || Sh.bhour, Sh.bhour) || ':' || " +
			"		IIF (Sh.bmin< 10,0 || Sh.bmin, Sh.bmin) AS TIME) " +
			"	AND (Sh.emid IS NULL OR Sh.emid = 0) " +
			"	AND Ds.DepNum IN (992092102, 992092953, 992092954, 992092955, 992092956, 764) " +
			"	AND Sh.OnlineType = 1";
		
		/* напоминание за 0-6 минут до начала докторам*/
		private string sqlQueryGetNewNotificationsForDocs =
			"SELECT " +
			"	CAST(LPAD(EXTRACT(DAY FROM Sh.workdate), 2, '0') AS VARCHAR(2)) || '.' || " +
			"	CAST(LPAD(EXTRACT(MONTH FROM Sh.workdate), 2, '0') AS VARCHAR(2)) || '.' || " +
			"	EXTRACT(YEAR FROM Sh.workdate) AS DATA, " +
			"	Sh.bhour, " +
			"	Sh.bMin, " +
			"	Sh.SchedId, " +
			"	D.PhoneInt " +
			"FROM Clients Cl " +
			"	JOIN Schedule Sh ON Cl.pcode = Sh.pcode " +
			"	JOIN Chairs Ch ON Ch.chid = Sh.chid " +
			"	JOIN Rooms R ON R.rid = Ch.rid " +
			"	JOIN DoctShedule Ds ON Ds.DCode = Sh.DCode AND Sh.WorkDate = Ds.WDate AND Sh.ChId = Ds.Chair " +
			"	JOIN Doctor D ON D.DCode = Sh.DCode " +
			"WHERE Sh.workdate = 'TODAY' " +
			"	AND CAST('NOW' AS TIME) BETWEEN " +
			"   CAST(IIF (Sh.bhour< 10,0 || Sh.bhour, Sh.bhour) || ':' || " +
			"	   IIF (Sh.bmin< 10,0 || Sh.bmin, Sh.bmin) AS TIME) - 360 " +
			"	AND CAST(IIF (Sh.bhour< 10,0 || Sh.bhour, Sh.bhour) || ':' || " +
			"		IIF (Sh.bmin< 10,0 || Sh.bmin, Sh.bmin) AS TIME) " +
			"	AND (Sh.emid IS NULL OR Sh.emid = 0) " +
			"	AND Ds.DepNum IN (992092102, 992092953, 992092954, 992092955, 992092956)";

		//992092102 - ТЕЛЕМЕДИЦИНА
		//992092953 - Телемедицина (терапия)
		//992092954 - Телемедицина (кардиология)
		//992092955 - Телемедицина (гинекология)
		//992092956 - Телемедицина (неврология)
		//764		- Педиатрия

		public EventSystem(bool isZabbixCheck = false) {
			this.isZabbixCheck = isZabbixCheck;
			previousDay = DateTime.Now.Day;
			previousDayTrueConfServer = previousDay;
			fbClient = new FBClient(
				Properties.Settings.Default.FbMisAddress,
				Properties.Settings.Default.FbMisName,
				isZabbixCheck);
		}

		public void CheckForNewEvents() {
			Timer timerNewEvents = new Timer(Properties.Settings.Default.UpdatePeriodInSeconds * 1000);
			timerNewEvents.Elapsed += TimerNewEvents_Elapsed;
			timerNewEvents.AutoReset = true;
			timerNewEvents.Start();
		}

		private void TimerNewEvents_Elapsed(object sender, ElapsedEventArgs e) {
			Console.WriteLine("---Timer_Elapsed");
			if (previousDay != DateTime.Now.Day) {
				LoggingSystem.LogMessageToFile("Обнуление списка уведомлений");

				foreach (List<string> list in new List<string>[] {
					sendedEarlierWebinars,
					sendedEarlierScheduleEvents,
					sendedEarlierRemindersEvents,
					sendedEarlierRemindersDocsEvents})
					if (list.Count > 10)
						list.RemoveRange(0, list.Count - 10);

				previousDay = DateTime.Now.Day;
			}

			CheckTrueConfEvents();
			CheckMisEvents();
		}

		private void CheckTrueConfEvents() {
			LoggingSystem.LogMessageToFile("---Проверка сервера TrueConf");
			try {
				Dictionary<string, Webinar> webinars = trueConf.GetAllWebinars().Result;
				LoggingSystem.LogMessageToFile("Количество существующих вебинаров: " + webinars.Count);
				foreach (KeyValuePair<string, Webinar> pair in webinars) {
					Webinar webinar = pair.Value;
					if (webinar.invitationTimestamp == 0)
						continue;

					DateTime creationTime = webinar.UnixTimeStampToDateTime();
					int minutesDelta = (int)creationTime.Subtract(DateTime.Now).TotalMinutes;

					if (!sendedEarlierWebinars.Contains(webinar.id))
						if (minutesDelta <= 5 && minutesDelta >= 4)
							SendReminder(webinar);

					if (creationTime.Subtract(DateTime.Now).TotalMinutes <= -30)
						DeleteWebinar(webinar);

					errorTrueConfCount = 0;
					mailSystemErrorSendedToStp = false;
				}
			} catch (Exception exception) {
				LoggingSystem.LogMessageToFile("Не удалось получить данные с сервера TrueConf: " + exception.Message);
				errorTrueConfCount++;
			}

			if (errorTrueConfCount > 1000 && !mailSystemErrorSendedToStp) {
				LoggingSystem.LogMessageToFile("Отправка заявки в СТП");
				string sendResult = MailSystem.SendErrorMessageToStp(MailSystem.ErrorType.TrueConf);
				if (string.IsNullOrEmpty(sendResult)) {
					mailSystemErrorSendedToStp = true;
				} else {
					LoggingSystem.LogMessageToFile("Не удалось отправить: " + sendResult);
				}
			}
		}

		private void CheckMisEvents() {
			LoggingSystem.LogMessageToFile("---Проверка базы МИС");

			DataTable newSchedulesTable = fbClient.GetDataTable(
				sqlQueryGetNewSchedule, new Dictionary<string, string>(), 
				ref errorMisFbCount, ref fbClientErrorSendedToStp);
			DataTable newNotificationsTable = fbClient.GetDataTable(
				sqlQueryGetNewNotifications, new Dictionary<string, string>(), 
				ref errorMisFbCount, ref fbClientErrorSendedToStp);
			DataTable newNotificationsForDocsTable = fbClient.GetDataTable(
				sqlQueryGetNewNotificationsForDocs, new Dictionary<string, string>(),
				ref errorMisFbCount, ref fbClientErrorSendedToStp);

			LoggingSystem.LogMessageToFile("Новых записей онлайн приемов: " + newSchedulesTable.Rows.Count);
			LoggingSystem.LogMessageToFile("Онлайн приемов, которые скоро начнутся: " + newNotificationsTable.Rows.Count);
			LoggingSystem.LogMessageToFile("Онлайн приемов, которые скоро начнутся для докторов: " + newNotificationsForDocsTable.Rows.Count);

			Dictionary<string, DataTable> tables = new Dictionary<string, DataTable>() {
				{ "Назначения", newSchedulesTable },
				{ "Напоминания", newNotificationsTable },
				{ "НапоминанияДоктора", newNotificationsForDocsTable }
			};

			foreach (KeyValuePair<string, DataTable> values in tables) {
				foreach (DataRow row in values.Value.Rows) {
					string schedID = "";
					try {
						schedID = row["SCHEDID"].ToString();
					} catch (Exception) {
						continue;
					}
				
					string mobileNumber = GetMobilePhoneNumber(row);
						

					if (string.IsNullOrEmpty(mobileNumber)) {
						LoggingSystem.LogMessageToFile("Не удалось определить корректный мобильный номер, пропуск");
						continue;
					}

					DateTime dateTime = GetDateTime(row);
					if (dateTime.Equals(new DateTime())) {
						LoggingSystem.LogMessageToFile("Не удалось определить дату назначения, пропуск");
						continue;
					}

					if (values.Key.Equals("Назначения")) {
						string message = Properties.Settings.Default.MessageNewAppointment;
						message = message.Replace("@dateTime", dateTime.ToString("HH:mm dd.MM.yyyy"));

						SendEventSMS(message, mobileNumber, schedID, ref sendedEarlierScheduleEvents);
					}

					if (values.Key.Equals("Напоминания")) {
						string message = Properties.Settings.Default.MessageReminderPatient;
						SendEventSMS(message, mobileNumber, schedID, ref sendedEarlierRemindersEvents);
					}

					if (values.Key.Equals("НапоминанияДоктора")) {
						string message = Properties.Settings.Default.MessageReminderDoctor;
						message = message.Replace("@time", dateTime.ToString("HH:mm"));
						SendEventSMS(message, mobileNumber, schedID, ref sendedEarlierRemindersDocsEvents);
					}
				}
			}

			if (errorMisFbCount > 1000 && !fbClientErrorSendedToStp) {
				LoggingSystem.LogMessageToFile("Отправка заявки в СТП");
				string sendResult = MailSystem.SendErrorMessageToStp(MailSystem.ErrorType.FireBird);
				if (string.IsNullOrEmpty(sendResult)) {
					fbClientErrorSendedToStp = true;
				} else {
					LoggingSystem.LogMessageToFile("Не удалось отправить: " + sendResult);
				}
			}
		}

		private void SendEventSMS(string message, string mobileNumber, string schedId, ref List<string> sendedEarlier) {
			if (sendedEarlier.Contains(schedId))
				return;

			ItemSendResult sendResult = SvyaznoyZagruzka.SendMessage(mobileNumber, message).Result;
			if (!sendResult.IsSuccessStatusCode) {
				LoggingSystem.LogMessageToFile("Не удалось отправить СМС: " + sendResult);
				return;
			}

			LoggingSystem.LogMessageToFile("Отправлено успешно, идентификатор: " + sendResult.MessageId);
			sendedEarlier.Add(schedId);
		}

		private DateTime GetDateTime(DataRow row) {
			try {
				string date = row["DATA"].ToString();
				string hour = row["BHOUR"].ToString();
				string minute = row["BMIN"].ToString();
				DateTime dateTime = DateTime.Parse(date + " " + hour + ":" + minute);
				return dateTime;
			} catch (Exception) {
			}

			return new DateTime();
		}

		private string GetMobilePhoneNumber(DataRow row) {
			foreach (string key in new string[] { "PHONE3", "PHONE2", "PHONE1", "PHONEINT" }) {
				if (!row.Table.Columns.Contains(key))
					continue;

				string mobileNumber = ParseMobilePhoneNumber(row[key].ToString());

				if (!string.IsNullOrEmpty(mobileNumber))
					return mobileNumber;
			}

			return "";
		}

		private string ParseMobilePhoneNumber(string number) {
			string returnValue = "";

			try {
				string clearedToDigit =  new string(number.Where(Char.IsDigit).ToArray());
				if (clearedToDigit.Length >= 10)
					clearedToDigit = clearedToDigit.Substring(clearedToDigit.Length - 10);
				if (clearedToDigit.StartsWith("9"))
					returnValue = clearedToDigit;
			} catch (Exception) {
			}

			return returnValue;
		}

		private void DeleteWebinar(Webinar webinar) {
			LoggingSystem.LogMessageToFile("Удаление вебинара: " + Environment.NewLine + webinar.ToString());

			try {
				if (webinar.State.Equals("Активная")) {
					string tmp = "";
					tmp = trueConf.StopWebinar(webinar.id).Result;
				}

				string result = trueConf.DeleteWebinar(webinar.id).Result;
				if (result.Contains(webinar.id)) {
					LoggingSystem.LogMessageToFile("Вебинар удален успешно");
					return;
				}
			} catch (Exception e) {
				LoggingSystem.LogMessageToFile("Возникла ошибка: " + e.Message);
			}

			LoggingSystem.LogMessageToFile("Не удалось удалить");
		}

		private void SendReminder(Webinar webinar) {
			LoggingSystem.LogMessageToFile("Попытка отправки смс-уведомления для вебинара: " + 
				Environment.NewLine +
				webinar.ToString());
			DateTime dateTime = webinar.UnixTimeStampToDateTime();
			string url = webinar.url;
			string phoneNumber = webinar.GetPhoneNumber();

			if (string.IsNullOrEmpty(phoneNumber)) {
				LoggingSystem.LogMessageToFile("Отсутствует номер телефона");
				return;
			}

			string message = Properties.Settings.Default.MessageNewAppointment;
			message = message.Replace("@dateTime", dateTime.ToString("HH:mm dd.MM.yyyy"));
			
			ItemSendResult sendResult = SvyaznoyZagruzka.SendMessage(phoneNumber, message).Result;
			if (!sendResult.IsSuccessStatusCode) {
				LoggingSystem.LogMessageToFile("Не удалось отправить смс: " + sendResult);
				return;
			}

			LoggingSystem.LogMessageToFile("Отправлено успешно, идентификатор: " + sendResult.MessageId);
			sendedEarlierWebinars.Add(webinar.id);
		}



		public void CheckTrueconfServerStateByTimer() {
			Timer timerCheckState = new Timer(Properties.Settings.Default.CheckStatePeriodInMinutes * 60 * 1000);
			timerCheckState.Elapsed += TimerCheckState_Elapsed;
			timerCheckState.AutoReset = true;
			timerCheckState.Start();
			TimerCheckState_Elapsed(null, null);
		}

		public void TimerCheckState_Elapsed(object sender, ElapsedEventArgs e) {
			if (previousDayTrueConfServer != DateTime.Now.Day)
				trueConfServerErrorSendedToStp = false;

			CheckTrueConfServer();
		}

		public int CheckTrueConfServer(bool isSingleCheck = false) {
			string checkResult = string.Empty;

			string currentMessage = "--- Проверка состояния сервера TrueConf";
			LoggingSystem.LogMessageToFile(currentMessage, !isZabbixCheck);
			checkResult += LoggingSystem.ToLogFormat(currentMessage, true);
			string errorMessage = string.Empty;
			try {
				DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
				long unixDateTime = (long)(DateTime.Now.AddMinutes(10).ToUniversalTime() - epoch).TotalSeconds;
				string timestamp = unixDateTime.ToString();

				currentMessage = "Создание тестового вебинара";
				LoggingSystem.LogMessageToFile(currentMessage, !isZabbixCheck);
				checkResult += LoggingSystem.ToLogFormat(currentMessage, true);

				Webinar webinar = null;
				try {
					webinar = trueConf.CreateNewWebinar("state_check", "nn-admin@ruh93.trueconf.name", timestamp).Result;
					currentMessage = "Вебинар создан, ID:" + webinar.id;
					LoggingSystem.LogMessageToFile(currentMessage, !isZabbixCheck);
					checkResult += LoggingSystem.ToLogFormat(currentMessage, true);
				} catch (Exception e) {
					string msg = e.Message + Environment.NewLine + e.StackTrace;
					LoggingSystem.LogMessageToFile(msg, !isZabbixCheck);
					checkResult += LoggingSystem.ToLogFormat(msg, true);
					errorMessage += msg + Environment.NewLine;
				}

				if (webinar != null) {
					currentMessage = "Получение списка всех вебинаров";
					LoggingSystem.LogMessageToFile(currentMessage, !isZabbixCheck);
					checkResult += LoggingSystem.ToLogFormat(currentMessage, true);

					Dictionary<string, Webinar> webinars = null;
					try {
						webinars = trueConf.GetAllWebinars().Result;
						currentMessage = "Список вебинаров содержит записей: " + webinars.Count;
						LoggingSystem.LogMessageToFile(currentMessage, !isZabbixCheck);
						checkResult += LoggingSystem.ToLogFormat(currentMessage, true);

						if (!webinars.ContainsKey(webinar.id)) {
							errorMessage = "!!! Созданный вебинар (" + webinar.id + ") отсутствует в списке" + Environment.NewLine;
							LoggingSystem.LogMessageToFile(errorMessage, !isZabbixCheck);
							checkResult += LoggingSystem.ToLogFormat(errorMessage, true);
						}
					} catch (Exception excAllWebinar) {
						string msgAllWebinar = excAllWebinar.Message + Environment.NewLine + excAllWebinar.StackTrace;
						LoggingSystem.LogMessageToFile(msgAllWebinar, !isZabbixCheck);
						checkResult += LoggingSystem.ToLogFormat(msgAllWebinar, true);
						errorMessage += msgAllWebinar + Environment.NewLine;
					}

					currentMessage = "Удаление тестового вебинара";
					LoggingSystem.LogMessageToFile(currentMessage, !isZabbixCheck);
					checkResult += LoggingSystem.ToLogFormat(currentMessage, true);

					try {
						string deleteResult = trueConf.DeleteWebinar(webinar.id).Result;
						currentMessage = "Результат удаления: " + deleteResult;
						LoggingSystem.LogMessageToFile(currentMessage, !isZabbixCheck);
						checkResult += LoggingSystem.ToLogFormat(currentMessage, true);
						
						if (!deleteResult.Contains(webinar.id)) {
							errorMessage += "!!! Тестовый вебинар отсутствует в списке удаленных" + Environment.NewLine;
							LoggingSystem.LogMessageToFile(errorMessage, !isZabbixCheck);
							checkResult += LoggingSystem.ToLogFormat(errorMessage, true);
						}
					} catch (Exception excDelete) {
						string msgDelete = excDelete.Message + Environment.NewLine + excDelete.StackTrace;
						LoggingSystem.LogMessageToFile(msgDelete, !isZabbixCheck);
						checkResult += LoggingSystem.ToLogFormat(msgDelete, true);
						errorMessage += msgDelete + Environment.NewLine;
					}
				}
			} catch (Exception exc) {
				if (!string.IsNullOrEmpty(errorMessage))
					errorMessage += Environment.NewLine;

				errorMessage += exc.Message + Environment.NewLine + exc.StackTrace;
				LoggingSystem.LogMessageToFile(errorMessage, !isZabbixCheck);
				checkResult += LoggingSystem.ToLogFormat(errorMessage, true);
			}

			if (string.IsNullOrEmpty(errorMessage)) {
				currentMessage = "--- Проверка выполнена успешно, ошибок не обнаружено";
				LoggingSystem.LogMessageToFile(currentMessage, !isZabbixCheck);
				checkResult += LoggingSystem.ToLogFormat(currentMessage);
				errorTrueConfServerCountByTimer = 0;
				
				if (isZabbixCheck) {
					Console.WriteLine("0");
					LoggingSystem.ToLogFormat("Результат проверки для Zabbix: 0");
					return 0;
				}
			} else {
				errorTrueConfServerCountByTimer++;

				if (errorTrueConfServerCountByTimer < 3) {
					LoggingSystem.LogMessageToFile("Ошибка проявилась менее 3 раз подряд, пропуск отправки заявки", !isZabbixCheck);
				} else {
					if (trueConfServerErrorSendedToStp) {
						LoggingSystem.LogMessageToFile("Сообщение об ошибке было отправлено ранее", !isZabbixCheck);
					} else {
						MailSystem.SendErrorMessageToStp(MailSystem.ErrorType.CheckStateError, errorMessage);
						trueConfServerErrorSendedToStp = true;
					}
				}

				if (isZabbixCheck) {
					Console.WriteLine("1");
					LoggingSystem.ToLogFormat("Результат проверки для Zabbix: 1");
					return 1;
				}
			}

			if (isSingleCheck)
				MailSystem.SendErrorMessageToStp(MailSystem.ErrorType.SingleCheck, checkResult);

			previousDayTrueConfServer = DateTime.Now.Day;

			return 0;
		}
	}
}
