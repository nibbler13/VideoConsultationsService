using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using VideoConsultationsService.TrueConfObjects;

namespace VideoConsultationsService {
	class TrueConf {
		private static readonly HttpClient httpClient = new HttpClient();
		private const string rootUrl = "https://portal2.bzklinika.ru";
		private const string secretKey = @"46fVt9rjbee:yXMJ_hh:3PmkaYL3noXX";
		private const string apiGetConferenceList = "/api/v3.1/conferences?access_token={secret_key}&page_id={page_id}";
		private const string apiGetUserList = "/api/v3.1/users?access_token={secret_key}&page_id={page_id}";
		private const string apiDisconnectUser = "/api/v3.1/users/{user_id}/disconnect?access_token={secret_key}";


		public TrueConf() {

		}


		public async Task<List<ObjectConference>> GetConferenceList(List<ObjectConference> conferenceList = null, int pageId = 1) {
			string url = rootUrl + apiGetConferenceList.Replace("{secret_key}", secretKey).Replace("{page_id}", pageId.ToString());
			HttpResponseMessage response = await httpClient.GetAsync(url);
			response.EnsureSuccessStatusCode();

			if (conferenceList == null)
				conferenceList = new List<ObjectConference>();

			string jsonString = await response.Content.ReadAsStringAsync();

			try {
				Conferences conferences = JsonConvert.DeserializeObject<Conferences>(jsonString);
				conferenceList.AddRange(conferences.conferenceList);

				if (pageId * 200 < conferences.Count)
					await GetConferenceList(conferenceList, pageId + 1);
			} catch (Exception e) {
				Console.WriteLine(e.Message + Environment.NewLine + e.StackTrace);
			}

			return conferenceList;
		}

		public async Task<List<ObjectUser>> GetUserList(List<ObjectUser> userList = null, int pageId = 0) {
			string url = rootUrl + apiGetUserList.Replace("{secret_key}", secretKey).Replace("{page_id}", pageId.ToString());
			HttpResponseMessage response = await httpClient.GetAsync(url);
			response.EnsureSuccessStatusCode();

			if (userList == null)
				userList = new List<ObjectUser>();

			string jsonString = await response.Content.ReadAsStringAsync();

			try {
				Users users = JsonConvert.DeserializeObject<Users>(jsonString);
				userList.AddRange(users.UserList);

				if (users.NextPageId != -1)
					await GetUserList(userList, users.NextPageId);
			} catch (Exception e) {
				Console.WriteLine(e.Message + Environment.NewLine + e.StackTrace);
			}

			return userList;
		}



		public async Task<bool> DisconnectUser(string id) {
			string url = rootUrl + apiDisconnectUser.Replace("{secret_key}", secretKey).Replace("{user_id}", id);
			StringContent stringContent = new StringContent(JsonConvert.SerializeObject(string.Empty), Encoding.UTF8, "application/json");
			HttpResponseMessage response = await httpClient.PostAsync(url, stringContent);
			response.EnsureSuccessStatusCode();

			string jsonString = await response.Content.ReadAsStringAsync();
			Logging.ToLog(jsonString);

			return false;
		}
	}
}
