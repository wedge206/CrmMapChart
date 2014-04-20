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
            base.RegisteredEvents.Add(new Tuple<int, string, string, Action<LocalPluginContext>>(40, "Create", "dd_chartmapentity", new Action<LocalPluginContext>(ExecutePostChartMapEntityCreate)));
            base.RegisteredEvents.Add(new Tuple<int, string, string, Action<LocalPluginContext>>(40, "Update", "dd_chartmapentity", new Action<LocalPluginContext>(ExecutePostChartMapEntityCreate)));
            base.RegisteredEvents.Add(new Tuple<int, string, string, Action<LocalPluginContext>>(40, "Delete", "dd_chartmapentity", new Action<LocalPluginContext>(ExecutePostChartMapEntityCreate)));
            base.RegisteredEvents.Add(new Tuple<int, string, string, Action<LocalPluginContext>>(40, "PublishAll", "", new Action<LocalPluginContext>(ExecutePostChartMapEntityCreate)));
        }

        protected void ExecutePostChartMapEntityCreate(LocalPluginContext localContext)
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
                EntityReference chartMapReference = new EntityReference("webresource", chartMapId);

                Entity newChart = new Entity("savedqueryvisualization")  // fyi: savedqueryvisualization = system chart
                {
                    Attributes = new AttributeCollection 
                    { 
                        { "primaryentitytypecode", Target.GetAttributeValue<string>("dd_entity")},
                        { "name", Target.GetAttributeValue<string>("dd_chartname") } ,
                        { "description", Target.GetAttributeValue<string>("dd_chartdescription") },
                        { "datadescription", Target.GetAttributeValue<string>("dd_entity") },
                        { "presentationdescription", Target.GetAttributeValue<string>("dd_entity") },
                        { "webresourceid", chartMapReference },
                        { "isdefault", false}
                    }
                };

                Guid chartId = Service.Create(newChart);
                tracingService.Trace("New chart created sucessfully with id: {0}", chartId.ToString());

                Entity updatedTarget = new Entity("dd_chartmapentity") { Id = Target.Id };
                updatedTarget["dd_chartid"] = chartId.ToString("B").ToUpper();  // Can't have relationships with charts, so we have to store the guid as a string, boo...
                updatedTarget["dd_fetchattributes"] = BuildFetch();

                Service.Update(updatedTarget);
                tracingService.Trace("Chart map config successfully updated");
            }

            public void RunUpdate()
            {
                Entity updatedTarget = new Entity("dd_chartmapentity") { Id = Target.Id };
                updatedTarget["dd_fetchattributes"] = BuildFetch();

                Service.Update(updatedTarget);
                tracingService.Trace("Chart map config successfully updated");
            }

            private string BuildFetch()
            {
                List<string> fieldList = new List<string>() { "dd_namefield", "dd_cityfield", "dd_addressfield", "dd_postalcodefield", "dd_stateprovincefield", "dd_countryfield", "dd_numericfield" };
                List<attribute> attributeList = new List<attribute>();

                foreach (string value in fieldList.Where(f => !string.IsNullOrWhiteSpace(PostImage.GetAttributeValue<string>(f))))
                {
                    string fieldValue = PostImage.GetAttributeValue<string>(value);  // Use the post image, to insure that we have all the values

                    if (fieldValue.Contains("."))
                    {
                        string[] values = fieldValue.Split('.');
                        attributeList.Add(new attribute(values[1], values[0], PostImage.GetAttributeValue<string>(value + "linkentity")));
                    }
                    else
                    {
                        attributeList.Add(new attribute(fieldValue));
                    }
                }

                string result = "";

                foreach (var group in attributeList.GroupBy(a => new { a.lookupname, a.linkentity }))
                {
                    if (!string.IsNullOrWhiteSpace(group.Key.linkentity))
                    {
                        if (group.Key.lookupname == "parentaccountid" || group.Key.lookupname == "parentcontactid")
                            result += string.Format("<link-entity name=\"{0}\" from=\"{0}id\" to=\"customerid\" link-type=\"outer\" alias=\"{1}\">", group.Key.linkentity, group.Key.lookupname);
                        else
                            result += string.Format("<link-entity name=\"{0}\" to=\"{1}\" link-type=\"outer\" alias=\"{1}\">", group.Key.linkentity, group.Key.lookupname);
                    }

                    foreach (attribute att in group)
                        result += string.Format("<attribute name=\"{0}\"/>", att.fieldname);

                    if (!string.IsNullOrWhiteSpace(group.Key.linkentity))
                        result += "</link-entity>";
                }

                tracingService.Trace("Created fetch query: " + result);
                return result;
            }

            class attribute
            {
                public string fieldname;
                public string lookupname;
                public string linkentity;

                public attribute(string field, string lookup = "", string link = "")
                {
                    fieldname = field;
                    lookupname = lookup;
                    linkentity = link;
                }
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
                    chartMapConfigRecord = new Entity("webresource");
                    chartMapConfigRecord["name"] = "dd_chartMapConfig.js";
                    chartMapConfigRecord["webresourcetype"] = new OptionSetValue(3);
                    chartMapConfigRecord["displayname"] = "dd_chartMapConfig.js";

                    chartMapConfigRecord.Id = Service.Create(chartMapConfigRecord);
                    tracingService.Trace("New web resource created: " + chartMapConfigRecord.Id.ToString());

                    string defaultcontent = "var configJSON = '{\"BingKey\":\"\",\"Published\":true,\"Zoom\":3,\"CenterLat\":56.130366,\"CenterLong\":-106.346771,\"Lang\":\"en-CA\",\"SSL\":\"1\",\"EnableClustering\":true,\"ClusterRadius\":30,\"ConfigId\":\"" + chartMapConfigRecord.Id.ToString() + "\"}';";

                    chartMapConfigRecord["content"] = defaultcontent.ToBase64String();
                    tracingService.Trace(defaultcontent);
                    tracingService.Trace(chartMapConfigRecord["content"].ToString());

                    Service.Update(chartMapConfigRecord);
                    tracingService.Trace("Config resource updated");

                    PublishConfig(chartMapConfigRecord.Id);
                    tracingService.Trace("Config resource published");
                }
                else
                {
                    string chartMapConfig = getConfigContent(chartMapConfigRecord);
                    tracingService.Trace("Existing chartMapConfig found: {0}", chartMapConfig);

                    isPublished = chartMapConfig.IndexOf("\"Published\":true") > -1;

                    setConfigIdIfNone(chartMapConfigRecord);

                    PublishConfig(chartMapConfigRecord.Id);
                    tracingService.Trace("Config resource published");
                }

                if (!isPublished)
                {
                    tracingService.Trace("Creating predefined maps for Account,Contact,Lead and Opportunity entities");
                    CreateChartConfig("account", "Account Locations", "name", "Displays Account locations on a Bing Map.", "address1_city", "address1_line1", "address1_postalcode", "address1_stateorprovince", "address1_country", "Multiple Accounts");
                    CreateChartConfig("contact", "Contact Locations", "fullname", "Displays Contact locations on a Bing Map", "address1_city", "address1_line1", "address1_postalcode", "address1_stateorprovince", "address1_country", "Multiple Contacts");
                    CreateChartConfig("lead", "Lead Locations", "fullname", "Displays Lead locations on a Bing Map.", "address1_city", "address1_line1", "address1_postalcode", "address1_stateorprovince", "address1_country", "Multiple Leads");
                    CreateHeatMap("opportunity", "Estimated Value Heat Map", "name", "Displays heat map of Estimated Value by Parent Account address", "parentaccountid.address1_city", "parentaccountid.address1_line1", "parentaccountid.address1_postalcode", "parentaccountid.address1_stateorprovince", "parentaccountid.address1_country", 2, "estimatedvalue", 1000000, 0.5m, 50, false, 1128000128, 1000000255, 1000128000, 1255255000, 1255000000);
                    tracingService.Trace("Predefined maps created sucessfully");

                    setPublishedFlag(chartMapConfigRecord);
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
                string chartMapConfig = getConfigContent(configRecord);

                if (chartMapConfig.IndexOf("\"ConfigId\":\"\"") > -1)
                {
                    tracingService.Trace("Setting Config Id");
                    updateConfig(configRecord, "\"ConfigId\":\"\"", "\"ConfigId\":\"" + configRecord.Id.ToString() + "\"");
                }
                else
                {
                    tracingService.Trace("Config Id already exists");
                }
            }

            private void setPublishedFlag(Entity configRecord)
            {
                tracingService.Trace("Setting published flag = true");
                updateConfig(configRecord, "\"Published\":false", "\"Published\":true");
            }

            private void updateConfig(Entity configRecord, string find, string replace)
            {
                tracingService.Trace(configRecord["content"].ToString());
                string config = getConfigContent(configRecord);
                config = config.Replace(find, replace);

                byte[] cleanConfig = Encoding.Convert(Encoding.UTF8, Encoding.GetEncoding(Encoding.ASCII.EncodingName, new EncoderReplacementFallback(string.Empty), new DecoderExceptionFallback()), Encoding.UTF8.GetBytes(config)); // mini hack to remove invalid character that get inserted for unknown reason
                tracingService.Trace("New config: {0}", config);

                Entity updatedConfig = new Entity("webresource");
                updatedConfig.Id = configRecord.Id;

                updatedConfig["content"] = Convert.ToBase64String(cleanConfig);

                Service.Update(updatedConfig);
                tracingService.Trace("Sucessfully updated conifg");
            }

            private void deletePluginStep()
            {
                tracingService.Trace("Deleting PublishAll plugin step");
                var stepId = dataContext.CreateQuery("sdkmessageprocessingstep").Where(s => s.GetAttributeValue<string>("name") == "CrmChartMap.PostPublishAll").Select(s => s.Id).SingleOrDefault();

                Service.Delete("sdkmessageprocessingstep", stepId);
                tracingService.Trace("PublishAll step sucessfully deleted");
            }

            private string getConfigContent(Entity configRecord)
            {
                tracingService.Trace("getConfigContent: {0}", configRecord["content"]);
                byte[] rawContent = Convert.FromBase64String((string)configRecord["content"]);

                return Encoding.UTF8.GetString(rawContent);
            }

            private void CreateHeatMap(string entityname, string entitydisplayname, string name, string description, string city, string address, string postcode, string province, string country, int intensityfactor, string numericfield, int weight, decimal intensity, int radius, bool meters, int colourstop1, int colourstop2, int colourstop3, int colourstop4, int colourstop5)
            {
                tracingService.Trace("CreateChartConfig: Entity: {0}, DisplayName: {1}, Name: {2}, City: {3}, Address: {4}, PostCode: {5}, State: {6}, Country: {7}", entityname, entitydisplayname, name, city, address, postcode, province, country);
                Entity newConfig = new Entity("dd_chartmapentity")
                {
                    Attributes = new AttributeCollection 
                    { 
                        { "dd_entity", entityname },
                        { "dd_maptype", new OptionSetValue(2) },
                        { "dd_chartname", entitydisplayname },
                        { "dd_chartdescription", description },
                        { "dd_namefield", name },
                        { "dd_cityfield", city },
                        { "dd_addressfield", address },
                        { "dd_postalcodefield", postcode },
                        { "dd_stateprovincefield", province },
                        { "dd_countryfield", country },
                        { "dd_heatmapbasedon", new OptionSetValue(intensityfactor) },
                        { "dd_numericfield", numericfield},
                        { "dd_intensityrange", new OptionSetValue(1) },
                        { "dd_intensitycalculation", new OptionSetValue(1) },
                        { "dd_deviations", 2.0 },
                        { "dd_weight", weight },
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

            private void CreateChartConfig(string entityname, string entitydisplayname, string name, string description, string city, string address, string postcode, string province, string country, string clustertitle)
            {
                tracingService.Trace("CreateChartConfig: Entity: {0}, DisplayName: {1}, Name: {2}, City: {3}, Address: {4}, PostCode: {5}, State: {6}, Country: {7}", entityname, entitydisplayname, name, city, address, postcode, province, country);
                Entity newConfig = new Entity("dd_chartmapentity")
                {
                    Attributes = new AttributeCollection 
                    { 
                        { "dd_entity", entityname },
                        { "dd_maptype", new OptionSetValue(1) },
                        { "dd_chartname", entitydisplayname },
                        { "dd_chartdescription", description },
                        { "dd_namefield", name },
                        { "dd_cityfield", city },
                        { "dd_addressfield", address },
                        { "dd_postalcodefield", postcode },
                        { "dd_stateprovincefield", province },
                        { "dd_countryfield", country },
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

    public static class extentions
    {
        public static string ToBase64String(this string s)
        {
            if (string.IsNullOrWhiteSpace(s))
                return "";

            byte[] cleanArray = Encoding.Convert(Encoding.UTF8, Encoding.GetEncoding(Encoding.ASCII.EncodingName, new EncoderReplacementFallback(string.Empty), new DecoderExceptionFallback()), Encoding.UTF8.GetBytes(s)); // mini hack to remove invalid character that get inserted for unknown reason
            return Convert.ToBase64String(cleanArray);
        }
    }
}

