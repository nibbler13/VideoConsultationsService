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
		private FBClient fbClient = new FBClient(
			Properties.Settings.Default.fbMisAddress,
			Properties.Settings.Default.fbMisName);
		
		private int previousDay;
		private uint errorTrueConfCount = 0;
		private uint errorMisFbCount = 0;
		private bool mailSystemErrorSendedToStp = false;
		private bool fbClientErrorSendedToStp = false;
		private List<string> notificatedWebinars = new List<string>();
		private List<string> notificatedScheduleEvents = new List<string>();
		private List<string> notificatedRemindersEvents = new List<string>();
		private List<string> notificatedRemindersDocsEvents = new List<string>();

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

		public EventSystem() {
			previousDay = DateTime.Now.Day;
		}

		public void CheckForNewEvents() {
			Timer timer = new Timer(Properties.Settings.Default.updatePeriodInSeconds * 1000);
			timer.Elapsed += Timer_Elapsed;
			timer.AutoReset = true;
			timer.Start();
		}

		private void Timer_Elapsed(object sender, ElapsedEventArgs e) {
			Console.WriteLine("---Timer_Elapsed");
			if (previousDay != DateTime.Now.Day) {
				LoggingSystem.LogMessageToFile("Обнуление списка уведомлений");

				foreach (List<string> list in new List<string>[] {
					notificatedWebinars,
					notificatedScheduleEvents,
					notificatedRemindersEvents,
					notificatedRemindersDocsEvents})
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

					if (!notificatedWebinars.Contains(webinar.id))
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
				string sendResult = SmsSystem.SendMessageToStp(true, false);
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

					if (values.Key.Equals("Назначения"))
						SendEventSMS(SmsSystem.SendScheduleNotification, schedID, mobileNumber, dateTime, ref notificatedScheduleEvents);
					if (values.Key.Equals("Напоминания"))
						SendEventSMS(SmsSystem.SendReminder, schedID, mobileNumber, dateTime, ref notificatedRemindersEvents);
					if (values.Key.Equals("НапоминанияДоктора"))
						SendEventSMS(SmsSystem.SendReminderForDocs, schedID, mobileNumber, dateTime, ref notificatedRemindersDocsEvents);
				}
			}

			if (errorMisFbCount > 1000 && !fbClientErrorSendedToStp) {
				LoggingSystem.LogMessageToFile("Отправка заявки в СТП");
				string sendResult = SmsSystem.SendMessageToStp(false, true);
				if (string.IsNullOrEmpty(sendResult)) {
					fbClientErrorSendedToStp = true;
				} else {
					LoggingSystem.LogMessageToFile("Не удалось отправить: " + sendResult);
				}
			}
		}

		private void SendEventSMS(
			Func<string, DateTime, string> sendFunction, 
			string schedId, 
			string mobileNumber,
			DateTime dateTime,
			ref List<string> notificatedList) {

			if (notificatedList.Contains(schedId))
				return;

			string sendResult = sendFunction(mobileNumber, dateTime);
			if (!string.IsNullOrEmpty(sendResult)) {
				LoggingSystem.LogMessageToFile("Не удалось отправить СМС: " + sendResult);
				return;
			}

			LoggingSystem.LogMessageToFile("Отправлено успешно");
			notificatedList.Add(schedId);
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

			string sendResult = SmsSystem.SendReminder(phoneNumber, dateTime);
			if (string.IsNullOrEmpty(sendResult)) {
				LoggingSystem.LogMessageToFile("Отправлено успешно");
				notificatedWebinars.Add(webinar.id);
				return;
			}

			LoggingSystem.LogMessageToFile("Не удалось отправить смс: " + sendResult);
		}
	}
}
