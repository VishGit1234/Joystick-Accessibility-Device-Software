using System.ComponentModel;

namespace Controller.Models
{
    /// <summary>
    /// Base class for model classes that implements INotifyPropertyChanged Interface
    /// </summary>
    abstract class ModelBaseClass : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
