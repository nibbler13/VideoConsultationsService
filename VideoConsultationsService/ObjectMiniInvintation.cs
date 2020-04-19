using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoConsultationsService {
	public class ObjectMiniInvintation {
		//v.3.1
		//Conference invitation object for using in Conference based object.
		//The object can contain information from ObjectUser.

		//Field				Type			Description
		//id				String			Identifier of user.
		//display_name		String			Display user name.


		[JsonProperty("id")]
		public string Id { get; set; }


		[JsonProperty("display_name")]
		public string DisplayName { get; set; }
	}
}
