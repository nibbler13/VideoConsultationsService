using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoConsultationsService {
	class ItemDeliveryState {
		public bool IsSuccessStatusCode { get; set; }
		public DateTime? DateTimeDelivery { get; private set; }
		public string Content { get; set; }

		private string deliveryState;
		public string DeliveryState {
			get {
				switch (deliveryState) {
					case "-1":
						return "В очереди передачи";
					case "0":
						return "Отвергнуто SMS-центром (ошибка доставки)";
					case "1":
						return "Не доставлено";
					case "2":
						return "Отправлено, статус доставки неизвестен";
					case "3":
						return "Доставлено";
					default:
						return "Ошибка обработки: " + Content;
				}
			}
		}

		public ItemDeliveryState() {
			IsSuccessStatusCode = false;
			DateTimeDelivery = null;
			Content = string.Empty;
			deliveryState = string.Empty;
		}

		public void ParseContent() {
			deliveryState = Content;

			if (!Content.Contains(" "))
				return;

			try {
				string[] splitted = Content.Split(' ');
				deliveryState = splitted[0];
				if (DateTime.TryParseExact(splitted[1], "yyyyMMddHHmm", null,
					System.Globalization.DateTimeStyles.None, out DateTime dateTime))
					DateTimeDelivery = dateTime;
			} catch (Exception) { }
		}
	}
}
