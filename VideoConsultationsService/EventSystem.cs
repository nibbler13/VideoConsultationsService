using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using VideoConsultationsService.TrueConfObjects;

namespace VideoConsultationsService {
	class EventSystem {
		private readonly TrueConf trueConf;
		private readonly FBClient fbClient;
		private readonly bool isZabbixCheck;
		
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
		private List<string> sendedEarlierNewPaymentNotification = new List<string>();
		private List<string> sendedEarlierPayment30minNotification = new List<string>();


		public EventSystem(bool isZabbixCheck = false) {
			trueConf = new TrueConf();
			this.isZabbixCheck = isZabbixCheck;
			previousDay = DateTime.Now.Day;
			previousDayTrueConfServer = previousDay;

			fbClient = new FBClient(
				Properties.Settings.Default.FbMisAddress,
				Properties.Settings.Default.FbMisName,
				isZabbixCheck);

			if (isZabbixCheck)
				Logging.ToLog("Проверка Zabbix");
		}

		public void CheckForNewEvents() {
			Timer timerNewEvents = new Timer(Properties.Settings.Default.UpdatePeriodInSeconds * 1000);
			timerNewEvents.Elapsed += TimerNewEvents_Elapsed;
			timerNewEvents.AutoReset = true;
			timerNewEvents.Start();

			TimerNewEvents_Elapsed(timerNewEvents, null);
		}

		private void TimerNewEvents_Elapsed(object sender, ElapsedEventArgs e) {
			if (previousDay != DateTime.Now.Day) {
				Logging.ToLog("Обнуление списка уведомлений");

				foreach (List<string> list in new List<string>[] {
					sendedEarlierWebinars,
					sendedEarlierScheduleEvents,
					sendedEarlierRemindersEvents,
					sendedEarlierRemindersDocsEvents,
					sendedEarlierNewPaymentNotification,
					sendedEarlierPayment30minNotification})
					if (list.Count > 10)
						list.RemoveRange(0, list.Count - 10);

				DisconnectUsers();

				previousDay = DateTime.Now.Day;
			}

			CheckTrueConfEvents();
			CheckMisEvents();
		}

		private void CheckTrueConfEvents() {
			Logging.ToLog("---Проверка сервера TrueConf");
			try {
				List<ObjectConference> conferenceList = trueConf.GetConferenceList().Result;
				Logging.ToLog("Количество существующих конференций: " + conferenceList.Count);
				errorTrueConfCount = 0;
				mailSystemErrorSendedToStp = false;
			} catch (Exception exception) {
				Logging.ToLog("Не удалось получить данные с сервера TrueConf: " + exception.Message);
				errorTrueConfCount++;
			}

			if (errorTrueConfCount > 1000 && !mailSystemErrorSendedToStp) {
				Logging.ToLog("Отправка заявки в СТП");
				string sendResult = MailSystem.SendErrorMessageToStp(MailSystem.ErrorType.TrueConf);
				if (string.IsNullOrEmpty(sendResult)) {
					mailSystemErrorSendedToStp = true;
				} else {
					Logging.ToLog("Не удалось отправить: " + sendResult);
				}
			}
		}

		private void CheckMisEvents() {
			Logging.ToLog("---Проверка базы МИС");

			DataTable newSchedulesTable = fbClient.GetDataTable(
				DbQueries.sqlQueryGetNewSchedule, new Dictionary<string, string>(), 
				ref errorMisFbCount, ref fbClientErrorSendedToStp);

			DataTable newNotificationsTable = fbClient.GetDataTable(
				DbQueries.sqlQueryGetNewNotifications, new Dictionary<string, string>(), 
				ref errorMisFbCount, ref fbClientErrorSendedToStp);

			DataTable newNotificationsForDocsTable = fbClient.GetDataTable(
				DbQueries.sqlQueryGetNewNotificationsForDocs, new Dictionary<string, string>(),
				ref errorMisFbCount, ref fbClientErrorSendedToStp);

			DataTable paymentNotificationTable = fbClient.GetDataTable(
				DbQueries.sqlQueryGetPaymentNotifications, new Dictionary<string, string>(),
				ref errorMisFbCount, ref fbClientErrorSendedToStp);

			Logging.ToLog("Новых записей онлайн приемов: " + newSchedulesTable.Rows.Count);
			Logging.ToLog("Онлайн приемов, которые скоро начнутся: " + newNotificationsTable.Rows.Count);
			Logging.ToLog("Онлайн приемов, которые скоро начнутся для докторов: " + newNotificationsForDocsTable.Rows.Count);
			Logging.ToLog("Приемов с онлайн-оплатой: " + paymentNotificationTable.Rows.Count);

			Dictionary<string, DataTable> tables = new Dictionary<string, DataTable>() {
				{ "Назначения", newSchedulesTable },
				{ "Напоминания", newNotificationsTable },
				{ "НапоминанияДоктора", newNotificationsForDocsTable }
			};

			foreach (KeyValuePair<string, DataTable> values in tables) {
				foreach (DataRow row in values.Value.Rows) {
					string schedID;
					try {
						schedID = row["SCHEDID"].ToString();
					} catch (Exception) {
						Logging.ToLog("Отсутствует поле SCHEDID, пропуск");
						continue;
					}
				
					string mobileNumber = GetMobilePhoneNumber(row);
						
					if (string.IsNullOrEmpty(mobileNumber)) {
						Logging.ToLog("Не удалось определить корректный мобильный номер, пропуск");
						continue;
					}

					DateTime dateTime = GetDateTime(row);
					if (dateTime.Equals(new DateTime())) {
						Logging.ToLog("Не удалось определить дату назначения, пропуск");
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

			foreach (DataRow row in paymentNotificationTable.Rows) {
				try {
					string schedid = row["SCHEDID"].ToString();
					string webPayType = row["WEBPAYTYPE"].ToString();
					string webAccessType = row["WEBACCESSTYPE"].ToString();
					double amountPayable = double.Parse(row["AMOUNT_PAYABLE"].ToString());
					double paidByClient = double.Parse(row["PAID_BY_CLIENT"].ToString());
					DateTime scheduleTime = DateTime.Parse(row["WORKDATE"].ToString().Replace(" 0:00:00", "") + " " + row["WORKTIME"].ToString());

					if (paidByClient == amountPayable) {
						Logging.ToLog("SCHEDID: " + schedid + " - прием уже оплачен, пропуск");
						continue;
					}

					if (sendedEarlierPayment30minNotification.Contains(schedid) ||
						sendedEarlierNewPaymentNotification.Contains(schedid)) {
						Logging.ToLog("Уведомление было отправлено ранее");
						continue;
					}

					string subject;

					if ((scheduleTime - DateTime.Now).TotalMinutes <= 30) {
						subject = "Прием телемедицины скоро начнется, но он не был оплачен";

					} else {
						if (webPayType.Equals("1") && webAccessType.Equals(3)) {
							Logging.ToLog("У пациента имеется ЛК и есть доступ к платежам, пропуск");
							continue;
						}

						subject = "Требуется оплата приема телемедицины через Яндекс-Деньги";
					}

					string filial = row["SHORTNAME"].ToString();
					string doctor = row["DNAME"].ToString();
					string patientName = row["FULLNAME"].ToString();
					string patientHistnum = row["HISTNUM"].ToString();
					string patientBirthday = row["BDATE"].ToString().Replace(" 0:00:00", "");
					string patientPhone1 = row["PHONE1"].ToString();
					string patientPhone2 = row["PHONE2"].ToString();
					string patientPhone3 = row["PHONE3"].ToString();

					string patientPhone = string.Empty;
					if (!string.IsNullOrEmpty(patientPhone1))
						patientPhone += patientPhone1 + "; ";
					if (!string.IsNullOrEmpty(patientPhone2))
						patientPhone += patientPhone2 + "; ";
					if (!string.IsNullOrEmpty(patientPhone3))
						patientPhone += patientPhone3 + "; ";
					patientPhone = patientPhone.TrimEnd(' ').TrimEnd(';');

					string createDate = row["CREATEDATE"].ToString();

					string body = subject + Environment.NewLine + Environment.NewLine;
					body += "<table border=\"1\">";
					body += "<caption>Информация о записи</caption>";
					body += "<tr><td>Филиал</td><td><b>" + filial + "</b></td></tr>";
					body += "<tr><td>Дата и время приема</td><td><b>" + scheduleTime.ToShortDateString() + " " + scheduleTime.ToShortTimeString() + "</b></td></tr>";
					body += "<tr><td>ФИО Доктора</td><td><b>" + doctor + "</b></td></tr>";
					body += "<tr><td>№ ИБ пациента</td><td><b>" + patientHistnum + "</b></td></tr>";
					body += "<tr><td>ФИО</td><td><b>" + patientName + "</b></td></tr>";
					body += "<tr><td>Дата рождения</td><td><b>" + patientBirthday + "</b></td></tr>";
					body += "<tr><td>Контактный номер</td><td><b>" + patientPhone + "</b></td></tr>";
					body += "<tr><td>Дата и время записи</td><td><b>" + createDate + "</b></td></tr>";
					body += "<tr><td>Сумма к оплате</td><td><b>" + amountPayable + "</b></td></tr>";
					body += "<tr><td>Оплачено</td><td><b>" + paidByClient + "</b></td></tr>";
					body += "</table>";

					string receiver = Properties.Settings.Default.MailPaymentNotificationAddress;
					MailSystem.SendMail(subject, body, receiver);
				} catch (Exception exc) {
					Logging.ToLog(exc.Message + Environment.NewLine + exc.StackTrace);
				}
			}

			if (errorMisFbCount > 1000 && !fbClientErrorSendedToStp) {
				Logging.ToLog("Отправка заявки в СТП");
				string sendResult = MailSystem.SendErrorMessageToStp(MailSystem.ErrorType.FireBird);
				if (string.IsNullOrEmpty(sendResult)) {
					fbClientErrorSendedToStp = true;
				} else {
					Logging.ToLog("Не удалось отправить: " + sendResult);
				}
			}
		}

		private void SendEventSMS(string message, string mobileNumber, string schedId, ref List<string> sendedEarlier) {
			if (sendedEarlier.Contains(schedId))
				return;

			ItemSendResult sendResult = SvyaznoyZagruzka.SendMessage(mobileNumber, message).Result;
			if (!sendResult.IsSuccessStatusCode) {
				Logging.ToLog("Не удалось отправить СМС: " + sendResult);
				return;
			}

			Logging.ToLog("Отправлено успешно, идентификатор: " + sendResult.MessageId);
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


		public int CheckTrueConfServer(bool isSingleCheck = false) {
			string checkResult = string.Empty;
			string currentMessage = "--- Проверка состояния сервера TrueConf";

			Logging.ToLog(currentMessage, !isZabbixCheck);
			checkResult += Logging.ToLogFormat(currentMessage, true);
			string errorMessage = string.Empty;

			currentMessage = "Получение списка всех конференций";
			Logging.ToLog(currentMessage, !isZabbixCheck);
			checkResult += Logging.ToLogFormat(currentMessage, true);

			List<ObjectConference> conferenceList = null;
			try {
				conferenceList = trueConf.GetConferenceList().Result;
				currentMessage = "Список конференций содержит записей: " + conferenceList.Count;
				Logging.ToLog(currentMessage, !isZabbixCheck);
				checkResult += Logging.ToLogFormat(currentMessage, true);
			} catch (Exception excAllWebinar) {
				string msgAllWebinar = excAllWebinar.Message + Environment.NewLine + excAllWebinar.StackTrace;
				Logging.ToLog(msgAllWebinar, !isZabbixCheck);
				checkResult += Logging.ToLogFormat(msgAllWebinar, true);
				errorMessage += msgAllWebinar + Environment.NewLine;
			}

			if (string.IsNullOrEmpty(errorMessage)) {
				currentMessage = "--- Проверка выполнена успешно, ошибок не обнаружено";
				Logging.ToLog(currentMessage, !isZabbixCheck);
				checkResult += Logging.ToLogFormat(currentMessage);
				errorTrueConfServerCountByTimer = 0;
				
				if (isZabbixCheck) {
					Console.WriteLine("0");
					Logging.ToLogFormat("Результат проверки для Zabbix: 0");
					return 0;
				}
			} else {
				errorTrueConfServerCountByTimer++;

				if (errorTrueConfServerCountByTimer < 3) {
					Logging.ToLog("Ошибка проявилась менее 3 раз подряд, пропуск отправки заявки", !isZabbixCheck);
				} else {
					if (trueConfServerErrorSendedToStp) {
						Logging.ToLog("Сообщение об ошибке было отправлено ранее", !isZabbixCheck);
					} else {
						MailSystem.SendErrorMessageToStp(MailSystem.ErrorType.CheckStateError, errorMessage);
						trueConfServerErrorSendedToStp = true;
					}
				}

				if (isZabbixCheck) {
					Console.WriteLine("1");
					Logging.ToLogFormat("Результат проверки для Zabbix: 1");
					return 1;
				}
			}

			if (isSingleCheck)
				MailSystem.SendErrorMessageToStp(MailSystem.ErrorType.SingleCheck, checkResult);

			previousDayTrueConfServer = DateTime.Now.Day;

			return 0;
		}

		private void DisconnectUsers() {
			Logging.ToLog("Получение списка пользователей TrueConf");
			try {
				List<ObjectUser> userList = trueConf.GetUserList().Result;
				Logging.ToLog("Пользователей в списке: " + userList.Count);
				int[] statusToDisconnect = new int[] { 1, 2, 5 };
				List<ObjectUser> userToDisconnectList = userList.Where(x => statusToDisconnect.Contains(x.Status)).ToList();
				Logging.ToLog("Пользователей со статусом ONLINE, BUSY, MULTIHOST: " + userToDisconnectList.Count);
				foreach (ObjectUser user in userToDisconnectList) {
					Logging.ToLog("Отключение пользователя: " + user.Id);
					Logging.ToLog("Усешно?: " + trueConf.DisconnectUser(user.Id).Result);
				}
			} catch (Exception e) {
				Logging.ToLog(e.Message + Environment.NewLine + e.StackTrace);
			}
		}
	}
}
