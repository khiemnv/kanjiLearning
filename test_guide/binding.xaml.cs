using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Windows.Input;
using test_guide.Model;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace test_guide
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class binding : Page
    {
        public MainPageViewModel MainModel { get; }

        public binding()
        {
            this.InitializeComponent();

            MainModel = new MainPageViewModel();

            ContactsCVS.Source = Contact.GetContactsGrouped(250);
            Loaded += pageLoaded;
        }

        private void pageLoaded(object sender, RoutedEventArgs e)
        {
        }

        private void showHideGroup(object sender, TappedRoutedEventArgs e)
        {
            TextBlock txt = (TextBlock)sender;
            ObservableCollection<GroupInfoList> groups = (ObservableCollection<GroupInfoList>)ContactsCVS.Source;
            var obj = groups.First(elem => {
                return txt.Text == elem.Key.ToString();
            });

            Debug.WriteLine("{0} {1}", obj.Key, obj.isShow);
            obj.isShow = (obj.isShow == Visibility.Visible)? Visibility.Collapsed : Visibility.Visible;
            foreach (Contact item in obj)
            {
                item.isShow = obj.isShow;
                Debug.WriteLine("{0} {1}", item.Name, item.isShow);
                //var lvi = contactLV.Items.First(( input)=>
                //{
                //    Contact c = (Contact)input;
                //    return (c == item);
                //});
            }
        }
    }

    public class Song
    {
        public string Title { get; set; }
        public string Genre { get; set; }
        public string Image { get; set; }
    }
    public class SongGroup
    {
        public string Title { get; set; }
        public string Image { get; set; }

        public ObservableCollection<Song> Items { get; set; }
    }

    public class MainPageViewModel
    {
        public ObservableCollection<SongGroup> GroupedMusicList { get; set; }

        public MainPageViewModel()
        {
            GroupedMusicList = GetMusicList();
        }

        private ObservableCollection<SongGroup> GetMusicList()
        {
            var ret = new ObservableCollection<SongGroup>
            {
                new SongGroup
                {
                    Title = "group1",
                    Image = "default",
                    Items = new ObservableCollection<Song>
                    {
                        new Song {Title = "song1", Genre = "genre1", Image="default" },
                    }
                }
            };
            return ret;
        }

        // Other stuff in view model
    }

    public class RelayCommand<TCommandParameter> : ICommand
    {
        /// <summary>
        /// Command to execute
        /// </summary>
        /// <typeparam name="T">Action parameter</typeparam>
        private readonly Action<TCommandParameter> execute;

        /// <summary>
        /// Return true if execute action available, false otherwise
        /// </summary>
        /// <typeparam name="T">Action parameter</typeparam>
        /// <typeparam name="bool">return parameter</typeparam>
        private readonly Func<TCommandParameter, bool> canExecute;

        /// <summary>
        /// Initializes a new instance of the <see cref="RelayCommand{TCommandParameter}" /> class.
        /// </summary>
        /// <param name="execute">The execution logic.</param>
        /// <param name="canExecute">The execution status logic.</param>
        public RelayCommand(Action<TCommandParameter> execute, Func<TCommandParameter, bool> canExecute = null)
        {
            if (execute == null)
            {
                throw new ArgumentNullException("execute");
            }

            this.execute = execute;
            this.canExecute = canExecute;
        }

        /// <summary>
        /// Raised when RaiseCanExecuteChanged is called.
        /// </summary>
        public event EventHandler CanExecuteChanged;

        /// <summary>
        /// Determines whether this <see cref="RelayCommand"/> can execute in its current state.
        /// </summary>
        /// <param name="parameter">Data used by the command.</param>
        public void Execute(object parameter = null)
        {
            execute((TCommandParameter)parameter);
        }

        /// <summary>
        /// Called by WPF. Executes the <see cref="RelayCommand"/> on the current command target.
        /// </summary>
        /// <param name="parameter">Data used by the command</param>
        /// <returns>True if control should be enabled</returns>
        public bool CanExecute(object parameter = null)
        {
            return canExecute == null ? true : canExecute((TCommandParameter)parameter);
        }

        /// <summary>
        /// Method used to raise the <see cref="CanExecuteChanged"/> event
        /// to indicate that the return value of the <see cref="CanExecute"/>
        /// method has changed.
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            var handler = CanExecuteChanged;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }
    }

    /// <summary>
    /// Implementation of <see cref="INotifyPropertyChanged"/> to simplify models.
    /// </summary>
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        protected ViewModelBase()
        {
            propertyStore = new Dictionary<string, object>();
        }

        /// <summary>
        /// Backing store for properties
        /// </summary>
        private Dictionary<string, object> propertyStore;

        /// <summary>
        /// Multicast event for property change notifications.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// For use with calculated properties. 
        /// Notify listeners that the specified property has changed. Only use in properties.
        /// </summary>
        /// <param name="calculatedPropertyName">Calculated property name</param>
        protected void NotifyPropertyUpdated(string calculatedPropertyName)
        {
            OnStateChanged(calculatedPropertyName);
        }

        /// <summary>
        /// Set property state.
        /// </summary>
        /// <typeparam name="T">Type of the property</typeparam>
        /// <param name="value">Desired value for the property</param>
        /// <param name="propertyName">Do not set</param>
        /// <returns>True if the value was changed, false otherwise</returns>
        protected bool SetState<T>(
            T value,
            SaveType saveType = SaveType.Application,
            [CallerMemberName] string propertyName = null)
        {
            switch (saveType)
            {
                case SaveType.Application:
                    if (propertyStore.ContainsKey(propertyName))
                    {
                        if (Equals(propertyStore[propertyName], value))
                        {
                            return false;
                        }
                        else
                        {
                            propertyStore[propertyName] = value;
                            OnStateChanged(propertyName);
                            return true;
                        }
                    }
                    else
                    {
                        propertyStore[propertyName] = value;
                        return true;
                    }

                case SaveType.Roaming:
                    if (ApplicationData.Current.RoamingSettings.Values[propertyName] == null)
                    {
                        ApplicationData.Current.RoamingSettings.Values[propertyName] = value;
                        ApplicationData.Current.SignalDataChanged();
                        OnStateChanged(propertyName);
                        return true;
                    }
                    else if (Equals(ApplicationData.Current.RoamingSettings.Values[propertyName], value))
                    {
                        return false;
                    }
                    else
                    {
                        ApplicationData.Current.RoamingSettings.Values[propertyName] = value;
                        ApplicationData.Current.SignalDataChanged();
                        OnStateChanged(propertyName);
                        return true;
                    }

                case SaveType.Local:
                    if (ApplicationData.Current.LocalSettings.Values[propertyName] == null)
                    {
                        ApplicationData.Current.LocalSettings.Values[propertyName] = value;
                        ApplicationData.Current.SignalDataChanged();
                        OnStateChanged(propertyName);
                        return true;
                    }
                    else if (Equals(ApplicationData.Current.LocalSettings.Values[propertyName], value))
                    {
                        return false;
                    }
                    else
                    {
                        ApplicationData.Current.LocalSettings.Values[propertyName] = value;
                        ApplicationData.Current.SignalDataChanged();
                        OnStateChanged(propertyName);
                        return true;
                    }

                default:
                    throw new NotImplementedException();
            }
        }



        /// <summary>
        /// Retrieve stored data
        /// </summary>
        /// <typeparam name="T">Data type to store</typeparam>
        /// <param name="initialValue">Initial value to set</param>
        /// <param name="saveType">Save type</param>
        /// <param name="propertyName">Leave blank.</param>
        /// <returns>Get stored value</returns>
        protected T GetState<T>(
            T initialValue = default(T),
            SaveType saveType = SaveType.Application,
            [CallerMemberName] string propertyName = null)
        {
            switch (saveType)
            {
                case SaveType.Application:
                    if (!propertyStore.ContainsKey(propertyName))
                    {
                        propertyStore[propertyName] = initialValue;
                    }

                    return (T)propertyStore[propertyName];

                case SaveType.Roaming:
                    if (ApplicationData.Current.RoamingSettings.Values[propertyName] == null)
                    {
                        ApplicationData.Current.RoamingSettings.Values[propertyName] = initialValue;
                        ApplicationData.Current.SignalDataChanged();
                    }

                    return (T)ApplicationData.Current.RoamingSettings.Values[propertyName];

                case SaveType.Local:
                    if (ApplicationData.Current.LocalSettings.Values[propertyName] == null)
                    {
                        ApplicationData.Current.LocalSettings.Values[propertyName] = initialValue;
                        ApplicationData.Current.SignalDataChanged();
                    }

                    return (T)ApplicationData.Current.LocalSettings.Values[propertyName];

                default:
                    throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Get relay command for the specified action. Only use in get property
        /// </summary>
        /// <typeparam name="TCommandParameter">WPF <code>CommandParameter</code></typeparam>
        /// <param name="execute">The action</param>
        /// <param name="canExecute">Is enabled Function</param>
        /// <param name="propertyName">Do not set</param>
        /// <returns>Relay command</returns>
        protected RelayCommand<TCommandParameter> Command<TCommandParameter>(
            Action<TCommandParameter> execute,
            Func<TCommandParameter, bool> canExecute = null,
            [CallerMemberName] string propertyName = null)
        {
            if (!propertyStore.ContainsKey(propertyName))
            {
                propertyStore[propertyName] = new RelayCommand<TCommandParameter>(execute, canExecute);
            }

            return (RelayCommand<TCommandParameter>)propertyStore[propertyName];
        }

        /// <summary>
        /// Notifies listeners that a property value has changed
        /// </summary>
        /// <param name="propertyName">Property whose name has been changed</param>
        private void OnStateChanged(string propertyName)
        {
            var eventHandler = PropertyChanged;
            if (eventHandler != null)
            {
                eventHandler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }

    /// <summary>
    /// View model Storage type.
    /// </summary>
    public enum SaveType
    {
        /// <summary>
        /// Application storage storage for settings
        /// </summary>
        Application,

        /// <summary>
        /// Local persistant storage for settings
        /// </summary>
        Local,

        /// <summary>
        /// Roaming persistant storage for settings
        /// </summary>
        Roaming
    }

    public class SongModel : ViewModelBase
    {
        public string Album
        {
            get
            {
                return GetState(string.Empty);
            }
            set
            {
                SetState(value);
            }
        }

        public string Artist
        {
            get
            {
                return GetState(string.Empty);
            }
            set
            {
                SetState(value);
            }
        }

        public string Title
        {
            get
            {
                return GetState(string.Empty);
            }
            set
            {
                SetState(value);
            }
        }
    }

    public class ModelGroupBase<T> : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public ModelGroupBase()
        {
            Items = new ObservableCollection<T>();
            Title = string.Empty;
            HasGroupDetails = false;

            Items.CollectionChanged += (sender, e) =>
            {
                HasGroupDetails = Items.Count > 0;
                OnStateChanged(nameof(HasGroupDetails));
            };
        }

        public ObservableCollection<T> Items { get; set; }

        public string Title { get; set; }

        public bool HasGroupDetails { get; set; }

        private void OnStateChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public override string ToString()
        {
            return Title;
        }
    }

    public class SongGroupModel : ModelGroupBase<SongModel>
    {
    }

}
