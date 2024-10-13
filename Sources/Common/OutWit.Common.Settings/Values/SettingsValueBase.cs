using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using OutWit.Common.Abstract;
using OutWit.Common.Aspects;
using OutWit.Common.Aspects.Utils;
using OutWit.Common.Exceptions;
using OutWit.Common.Interfaces;
using OutWit.Common.Settings.Elements;
using OutWit.Common.Settings.Interfaces;

namespace OutWit.Common.Settings.Values
{
	public abstract class SettingsValueBase<TValue> : ModelBase, ISettingsValue, INotifyPropertyChanged
	{
		#region Events

        public event PropertyChangedEventHandler PropertyChanged = delegate { };

		#endregion

        #region Constructors

		protected SettingsValueBase(SettingsElementBase defaultElement, SettingsElementBase userElement)
        {
            Key = defaultElement.Key;

            DefaultElement = defaultElement;
			UserElement = userElement;

            IsDefault = false;

			PropertyChanged += OnPropertyChanged;
        }

        #endregion

		#region Functions

		public override string ToString()
		{
			return $"Key = {Key}, DefaultValue = {DefaultValue}, UserValue = {UserValue}";
		}

        public bool Is(ISettingsValue value)
        {
            return Is(value as ModelBase);
        }

        public abstract void Reset(IResources resources);
		public abstract void Update(IResources resources);
        protected abstract void CheckValues();

        private static TValue Cast(object value)
		{
			if (!(value is TValue))
				throw new ExceptionOf<ISettingsValue>();

			return (TValue)value;
		}

		#endregion

		#region ModelBase

        public override bool Is(ModelBase modelBase, double tolerance = DEFAULT_TOLERANCE)
        {
            if (!(modelBase is SettingsValueBase<TValue> settingsValue))
                return false;

            return DefaultElement.Key == settingsValue.DefaultElement.Key &&
                   DefaultElement.Value == settingsValue.DefaultElement.Value &&
                   DefaultElement.Hidden == settingsValue.DefaultElement.Hidden &&

                   UserElement.Key == settingsValue.UserElement.Key &&
                   UserElement.Value == settingsValue.UserElement.Value &&
                   UserElement.Hidden == settingsValue.UserElement.Hidden;

        }

        #endregion

		#region Event Handlers
		private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if(e.IsProperty((ISettingsValue val)=>val.UserValue))
				CheckValues();

            if (e.IsProperty((ISettingsValue val) => val.DefaultValue))
                CheckValues();
        } 

		#endregion

		#region ISettingsValue

        object ISettingsValue.DefaultValue
        {
            get => DefaultValue;
            set => DefaultValue = Cast(value);
        }

		object ISettingsValue.UserValue
		{
			get => UserValue;
			set => UserValue = Cast(value);
		}

        #endregion

		#region Properties

		public string Key { get; }

        public bool HasUserValue => UserElement != null;
        public bool HasDefaultValue => DefaultElement != null;

        [Notify]
        public string Name { get; set; }

        [Notify]
		public TValue DefaultValue { get; set; }

        [Notify]
		public TValue UserValue { get; set; }

		[Notify]
        public bool IsDefault { get; protected set; }

		public bool DefaultHidden => DefaultElement.Hidden;
		public bool UserHidden => !HasUserValue || UserElement.Hidden;

        public string DefaultTag => DefaultElement.Tag;
        public string UserTag => HasUserValue? UserElement.Tag : "";

        protected SettingsElementBase DefaultElement { get; }
		protected SettingsElementBase UserElement { get; }

        #endregion
    }
}
