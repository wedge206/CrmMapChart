using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;

namespace CrmChartMap.CrmChartMapPlugin
{
	public class UninstallSolution : Plugin
	{
		// TODO: fix up this plugin so that it works, currently not in use

		public UninstallSolution()
			: base(typeof(UninstallSolution))
		{
			//base.RegisteredEvents.Add(new Tuple<int, string, string, Action<LocalPluginContext>>(20, "Delete", "solution", new Action<LocalPluginContext>(ExecuteDeleteSolution)));
		}

		private IOrganizationService Service;
		private ITracingService tracingService;
		private OrganizationServiceContext dataContext;

		private Entity PreImage;

		protected void Run(LocalPluginContext localContext)
		{
			Service = localContext.OrganizationService;
			dataContext = new OrganizationServiceContext(Service);
			tracingService = localContext.TracingService;

			PreImage = localContext.PluginExecutionContext.PostEntityImages["PreImage"] as Entity;

			if (PreImage.GetAttributeValue<string>("uniquename") == "ChartMaps")
			{
				List<Guid> recordList = dataContext.CreateQuery("dd_chartmapentity").Select(c => c.GetAttributeValue<Guid>("dd_chartmapentityid")).ToList();

				foreach (Guid id in recordList)
				{
					Service.Delete("dd_chartmapentity", id);
				}
			}
		}
	}
}
