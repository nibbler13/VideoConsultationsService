﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Xml.Serialization;
using SmsNotificationService.TrueConfObjects;
using Timer = System.Timers.Timer;

namespace SmsNotificationService {
	class EventSystem {
		private readonly TrueConf trueConf;
		private readonly FBClient fbClient;
		private readonly VerticaClient verticaClient;
		private readonly bool isZabbixCheck;
		
		private int previousDay;
		private uint errorTrueConfCount = 0;
		private uint errorTrueConfServerCountByTimer = 0;
		private uint errorMisFbCount = 0;
		private uint errorVerticaDbCount = 0;
		private uint errorNewFileCallCenter = 0;
		private bool mailSystemErrorSendedToStp = false;
		private bool fbClientErrorSendedToStp = false;
		private bool verticaClientErrorSendedToStp = false;
		private bool trueConfServerErrorSendedToStp = false;
		private bool fileCallCenterSendedToStp = false;
		private List<string> sendedWebinars = new List<string>();
		private List<string> sendedScheduleEvents = new List<string>();
		private List<string> sendedRemindersEvents = new List<string>();
		private List<string> sendedRemindersDocsEvents = new List<string>();
		private List<string> sendedNewPaymentNotification = new List<string>();
		private List<string> sendedPayment30minNotification = new List<string>();
		private List<string> sendedFileCallCenterNotification = new List<string>();

		//pcode
		private List<string> sendedRateSurveyNotificationToday = new List<string>();
		private const string sendedRateSurveyNotificationTodayFile = "sendedRateSurveyNotificationTodayFile.xml";
		private List<string> schedidChecked = new List<string>();

		private readonly Dictionary<string, string> generalRateLinksByFilials = new Dictionary<string, string> {
			{ "МДМ", "https://klinikabudzdorov.ru/otzyv" },
			{ "С-Пб.", "https://spb.klinikabudzdorov.ru/otzyv" },
			{ "М-СРЕТ", "https://klinikabudzdorov.ru/otzyv" },
			{ "Красн", "https://krasnodar.klinikabudzdorov.ru/otzyv" },
			{ "Казань", "https://kazan.klinikabudzdorov.ru/otzyv" },
			{ "СУЩ", "https://klinikabudzdorov.ru/otzyv" },
			{ "К-УРАЛ", "https://kamensk.klinikabudzdorov.ru/otzyv" },
			{ "Сочи", "https://sochi.klinikabudzdorov.ru/otzyv" },
			{ "Уфа", "https://ufa.klinikabudzdorov.ru/otzyv" }
		};


		public EventSystem(bool isZabbixCheck = false) {
			trueConf = new TrueConf();

			this.isZabbixCheck = isZabbixCheck;
			previousDay = DateTime.Now.Day;

			fbClient = new FBClient(
				Properties.Settings.Default.FbMisAddress,
				Properties.Settings.Default.FbMisName,
				isZabbixCheck);

			verticaClient = new VerticaClient(
				Properties.Settings.Default.VerticaDbAddress, 
				Properties.Settings.Default.VerticaDbName, 
				Properties.Settings.Default.VerticaDbUser, 
				Properties.Settings.Default.VerticaDbPassword);

			if (isZabbixCheck)
				Logging.ToLog("Проверка Zabbix");

			if (File.Exists(sendedRateSurveyNotificationTodayFile)) {
				try {
					Logging.ToLog("Считывание ранее отправленных СМС для оценки работы");
					XmlSerializer xmlSerializer = new XmlSerializer(sendedRateSurveyNotificationToday.GetType());
					using (Stream reader = new FileStream(sendedRateSurveyNotificationTodayFile, FileMode.Open))
						sendedRateSurveyNotificationToday = (List<string>)xmlSerializer.Deserialize(reader);
				} catch (Exception e) {
					Logging.ToLog(e.Message + Environment.NewLine + e.StackTrace);
				}
			}

			if (Debugger.IsAttached)
				CheckNewFilesInFoldersPndCallCenter();
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

				List<string>[] sendedEarlier = new List<string>[] {
					sendedWebinars,
					sendedScheduleEvents,
					sendedRemindersEvents,
					sendedRemindersDocsEvents,
					sendedNewPaymentNotification,
					sendedPayment30minNotification,
					sendedFileCallCenterNotification
				};

				foreach (List<string> list in sendedEarlier)
					if (list.Count > 10)
						list.RemoveRange(0, list.Count - 10);

				sendedRateSurveyNotificationToday.Clear();
				schedidChecked.Clear();

				previousDay = DateTime.Now.Day;
			}

			if (DateTime.Now.Minute % 10 == 0) {
				Thread thread1 = new Thread(DisconnectUsers);
				thread1.Start();

				Thread thread2 = new Thread(CheckRateSurveyNotifications);
				thread2.Start();

				Thread thread3 = new Thread(CheckNewFilesInFoldersPndCallCenter);
				thread3.Start();
			}

			if (!Debugger.IsAttached) {
				CheckTrueConfEvents();
				CheckMisEvents();
				//CheckNewFilesInFoldersPndCallCenter();
			}
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

			//DataTable paymentNotificationTable = fbClient.GetDataTable(
			//	DbQueries.sqlQueryGetPaymentNotifications + DbQueries.NotificationByCreateDate, 
			//	new Dictionary<string, string>(), ref errorMisFbCount, ref fbClientErrorSendedToStp);

			//paymentNotificationTable.Merge(fbClient.GetDataTable(
			//	DbQueries.sqlQueryGetPaymentNotifications + DbQueries.NotificationByWorktime,
			//	new Dictionary<string, string>(), ref errorMisFbCount, ref fbClientErrorSendedToStp));

			//paymentNotificationTable.Merge(fbClient.GetDataTable(
			//	DbQueries.sqlQueryGetPaymentNotifications + DbQueries.NotificationByWorktimeOffset2H,
			//	new Dictionary<string, string>(), ref errorMisFbCount, ref fbClientErrorSendedToStp));

			//paymentNotificationTable.Merge(fbClient.GetDataTable(
			//	DbQueries.sqlQueryGetPaymentNotifications + DbQueries.NotificationByWorktimeOffset2H,
			//	new Dictionary<string, string>(), ref errorMisFbCount, ref fbClientErrorSendedToStp));

			Logging.ToLog("Новых записей онлайн приемов: " + newSchedulesTable.Rows.Count);
			Logging.ToLog("Онлайн приемов, которые скоро начнутся: " + newNotificationsTable.Rows.Count);
			Logging.ToLog("Онлайн приемов, которые скоро начнутся для докторов: " + newNotificationsForDocsTable.Rows.Count);
			//Logging.ToLog("Приемов с онлайн-оплатой: " + paymentNotificationTable.Rows.Count);

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

						SendEventSMS(message, mobileNumber, schedID, ref sendedScheduleEvents);
					}

					if (values.Key.Equals("Напоминания")) {
						string message = Properties.Settings.Default.MessageReminderPatient;

						SendEventSMS(message, mobileNumber, schedID, ref sendedRemindersEvents);
					}

					if (values.Key.Equals("НапоминанияДоктора")) {
						string message = Properties.Settings.Default.MessageReminderDoctor;
						message = message.Replace("@time", dateTime.ToString("HH:mm"));

						SendEventSMS(message, mobileNumber, schedID, ref sendedRemindersDocsEvents);
					}
				}
			}

			//foreach (DataRow row in paymentNotificationTable.Rows) {
			//	try {
			//		string schedid = row["SCHEDID"].ToString();
			//		string onlineAccount = row["ONLINE_ACCOUNT"].ToString().Trim(' ');
			//		string payType = row["PAYTYPE"].ToString().Trim(' ');
			//		double amountPayable = double.Parse(row["AMOUNT_PAYABLE"].ToString());
			//		double paidByClient = double.Parse(row["PAID_BY_CLIENT"].ToString());
			//		DateTime scheduleTime = DateTime.Parse(row["WORKDATE"].ToString().Replace(" 0:00:00", "") + " " + row["WORKTIME"].ToString());
			//		Logging.ToLog("SCHEDID: " + schedid);

			//		if (payType.Equals("Предсчет") && paidByClient == amountPayable) {
			//			Logging.ToLog("Прием уже оплачен, пропуск");
			//			continue;
			//		}

			//		string subject = string.Empty;
			//		string body = string.Empty;

			//		if ((scheduleTime - DateTime.Now).TotalMinutes <= 30) {
			//			if (sendedEarlierPayment30minNotification.Contains(schedid)) {
			//				Logging.ToLog("Уведомление было отправлено ранее");
			//				continue;
			//			}

			//			//s.workdate = current_date and datediff(minute, current_time, dateadd(minute, S.BHOUR* 60 + S.BMIN, cast('00:00' as time))) between 0 and 30
			//			//2 событие - за 30 минут до начала
			//			//если amount_payable != paid_by_client - клиент не оплатил прием, который скоро начнется, отправить сообщение менеджеру
			//			subject = "Прием телемедицины скоро начнется, но он не был оплачен";
			//			body = subject;
			//			sendedEarlierPayment30minNotification.Add(schedid);

			//		} else {
			//			//1.3 предсчет и online_account = да - пропускаем, пациент оплатит самостоятельно
			//			if (onlineAccount.Equals("Да") && payType.Equals("Предсчет")) {
			//				Logging.ToLog("У пациента имеется ЛК и есть доступ к платежам, пропуск");
			//				continue;
			//			}

			//			if (sendedEarlierNewPaymentNotification.Contains(schedid)) {
			//				Logging.ToLog("Уведомление было отправлено ранее");
			//				continue;
			//			}

			//			subject = "Требуется оплата приема телемедицины";

			//			//s.createdate between dateadd(minute, -30, current_timestamp) and dateadd(minute, 30, current_timestamp) 
			//			//1 событие - появилась новая запись на телемедицину
			//			//если(amount_payable != paid_by_client)
			//			//1.4 предсчет и online_account = нет - клиент записался через ЛК, нет доступа к оплате, отправить менеджеру сообщение для оплаты через яндекс-деньги
			//			if (payType.Equals("Предсчет"))
			//				body = "Пациент записался через ЛК, у него нет доступа к платежам. Требуется оплата приема телемедицины через Яндекс-Деньги.";

			//			//1.2 услуга запланирована и online_account = да, то клиент записался через КЦ, отправить сообщение менеджеру для оплаты через ЛК
			//			else if (onlineAccount.Equals("Да"))
			//				body = "Пациент записался через КЦ, у него есть ЛК и доступ к платежам. Необходимо сформировать предварительный счет в МИС Инфоклиника.";

			//			//1.1 услуга запланирована и online_account = нет, то клиент записался через КЦ, отправить сообщение менеджеру для оплаты через яндекс-деньги
			//			else
			//				body = "Пациент записался через КЦ, у него нет доступа к платежам в ЛК. Требуется оплата приема телемедицины через Яндекс-деньги.";

			//			sendedEarlierNewPaymentNotification.Add(schedid);
			//		}

			//		string filial = row["SHORTNAME"].ToString();
			//		string doctor = row["DNAME"].ToString();
			//		string patientName = row["FULLNAME"].ToString();
			//		string patientHistnum = row["HISTNUM"].ToString();
			//		string patientBirthday = row["BDATE"].ToString().Replace(" 0:00:00", "");
			//		string patientPhone1 = row["PHONE1"].ToString();
			//		string patientPhone2 = row["PHONE2"].ToString();
			//		string patientPhone3 = row["PHONE3"].ToString();
			//		string clhistinfo = row["CLHISTINFO"].ToString();
			//		if (string.IsNullOrEmpty(clhistinfo) || string.IsNullOrWhiteSpace(clhistinfo))
			//			clhistinfo = "Наличный расчет";
			//		else
			//			clhistinfo = "Действующие страховки:<br>" + clhistinfo;

			//		string patientPhone = string.Empty;
			//		if (!string.IsNullOrEmpty(patientPhone1))
			//			patientPhone += patientPhone1 + "; ";
			//		if (!string.IsNullOrEmpty(patientPhone2))
			//			patientPhone += patientPhone2 + "; ";
			//		if (!string.IsNullOrEmpty(patientPhone3))
			//			patientPhone += patientPhone3 + "; ";
			//		patientPhone = patientPhone.TrimEnd(' ').TrimEnd(';');

			//		string createDate = row["CREATEDATE"].ToString();
			//		string depname = row["DEPNAME"].ToString();

			//		//if (depname.Equals("ТЕРАПИЯ ОНЛАЙН") || depname.Equals("Педиатрия онлайн"))
			//		//	if (clhistinfo.ToLower().Contains("ингос")) {
			//		//		Logging.ToLog("У пациента страховка Ингосстрах и отделение входит в страховку, пропуск");
			//		//		continue;
			//		//	}

			//		body += Environment.NewLine + Environment.NewLine;
			//		body += "<table border=\"1\">";
			//		body += "<caption>Информация о записи</caption>";
			//		body += "<tr><td>Филиал</td><td><b>" + filial + "</b></td></tr>";
			//		body += "<tr><td>Дата и время приема</td><td><b>" + scheduleTime.ToShortDateString() + " " + scheduleTime.ToShortTimeString() + "</b></td></tr>";
			//		body += "<tr><td>Отделение</td><td><b>" + depname + "</b></td></tr>";
			//		body += "<tr><td>ФИО Доктора</td><td><b>" + doctor + "</b></td></tr>";
			//		body += "<tr><td>№ ИБ пациента</td><td><b>" + patientHistnum + "</b></td></tr>";
			//		body += "<tr><td>ФИО</td><td><b>" + patientName + "</b></td></tr>";
			//		body += "<tr><td>Дата рождения</td><td><b>" + patientBirthday + "</b></td></tr>";
			//		body += "<tr><td>Контактный номер</td><td><b>" + patientPhone + "</b></td></tr>";
			//		body += "<tr><td>Способ оплаты</td><td><b>" + clhistinfo + "</b></td></tr>";
			//		body += "<tr><td>Дата и время записи</td><td><b>" + createDate + "</b></td></tr>";
			//		body += "<tr><td>Наличие ЛК и доступа к платежам</td><td><b>" + onlineAccount + "</b></td></tr>";
			//		body += "<tr><td>Тип оплаты</td><td><b>" + payType + "</b></td></tr>";
			//		body += "<tr><td>Сумма к оплате</td><td><b>" + amountPayable + "</b></td></tr>";
			//		body += "<tr><td>Оплачено</td><td><b>" + paidByClient + "</b></td></tr>";
			//		body += "</table>";

			//		string receiver = Properties.Settings.Default.MailPaymentNotificationAddress;

			//		switch (filial) {
			//			case "МДМ":
			//				receiver += ";l.v.shevtsova@bzklinika.ru;stadmdm@bzklinika.ru;mskm-kassa@bzklinika.ru";
			//				break;
			//			case "С-Пб.":
			//				receiver += ";regist_splp@bzklinika.ru;a.holdina@bzklinika.ru";
			//				break;
			//			case "М-СРЕТ":
			//				receiver += ";Reception_mspo@bzklinika.ru";
			//				break;
			//			case "Красн":
			//				receiver += ";reception_krd@bzklinika.ru;n.zachepilo@bzklinika.ru";
			//				break;
			//			case "Уфа":
			//				receiver += ";ufkk-kassa@bzklinika.ru;shakirova@bzklinika.ru";
			//				break;
			//			case "Казань":
			//				receiver += ";info_kzn@bzklinika.ru";
			//				break;
			//			case "СУЩ":
			//				receiver += ";kassa-mssu@bzklinika.ru";
			//				break;
			//			case "К-УРАЛ":
			//				receiver += ";g.gilyazova@bzklinika.ru;zh.soloshenko@bzklinika.ru";
			//				break;
			//			case "Сочи":
			//				receiver += ";reception_sochi@bzklinika.ru";
			//				break;
			//			default:
			//				break;
			//		}
					 
			//		MailSystem.SendMail(subject, body, receiver);
			//	} catch (Exception exc) {
			//		Logging.ToLog(exc.Message + Environment.NewLine + exc.StackTrace);
			//	}
			//}

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

			return 0;
		}

		private void DisconnectUsers() {
			Logging.ToLog("Получение списка пользователей TrueConf");
			try {
				List<ObjectUser> userListTC = trueConf.GetUserList().Result;
				Logging.ToLog("Пользователей в списке: " + userListTC.Count);
				int[] statusToDisconnect = new int[] { 1, 2, 5 };
				List<ObjectUser> userToDisconnectList = userListTC.Where(x => statusToDisconnect.Contains(x.Status)).ToList();
				Logging.ToLog("Пользователей со статусом ONLINE, BUSY, MULTIHOST: " + userToDisconnectList.Count);
				Logging.ToLog(string.Join("; ", userToDisconnectList.Select(x => x.Id)));

				int maxOnlineUsers = Properties.Settings.Default.MaxTrueConfOnlineUsers;
				int currentOnlineUsers = userToDisconnectList.Count;
				if (currentOnlineUsers <= maxOnlineUsers) {
					Logging.ToLog("Кол-во онлайн пользователей не превышает максимально допустимое кол-во (" 
						+ maxOnlineUsers + "). Пропуск отключения пользователей");
					return;
				}

				Logging.ToLog("Получение списка пользователей МИС Инфоклиника");
				DataTable userListMIS = fbClient.GetDataTable(
					DbQueries.GetTelemedicineUserList, 
					new Dictionary<string, string>(), 
					ref errorMisFbCount, 
					ref fbClientErrorSendedToStp);

				Logging.ToLog("Получено строк: " + userListMIS.Rows.Count);

				foreach (ObjectUser user in userToDisconnectList) {
					try {
						string userId = user.Id;

						DataRow[] selectResultByUser = userListMIS.Select("USERID = '" + userId + "'");
						if (selectResultByUser.Length == 0) {
							Logging.ToLog("Пользователь отсутствует в списке докторов МИС, " +
								"ведущих прием телемедицины, отключение. UserID: " + userId);
							Logging.ToLog("Успешно?: " + trueConf.DisconnectUser(userId).Result);
							currentOnlineUsers--;

							if (currentOnlineUsers <= maxOnlineUsers)
								break;

							continue;
						}

						int clientsToday = int.Parse(selectResultByUser[0]["LAST_PAC_DAY"].ToString());
						if (clientsToday == 0) {
							Logging.ToLog("У сотрудника нет пациентов на телемедицину на сегодняшний день, отключение. UserID: " + userId);
							Logging.ToLog("Успешно?: " + trueConf.DisconnectUser(userId).Result);
							currentOnlineUsers--;

							if (currentOnlineUsers <= maxOnlineUsers)
								break;

							continue;
						}

						int clientIn2Hours = int.Parse(selectResultByUser[0]["LAST_PAC_2HOURS"].ToString());
						if (clientIn2Hours == 0) {
							Logging.ToLog("У сотрудника нет пациентов на телемедицину на ближайшие 2 часа, отключение. UserID: " + userId);
							Logging.ToLog("Успешно?: " + trueConf.DisconnectUser(userId).Result);
							currentOnlineUsers--;

							if (currentOnlineUsers <= maxOnlineUsers)
								break;

							continue;
						}

						Logging.ToLog("У пользователя имеются приемы телемедицины на ближайшее время. UserID: " + userId);
					} catch (Exception exc) {
						Logging.ToLog(exc.Message + Environment.NewLine + exc.StackTrace);
					}
				}

				Logging.ToLog("Количество онлайн пользователей после отключения: " + currentOnlineUsers);
			} catch (Exception e) {
				Logging.ToLog(e.Message + Environment.NewLine + e.StackTrace);
			}
		}

		private void CheckRateSurveyNotifications() {
			Logging.ToLog("Проверка событий для отправки СМС с оценкой работы");

			if (DateTime.Now.Hour < 11 || DateTime.Now.Hour >= 21) {
				Logging.ToLog("Нерабочее время, пропуск");
				return;
			}

			DataTable dataTable = verticaClient.GetDataTable(DbQueries.GetRateSurveyNotifications, 
				ref errorVerticaDbCount, ref verticaClientErrorSendedToStp);

			if (errorVerticaDbCount > 10 && !verticaClientErrorSendedToStp) {
				Logging.ToLog("Отправка заявки в СТП");
				string sendResult = MailSystem.SendErrorMessageToStp(MailSystem.ErrorType.FireBird);
				if (string.IsNullOrEmpty(sendResult)) {
					verticaClientErrorSendedToStp = true;
				} else {
					Logging.ToLog("Не удалось отправить: " + sendResult);
				}
			}

			if (dataTable == null) {
				Logging.ToLog("Таблица с результами не существует, отмена проверки");
				return;
			}

			if (dataTable.Rows.Count == 0) {
				Logging.ToLog("Таблица с результатами не содержит записей, пропуск");
				return;
			}

			// 0 - чистка данных по условиям, 1 - фактичесткая отправка СМС
			for (int i = 0; i < 2; i++) {
				foreach (DataRow row in dataTable.Rows) {
					try {
						string schedid = row["schedid"].ToString();

						if (schedidChecked.Contains(schedid))
							continue;

						DateTime scheduleDate = DateTime.Parse(row["schedule_date"].ToString());
						string pcode = row["pcode"].ToString();
						string patient = row["patient"].ToString();
						//string ageInYears = row["age_in_years"].ToString();
						string phone = row["phone"].ToString();
						string phone2 = row["phone2"].ToString();
						string phone3 = row["phone3"].ToString();
						//string dcode = row["dcode"].ToString();
						//string doctor = row["doctor"].ToString();
						string link = row["link"].ToString();
						//string profname = row["profname"].ToString();
						//string doctpost = row["doctpost"].ToString();
						string targetFilShortname = row["target_fil_shortname"].ToString();
						//string department = row["department"].ToString();
						//string docname = row["docname"].ToString();

						string rowInfo =
							"schedid: " + schedid +
							", scheduleDate : " + scheduleDate.ToShortDateString() + " " + scheduleDate.ToShortTimeString() +
							", pcode: " + pcode;// +
							//", patient: " + patient +
							//", ageInYears: " + ageInYears +
							//", phone: " + phone +
							//", phone2: " + phone2 +
							//", phone3: " + phone3 +
							//", dcode: " + dcode +
							//", doctor: " + doctor +
							//", link: " + link +
							//", profname: " + profname +
							//", doctpost: " + doctpost +
							//", targetFilShortname: " + targetFilShortname +
							//", department: " + department +
							//", docname: " + docname;

						if (i == 0) {
							// 0 - ok, 1 - прием после 18 часов, 2 - предыдущий день, 3 - меньше 3 часов с приема, 4 - предыдущий день прием до 18 часов
							int hasToBeSkipped = 0;
							if (scheduleDate.Day == DateTime.Now.Day && scheduleDate.Hour >= 18)
								hasToBeSkipped = 1;
							else if (scheduleDate.Day == DateTime.Now.Day - 1 && DateTime.Now.Hour != 11)
								hasToBeSkipped = 2;
							else if (scheduleDate.Day == DateTime.Now.Day && (DateTime.Now - scheduleDate).TotalHours < 3)
								hasToBeSkipped = 3;
							else if (scheduleDate.Day == DateTime.Now.Day - 1 && scheduleDate.Hour < 18)
								hasToBeSkipped = 4;

							if (hasToBeSkipped > 0) {
								Logging.ToLog("rowInfo: " + rowInfo);

								string skipText = "--- Назначение schedid: " + schedid + " пропуск, ";
								if (hasToBeSkipped == 1)
									skipText += "прием после 18 часов, отправка на следующий день";
								else if (hasToBeSkipped == 2)
									skipText += "прием за предыдущий день, отправка в 11 часов";
								else if (hasToBeSkipped == 3)
									skipText += "с момента приема прошло меньше 3 часов";
								else if (hasToBeSkipped == 4)
									skipText += "прием за предыдущий день, начало приема до 18 часов, отправлено ранее";

								Logging.ToLog(skipText);
								row["accepted_to_send"] = "0";

								//3 - отправка будет позднее в этот же день
								if (hasToBeSkipped != 3)
									schedidChecked.Add(schedid);
							}

							continue;
						}

						if (sendedRateSurveyNotificationToday.Contains(pcode)) {
							Logging.ToLog("rowInfo: " + rowInfo);
							Logging.ToLog("--- Назначение schedid: " + schedid + " пропуск, данному пациенту сегодня уже было отправлено СМС");
							row["accepted_to_send"] = "0";
							schedidChecked.Add(schedid);
							continue;
						}

						schedidChecked.Add(schedid);
						Logging.ToLog("rowInfo: " + rowInfo);

						string phoneToSend = phone;
						if (string.IsNullOrEmpty(phoneToSend))
							phoneToSend = phone2;
						if (string.IsNullOrEmpty(phoneToSend))
							phoneToSend = phone3;
						if (string.IsNullOrEmpty(phoneToSend)) {
							Logging.ToLog("--- Назначение schedid: " + schedid + " пропуск, не указан номер пациента для отправки");
							continue;
						}

						//if (Debugger.IsAttached)
						//	phoneToSend = "79601811873";

						DataRow[] dataRows = dataTable.Select("pcode = " + pcode + " and accepted_to_send = '' and schedid <> " + schedid);
						string smsText = (dataRows.Length == 0 ? "Оставьте отзыв о враче: " : "Оставьте отзыв: ") + 
							(dataRows.Length == 0 ? link : 
							(generalRateLinksByFilials.ContainsKey(targetFilShortname) ? 
							generalRateLinksByFilials[targetFilShortname] : link));

						string logSend = "+++ Отправка сообщения для номера " + phoneToSend + " с текстом: " + smsText +
							", назначение schedid: " + schedid + ", дата приема: " + scheduleDate;
						Logging.ToLog(logSend);

						ItemSendResult itemSendResult = SvyaznoyZagruzka.SendMessage(phoneToSend, smsText).Result;

						if (itemSendResult.IsSuccessStatusCode) {
							WriteSendInfoToDB(itemSendResult.MessageId, patient, phoneToSend, smsText);
							sendedRateSurveyNotificationToday.Add(pcode);
						}
					} catch (Exception e) {
						Logging.ToLog(e.Message + Environment.NewLine + e.StackTrace);
					}
				}
			}

			try {
				XmlSerializer xmlSerializer = new XmlSerializer(sendedRateSurveyNotificationToday.GetType());
				using (StreamWriter stream = new StreamWriter(sendedRateSurveyNotificationTodayFile))
					xmlSerializer.Serialize(stream, sendedRateSurveyNotificationToday);
			} catch (Exception e) {
				Logging.ToLog(e.Message + Environment.NewLine + e.StackTrace);
			}
		}

		private void WriteSendInfoToDB(string smsId, string clientName, string clientNumber, string smsText) {
			Dictionary<string, string> param = new Dictionary<string, string> {
				{ "@smsID",  smsId},
				{ "@sendDate", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss") },
				{ "@recipientName", clientName },
				{ "@recipientPhone", clientNumber },
				{ "@smsText", smsText },
				{ "@isRightNow", "1" },
				{ "@isDelayed", "0" },
				{ "@delayedTime", null }
			};

			try {
				fbClient.ExecuteUpdateQuery(DbQueries.sqlInsertSmsSendInfo, param);
			} catch (Exception e) {
				Logging.ToLog(e.Message + Environment.NewLine + e.StackTrace);
			}
		}


		private void CheckNewFilesInFoldersPndCallCenter() {
			Logging.ToLog("---Проверка новых файлов заявок ПНД для КЦ");

			string pathToCheck = @"\\mskzk-fs-01\PND\";

			string[] emails = new string[] {
				"pnd.zayavka@bzklinika.ru"
			};

			//string[] phoneNumbers = new string[] {
			//	"79506145587",
			//	"79875535061",
			//	"79107991396"
			//};

			bool hasError = false;
			Dictionary<string, string> newFiles = new Dictionary<string, string>();
			try {
				if (Directory.Exists(pathToCheck)) {
					string[] files = Directory.GetFiles(pathToCheck);
					foreach (string file in files) {
						if (sendedFileCallCenterNotification.Contains(file))
							continue;

						DateTime creationTime = File.GetCreationTime(file);
						if ((DateTime.Now - creationTime).TotalMinutes > 120) 
							continue;

						newFiles.Add(file, creationTime.ToString("yyyy.MM.dd HH:mm:ss"));
						sendedFileCallCenterNotification.Add(file);
					}
				} else {
					Logging.ToLog("Не удается получить доступ к папке: " + pathToCheck);
					hasError = true;
				}
			} catch (Exception e) {
				Logging.ToLog(e.Message + Environment.NewLine + e.StackTrace);
				hasError = true;
			}

			if (newFiles.Count > 0) {
				string body = "В папке <a href=\"" + pathToCheck + "\">" + pathToCheck + "</a> появился новый файл(ы): <br>";
				foreach (KeyValuePair<string, string> file in newFiles) 
					body += "<a href=\"" + file.Key + "\">" + Path.GetFileName(file.Key) + "</a>, время создания: " + file.Value + "<br>";

				foreach (string email in emails)
					MailSystem.SendMail("Новая заявка ПНД от Ингосстрах", body, email);

				//foreach (string phoneNumber in phoneNumbers)
				//	SvyaznoyZagruzka.SendMessage(phoneNumber, "Появилась новая заявка ПНД ИГС").Wait();
			}

			if (!hasError) {
				errorNewFileCallCenter = 0;
				fileCallCenterSendedToStp = false;
			} else {
				if (errorNewFileCallCenter > 10) {
					if (!fileCallCenterSendedToStp) {
						MailSystem.SendErrorMessageToStp(MailSystem.ErrorType.CallCenterPndFiles);
						fileCallCenterSendedToStp = true;
					}

					return;
				};
				
				errorNewFileCallCenter++;
			}
		}
	}
}
