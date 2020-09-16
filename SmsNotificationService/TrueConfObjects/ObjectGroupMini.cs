using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmsNotificationService.TrueConfObjects {
	public class ObjectGroupMini {
		/*
		v.3.1
		Structure		
		Field			Type	Description
		id				String	Unique group ID.
		display_name	String	Unique group name.
		 */

		[JsonProperty("id")]
		public string Id { get; set; }


		[JsonProperty("display_name")]
		public string DisplayName { get; set; }
	}
}
