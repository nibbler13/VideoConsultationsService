using System.Net.Mail;
using System;
using System.Collections.Generic;

namespace VideoConsultationsService {
	class MailSystem {
		public enum ErrorType { FireBird, TrueConf }

		public static string SendErrorMessageToStp (ErrorType errorType) {
			try {
				MailAddress to = new MailAddress(Properties.Settings.Default.MailStpAddress);
				MailAddress from = new MailAddress(
					Properties.Settings.Default.MailUserName + "@" + 
					Properties.Settings.Default.MailUserDomain, "TrueConfApiTest");

				string subject = "Ошибки в работе VideoConsultationsSevice";
				string body = "На группу поддержки бизнес-приложений" + Environment.NewLine +
					"Сервису VideoConsultationsSevice не удалось корректно загрузить" +
					" данные с сервера @ в течение длительного периода" + Environment.NewLine +
					Environment.NewLine + "Это автоматически сгенерированное сообщение" +
					Environment.NewLine + "Просьба не отвечать на него" + Environment.NewLine +
					 "Имя системы: " + Environment.MachineName;

				if (errorType == ErrorType.TrueConf)
					body = body.Replace("@", "TrueConf");

				if (errorType == ErrorType.FireBird)
					body = body.Replace("@", "FireBird " + Properties.Settings.Default.FbMisAddress + ":" +
						Properties.Settings.Default.FbMisName);

				LoggingSystem.LogMessageToFile("Отправка сообщения, тема: " + subject + ", текст: " + body);
				
				using (MailMessage message = new MailMessage()) {
					message.To.Add(to);
					message.From = from;

					message.Subject = subject;
					message.Body = body;
					if (!string.IsNullOrEmpty(Properties.Settings.Default.MailCopyAddresss))
						message.CC.Add(Properties.Settings.Default.MailCopyAddresss);

					SmtpClient client = new SmtpClient(Properties.Settings.Default.MailServer, 25);
					client.UseDefaultCredentials = false;
					client.Credentials = new System.Net.NetworkCredential(
						Properties.Settings.Default.MailUserName,
						Properties.Settings.Default.MailUserPassword,
						Properties.Settings.Default.MailUserDomain);

					client.Send(message);
					return string.Empty;
				}
			} catch (Exception e) {
				return e.Message + " " + e.StackTrace;
			}
		}
	}
}
