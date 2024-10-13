using System.IO;
using System.Collections.Generic;
using Prism.Modularity;

namespace OutWit.Common.Prism
{
    public class DirectoriesModuleCatalog : DirectoryModuleCatalog
	{
		private readonly IList<string> m_pathsToProbe;

		public DirectoriesModuleCatalog(string modulePath, string filter = "*", SearchOption option = SearchOption.TopDirectoryOnly)
		{
			m_pathsToProbe = new List<string> { modulePath };

			foreach (var path in Directory.GetDirectories(modulePath, filter, option))
				m_pathsToProbe.Add(path);
		}

		protected override void InnerLoad()
		{
			foreach (var path in m_pathsToProbe)
			{
				ModulePath = path;
				base.InnerLoad();			
			}
		}
	}
}
