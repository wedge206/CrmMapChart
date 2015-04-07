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
                        { "datadescription", new DataDescription(Target).ToJSON() },
                        { "presentationdescription", new PresentationDescription(Target).ToJSON() },
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
                        { "dd_chartid", chartId.ToFormattedString() }  // Can't have relationships with charts, so we have to store the guid as a string, boo...
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
                        { "datadescription", new DataDescription(PostImage).ToJSON() },
                        { "presentationdescription", new PresentationDescription(PostImage).ToJSON() }
                    }
				};

				Service.Update(mapChart);
				tracingService.Trace("Chart map config successfully updated");

				PublishEntity(PostImage.LogicalName);
			}

			public void PublishEntity(string EntityName)
			{
				tracingService.Trace("Now Publishing updated chart");

				OrganizationRequest publishRequest = new OrganizationRequest("PublishXml")
				{
					Parameters = new ParameterCollection()
					{
						{ "ParameterXml", String.Format("<importexportxml><entities><entity>{0}</entity><entity>savedqueryvisualization</entity></entities></importexportxml>", EntityName) }
					}
				};

				Service.Execute(publishRequest);

				tracingService.Trace("Chart successfully published.");
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

				// Create Defaults
				if (!dataContext.CreateQuery("dd_chartmapentity").Select(c => c.GetAttributeValue<Guid>("dd_chartmapentityid")).ToList().Any())
				{
					tracingService.Trace("Creating predefined maps for Account,Contact,Lead and Opportunity entities");
					CreateChartConfig("account", "Account Locations", "name", "Displays Account locations on a Bing Map.", "address1_city", "address1_line1", "address1_postalcode", "address1_stateorprovince", "address1_country", "address1_latitude", "address1_longitude", "Multiple Accounts");
					CreateChartConfig("contact", "Contact Locations", "fullname", "Displays Contact locations on a Bing Map", "address1_city", "address1_line1", "address1_postalcode", "address1_stateorprovince", "address1_country", "address1_latitude", "address1_longitude", "Multiple Contacts");
					CreateChartConfig("lead", "Lead Locations", "fullname", "Displays Lead locations on a Bing Map.", "address1_city", "address1_line1", "address1_postalcode", "address1_stateorprovince", "address1_country", "address1_latitude", "address1_longitude", "Multiple Leads");
					CreateHeatMap("opportunity", "Estimated Value Heat Map", "name", "Displays heat map of Estimated Value by Parent Account address", "parentaccountid.address1_city", "parentaccountid.address1_line1", "parentaccountid.address1_postalcode", "parentaccountid.address1_stateorprovince", "parentaccountid.address1_country", "parentaccountid.address1_latitude", "parentaccountid.address1_longitude", 2, "estimatedvalue", 1000000, 0.5m, 50, false, 1128000128, 1000000255, 1000128000, 1255255000, 1255000000);
					tracingService.Trace("Predefined maps created sucessfully");
				}
				else
				{
					#region Convert Existing Charts to new format

					#region Legacy Conversion
					// this bit can be removed in the future, once there are no legacy instances
					// find all records, and fill in config values if blank
					tracingService.Trace("Try to find ChartMapConfig.js");
					ChartMapConfig configSettings = new ChartMapConfig();
					Entity chartMapConfigRecord = dataContext.CreateQuery("webresource").Where(r => r.GetAttributeValue<string>("name") == "dd_chartmapconfig.js").SingleOrDefault();

					if (chartMapConfigRecord == default(Entity))
					{
						tracingService.Trace("Config file not found, using defaults");
						configSettings = ChartMapConfig.defaultConfig();
					}
					else
					{
						tracingService.Trace("Config file exists");
						configSettings = ChartMapConfig.getConfig(chartMapConfigRecord);
						SetBingKey(configSettings.BingKey);
					}

					var allConfigRecords = dataContext.CreateQuery("dd_chartmapentity")
													  .Select(c => new
													  {
														  Id = c.GetAttributeValue<Guid>("dd_chartmapentityid"),
														  HasZoom = c.Contains("dd_zoom"),
														  HasLatitude = c.Contains("dd_latitude"),
														  HasLongitude = c.Contains("dd_longitude"),
														  HasLanguage = c.Contains("dd_language"),
														  HasEnableCaching = c.Contains("dd_enablecaching"),
														  HasShowAllRecords = c.Contains("dd_showallrecords")
													  });

					foreach (var chartMapConfig in allConfigRecords)
					{
						Entity updatedConfig = new Entity("dd_chartmapentity");
						updatedConfig.Id = chartMapConfig.Id;

						bool updateNeeded = false;
						if (!chartMapConfig.HasZoom)
						{
							updatedConfig["dd_zoom"] = configSettings.Zoom;
							updateNeeded = true;
						}
						if (!chartMapConfig.HasLatitude)
						{
							updatedConfig["dd_latitude"] = configSettings.CenterLat;
							updateNeeded = true;
						}
						if (!chartMapConfig.HasLongitude)
						{
							updatedConfig["dd_longitude"] = configSettings.CenterLong;
							updateNeeded = true;
						}
						if (!chartMapConfig.HasLanguage)
						{
							updatedConfig["dd_language"] = new OptionSetValue(DataDescription.LangCodes.Single(l => l.Value == configSettings.Lang).Key);
							updateNeeded = true;
						}
						if (!chartMapConfig.HasEnableCaching)
						{
							updatedConfig["dd_enablecaching"] = false;
							updateNeeded = true;
						}
						if (!chartMapConfig.HasShowAllRecords)
						{
							updatedConfig["dd_showallrecords"] = false;
							updateNeeded = true;
						}
					#endregion

						if (updateNeeded)
						{
							Service.Update(updatedConfig);
						}
						else
						{
							// Force update chart to latest version
							updatedConfig["dd_fetchattributes"] = "";  // This is just an old field, not used anymore.  Being used here to force the update.
							Service.Update(updatedConfig);
						}
					}
					#endregion
				}

				// Remove this plugin step after the first time it runs successfully
				deletePluginStep("CrmChartMap.PostPublishAll");
			}

			private void SetBingKey(string key)
			{
				tracingService.Trace("Saving Bing Maps API Key to Organization Settings");

				var orgSettings = dataContext.CreateQuery("organization").Select(o => new { Id = o.GetAttributeValue<Guid>("organizationid"), ApiKey = o.GetAttributeValue<string>("bingmapsapikey") }).First();

				if (String.IsNullOrWhiteSpace(orgSettings.ApiKey))
				{
					Entity updatedOrg = new Entity("organization");
					updatedOrg.Id = orgSettings.Id;
					updatedOrg["bingmapsapikey"] = key;

					Service.Update(updatedOrg);
					tracingService.Trace("Successfully added Bing Maps API Key to Organization Settings");
				}
				else
				{
					tracingService.Trace("Organization has an existing key.  Leaving it alone, no changes performed.");
				}

			}

			private void deletePluginStep(string name)
			{
				tracingService.Trace("Deleting " + name + " plugin step");
				Guid stepId = dataContext.CreateQuery("sdkmessageprocessingstep").Where(s => s.GetAttributeValue<string>("name") == name).Select(s => s.Id).Single();
				tracingService.Trace("Found step id: " + stepId.ToFormattedString());

				Service.Delete("sdkmessageprocessingstep", stepId);
				tracingService.Trace("Step sucessfully deleted");
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
                        { "dd_colourstop5", new OptionSetValue(colourstop5) },
						{ "dd_zoom", 3 },
						{ "dd_language", new OptionSetValue(899300005) },
						{ "dd_latitude", 56.130366m },
						{ "dd_longitude", -106.346771m }
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
                        { "dd_pinsize", new OptionSetValue(1) },
						{ "dd_zoom", 3 },
						{ "dd_language", new OptionSetValue(899300005) },
						{ "dd_latitude", 56.130366m },
						{ "dd_longitude", -106.346771m }
                    }
				};

				Service.Create(newConfig);
			}
		}
	}
}
