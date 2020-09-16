using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace SmsNotificationService {
	class SvyaznoyZagruzka {
		public async static Task<ItemSendResult> SendMessage(string phoneNumber, string message, DateTime? dateTime = null) {
			Logging.ToLog("Отправка СМС для абонента " + phoneNumber + " со следующим текстом: " + message);
			ItemSendResult itemSendResult = new ItemSendResult();

			try {
				HttpClient client = new HttpClient();

				if (phoneNumber.Length == 10)
					phoneNumber = "7" + phoneNumber;

				Uri uri = new Uri(
						"http://lk.zagruzka.com:9002/budzdorov?" +
						"msisdn=" + phoneNumber + "&" +
						"message=" + message);

				if (dateTime != null)
					uri = new Uri(uri.ToString() + "&send_time=" + ((DateTime)dateTime).ToString("yyyyMMddHHmm") + "00");

				client.BaseAddress = uri;
				HttpResponseMessage responseMessage = await client.GetAsync(string.Empty);
				string content = await responseMessage.Content.ReadAsStringAsync();

				itemSendResult.IsSuccessStatusCode = responseMessage.IsSuccessStatusCode;
				itemSendResult.Content = content;
				itemSendResult.DateTimeSelected = dateTime;

				if (!itemSendResult.IsSuccessStatusCode)
					return itemSendResult;
				
				itemSendResult.MessageId = content.Replace("\r\n", "");
			} catch (Exception e) {
				itemSendResult.Content = e.Message + Environment.NewLine + e.StackTrace;
			}

			return itemSendResult;
		}

		public static ItemDeliveryState GetDeliveryStateContent(string messageId) {
			ItemDeliveryState itemDeliveryState = new ItemDeliveryState();

			try {
				HttpClient client = new HttpClient();
				client.BaseAddress = new Uri("http://lk.zagruzka.com:9002/budzdorov/delivery_report?mt_num=" + messageId + "&show_date=Y");
				HttpResponseMessage responseMessage = client.GetAsync(string.Empty).Result;
				string content = responseMessage.Content.ReadAsStringAsync().Result;

				itemDeliveryState.IsSuccessStatusCode = responseMessage.IsSuccessStatusCode;
				itemDeliveryState.Content = content.Replace("\r\n", "");
				itemDeliveryState.ParseContent();
			} catch (Exception e) {
				itemDeliveryState.Content = e.Message + Environment.NewLine + e.StackTrace;
			}

			return itemDeliveryState;
		}
	}
}
