using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace OutWit.Common.Controls.Interfaces
{
    public interface ISuggestionProvider
    {
        IEnumerable GetSuggestions(string filter);
    }
}
