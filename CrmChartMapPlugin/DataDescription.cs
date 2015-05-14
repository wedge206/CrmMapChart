using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Microsoft.Xrm.Sdk;

namespace CrmChartMap.CrmChartMapPlugin
{
	[DataContract]
	public class DataDescription
	{
		[DataMember]
		public string NameField;
		[DataMember]
		public string EntitySchemaName;
		[DataMember]
		public string LatitudeField;
		[DataMember]
		public string LongitudeField;
		[DataMember]
		public string LatitudeSchemaName;
		[DataMember]
		public string LongitudeSchemaName;

		[DataMember]
		public string AddressField;
		[DataMember]
		public string CityField;
		[DataMember]
		public string StateField;
		[DataMember]
		public string CountryField;
		[DataMember]
		public string PostCodeField;
		[DataMember]
		public string NumericField;
		[DataMember]
		public string fetchXML;
		[DataMember]
		public string BingMapScriptUrl;
		[DataMember]
		public decimal CenterLat;
		[DataMember]
		public decimal CenterLong;
		[DataMember]
		public int CenterZoom;

		public DataDescription() { }

		public DataDescription(Entity entity)
		{
			EntitySchemaName = entity.GetAttributeValue<string>("dd_entityschemaname");
			LatitudeField = entity.GetAttributeValue<string>("dd_latitudefield");
			LongitudeField = entity.GetAttributeValue<string>("dd_longitudefield");
			LatitudeSchemaName = entity.GetAttributeValue<string>("dd_latitudeschemaname");
			LongitudeSchemaName = entity.GetAttributeValue<string>("dd_longitudeschemaname");
			NameField = entity.GetAttributeValue<string>("dd_namefield");
			AddressField = entity.GetAttributeValue<string>("dd_addressfield");
			CityField = entity.GetAttributeValue<string>("dd_cityfield");
			StateField = entity.GetAttributeValue<string>("dd_stateprovincefield");
			CountryField = entity.GetAttributeValue<string>("dd_countryfield");
			PostCodeField = entity.GetAttributeValue<string>("dd_postalcodefield");
			NumericField = entity.GetAttributeValue<string>("dd_numericfield");
			BingMapScriptUrl = String.Format("ecn.dev.virtualearth.net/mapcontrol/mapcontrol.ashx?v=7.0&mkt={0}", LangCodes[entity.GetAttributeValue<OptionSetValue>("dd_language").Value]);
			CenterLat = entity.GetAttributeValue<decimal>("dd_latitude");
			CenterLong = entity.GetAttributeValue<decimal>("dd_longitude");
			CenterZoom = entity.GetAttributeValue<int>("dd_zoom");
			fetchXML = BuildFetchXML(entity);
		}

		public static string BuildFetchXML(Entity configEntity)
		{
			List<string> fieldList = new List<string>() { "dd_namefield", "dd_cityfield", "dd_addressfield", "dd_postalcodefield", "dd_stateprovincefield", "dd_countryfield", "dd_numericfield", "dd_latitudefield", "dd_longitudefield" };
			List<attribute> attributeList = new List<attribute>();

			foreach (string value in fieldList.Where(f => !String.IsNullOrWhiteSpace(configEntity.GetAttributeValue<string>(f))))
			{
				string fieldValue = configEntity.GetAttributeValue<string>(value);

				if (fieldValue.Contains("."))
				{
					string[] values = fieldValue.Split('.');
					attributeList.Add(new attribute(values[1], values[0], configEntity.GetAttributeValue<string>(value + "linkentity")));
				}
				else
				{
					attributeList.Add(new attribute(fieldValue));
				}
			}

			string attributeXML = "";

			foreach (var group in attributeList.GroupBy(a => new { a.lookupname, a.linkentity }))
			{
				if (!String.IsNullOrWhiteSpace(group.Key.linkentity))
				{
					if (group.Key.lookupname == "parentaccountid" || group.Key.lookupname == "parentcontactid")
						attributeXML += String.Format("<link-entity name=\"{0}\" from=\"{0}id\" to=\"customerid\" link-type=\"outer\" alias=\"{1}\">", group.Key.linkentity, group.Key.lookupname);
					else
						attributeXML += String.Format("<link-entity name=\"{0}\" to=\"{1}\" link-type=\"outer\" alias=\"{1}\">", group.Key.linkentity, group.Key.lookupname);
				}

				foreach (attribute att in group)
					attributeXML += String.Format("<attribute name=\"{0}\" />", att.fieldname);

				if (!String.IsNullOrWhiteSpace(group.Key.linkentity))
					attributeXML += "</link-entity>";
			}

			return String.Format("<fetch version=\"1.0\" output-format=\"xml-platform\" mapping=\"logical\" distinct=\"false\" count=\"{1}\" page=\"1\"><entity name=\"{2}\">{0}<order/><filter/></entity></fetch>", attributeXML, configEntity.GetAttributeValue<bool>("dd_showallrecords") ? "5000" : "250", configEntity.GetAttributeValue<string>("dd_entity")); ;
		}

		private class attribute
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

		internal static Dictionary<int, string> LangCodes = new Dictionary<int, string>()		
		{
			{ 899300000, "cs-CZ" },
			{ 899300001, "da-DK" },
			{ 899300002, "nl-BE"},
			{ 899300003, "nl-NL"},
			{ 899300004, "en-AU"},
			{ 899300005, "en-CA"},
			{ 899300006, "en-IN"},
			{ 899300007, "en-GB"},
			{ 899300008, "en-US"},
			{ 899300009, "fi-FI"},
			{ 899300010, "fr-BE"},
			{ 899300011, "fr-CA"},
			{ 899300012, "fr-CH" },
			{ 899300013, "fr-FR" },
			{ 899300014, "de-DE" },
			{ 899300015, "ja-JP" },
			{ 899300016, "Ko-KR" },
			{ 899300017, "nb-NO" },
			{ 899300018, "pl-PL" },
			{ 899300019, "pt-BR" },
			{ 899300020, "pt-PT" },
			{ 899300021, "ru-RU" },
			{ 899300022, "es-MX" },
			{ 899300023, "es-ES" },
			{ 899300024, "es-US" },
			{ 899300025, "sv-SE" },
			{ 899300026, "zh-HK" },
			{ 899300027, "zh-TW" }
		};
	}
}
