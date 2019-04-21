using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace LiveSplit.Model
{
    public class OngoingRun
    {
        public List<int> SplitDeaths { get; set; } = new List<int>();
        public List<Time> SplitEndTimes { get; set; } = new List<Time>();

        public XmlNode ToXml(XmlDocument document)
        {
            var run = document.CreateElement("OngoingRun");

            for( int i = 0; i < SplitDeaths.Count; i++ )
            {
                var splitDeaths = document.CreateAttribute("deathCount");
                splitDeaths.InnerText = SplitDeaths[i].ToString();
                var timeNode = SplitEndTimes[i].ToXml( document );

                var splitChild = document.CreateElement("Split");
                splitChild.AppendChild(timeNode);
                splitChild.Attributes.Append(splitDeaths);
                run.AppendChild(splitChild);
            }
            return run;
        }

        public static OngoingRun ParseXml( XmlNode node )
        {
            OngoingRun result = new OngoingRun();

            var size = node.ChildNodes.Count;
            for( int i = 0; i < size; i++ )
            {
                var child = node.ChildNodes[i];
                var deaths = int.Parse( child.Attributes["deathCount"].InnerText );
                var time = Time.FromXml( child["Time"] );
                result.SplitDeaths.Add( deaths );
                result.SplitEndTimes.Add( time );
            }
            return result;
        }
    }
}
