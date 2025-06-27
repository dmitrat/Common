using System;
using System.Collections.Generic;
using System.Text;
using Prism.Navigation.Regions;

namespace OutWit.Common.Prism.Interfaces
{
    public interface INavigationControl : INavigationAware
    {
        void Deselect();
        void Collapse();
        void Expand();

        INavigationContext Context { get; }
    }
}
