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
		private string rootUrl = "https://portal2.bzklinika.ru";
		private string secretKey = @"46fVt9rjbee:yXMJ_hh:3PmkaYL3noXX";
		private string apiGetAllWebinars = "/api/v2/webinar?access_token={secret_key}";
		private string apiDeleteWebianr = "/api/v2/webinar/{webinar_id}?access_token={secret_key}";
		private string apiPostStopWebinar = "/api/v2/webinar/{webinar_id}/stop?access_token={secret_key}";

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

			Dictionary<string, Webinar> webinars;
			string jsonString = await response.Content.ReadAsStringAsync();
			if (!jsonString.Contains("id")) {
				webinars = new Dictionary<string, Webinar>();
			} else {
				webinars = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, Webinar>>>(jsonString).Values.First();
			}
			return webinars;
		}
	}
}
