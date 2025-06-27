using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OutWit.Common.Interfaces;

namespace OutWit.Common.Settings.Interfaces
{
	public interface ISettingsValue
	{
		string Key { get; }
        string Name { get; }

		object UserValue { get; set; }
		object DefaultValue { get; set; }
		
        bool UserHidden { get; }
        bool DefaultHidden { get; }

        string UserTag { get; }
        string DefaultTag { get; }

		bool HasUserValue { get; }
		bool HasDefaultValue { get; }

        bool IsDefault { get; }

        bool Is(ISettingsValue value);

        void Reset(IResources resources);
		void Update(IResources resources);
	}
}
