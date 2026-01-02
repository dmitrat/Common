using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using OutWit.Common.MVVM.WPF.Commands;

namespace OutWit.Common.MVVM.WPF.Sample
{
    /// <summary>
    /// Main ViewModel demonstrating DelegateCommand and BindingProxy usage.
    /// </summary>
    public class MainViewModel : INotifyPropertyChanged
    {
        #region Events

        public event PropertyChangedEventHandler? PropertyChanged;

        #endregion

        #region Fields

        private string m_controlTitle = "Demo Control";
        private int m_counter;
        private bool m_isHighlighted;
        private string m_newItemName = "";
        private string m_statusMessage = "Ready";
        private int m_nextId = 1;

        #endregion

        #region Constructors

        public MainViewModel()
        {
            Items = new ObservableCollection<ItemModel>();
            
            InitCommands();
            AddSampleItems();
        }

        #endregion

        #region Initialization

        private void InitCommands()
        {
            // WPF DelegateCommand uses CommandManager.RequerySuggested for automatic CanExecute updates
            IncrementCommand = new DelegateCommand(_ => Counter++);
            DecrementCommand = new DelegateCommand(_ => Counter--, _ => Counter > 0);
            ResetCommand = new DelegateCommand(_ => Counter = 0, _ => Counter != 0);
            
            AddItemCommand = new DelegateCommand(
                _ => AddItem(),
                _ => !string.IsNullOrWhiteSpace(NewItemName));
            
            DeleteItemCommand = new DelegateCommand(
                param => DeleteItem(param as ItemModel));
        }

        #endregion

        #region Functions

        private void AddItem()
        {
            var item = new ItemModel { Id = m_nextId++, Name = NewItemName };
            Items.Add(item);
            StatusMessage = $"Added item: {item.Name}";
            NewItemName = "";
        }

        private void DeleteItem(ItemModel? item)
        {
            if (item != null && Items.Remove(item))
            {
                StatusMessage = $"Deleted item: {item.Name}";
            }
        }

        private void AddSampleItems()
        {
            Items.Add(new ItemModel { Id = m_nextId++, Name = "Sample Item 1" });
            Items.Add(new ItemModel { Id = m_nextId++, Name = "Sample Item 2" });
            Items.Add(new ItemModel { Id = m_nextId++, Name = "Sample Item 3" });
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }

        #endregion

        #region Properties

        public string ControlTitle
        {
            get => m_controlTitle;
            set => SetProperty(ref m_controlTitle, value);
        }

        public int Counter
        {
            get => m_counter;
            set
            {
                if (SetProperty(ref m_counter, value))
                {
                    // WPF CommandManager automatically re-queries CanExecute
                    CommandManager.InvalidateRequerySuggested();
                    StatusMessage = $"Counter changed to {value}";
                }
            }
        }

        public bool IsHighlighted
        {
            get => m_isHighlighted;
            set => SetProperty(ref m_isHighlighted, value);
        }

        public string NewItemName
        {
            get => m_newItemName;
            set
            {
                if (SetProperty(ref m_newItemName, value))
                {
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public string StatusMessage
        {
            get => m_statusMessage;
            set => SetProperty(ref m_statusMessage, value);
        }

        public ObservableCollection<ItemModel> Items { get; }

        public DelegateCommand IncrementCommand { get; private set; } = null!;
        public DelegateCommand DecrementCommand { get; private set; } = null!;
        public DelegateCommand ResetCommand { get; private set; } = null!;
        public DelegateCommand AddItemCommand { get; private set; } = null!;
        public DelegateCommand DeleteItemCommand { get; private set; } = null!;

        #endregion
    }

    /// <summary>
    /// Simple item model for the DataGrid demo.
    /// </summary>
    public class ItemModel
    {
        #region Properties

        public int Id { get; set; }

        public string Name { get; set; } = "";

        #endregion
    }
}
