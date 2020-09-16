using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace SmsNotificationService {
	public class ItemPhoneNumber : INotifyPropertyChanged {
		public event PropertyChangedEventHandler PropertyChanged;
		private void NotifyPropertyChanged([CallerMemberName] String propertyName = "") {
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		private string _name = string.Empty;
		public string Name {
			get {
				return _name;
			}
			set {
				if (value != _name) {
					_name = value;
					NotifyPropertyChanged();
				}
			}
		}

		private string _phoneNumber = string.Empty;
		public string PhoneNumber {
			get {
				return _phoneNumber;
			}
			set {
				if (value != _phoneNumber) {
					_phoneNumber = value;
					NotifyPropertyChanged();
				}
			}
		}

		public string GetClearedNumber() {
			return new String(_phoneNumber.Where(Char.IsDigit).ToArray());
		}
	}
}
