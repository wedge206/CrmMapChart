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
        public PreChartMapEntityCreate()
            : base(typeof(PreChartMapEntityCreate))
        {
            base.RegisteredEvents.Add(new Tuple<int, string, string, Action<LocalPluginContext>>(20, "Create", "dd_chartmapentity", new Action<LocalPluginContext>(ExecutePreChartMapEntityCreate)));
            base.RegisteredEvents.Add(new Tuple<int, string, string, Action<LocalPluginContext>>(20, "Update", "dd_chartmapentity", new Action<LocalPluginContext>(ExecutePreChartMapEntityCreate)));
        }

        protected void ExecutePreChartMapEntityCreate(LocalPluginContext localContext)
        {
            if (localContext == null)
            {
                throw new ArgumentNullException("localContext");
            }

            PreChartMapCreate preCreate = new PreChartMapCreate(localContext);
            EntityMetadata metadata = preCreate.Run();
            preCreate.UpdateSchemaNames(metadata);
        }

        protected class PreChartMapCreate
        {
            private IOrganizationService Service;
            private ITracingService tracingService;
            private OrganizationServiceContext dataContext;

            private Entity Target;
			private Entity PreImage;

            public PreChartMapCreate(LocalPluginContext localContext)
            {
                Service = localContext.OrganizationService;
                dataContext = new OrganizationServiceContext(Service);
                tracingService = localContext.TracingService;

                Target = localContext.PluginExecutionContext.InputParameters["Target"] as Entity;
				localContext.PluginExecutionContext.PreEntityImages.TryGetValue("PreImage", out PreImage);
            }

            public EntityMetadata Run()
            {
                List<string> fieldList = new List<string>() { "dd_namefield", "dd_cityfield", "dd_addressfield", "dd_postalcodefield", "dd_stateprovincefield", "dd_countryfield", "dd_latitudefield", "dd_longitudefield", "dd_numericfield" };

                tracingService.Trace("Getting MetaData");

				string entityname = Target.GetAttributeValue<string>("dd_entity") ?? PreImage.GetAttributeValue<string>("dd_entity"); //Service.Retrieve(Target.LogicalName, Target.Id, new ColumnSet("dd_entity")).GetAttributeValue<string>("dd_entity");
                EntityMetadata entityData = GetEntityMetaData(entityname);

                foreach (string field in fieldList)
                {
                    if (Target.Contains(field))
                    {
                        tracingService.Trace("Working on field: " + field);
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
                                        tracingService.Trace("Lookup field is valid");
                                        LookupAttributeMetadata lookupField = attributeData as LookupAttributeMetadata;

                                        EntityMetadata foundEntityMetaData = FindLookupEntity(values[1], lookupField.Targets);

                                        tracingService.Trace("Updating Target");
                                        Target[field + "linkentity"] = foundEntityMetaData.LogicalName;
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

                                tracingService.Trace("Updating Target");
                                Target[field + "linkentity"] = "";
                            }
                        }
                    }
                }

                tracingService.Trace("Completed Field Validation");

                return entityData;
            }

            public void UpdateSchemaNames(EntityMetadata metaData)
            {
                tracingService.Trace("Updating schema names");

                updateSchemaName("dd_latitudefield", "dd_latitudeschemaname", metaData);
                updateSchemaName("dd_longitudefield", "dd_longitudeschemaname", metaData);
                
                tracingService.Trace("Schema name update complete");
            }

            private void updateSchemaName(string TargetField, string AddedField, EntityMetadata metaData)
            {
                if (Target.Contains(TargetField))
                {
                    if (Target.GetAttributeValue<string>(TargetField).Contains(".")) 
                    {
                        tracingService.Trace("LinkEntity Field");
                        string[] values = Target.GetAttributeValue<string>(TargetField).Split('.');

                        AttributeMetadata attributeData = metaData.Attributes.Where(a => a.LogicalName == values[0]).Single();
                        LookupAttributeMetadata lookupField = attributeData as LookupAttributeMetadata;

                        EntityMetadata parentMetadata = FindLookupEntity(values[1], lookupField.Targets);

                        tracingService.Trace("Adding " + AddedField + " to Target for: " + Target.GetAttributeValue<string>(TargetField));
                        Target[AddedField] = parentMetadata.Attributes.Single(a => a.LogicalName == values[1]).SchemaName;
                        UpdateEntitySchemaName(parentMetadata.SchemaName);
                    }
                    else  
                    {
                        UpdateEntitySchemaName(metaData.SchemaName);

                        tracingService.Trace("Target Field");
                        if (!String.IsNullOrWhiteSpace(Target.GetAttributeValue<string>(TargetField)))
                        {
                            tracingService.Trace("Adding " + AddedField + " to Target for: " + Target.GetAttributeValue<string>(TargetField));
                            Target[AddedField] = metaData.Attributes.Single(a => a.LogicalName == Target.GetAttributeValue<string>(TargetField)).SchemaName;
                        }
                        else
                        {
                            tracingService.Trace("Clearing " + AddedField);
                            Target[AddedField] = "";
                        }
                    }
                }
                
            }

            private EntityMetadata FindLookupEntity(string searchValue, string[] EntityList)
            {
                tracingService.Trace("Searching for entity: " + searchValue);
                foreach (string lookupTarget in EntityList)
                {
                    EntityMetadata relatedEntityMetadata = GetEntityMetaData(lookupTarget);

                    if (relatedEntityMetadata.Attributes.Select(a => a.LogicalName).Any(a => a == searchValue))
                    {
                        tracingService.Trace("Found entity: " + lookupTarget);

                        return relatedEntityMetadata;
                    }
                }

                throw new InvalidPluginExecutionException("Error: '" + searchValue + "' is not a valid field name");
            }

            public void UpdateEntitySchemaName(string SchemaName)
            {
                tracingService.Trace("Adding dd_entityschemaname to Target: " + SchemaName);
                Target["dd_entityschemaname"] = SchemaName;
            }

            private EntityMetadata GetEntityMetaData(string entityName)
            {
                RetrieveEntityRequest metadataRequest = new RetrieveEntityRequest();
                metadataRequest.LogicalName = entityName;
                metadataRequest.EntityFilters = EntityFilters.Entity | EntityFilters.Attributes;
                metadataRequest.RetrieveAsIfPublished = false;

                try
                {
                    tracingService.Trace("Getting metadata for " + entityName);
                    RetrieveEntityResponse entityResponse = (RetrieveEntityResponse)Service.Execute(metadataRequest);

                    return entityResponse.EntityMetadata;
                }
                catch (FaultException<OrganizationServiceFault> ex)
                {
                    tracingService.Trace("Failed to get metadata");
                    throw new InvalidPluginExecutionException("Error: " + ex.Message);
                }
            }
        }
    }
}
