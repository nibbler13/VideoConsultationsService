using System.Net.Mail;
using System;

namespace VideoConsultationsService {
	class SmsSystem {
		private const string HEADER = "<!godmode> ";

		public static string SendMessageToStp(bool stpTrueConf, bool stpFbClient) {
			return SendMailToSmsGate("", "", stpTrueConf, stpFbClient);
		}

		public static string SendScheduleNotification(string number, DateTime dateTime) {
			string subject = "СМС о записи";
			string body = HEADER + number + " Вы записаны на видеоконсультацию, которая состоится в " +
				dateTime.ToShortTimeString() + " " + dateTime.ToShortDateString() +
				". В назначенное время зайдите в раздел 'Мои записи' в личном кабинете " +
				"и нажмите 'онлайн приём'";
			return SendMailToSmsGate(subject, body);
		}

		public static string SendReminder(string number, DateTime dateTime) {
			string subject = "Напоминание о видеоконсультации";
			string body = HEADER + number + " Напоминаем, что Вы записаны на видеоконсультацию, которая состоится в " +
				dateTime.ToShortTimeString() + ". Обращаем внимание, что онлайн консультация не может заменить визит к врачу.";
			return SendMailToSmsGate(subject, body);
		}

		public static string SendReminderForDocs(string number, DateTime dateTime) {
			string subject = "Напоминание доктору о видеоконсультации";
			string body = HEADER + number + " Напоминаем, что к Вам записан пациент на консультацию, которая состоится в " +
				dateTime.ToShortTimeString() + ".";
			return SendMailToSmsGate(subject, body);
		}

		private static string SendMailToSmsGate (string subject, string body, bool stpTrueConf = false, bool stpFbClient = false) {
			LoggingSystem.LogMessageToFile("Отправка сообщения, тема: " + subject + ", текст: " + body);

			try {
				MailAddress from = new MailAddress(
					Properties.Settings.Default.mailUserName + "@" + 
					Properties.Settings.Default.mailUserDomain, "TrueConfApiTest");
				MailAddress to = new MailAddress(Properties.Settings.Default.mailSmsAddress);

				if (stpFbClient || stpTrueConf) {
					to = new MailAddress("stp@bzklinika.ru");
					subject = "Ошибки в работе VideoConsultationsSevice";
					body = "На группу поддержки бизнес-приложений" + Environment.NewLine +
						"Сервису VideoConsultationsSevice не удалось корректно загрузить" +
						" данные с сервера @ в течение длительного периода" + Environment.NewLine +
						Environment.NewLine + "Это автоматически сгенерированное сообщение" +
						Environment.NewLine + "Просьба не отвечать на него" + Environment.NewLine +
 						"Имя системы: " + Environment.MachineName;
				}

				if (stpTrueConf)
					body = body.Replace("@", "TrueConf");

				if (stpFbClient)
					body = body.Replace("@", "FireBird " + Properties.Settings.Default.fbMisAddress + ":" +
						Properties.Settings.Default.fbMisName);
				
				using (MailMessage message = new MailMessage(from, to)) {
					message.Subject = subject;
					message.Body = body;
					if (!string.IsNullOrEmpty(Properties.Settings.Default.mailCopyAddresss))
						message.CC.Add(Properties.Settings.Default.mailCopyAddresss);

					SmtpClient client = new SmtpClient(Properties.Settings.Default.mailServer, 25);
					client.UseDefaultCredentials = false;
					client.Credentials = new System.Net.NetworkCredential(
						Properties.Settings.Default.mailUserName,
						Properties.Settings.Default.mailUserPassword,
						Properties.Settings.Default.mailUserDomain);

					client.Send(message);
					return "";
				}
			} catch (Exception e) {
				return e.Message + " " + e.StackTrace;
			}
		}
	}
}
