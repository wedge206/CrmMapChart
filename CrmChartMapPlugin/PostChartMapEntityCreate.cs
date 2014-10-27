// <copyright file="PostChartMapEntityCreate.cs" company="">
// Copyright (c) 2014 All Rights Reserved
// </copyright>
// <author>Matt Barnes</author>
// <date>3/16/2014</date>
// <summary>Implements the PostChartMapEntityCreate Plugin.</summary>

namespace CrmChartMap.CrmChartMapPlugin
{
	using System;
	using System.Text;
	using System.Linq;
	using System.ServiceModel;
	using System.Collections.Generic;

	using Microsoft.Xrm.Sdk;
	using Microsoft.Xrm.Sdk.Client;

	/// <summary>
	/// PostChartMapEntityCreate Plugin.
	/// </summary>    
	public class PostChartMapEntityCreate : Plugin
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="PostChartMapEntityCreate"/> class.
		/// </summary>
		public PostChartMapEntityCreate()
			: base(typeof(PostChartMapEntityCreate))
		{
			base.RegisteredEvents.Add(new Tuple<int, string, string, Action<LocalPluginContext>>(40, "Create", "dd_chartmapentity", new Action<LocalPluginContext>(ExecutePostChartMapEntity)));
			base.RegisteredEvents.Add(new Tuple<int, string, string, Action<LocalPluginContext>>(40, "Update", "dd_chartmapentity", new Action<LocalPluginContext>(ExecutePostChartMapEntity)));
			base.RegisteredEvents.Add(new Tuple<int, string, string, Action<LocalPluginContext>>(40, "Delete", "dd_chartmapentity", new Action<LocalPluginContext>(ExecutePostChartMapEntity)));
			base.RegisteredEvents.Add(new Tuple<int, string, string, Action<LocalPluginContext>>(40, "PublishAll", "", new Action<LocalPluginContext>(ExecutePostChartMapEntity)));
			base.RegisteredEvents.Add(new Tuple<int, string, string, Action<LocalPluginContext>>(20, "Delete", "solution", new Action<LocalPluginContext>(ExecuteDeleteSolution)));
		}

		protected void ExecutePostChartMapEntity(LocalPluginContext localContext)
		{
			if (localContext == null)
			{
				throw new ArgumentNullException("localContext");
			}

			switch (localContext.PluginExecutionContext.MessageName.ToLower())
			{
				case "create":
					// Create a new system chart that coresponds to the config record just created
					PostChartMapCreate postChartMapCreate = new PostChartMapCreate(localContext);
					postChartMapCreate.Run();
					break;
				case "update":
					// Update the fetchXml query for existing chart
					PostChartMapCreate postChartMapUpdate = new PostChartMapCreate(localContext);
					postChartMapUpdate.RunUpdate();
					break;
				case "delete":
					// Delete the system chart that was associated with the config record just deleted
					PostDelete postDelete = new PostDelete(localContext);
					postDelete.Run();
					break;
				case "publishall":
					// Create the "OOB" charts on the initial publish all.  This step deletes itself once it is done.
					PostPublishAll postPublishAll = new PostPublishAll(localContext);
					postPublishAll.Run();
					break;
			}
		}

		protected void ExecuteDeleteSolution(LocalPluginContext localContext)
		{
			//make sure its our solution
			if (localContext == null)
			{
				throw new ArgumentNullException("localContext");
			}

			PreDeleteSolution deleteSolution = new PreDeleteSolution(localContext);
			deleteSolution.Run();
		}

		protected class PreDeleteSolution
		{
			const bool DELETECONFIG = false;

			private IOrganizationService Service;
			private ITracingService tracingService;
			private OrganizationServiceContext dataContext;

			private Entity PreImage;

			public PreDeleteSolution(LocalPluginContext localContext)
			{
				Service = localContext.OrganizationService;
				dataContext = new OrganizationServiceContext(Service);
				tracingService = localContext.TracingService;

				PreImage = localContext.PluginExecutionContext.PostEntityImages["PreImage"] as Entity;
			}

			public void Run()
			{
				if (PreImage.GetAttributeValue<string>("uniquename") == "ChartMaps")
				{
					List<Guid> recordList = dataContext.CreateQuery("dd_chartmapentity").Select(c => c.GetAttributeValue<Guid>("dd_chartmapentityid")).ToList();

					foreach (Guid id in recordList)
					{
						Service.Delete("dd_chartmapentity", id);
					}

					if (DELETECONFIG)
					{
						Guid chartMapConfigId = dataContext.CreateQuery("webresource").Where(r => (string)r["name"] == "dd_chartmapconfig.js").Select(r => r.GetAttributeValue<Guid>("webresourceid")).SingleOrDefault();
						if (chartMapConfigId != Guid.Empty)
						{
							Service.Delete("webresource", chartMapConfigId);
						}
					}
				}
			}
		}

		protected class PostChartMapCreate
		{
			private IOrganizationService Service;
			private ITracingService tracingService;
			private OrganizationServiceContext dataContext;

			private Entity Target;
			private Entity PostImage;

			public PostChartMapCreate(LocalPluginContext localContext)
			{
				Service = localContext.OrganizationService;
				dataContext = new OrganizationServiceContext(Service);
				tracingService = localContext.TracingService;

				Target = localContext.PluginExecutionContext.InputParameters["Target"] as Entity;
				PostImage = localContext.PluginExecutionContext.PostEntityImages["PostImage"] as Entity;
			}

			public void Run()
			{
				tracingService.Trace("Beginning PostChartMapCreate.  Entity: {0}, Display Name: {1}", Target["dd_entity"], Target["dd_chartname"]);

				Guid chartMapId = dataContext.CreateQuery("webresource").Where(r => (string)r["name"] == "dd_chartMap.htm").Select(r => r.Id).Single();
				Entity newChart = new Entity("savedqueryvisualization")  // fyi: savedqueryvisualization = system chart
				{
					Attributes = new AttributeCollection 
                    { 
                        { "primaryentitytypecode", Target.GetAttributeValue<string>("dd_entity")},
                        { "name", Target.GetAttributeValue<string>("dd_chartname") } ,
                        { "description", Target.GetAttributeValue<string>("dd_chartdescription") },
                        { "datadescription", DataDescription.FromEntity(Target).ToJSON() },
                        { "presentationdescription", PresentationDescription.FromEntity(Target).ToJSON() },
                        { "webresourceid", new EntityReference("webresource", chartMapId) },
                        { "isdefault", false}
                    }
				};

				Guid chartId = Service.Create(newChart);
				tracingService.Trace("New chart created sucessfully with id: {0}", chartId.ToString());

				Entity updatedTarget = new Entity("dd_chartmapentity")
				{
					Id = Target.Id,
					Attributes = new AttributeCollection()
                    {
                        { "dd_chartid", chartId.ToString("B").ToUpper() }  // Can't have relationships with charts, so we have to store the guid as a string, boo...
                    }
				};

				Service.Update(updatedTarget);
				tracingService.Trace("Chart map config successfully updated");
			}

			public void RunUpdate()
			{
				Entity mapChart = new Entity("savedqueryvisualization")
				{
					Id = PostImage.GetAttributeValue<string>("dd_chartid").ToGuid(),
					Attributes = new AttributeCollection() {
                        { "datadescription", DataDescription.FromEntity(PostImage).ToJSON() },
                        { "presentationdescription", PresentationDescription.FromEntity(PostImage).ToJSON() }
                    }
				};

				Service.Update(mapChart);
				tracingService.Trace("Chart map config successfully updated");
			}
		}

		protected class PostDelete
		{
			private IOrganizationService Service;
			private ITracingService tracingService;
			private Entity PreImage;

			public PostDelete(LocalPluginContext localContext)
			{
				Service = localContext.OrganizationService;
				tracingService = localContext.TracingService;

				PreImage = localContext.PluginExecutionContext.PreEntityImages["PreImage"] as Entity;
			}

			public void Run()
			{
				tracingService.Trace("Beginning PostDelete.  Chart Id: {0}", PreImage["dd_chartid"].ToString());
				Guid chartId;

				if (Guid.TryParse((string)PreImage["dd_chartid"], out chartId))
				{
					Service.Delete("savedqueryvisualization", chartId);
					tracingService.Trace("Sucessfully deleted chart");
				}
				else
				{
					throw new InvalidPluginExecutionException("Failed to parse Id.  Chart not deleted");
				}
			}
		}

		protected class PostPublishAll
		{
			private IOrganizationService Service;
			private ITracingService tracingService;
			private OrganizationServiceContext dataContext;

			public PostPublishAll(LocalPluginContext localContext)
			{
				Service = localContext.OrganizationService;
				tracingService = localContext.TracingService;
				dataContext = new OrganizationServiceContext(Service);
			}

			public void Run()
			{
				tracingService.Trace("Beginning PostPublishAll");
				Entity chartMapConfigRecord = dataContext.CreateQuery("webresource").Where(r => (string)r["name"] == "dd_chartmapconfig.js").SingleOrDefault();

				bool isPublished = false;

				if (chartMapConfigRecord == null)
				{
					// Create new webresource
					tracingService.Trace("Config resource not found.  Generating default config");

					Guid Id = Guid.NewGuid();
					chartMapConfigRecord = new Entity("webresource")
					{
						Id = Id,
						Attributes = new AttributeCollection()
						{
							{ "name", "dd_chartMapConfig.js" },
							{ "webresourcetype", new OptionSetValue(3) },
							{ "displayname", "dd_chartMapConfig.js" },
							{ "content", ChartMapConfig.defaultConfig(Id).makeContent() }
						}
					};

					Service.Create(chartMapConfigRecord);
					tracingService.Trace("New web resource created: " + chartMapConfigRecord.Id.ToString());

					PublishConfig(chartMapConfigRecord.Id);
					tracingService.Trace("Config resource published");
				}
				else
				{
					tracingService.Trace("Config file exists");
					ChartMapConfig chartMapConfig = ChartMapConfig.getConfig(chartMapConfigRecord);
					tracingService.Trace("Found config with version: " + chartMapConfig.configVersion.ToString());

					if (chartMapConfig.configVersion < 2)  // Upgrading from 2.x
					{
						isPublished = chartMapConfig.Published;
						string SSL = chartMapConfig.SSL ?? "1";
						string Lang = chartMapConfig.Lang ?? "en-CA";
						
						tracingService.Trace("Beginning upgrade procedure");
						ChartMapConfig upgradedConfig = new ChartMapConfig()
						{
							BingKey = chartMapConfig.BingKey,
							Zoom = chartMapConfig.Zoom,
							CenterLat = chartMapConfig.CenterLat,
							CenterLong = chartMapConfig.CenterLong,
							Lang = Lang,
							SSL = SSL,
							ConfigId = chartMapConfig.ConfigId,
							configVersion = 2,
							bingMapScriptUrl = String.Format("{2}://ecn.dev.virtualearth.net/mapcontrol/mapcontrol.ashx?v=7.0&mkt={1}&s={0}", SSL, Lang, SSL == "1" ? "https" : "http"),
							Published = true
						};
						updateConfig(chartMapConfigRecord.Id, upgradedConfig);
						tracingService.Trace("Completed Configuration file upgrade");

						var entityList = dataContext.CreateQuery("dd_chartmapentity");
						foreach (var e in entityList)
						{
							tracingService.Trace("Upgrading chart: " + e.Id.ToString("B"));
							Entity upgradedChart = new Entity("savedqueryvisualization")  // fyi: savedqueryvisualization = system chart
							{
								Id = Guid.Parse(e.GetAttributeValue<string>("dd_chartid")),
								Attributes = new AttributeCollection 
								{ 
									{ "datadescription", DataDescription.FromEntity(e).ToJSON() },
									{ "presentationdescription", PresentationDescription.FromEntity(e).ToJSON() },
								}
							};

							Service.Update(upgradedChart);
							tracingService.Trace("Chart upgrade complete.");
						}
						tracingService.Trace("Upgrade procedure complete");
					}
					else  // upgrading from 3.x
					{
						tracingService.Trace("No Upgrade needed");
						isPublished = chartMapConfig.Published;
						setConfigIdIfNone(chartMapConfigRecord);

						PublishConfig(chartMapConfigRecord.Id);
						tracingService.Trace("Config resource published");
					}
				}

				if (!isPublished)
				{
					tracingService.Trace("Creating predefined maps for Account,Contact,Lead and Opportunity entities");
					CreateChartConfig("account", "Account Locations", "name", "Displays Account locations on a Bing Map.", "address1_city", "address1_line1", "address1_postalcode", "address1_stateorprovince", "address1_country", "address1_latitude", "address1_longitude", "Multiple Accounts");
					CreateChartConfig("contact", "Contact Locations", "fullname", "Displays Contact locations on a Bing Map", "address1_city", "address1_line1", "address1_postalcode", "address1_stateorprovince", "address1_country", "address1_latitude", "address1_longitude", "Multiple Contacts");
					CreateChartConfig("lead", "Lead Locations", "fullname", "Displays Lead locations on a Bing Map.", "address1_city", "address1_line1", "address1_postalcode", "address1_stateorprovince", "address1_country", "address1_latitude", "address1_longitude", "Multiple Leads");
					CreateHeatMap("opportunity", "Estimated Value Heat Map", "name", "Displays heat map of Estimated Value by Parent Account address", "parentaccountid.address1_city", "parentaccountid.address1_line1", "parentaccountid.address1_postalcode", "parentaccountid.address1_stateorprovince", "parentaccountid.address1_country", "parentaccountid.address1_latitude", "parentaccountid.address1_longitude", 2, "estimatedvalue", 1000000, 0.5m, 50, false, 1128000128, 1000000255, 1000128000, 1255255000, 1255000000);
					tracingService.Trace("Predefined maps created sucessfully");

					setPublishedFlag(chartMapConfigRecord.Id, ChartMapConfig.getConfig(chartMapConfigRecord));
				}
				else
				{
					tracingService.Trace("Solution has already been published.");
				}

				// Remove this plugin step after the first time it runs successfully
				deletePluginStep();
			}

			private void PublishConfig(Guid ConfigId)
			{
				OrganizationRequest request = new OrganizationRequest
				{
					RequestName = "PublishXml",
					Parameters = new ParameterCollection() { new KeyValuePair<string, object>("ParameterXml", string.Format("<importexportxml><webresources><webresource>{0}</webresource></webresources></importexportxml>", ConfigId.ToString())) }
				};

				Service.Execute(request);
			}

			private void setConfigIdIfNone(Entity configRecord)
			{
				ChartMapConfig chartMapConfig = ChartMapConfig.getConfig(configRecord);

				if (chartMapConfig.ConfigId == Guid.Empty)
				{
					tracingService.Trace("Setting Config Id");
					chartMapConfig.ConfigId = configRecord.Id;
				}
				else
				{
					tracingService.Trace("Config Id already exists");
				}
			}

			private void setPublishedFlag(Guid configRecordId, ChartMapConfig config)
			{
				tracingService.Trace("Setting published flag = true");
				config.Published = true;
				updateConfig(configRecordId, config);
			}

			private void updateConfig(Guid Id, ChartMapConfig config)
			{
				Entity configEntity = new Entity("webresource");
				configEntity.Id = Id;
				configEntity["content"] = config.makeContent();

				Service.Update(configEntity);
				tracingService.Trace("Sucessfully updated conifg");
			}

			private void deletePluginStep()
			{
				tracingService.Trace("Deleting PublishAll plugin step");
				Guid stepId = dataContext.CreateQuery("sdkmessageprocessingstep").Where(s => s.GetAttributeValue<string>("name") == "CrmChartMap.PostPublishAll").Select(s => s.Id).SingleOrDefault();
				tracingService.Trace("Found step id: " + stepId.ToString("B"));

				Service.Delete("sdkmessageprocessingstep", stepId);
				tracingService.Trace("PublishAll step sucessfully deleted");
			}

			private void CreateHeatMap(string entityname, string entitydisplayname, string name, string description, string city, string address, string postcode, string province, string country, string latitude, string longitude, int intensityfactor, string numericfield, int weight, decimal intensity, int radius, bool meters, int colourstop1, int colourstop2, int colourstop3, int colourstop4, int colourstop5)
			{
				tracingService.Trace("CreateChartConfig: Entity: {0}, DisplayName: {1}, Name: {2}, City: {3}, Address: {4}, PostCode: {5}, State: {6}, Country: {7}", entityname, entitydisplayname, name, city, address, postcode, province, country);
				Entity newConfig = new Entity("dd_chartmapentity")
				{
					Attributes = new AttributeCollection 
                    { 
                        { "dd_entity", entityname },
                        { "dd_enablecaching", false },
                        { "dd_showallrecords", false },
                        { "dd_maptype", new OptionSetValue(2) },
                        { "dd_chartname", entitydisplayname },
                        { "dd_chartdescription", description },
                        { "dd_namefield", name },
                        { "dd_cityfield", city },
                        { "dd_addressfield", address },
                        { "dd_postalcodefield", postcode },
                        { "dd_stateprovincefield", province },
                        { "dd_countryfield", country },
                        { "dd_latitudefield", latitude },
                        { "dd_longitudefield", longitude },
                        { "dd_heatmapbasedon", new OptionSetValue(intensityfactor) },
                        { "dd_numericfield", numericfield},
                        { "dd_intensityrange", new OptionSetValue(1) },
                        { "dd_intensitycalculation", new OptionSetValue(1) },
                        { "dd_deviations", 2.0m },
                        { "dd_intensity", intensity},
                        { "dd_radius", radius},
                        { "dd_radiusunits", meters },
                        { "dd_colourstop1", new OptionSetValue(colourstop1) },
                        { "dd_colourstop2", new OptionSetValue(colourstop2) },
                        { "dd_colourstop3", new OptionSetValue(colourstop3) },
                        { "dd_colourstop4", new OptionSetValue(colourstop4) },
                        { "dd_colourstop5", new OptionSetValue(colourstop5) }
                    }
				};

				Service.Create(newConfig);
			}

			private void CreateChartConfig(string entityname, string entitydisplayname, string name, string description, string city, string address, string postcode, string province, string country, string latitude, string longitude, string clustertitle)
			{
				tracingService.Trace("CreateChartConfig: Entity: {0}, DisplayName: {1}, Name: {2}, City: {3}, Address: {4}, PostCode: {5}, State: {6}, Country: {7}", entityname, entitydisplayname, name, city, address, postcode, province, country);
				Entity newConfig = new Entity("dd_chartmapentity")
				{
					Attributes = new AttributeCollection 
                    { 
                        { "dd_entity", entityname },
                        { "dd_enablecaching", false },
                        { "dd_showallrecords", false },
                        { "dd_maptype", new OptionSetValue(1) },
                        { "dd_chartname", entitydisplayname },
                        { "dd_chartdescription", description },
                        { "dd_namefield", name },
                        { "dd_cityfield", city },
                        { "dd_addressfield", address },
                        { "dd_postalcodefield", postcode },
                        { "dd_stateprovincefield", province },
                        { "dd_countryfield", country },
                        { "dd_latitudefield", latitude},
                        { "dd_longitudefield", longitude},
                        { "dd_enableclustering", true },
                        { "dd_clusterradius", 30 },
                        { "dd_clustername", clustertitle },
                        { "dd_pinsize", new OptionSetValue(1) }
                    }
				};

				Service.Create(newConfig);
			}
		}
	}
}
