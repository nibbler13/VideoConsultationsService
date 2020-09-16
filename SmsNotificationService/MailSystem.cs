using System.Net.Mail;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Net.Mime;
using System.IO;

namespace SmsNotificationService {
	class MailSystem {
		public enum ErrorType { FireBird, Vertica, TrueConf, CheckStateError, SingleCheck }

		public static string SendErrorMessageToStp (ErrorType errorType, string errorMessage = "") {
				string subject = "Ошибки в работе VideoConsultationsSevice";
				string body = "На группу бизнес-анализа" + Environment.NewLine;

			try {
				string address = Properties.Settings.Default.MailStpAddress;
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

					case ErrorType.SingleCheck:
						body = errorMessage;
						address = Properties.Settings.Default.MailToSingleCheck;
						subject = "Результаты проверки сервера TrueConf - "  + 
							(errorMessage.Contains("!") ? " Внимание! Обнаружены ошибки!" : "ошибок не обнаружено");
						break;

					case ErrorType.Vertica:
						body += "Сервису VideoConsultationsSevice не удалось корректно загрузить" +
							" данные с сервера Vertica " + Properties.Settings.Default.VerticaDbAddress + ":" +
							Properties.Settings.Default.VerticaDbName + " в течение длительного периода";
						break;

					default:
						break;
				}

				body += Environment.NewLine + Environment.NewLine + "Это автоматически сгенерированное сообщение" +
					Environment.NewLine + "Просьба не отвечать на него" + Environment.NewLine +
					 "Имя системы: " + Environment.MachineName;

				Logging.ToLog("Отправка сообщения, тема: " + subject + ", текст: " + body);

				SendMail(subject, body, address);

				return string.Empty;
			} catch (Exception e) {
				return e.Message + " " + e.StackTrace;
			}
		}


		public static void SendMail(string subject, string body, string receiver, string[] attachmentsPath = null, bool addSignature = true) {
			if (string.IsNullOrEmpty(receiver)) {
				Logging.ToLog("В настройках не задан получатель письма");
				return;
			}

			Logging.ToLog("Отправка письма с темой: " + subject);
			Logging.ToLog("Текст: " + body);
			Logging.ToLog("Получатели: " + receiver);

			try {
				string appName = Assembly.GetExecutingAssembly().GetName().Name;

				MailAddress from = new MailAddress(
					Properties.Settings.Default.MailUserName + "@" + 
					Properties.Settings.Default.MailUserDomain,
					appName);

				List<MailAddress> mailAddressesTo = new List<MailAddress>();

				if (receiver.Contains(";")) {
					string[] receivers = receiver.Split(';');
					foreach (string address in receivers)
						mailAddressesTo.Add(new MailAddress(address));
				} else
					mailAddressesTo.Add(new MailAddress(receiver));

				if (addSignature)
					body += Environment.NewLine + Environment.NewLine +
						"___________________________________________" + Environment.NewLine +
						"Это автоматически сгенерированное сообщение" + Environment.NewLine +
						"Просьба не отвечать на него" + Environment.NewLine +
 						"Имя системы: " + Environment.MachineName;

				MailMessage message = new MailMessage();

				foreach (MailAddress mailAddress in mailAddressesTo)
					message.To.Add(mailAddress);

				message.IsBodyHtml = body.Contains("<") && body.Contains(">");

				if (message.IsBodyHtml)
					body = body.Replace(Environment.NewLine, "<br>");

				if (attachmentsPath != null)
					foreach (string attachmentPath in attachmentsPath) {
						if (string.IsNullOrEmpty(attachmentPath) || !File.Exists(attachmentPath))
							continue;

						Attachment attachment = new Attachment(attachmentPath, MediaTypeNames.Application.Octet);

						if (message.IsBodyHtml && attachmentPath.EndsWith(".jpg")) {
							attachment.ContentDisposition.Inline = true;

							LinkedResource inline = new LinkedResource(attachmentPath, MediaTypeNames.Image.Jpeg);
							inline.ContentId = Guid.NewGuid().ToString();

							body = body.Replace("Фотография с камеры терминала:", "Фотография с камеры терминала:<br>" +
								string.Format(@"<img src=""cid:{0}"" />", inline.ContentId));

							AlternateView avHtml = AlternateView.CreateAlternateViewFromString(body, null, MediaTypeNames.Text.Html);
							avHtml.LinkedResources.Add(inline);

							message.AlternateViews.Add(avHtml);
						} else
							message.Attachments.Add(attachment);
					}

				message.From = from;
				message.Subject = subject;
				message.Body = body;

				if (!string.IsNullOrEmpty(Properties.Settings.Default.MailCopyAddresss))
					message.CC.Add(Properties.Settings.Default.MailCopyAddresss);

				SmtpClient client = new SmtpClient(Properties.Settings.Default.MailServer, 587);
				client.UseDefaultCredentials = false;
				client.DeliveryMethod = SmtpDeliveryMethod.Network;
				client.EnableSsl = false;
				client.Credentials = new System.Net.NetworkCredential(
					Properties.Settings.Default.MailUserName,
					Properties.Settings.Default.MailUserPassword);

				client.Send(message);
				client.Dispose();

				foreach (Attachment attach in message.Attachments)
					attach.Dispose();

				message.Dispose();
				Logging.ToLog("Письмо отправлено успешно");
			} catch (Exception e) {
				Logging.ToLog("Не удалось отправить письмо:  " + Environment.NewLine + e.Message + Environment.NewLine + e.StackTrace);

				if (e.InnerException != null)
					Logging.ToLog(e.InnerException.Message + Environment.NewLine + e.InnerException.StackTrace);
			}
		}
	}
}
