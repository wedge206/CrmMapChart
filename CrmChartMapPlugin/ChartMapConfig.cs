using System;
using System.Runtime.Serialization;
using System.Text;
using Microsoft.Xrm.Sdk;

// NOTE: This class is legacy - only used for upgrading from pre-3.0
namespace CrmChartMap.CrmChartMapPlugin
{
	[DataContract]
	public class ChartMapConfig
	{
		[DataMember]
		public string BingKey;   // This can be stored in CRM Organization table
		[DataMember]
		public bool Published;  // we can detect if defaults are created without using this flag
		[DataMember]
		public int Zoom;  // This can be map specifc
		[DataMember]
		public decimal CenterLat;  // map specific
		[DataMember]
		public decimal CenterLong;  // map specific
		[DataMember]
		public string Lang;  // should be org wide setting
		[DataMember]
		public string SSL;  // can be detected onload
		[DataMember]
		public bool EnableClustering;  // No longer used
		[DataMember]
		public int ClusterRadius;  // No longer used
		[DataMember]
		public Guid ConfigId;  // this is its own webresourceid in crm - not needed if we eliminate the rest of this
		[DataMember]
		public string bingMapScriptUrl;  // can be built on load
		[DataMember]
		public int configVersion;  // if we eliminate config then this not needed

		public string makeContent()
		{
			return String.Format("var configJSON = {0};", this.ToJSON()).ToBase64String();
		}

		public static ChartMapConfig defaultConfig()
		{
			return defaultConfig(Guid.Empty);
		}

		public static ChartMapConfig defaultConfig(Guid Id)
		{
			return new ChartMapConfig()
			{
				BingKey = "",
				Published = true,
				Zoom = 3,
				CenterLat = 56.130366m,
				CenterLong = -106.346771m,
				Lang = "en-CA",
				SSL = "1",
				EnableClustering = true,
				ClusterRadius = 30,
				ConfigId = Id,
				bingMapScriptUrl = "https://ecn.dev.virtualearth.net/mapcontrol/mapcontrol.ashx?v=7.0&mkt=en-CA&s=1",
				configVersion = 2
			};
		}

		public static ChartMapConfig getConfig(Entity configEntity)
		{
			byte[] rawContent = Convert.FromBase64String(configEntity.GetAttributeValue<string>("content"));

			return Encoding.UTF8.GetString(rawContent).Remove(0, 17).TrimStart('\'').TrimEnd('\'',';').ParseJSON<ChartMapConfig>();
		}
	}
}
