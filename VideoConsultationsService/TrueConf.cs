using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace VideoConsultationsService {
	class TrueConf {
		private static readonly HttpClient httpClient = new HttpClient();
		private readonly string rootUrl = "https://portal2.bzklinika.ru";
		private readonly string secretKey = @"46fVt9rjbee:yXMJ_hh:3PmkaYL3noXX";
		private string apiGetAllWebinars = "/api/v2/webinar?access_token={secret_key}";
		private string apiDeleteWebianr = "/api/v2/webinar/{webinar_id}?access_token={secret_key}";
		private string apiPostStopWebinar = "/api/v2/webinar/{webinar_id}/stop?access_token={secret_key}";
		private string apiPostCreateWebinar = "/api/v2/webinar?access_token={secret_key}";

		public TrueConf() {

		}

		public async Task<string> StopWebinar(string webinarId) {
			string url = rootUrl + apiPostStopWebinar.
				Replace("{secret_key}", secretKey).Replace("{webinar_id}", webinarId);
			HttpResponseMessage response = await httpClient.PostAsync(url,
				new FormUrlEncodedContent(new Dictionary<string, string>()));
			response.EnsureSuccessStatusCode();
			return await response.Content.ReadAsStringAsync();
		}

		public async Task<string> DeleteWebinar(string webinarId) {
			string url = rootUrl + apiDeleteWebianr.
				Replace("{secret_key}", secretKey).Replace("{webinar_id}", webinarId);
			HttpResponseMessage response = await httpClient.DeleteAsync(url);
			response.EnsureSuccessStatusCode();
			return await response.Content.ReadAsStringAsync();
		}

		public async Task<Dictionary<string, Webinar>> GetAllWebinars() {
			string url = rootUrl + apiGetAllWebinars.Replace("{secret_key}", secretKey);
			HttpResponseMessage response = await httpClient.GetAsync(url);
			response.EnsureSuccessStatusCode();

			Dictionary<string, Webinar> webinars = new Dictionary<string, Webinar>(); 
			string jsonString = await response.Content.ReadAsStringAsync();

			try {
				webinars = JsonConvert.DeserializeObject<Webinars>(jsonString).Dict;
			} catch (Exception e) {
				Console.WriteLine(e.Message + Environment.NewLine + e.StackTrace);
			}

			return webinars;
		}

		public async Task<Webinar> CreateNewWebinar(string topic, string owner, string timestamp) {
			Dictionary<string, string> values = new Dictionary<string, string>() {
				{ "topic", topic },
				{ "owner", owner },
				{ "allow_guests_audio_video", "true" },
				{ "allow_guests_message", "true" },
				{ "invitation_type", "1" },
				{ "invitation_timestamp",  timestamp },
				{ "max_participants", "5" }
			};

			FormUrlEncodedContent content = new FormUrlEncodedContent(values);
			ServicePointManager.Expect100Continue = false;
			string url = rootUrl + apiPostCreateWebinar.Replace("{secret_key}", secretKey);
			HttpResponseMessage response = await httpClient.PostAsync(url, content);
			response.EnsureSuccessStatusCode();
			string jsonString = await response.Content.ReadAsStringAsync();
			return JsonConvert.DeserializeObject<Webinar>(jsonString);
		}
	}
}
