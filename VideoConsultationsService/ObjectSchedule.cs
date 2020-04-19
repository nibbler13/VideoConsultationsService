using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoConsultationsService {
	public class ObjectSchedule {
		//v.3.1
		//Field			Type		Description
		//type			Integer		Conference schedule type: -1 - without schedule 0 - weekly 1 - one-time
		//start_time	Integer		The next conference start time in Unix Timestamp for weekly schedule.
		//duration		Integer		Conference duration.
		//days			String[]	Weekly conference launch days. Format["monday", "tuesday", "wednesday", "thursday", "friday", "saturday", "sunday"]
		//time			String		Weekly conference start time in hh:mm (24:00) format according to the server time zone.
		//time_offset	Integer		Server time zone shown in minutes.


		[JsonProperty("type")]
		public int Type { get; set; }


		[JsonProperty("start_time")]
		public int StartTime { get; set; }


		[JsonProperty("duration")]
		public int Duration { get; set; }


		[JsonProperty("days")]
		public string[] Days { get; set; }


		[JsonProperty("time")]
		public string Time { get; set; }


		[JsonProperty("time_offset")]
		public int TimeOffset { get; set; }
	}
}
