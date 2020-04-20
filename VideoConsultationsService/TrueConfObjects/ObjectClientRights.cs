using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoConsultationsService.TrueConfObjects {
	public class ObjectClientRights {
		//v.3.1
		//Field					Type		Description
		//chat_send				Boolean		A flag indicating that the client has permission to send messages to conference chat.
		//chat_rcv				Boolean		A flag indicating that the client has permission to receive messages from conference chat.
		//slide_show_send		Boolean		A flag indicating that the client has permission to display slide show to other conference participants.
		//slide_show_rcv		Boolean		A flag indicating that the client has permission to view slide show from other conference participants.
		//white_board_send		Boolean		A flag indicating that the client has permission to edit white board of conference.
		//white_board_rcv		Boolean		A flag indicating that the client has permission to view white board of conference.
		//file_transfer_send	Boolean		A flag indicating that the client has permission to send files to other conference participants.
		//file_transfer_rcv		Boolean		A flag indicating that the client has permission to receive from other conference participants.
		//desktop_sharing		Boolean		A flag indicating that the client has permission to display desktop to other conference participants.
		//recording				Boolean		A flag indicating that the client has permission to view desktop from other conference participants.
		//audio_send			Boolean		A flag indicating that the client has permission to send audio to other conference participants.
		//audio_rcv				Boolean		A flag indicating that the client has permission to receive audio to other conference participants.
		//video_send			Boolean		A flag indicating that the client has permission to send video to other conference participants.
		//video_rcv				Boolean		A flag indicating that the client has permission to receive video to other conference participants.

		[JsonProperty("chat_send")]
		public bool ChatSend { get; set; }


		[JsonProperty("chat_rcv")]
		public bool ChatRcv { get; set; }


		[JsonProperty("slide_show_send")]
		public bool SlideShowSend { get; set; }


		[JsonProperty("slide_show_rcv")]
		public bool SlideShowRcv { get; set; }


		[JsonProperty("white_board_send")]
		public bool WhiteBoardSend { get; set; }


		[JsonProperty("white_board_rcv")]
		public bool WhiteBoardRcv { get; set; }


		[JsonProperty("file_transfer_send")]
		public bool FileTransferSend { get; set; }


		[JsonProperty("file_transfer_rcv")]
		public bool FileTransferRcv { get; set; }


		[JsonProperty("desktop_sharing")]
		public bool DesktopSharing { get; set; }


		[JsonProperty("recording")]
		public bool Recording { get; set; }


		[JsonProperty("audio_send")]
		public bool AudioSendA { get; set; }


		[JsonProperty("audio_rcv")]
		public bool AudioRcv { get; set; }


		[JsonProperty("video_send")]
		public bool VideoSend { get; set; }


		[JsonProperty("video_rcv")]
		public bool VideoRcv { get; set; }
	}
}
