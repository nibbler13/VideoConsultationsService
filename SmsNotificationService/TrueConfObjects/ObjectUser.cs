using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmsNotificationService.TrueConfObjects {
	public class Users {
		[JsonProperty("next_page_id")]
		public int NextPageId { get; set; }


		[JsonProperty("users")]
		public List<ObjectUser> UserList { get; set; }
	}

	public class ObjectUser {
		/*
		v.3.1
		Structure
		Field			Type		Description
		id				String		User unique identifier.
		uid				String		User unique identifier for calls.
		avatar			String		User avatar address.If there is no avatar, null is returned.
		login_name		String		User username.
		password		String		User password.
		email			String		User unique email address.
		display_name	String		User display name.
		first_name		String		User first name.
		last_name		String		User last name.
		company			String		User company.
		groups			Object[]	List of ObjectGroupMini.
		mobile_phone	String		User mobile phone.
		work_phone		String		User work phone.
		home_phone		String		User home phone.
		status			Integer		NOT_ACTIVE: -2, INVALID: -1, OFFLINE: 0, ONLINE: 1, BUSY: 2, MULTIHOST: 5 (The user is in the conference and is the conference owner).
		is_active		Integer		Account status: 1 - enabled, 0 - disabled.
		*/

		[JsonProperty("id")]
		public string Id { get; set; }


		[JsonProperty("uid")]
		public string Uid { get; set; }


		[JsonProperty("avatar")]
		public string Avatar { get; set; }


		[JsonProperty("login_name")]
		public string LoginName { get; set; }


		[JsonProperty("password")]
		public string Password { get; set; }


		[JsonProperty("email")]
		public string Email { get; set; }


		[JsonProperty("display_name")]
		public string DisplayName { get; set; }


		[JsonProperty("first_name")]
		public string FirstName { get; set; }


		[JsonProperty("last_name")]
		public string Last_name { get; set; }


		[JsonProperty("company")]
		public string Company { get; set; }


		[JsonProperty("groups")]
		public ObjectGroupMini[] Groups { get; set; }


		[JsonProperty("mobile_phone")]
		public string MobilePhone { get; set; }


		[JsonProperty("work_phone")]
		public string WorkPhone { get; set; }


		[JsonProperty("home_phone")]
		public string HomePhone { get; set; }


		[JsonProperty("status")]
		public int Status { get; set; }


		[JsonProperty("is_active")]
		public int IsActive { get; set; }
	}
}
