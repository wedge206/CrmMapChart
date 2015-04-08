using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Microsoft.Xrm.Sdk;
using System.Runtime.Serialization;

namespace CrmChartMap.CrmChartMapPlugin
{
	[DataContract]
	public class PresentationDescription
	{
		// General Attributes
		[DataMember]
		public int MapType;
		[DataMember]
		public bool ShowAllRecords;
		[DataMember]
		public bool EnableCaching;

		// Pin Map Attributes
		[DataMember]
		public bool EnableClustering;
		[DataMember]
		public int ClusterRadius;
		[DataMember]
		public string ClusterTitle;

		// Heat Map Attributes
		[DataMember]
		public int HeatMapType;
		[DataMember]
		public decimal Intensity;
		[DataMember]
		public int IntensityRange;
		[DataMember]
		public int IntensityCalculation;
		[DataMember]
		public decimal Deviations;
		[DataMember]
		public decimal MaxValue;
		[DataMember]
		public decimal MinValue;
		[DataMember]
		public int Radius;
		[DataMember]
		public string RadiusUnits;
		[DataMember]
		public bool DynamicRadius;
		[DataMember]
		public string Colour1;
		[DataMember]
		public string Colour2;
		[DataMember]
		public string Colour3;
		[DataMember]
		public string Colour4;
		[DataMember]
		public string Colour5;

		public PresentationDescription() { }

		public PresentationDescription(Entity entity)
		{
			MapType = entity.GetAttributeValue<OptionSetValue>("dd_maptype").Value;
			ShowAllRecords = entity.GetAttributeValue<bool>("dd_showallrecords");
			EnableCaching = entity.GetAttributeValue<bool>("dd_enablecaching");

			EnableClustering = entity.GetAttributeValue<bool>("dd_enableclustering");
			ClusterRadius = entity.GetAttributeValue<int>("dd_clusterradius");
			ClusterTitle = entity.GetAttributeValue<string>("dd_clustername");

			HeatMapType = entity.GetAttributeValue<OptionSetValue>("dd_heatmapbasedon").Value;
			Intensity = entity.GetAttributeValue<decimal>("dd_intensity");
			IntensityRange = entity.GetAttributeValue<OptionSetValue>("dd_intensityrange").Value;
			IntensityCalculation = entity.GetAttributeValue<OptionSetValue>("dd_intensitycalculation").Value;
			Deviations = entity.GetAttributeValue<decimal>("dd_deviations");
			MaxValue = entity.GetAttributeValue<decimal>("dd_maximumvalue");
			MinValue = entity.GetAttributeValue<decimal>("dd_minimumvalue");
			Radius = entity.GetAttributeValue<int>("dd_radius");
			RadiusUnits = entity.GetAttributeValue<bool>("dd_RadiusUnits") ? Units.meters.ToString() : Units.pixels.ToString();
			DynamicRadius = entity.GetAttributeValue<bool>("dd_usedynamicradius");
			Colour1 = makeColor(entity.GetAttributeValue<OptionSetValue>("dd_colourstop1") != null ? entity.GetAttributeValue<OptionSetValue>("dd_colourstop1").Value : 1128000128, 10);
			Colour2 = makeColor(entity.GetAttributeValue<OptionSetValue>("dd_colourstop2") != null ? entity.GetAttributeValue<OptionSetValue>("dd_colourstop2").Value : 1000000255, 40);
			Colour3 = makeColor(entity.GetAttributeValue<OptionSetValue>("dd_colourstop3") != null ? entity.GetAttributeValue<OptionSetValue>("dd_colourstop3").Value : 1000128000, 80);
			Colour4 = makeColor(entity.GetAttributeValue<OptionSetValue>("dd_colourstop4") != null ? entity.GetAttributeValue<OptionSetValue>("dd_colourstop4").Value : 1255255000, 120);
			Colour5 = makeColor(entity.GetAttributeValue<OptionSetValue>("dd_colourstop5") != null ? entity.GetAttributeValue<OptionSetValue>("dd_colourstop5").Value : 1255000000, 150);
		}

		private static string makeColor(int value, int a = 255)
		{
			string r = value.ToString().Substring(1, 3);
			string g = value.ToString().Substring(4, 3);
			string b = value.ToString().Substring(7, 3);

			return String.Format("rgba({0},{1},{2},{3})", r, g, b, a);
		}

		public enum Units
		{
			meters,
			pixels
		}
	}
}
