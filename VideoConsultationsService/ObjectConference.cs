using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoConsultationsService {
	public class Conferences {
		[JsonProperty("cnt")]
		public int Count { get; set; }


		[JsonProperty("conferences")]
		public List<ObjectConference> conferenceList { get; set; }
	}


	public class ObjectConference {
		//v.3.1
		//Conference Structure
		//Field					Type			Description
		//id					string			Unique conference identifier. To create conferences with unique ID a webinar license is required. Size range: 1..128
		//topic					string			Conference subject. Size range: 1..240
		//description			string			Conference description.
		//owner					string			User identifier (user_id) of the conference owner.
		//type					integer			Conference type: 0 - symmetric 1 - asymmetric 3 - role-based
		//invitations			object[]		Invitations list. They are used to automatically invite to the conferences. List of ObjectInvitationMini.
		//max_podiums			integer			The maximum number of conference speakers. The parameter can be changed for role-based conference. In symmetric conferences the number of speakers equals the number of participants, while in assymetrical conference the parameter is 1. Default value: $max_allowed
		//max_participants		integer			Maximum number of simultaneous conference participants. Maximum number depends on the server license and conference type: symmetric conference - up to 49 simultaneous participants. asymmetric conference - up to 49 simultaneous participants. role-based conference - the number of simultaneous participants depends on max_podiums parameter according to the following scheme: [6x120, 5x130, 4x160, 3x190, 2x240, 1x250]. Default value: $max_allowed
		//schedule				object			Schedule object.
		//allow_guests			boolean			Permission to invite guests to the conference. For editing you need a webinar license. Default value: false
		//rights				object[]		ClientRights objects.
		//auto_invite			integer			his parameter is responsible for sending automatic invitations at the conference start: 0 - disabled 1 - when any participant joins the conference 2 - when any invited participant joins the conference Default value: 0
		//url					string			Conference page.
		//webclient_url			string			Conference widget.
		//state					string			Conference state. Allowed values: "running", "stopped"
		//tags					array			Conference tags for fast search.
		//recording				integer			Conference recording state. Allowed values: 0, 1
		

		[JsonProperty("id")]
		public string Id { get; set; }


		[JsonProperty("topic")]
		public string Topic { get; set; }


		[JsonProperty("description")]
		public string Description { get; set; }


		[JsonProperty("owner")]
		public string Owner { get; set; }


		[JsonProperty("type")]
		public int Type { get; set; }


		[JsonProperty("invintations")]
		public List<ObjectMiniInvintation> Invintations { get; set; }


		[JsonProperty("max_podiums")]
		public int MaxPodiums { get; set; }


		[JsonProperty("max_participants")]
		public int MaxParticipants { get; set; }


		[JsonProperty("schedule")]
		public ObjectSchedule Schedule { get; set; }


		[JsonProperty("allow_guests")]
		public bool AllowGuests { get; set; }


		[JsonProperty("rights")]
		public Dictionary<string, ObjectClientRights> Rights { get; set; }


		[JsonProperty("auto_invite")]
		public int AutoInvite { get; set; }
		

		[JsonProperty("url")]
		public string Url { get; set; }


		[JsonProperty("webclient_url")]
		public string WebclientUrl { get; set; }


		[JsonProperty("state")]
		public string State { get; set; }
		//public string State {
		//	get {
		//		if (state.Equals("running"))
		//			return "Активная";
		//		if (state.Equals("stopped"))
		//			return "Неактивная";
		//		return this.state;
		//	}
		//	set {
		//		this.state = value;
		//	}
		//}


		[JsonProperty("tags")]
		public string[] Tags { get; set; }


		[JsonProperty("recording")]
		public int Recording { get; set; }




		public override string ToString() {
			string result = base.ToString() + Environment.NewLine;

			if (Id != null) result += "\tID: \t\t\t" + Id + Environment.NewLine;
			if (Url != null) result += "\tURL: \t\t\t" + Url + Environment.NewLine;
			result += "\tTYPE: \t\t\t" + (Type == 0 ? "symmetric" : Type == 1 ? "assymetric" : Type == 3 ? "role-based" : "unknown") + Environment.NewLine;
			if (Topic != null) result += "\tTOPIC: \t\t\t" + Topic + Environment.NewLine;
			if (Owner != null) result += "\tOWNER: \t\t" + Owner + Environment.NewLine;
			//result += "\tSIZE_PARTICIPANT: \t" + sizeParticipants + Environment.NewLine;
			//if (invitationType != null)
			//	result += "\tINVITATION_TYPE: \t" +
			//	(invitationType.Equals("-1") ? "without schedule" :
			//	invitationType.Equals("0") ? "every week" :
			//	invitationType.Equals("1") ? "nonrecurrent" : "unknown") +
			//	Environment.NewLine;
			//result += "\tINVITATION_TIMESTAMP: \t" + invitationTimestamp + Environment.NewLine;
			//if (invitationTime != null)
			//	result += "\tINVITATION_TIME: \t" + invitationTime + Environment.NewLine;
			//if (invitationDays != null)
			//	result += "\tINVITATION_DAYS: \t" + string.Join(";", invitationDays) + Environment.NewLine;
			result += "\tPODIUMS: \t\t" + MaxPodiums + Environment.NewLine;
			result += "\tMAX_PARTICIPANTS: \t" + MaxParticipants + Environment.NewLine;
			if (State != null) result += "\tSTATE: \t\t\t" + State;

			return result;
		}
	}
}
