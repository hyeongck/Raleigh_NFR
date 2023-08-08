using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace ClothoSharedItems.ViewModel
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public IEnumerable<SelectableItem<RunOption>> AvailableItems { get; private set; }

        public MainViewModel()
        {
            AvailableItems = typeof(RunOption).GetEnumValues().Cast<RunOption>().Select((e) => new SelectableItem<RunOption>(e)).ToList();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class SelectableItem<T> : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public SelectableItem(T val)
        {
            Value = val;
            _isSelected = false;
        }

        public T Value { get; private set; }

        private bool _isSelected;

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected == value) return;
                _isSelected = value;
                OnPropertyChanged();

                if (ClothoDataObject.Instance != null)
                {
                    if (_isSelected == false) ClothoDataObject.Instance.RunOptions &= ~(RunOption)Enum.Parse(Value.GetType(), Value.ToString());
                    else if (_isSelected == true) ClothoDataObject.Instance.RunOptions |= (RunOption)Enum.Parse(Value.GetType(), Value.ToString());
                }
            }
        }
    }
}