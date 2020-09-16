using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vertica.Data.VerticaClient;

namespace SmsNotificationService {
	class VerticaClient {
		private VerticaConnection connection;

		public VerticaClient(string host, string database, string user, string password) {
			Logging.ToLog("Создание подключения к VerticaDB: " + host + ":" + database);

			VerticaConnectionStringBuilder builder = new VerticaConnectionStringBuilder {
				Host = host,
				Database = database,
				User = user,
				Password = password
			};

			connection = new VerticaConnection(builder.ToString());
			IsConnectionOpened();
		}

		private bool IsConnectionOpened() {
			if (connection.State != ConnectionState.Open) {
				try {
					connection.Open();
				} catch (Exception e) {
					string subject = "Ошибка подключения к БД";
					string body = e.Message + Environment.NewLine + e.StackTrace;
					MailSystem.SendMail(subject, body, Properties.Settings.Default.MailCopyAddresss);
					Logging.ToLog(subject + " " + body);
				}
			}

			return connection.State == ConnectionState.Open;
		}

		public DataTable GetDataTable(string query, ref uint errorCounter, ref bool sendedToStp, Dictionary<string, object> parameters = null) {
			DataTable dataTable = new DataTable();

			if (!IsConnectionOpened())
				return dataTable;

			try {
				using (VerticaCommand command = new VerticaCommand(query, connection)) {
					if (parameters != null && parameters.Count > 0)
						foreach (KeyValuePair<string, object> parameter in parameters)
							if (query.Contains(parameter.Key))
								command.Parameters.Add(new VerticaParameter(parameter.Key, parameter.Value));

					using (VerticaDataAdapter fbDataAdapter = new VerticaDataAdapter(command))
						fbDataAdapter.Fill(dataTable);

					errorCounter = 0;
					sendedToStp = false;
				}
			} catch (Exception e) {
				string subject = "Ошибка выполнения запроса к БД";
				string body = e.Message + Environment.NewLine + e.StackTrace;
				//MailSystem.SendMail(subject, body, Properties.Settings.Default.MailCopyAddresss);
				Logging.ToLog(subject + " " + body);
				connection.Close();
				errorCounter++;
			}

			return dataTable;
		}

        public bool ExecuteUpdateQuery(string query, Dictionary<string, object> parameters) {
			bool updated = false;

			if (!IsConnectionOpened())
				return updated;

			try {
				VerticaCommand update = new VerticaCommand(query, connection);

				if (parameters.Count > 0) {
					foreach (KeyValuePair<string, object> parameter in parameters)
						update.Parameters.Add(new VerticaParameter(parameter.Key, parameter.Value));
				}

				updated = update.ExecuteNonQuery() > 0 ? true : false;
			} catch (Exception e) {
				string subject = "Ошибка выполнения запроса к БД";
				string body = e.Message + Environment.NewLine + e.StackTrace;
				MailSystem.SendMail(subject, body, Properties.Settings.Default.MailCopyAddresss);
				Logging.ToLog(subject + " " + body);
				connection.Close();
			}

			return updated;
		}

        public void Close() {
			connection.Close();
        }

        public string GetName() {
			return connection.Database;
        }
    }
}
