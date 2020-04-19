using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

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

		/* новая запись в расписании от сегодня и вперед */
		private readonly string sqlQueryGetNewSchedule =
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
			"	AND Ds.DepNum IN (992582623, 992582624, 992582625, 992582626, 992582628, 992092102, 992582680) " +
			"	AND Sh.OnlineType = 1";

		/* напоминание за 0-6 минут до начала пациентам*/
		private readonly string sqlQueryGetNewNotifications = 
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
			"	AND Ds.DepNum IN (992582623, 992582624, 992582625, 992582626, 992582628, 992092102, 992582680) " +
			"	AND Sh.OnlineType = 1";
		
		/* напоминание за 0-6 минут до начала докторам*/
		private readonly string sqlQueryGetNewNotificationsForDocs =
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
			"	AND Ds.DepNum IN (992582623, 992582624, 992582625, 992582626, 992582628, 992092102, 992582680)";

		//new
		//992582623	Онлайн-консультация - гинекология
		//992582624	Онлайн-консультация - кардиология
		//992582625	Онлайн-консультация - неврология
		//992582626	Онлайн-консультация - терапия
		//992582628	Онлайн-консультация - оториноларингология
		//992092102	Санитарно просветительская работа
		//992582680 Педиатрия онлайн

		//проверка наличия напоминаний об оплате
		private const string sqlQueryGetPaymentNotifications = 
			"select " +
			"s.schedid, " +
			"f.shortname, " +
			"d.dname, " +
			"s.workdate, " +
			"lpad(dateadd(minute, S.BHOUR * 60 + S.BMIN, cast('00:00' as time)), 5) WORKTIME, " +
			"c.fullname, " +
			"c.histnum, " +
			"c.bdate, " +
			"c.phone3, " +
			"c.phone2, " +
			"c.phone1, " +
			"s.createdate, " +
			"p.SUMMARUB_DISC amount_payable, " +
			"coalesce(pay.amountrub, 0) paid_by_client, " +
			"coalesce(c.WEBPAYTYPE, 0) as webpaytype, " +
			"coalesce(c.WEBACCESSTYPE, 0) as webaccesstype " +
			"from schedule s " +
			"join filials f on f.filid = s.filial " +
			"join doctor d on d.dcode = s.dcode " +
			"join clients c on c.pcode = s.pcode " +
			"join doctshedule ds on ds.schedident = s.schedident and coaLESCE(ONLINEMODE, 0) = 1 " +
			"join dailyplan p on p.did = s.planid and p.PLANTYPE = 204 " +
			"left join ( " +
			"select pcode, planid, cashid, sum(iif(typeoper in (2,5, 102),-amountrub,amountrub)) amountrub from ( " +
			"  select PCode, paydate, DCode, iif(PayCode = 5, 2, 1) typeoper, cashid, planid, AmountRub from Incom " +
			"  where PayCode in (1,3) or (PayCode = 5 and IncRef not in (10,11)) " +
			"union all select PCode, lcdate, DCode, 1 TypeOper, cashid, planid, AmountRub from LoseCredit " +
			"union all select PCode, pmdate, DCode, 2 TypeOper, cashID, planid, AmountRub from JPPayments " +
			"  where OperType = 5 and (not IncRef  in (10, 11)) " +
			"union all select PCode, paydate, DCode,  TypeOper, cashid, planid, AmountRub from ClAvans) group by 1,2,3) pay on pay.planid = s.planid and pay.pcode = s.pcode " +
			"where s.workdate = current_date";


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

				previousDay = DateTime.Now.Day;
			}

			CheckTrueConfEvents();
			CheckMisEvents();
		}

		private void CheckTrueConfEvents() {
			Logging.ToLog("---Проверка сервера TrueConf");
			try {
				List<ObjectConference> conferenceList = trueConf.GetConferenceList().Result;
				Logging.ToLog("Количество существующих вебинаров: " + conferenceList.Count);
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
				sqlQueryGetNewSchedule, new Dictionary<string, string>(), 
				ref errorMisFbCount, ref fbClientErrorSendedToStp);

			DataTable newNotificationsTable = fbClient.GetDataTable(
				sqlQueryGetNewNotifications, new Dictionary<string, string>(), 
				ref errorMisFbCount, ref fbClientErrorSendedToStp);

			DataTable newNotificationsForDocsTable = fbClient.GetDataTable(
				sqlQueryGetNewNotificationsForDocs, new Dictionary<string, string>(),
				ref errorMisFbCount, ref fbClientErrorSendedToStp);

			DataTable paymentNotificationTable = fbClient.GetDataTable(
				sqlQueryGetPaymentNotifications, new Dictionary<string, string>(),
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
					DateTime scheduleTime = DateTime.Parse(row["WORKDATE"].ToString() + " " + row["WORKTIME"].ToString());

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
						subject = "Прием телемедицины скоро начнется, но не был оплачен";

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
					string patientBirthday = row["BDATE"].ToString();
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
	}
}
