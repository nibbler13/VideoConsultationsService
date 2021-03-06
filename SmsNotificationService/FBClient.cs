﻿using System;
using System.Data;
using System.Collections.Generic;
using FirebirdSql.Data.FirebirdClient;

namespace SmsNotificationService {
    class FBClient {
        private FbConnection connection;
		private bool isZabbixCheck;

		public FBClient(string ipAddress, string baseName, bool isZabbixCheck = false) {
			this.isZabbixCheck = isZabbixCheck;

			Logging.ToLog("Создание подключения к базе FB: " + 
				ipAddress + ":" + baseName, !isZabbixCheck);

			FbConnectionStringBuilder cs = new FbConnectionStringBuilder();
            cs.DataSource = ipAddress;
            cs.Database = baseName;
            cs.UserID = Properties.Settings.Default.FbMisDbUser;
            cs.Password = Properties.Settings.Default.FbMisDbPassword;
            cs.Charset = "NONE";
            cs.Pooling = false;

            connection = new FbConnection(cs.ToString());
		}

		public DataTable GetDataTable(string query, Dictionary<string, string> parameters, 
			ref uint errorCounter, ref bool sendedToStp) {
			DataTable dataTable = new DataTable();

			try {
				connection.Open();
				using (FbCommand command = new FbCommand(query, connection)) {
					if (parameters.Count > 0)
						foreach (KeyValuePair<string, string> parameter in parameters)
							command.Parameters.AddWithValue(parameter.Key, parameter.Value);

					using (FbDataAdapter fbDataAdapter = new FbDataAdapter(command))
						fbDataAdapter.Fill(dataTable);

					errorCounter = 0;
					sendedToStp = false;
				}
			} catch (Exception e) {
				Logging.ToLog("Не удалось получить данные, запрос: " + query + 
					Environment.NewLine + e.Message + " @ " + e.StackTrace, !isZabbixCheck);
				errorCounter++;
			} finally {
				connection.Close();
			}

			return dataTable;
		}

		public bool ExecuteUpdateQuery(string query, Dictionary<string, string> parameters) {
			bool updated = false;
			try {
				connection.Open();
				FbCommand update = new FbCommand(query, connection);

				if (parameters.Count > 0) {
					foreach (KeyValuePair<string, string> parameter in parameters)
						update.Parameters.AddWithValue(parameter.Key, parameter.Value);
				}

				updated = update.ExecuteNonQuery() > 0 ? true : false;
			} catch (Exception e) {
				Logging.ToLog("Не удалось выполнить запрос: " + query + 
					Environment.NewLine + e.Message + " @ " + e.StackTrace, !isZabbixCheck);
			} finally {
				connection.Close();
			}

			return updated;
		}
    }
}
