using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OutWit.Common.Interfaces
{
	public interface IResources
	{
		string this[string key] { get; }

		string this[string key, CultureInfo culture] { get; }

		void ResetCulture(string cultureName);

        bool HasStringFor(string key);

        bool HasStringFor(string key, CultureInfo culture);
    }
}
