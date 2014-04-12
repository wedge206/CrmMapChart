// <copyright file="PreChartMapEntityCreate.cs" company="">
// Copyright (c) 2014 All Rights Reserved
// </copyright>
// <author>Matt Barnes</author>
// <date>3/24/2014 9:41:06 PM</date>
// <summary>Implements the PreChartMapEntityCreate Plugin.</summary>

namespace CrmChartMap.CrmChartMapPlugin
{
    using System;
    using System.Linq;
    using System.ServiceModel;
    using System.Collections.Generic;

    using Microsoft.Xrm.Sdk;
    using Microsoft.Xrm.Sdk.Client;
    using Microsoft.Xrm.Sdk.Messages;
    using Microsoft.Xrm.Sdk.Metadata;
    using Microsoft.Xrm.Sdk.Query;

    /// <summary>
    /// PreChartMapEntityCreate Plugin.
    /// </summary>    
    public class PreChartMapEntityCreate : Plugin
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PreChartMapEntityCreate"/> class.
        /// </summary>
        public PreChartMapEntityCreate()
            : base(typeof(PreChartMapEntityCreate))
        {
            base.RegisteredEvents.Add(new Tuple<int, string, string, Action<LocalPluginContext>>(20, "Create", "dd_chartmapentity", new Action<LocalPluginContext>(ExecutePreChartMapEntityCreate)));
            base.RegisteredEvents.Add(new Tuple<int, string, string, Action<LocalPluginContext>>(20, "Update", "dd_chartmapentity", new Action<LocalPluginContext>(ExecutePreChartMapEntityCreate)));
        }

        /// <summary>
        /// Executes the plug-in.
        /// </summary>
        /// <param name="localContext">The <see cref="LocalPluginContext"/> which contains the
        /// <see cref="IPluginExecutionContext"/>,
        /// <see cref="IOrganizationService"/>
        /// and <see cref="ITracingService"/>
        /// </param>
        /// <remarks>
        /// For improved performance, Microsoft Dynamics CRM caches plug-in instances.
        /// The plug-in's Execute method should be written to be stateless as the constructor
        /// is not called for every invocation of the plug-in. Also, multiple system threads
        /// could execute the plug-in at the same time. All per invocation state information
        /// is stored in the context. This means that you should not use global variables in plug-ins.
        /// </remarks>
        protected void ExecutePreChartMapEntityCreate(LocalPluginContext localContext)
        {
            if (localContext == null)
            {
                throw new ArgumentNullException("localContext");
            }

            PreChartMapCreate preCreate = new PreChartMapCreate(localContext);
            preCreate.Run();
        }

        protected class PreChartMapCreate
        {
            private IOrganizationService Service;
            private ITracingService tracingService;
            private OrganizationServiceContext dataContext;

            private Entity Target;

            public PreChartMapCreate(LocalPluginContext localContext)
            {
                Service = localContext.OrganizationService;
                dataContext = new OrganizationServiceContext(Service);
                tracingService = localContext.TracingService;

                Target = localContext.PluginExecutionContext.InputParameters["Target"] as Entity;
            }

            public void Run()
            {
                List<string> fieldList = new List<string>() { "dd_namefield", "dd_cityfield", "dd_addressfield", "dd_postalcodefield", "dd_stateprovincefield", "dd_countryfield", "dd_numericfield" };

                tracingService.Trace("Getting MetaData");

                string entityname = Target.GetAttributeValue<string>("dd_entity") ?? Service.Retrieve(Target.LogicalName, Target.Id, new ColumnSet("dd_entity")).GetAttributeValue<string>("dd_entity");
                EntityMetadata entityData = GetEntityMetaData(entityname);

                foreach (string field in fieldList)
                {
                    if (Target.Contains(field))
                    {
                        string fieldValue = Target.GetAttributeValue<string>(field);
                        if (!String.IsNullOrWhiteSpace(fieldValue))
                        {
                            tracingService.Trace("Checking field " + field + " " + fieldValue);
                            if (fieldValue.Contains("."))
                            {
                                tracingService.Trace("LinkEntity Field");
                                string[] values = fieldValue.Split('.');

                                if (!entityData.Attributes.Select(a => a.LogicalName).Any(a => a == values[0]))
                                {
                                    throw new InvalidPluginExecutionException("Error: '" + values[0] + "' is not a valid field name");
                                }
                                else
                                {
                                    tracingService.Trace("Lookup fieldname correct");
                                    AttributeMetadata attributeData = entityData.Attributes.Where(a => a.LogicalName == values[0]).Single();
                                    if (attributeData.AttributeType != AttributeTypeCode.Lookup && attributeData.AttributeType != AttributeTypeCode.Customer)
                                    {
                                        throw new InvalidPluginExecutionException("Error: '" + values[0] + "' is not a Lookup Field");
                                    }
                                    else
                                    {
                                        tracingService.Trace("lookup field is valid");
                                        LookupAttributeMetadata lookupField = attributeData as LookupAttributeMetadata;

                                        string foundEntity = "";

                                        foreach (string lookupTarget in lookupField.Targets)
                                        {
                                            EntityMetadata relatedEntityMetadata = GetEntityMetaData(lookupTarget);

                                            if (relatedEntityMetadata.Attributes.Select(a => a.LogicalName).Any(a => a == values[1]))
                                            {
                                                foundEntity = lookupTarget;
                                            }
                                        }

                                        tracingService.Trace("found entity: " + foundEntity);
                                        if (foundEntity == "")
                                        {
                                            throw new InvalidPluginExecutionException("Error: '" + values[1] + "' is not a valid field name");
                                        }

                                        tracingService.Trace("updating target");
                                        Target[field + "linkentity"] = foundEntity;
                                    }

                                }
                            }
                            else
                            {
                                tracingService.Trace("Local field");
                                if (!entityData.Attributes.Select(a => a.LogicalName).Any(a => a == fieldValue))
                                {
                                    throw new InvalidPluginExecutionException("Error: '" + fieldValue + "' is not a valid field name");
                                }

                                tracingService.Trace("updating target");
                                Target[field + "linkentity"] = "";
                            }
                        }
                    }
                }
            }

            private EntityMetadata GetEntityMetaData(string entityName)
            {
                RetrieveEntityRequest metadataRequest = new RetrieveEntityRequest();
                metadataRequest.LogicalName = entityName;
                metadataRequest.EntityFilters = EntityFilters.Attributes;
                metadataRequest.RetrieveAsIfPublished = false;

                try
                {
                    tracingService.Trace("getting metadata for " + entityName);
                    RetrieveEntityResponse entityResponse = (RetrieveEntityResponse)Service.Execute(metadataRequest);

                    return entityResponse.EntityMetadata;
                }
                catch (FaultException<OrganizationServiceFault> ex)
                {
                    tracingService.Trace("failed to get metadata");
                    throw new InvalidPluginExecutionException("Error: " + ex.Message);
                }
            }
        }
    }
}