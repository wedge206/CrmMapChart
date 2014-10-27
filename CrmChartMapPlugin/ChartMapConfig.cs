using System;
using System.Runtime.Serialization;
using System.Text;
using Microsoft.Xrm.Sdk;

namespace CrmChartMap.CrmChartMapPlugin
{
	[DataContract]
	public class ChartMapConfig
	{
		[DataMember]
		public string BingKey;
		[DataMember]
		public bool Published;
		[DataMember]
		public int Zoom;
		[DataMember]
		public decimal CenterLat;
		[DataMember]
		public decimal CenterLong;
		[DataMember]
		public string Lang;
		[DataMember]
		public string SSL;
		[DataMember]
		public bool EnableClustering;  // No longer used
		[DataMember]
		public int ClusterRadius;  // No longer used
		[DataMember]
		public Guid ConfigId;
		[DataMember]
		public string bingMapScriptUrl;
		[DataMember]
		public int configVersion;

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
