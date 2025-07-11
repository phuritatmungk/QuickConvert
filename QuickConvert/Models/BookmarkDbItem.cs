using SQLite;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace QuickConvert.Models
{
    public class BookmarkDbItem : DatabaseItem
    {
        private string _currencyCode = string.Empty;
        public string CurrencyCode
        {
            get => _currencyCode;
            set
            {
                if (_currencyCode != value)
                {
                    _currencyCode = value;
                    OnPropertyChanged();
                }
            }
        }
        
        private DateTime _createdAt = DateTime.Now;
        public DateTime CreatedAt 
        {
            get => _createdAt;
            set
            {
                if (_createdAt != value)
                {
                    _createdAt = value;
                    OnPropertyChanged();
                }
            }
        }
    }
} 
