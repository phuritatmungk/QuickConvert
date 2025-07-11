using SQLite;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace QuickConvert.Models
{
    public abstract class DatabaseItem : INotifyPropertyChanged
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class WalletDbItem : DatabaseItem
    {
        private string _currency = string.Empty;
        public string Currency
        {
            get => _currency;
            set
            {
                if (_currency != value)
                {
                    _currency = value;
                    OnPropertyChanged();
                }
            }
        }

        private decimal _balance;
        public decimal Balance
        {
            get => _balance;
            set
            {
                if (_balance != value)
                {
                    _balance = value;
                    OnPropertyChanged();
                }
            }
        }
    }
} 
