using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoConsultationsService {
	public class ItemSendResult {
		public bool IsSuccessStatusCode { get; set; }
		public string MessageId { get; set; }
		public string Content { get; set; }
		public ItemPhoneNumber ItemPhoneNumber { get; set; }
		public DateTime? DateTimeSelected { get; set; }

		public ItemSendResult() {
			IsSuccessStatusCode = false;
			MessageId = string.Empty;
			Content = string.Empty;
			ItemPhoneNumber = new ItemPhoneNumber();
			DateTimeSelected = null;
		}
	}
}
