using System.Net.Mail;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace VideoConsultationsService {
	class MailSystem {
		public enum ErrorType { FireBird, TrueConf, CheckStateError }

		public static string SendErrorMessageToStp (ErrorType errorType, string errorMessage = "") {
			if (Debugger.IsAttached)
				return string.Empty;

			try {
				MailAddress to = new MailAddress(Properties.Settings.Default.MailStpAddress);
				MailAddress from = new MailAddress(
					Properties.Settings.Default.MailUserName + "@" + 
					Properties.Settings.Default.MailUserDomain, "TrueConfApiTest");

				string subject = "Ошибки в работе VideoConsultationsSevice";
				string body = "На группу бизнес-анализа" + Environment.NewLine;


				switch (errorType) {
					case ErrorType.FireBird:
						body += "Сервису VideoConsultationsSevice не удалось корректно загрузить" +
							" данные с сервера FireBird " + Properties.Settings.Default.FbMisAddress + ":" +
							Properties.Settings.Default.FbMisName + " в течение длительного периода";
						break;
					case ErrorType.TrueConf:
						body = "Сервису VideoConsultationsSevice не удалось корректно загрузить" +
							" данные с сервера TrueConf в течение длительного периода";
						break;
					case ErrorType.CheckStateError:
						body += "В ходе проверки состояния сервера TrueConf возникла ошибка: " +
							Environment.NewLine + errorMessage;
						break;
					default:
						break;
				}

				body += Environment.NewLine + Environment.NewLine + "Это автоматически сгенерированное сообщение" +
					Environment.NewLine + "Просьба не отвечать на него" + Environment.NewLine +
					 "Имя системы: " + Environment.MachineName;

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
