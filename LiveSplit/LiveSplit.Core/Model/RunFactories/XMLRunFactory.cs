﻿using LiveSplit.Model.Comparisons;
using System;
using System.IO;
using System.Linq;
using System.Xml;
using static LiveSplit.UI.SettingsHelper;
using static LiveSplit.Model.IndexedTimeHelper;

namespace LiveSplit.Model.RunFactories
{
    public class XMLRunFactory : IRunFactory
    {
        public Stream Stream { get; set; }
        public string FilePath { get; set; }

        public XMLRunFactory(Stream stream = null, string filePath = null)
        {
            Stream = stream;
            FilePath = filePath;
        }

        private static void ParseAttemptHistory(Version version, XmlElement parent, IRun run, out int maxAttemptIndex )
        {
            maxAttemptIndex = 1;
            if (version >= new Version(1, 5, 0))
            {
                var attemptHistory = parent["AttemptHistory"];
                foreach (var attemptNode in attemptHistory.GetElementsByTagName("Attempt"))
                {
                    var attempt = Attempt.ParseXml(attemptNode as XmlElement);
                    maxAttemptIndex = Math.Max(maxAttemptIndex, attempt.Index);
                    run.AttemptHistory.Add(attempt);
                }
            }
            else if (version >= new Version(1, 4, 1))
            {
                var runHistory = parent["RunHistory"];
                foreach (var runHistoryNode in runHistory.GetElementsByTagName("Time"))
                {
                    var indexedTime = ParseXml(runHistoryNode as XmlElement);
                    maxAttemptIndex = Math.Max( maxAttemptIndex, indexedTime.Index );
                    var attempt = new Attempt(indexedTime.Index, indexedTime.Time, null, null, null);
                    run.AttemptHistory.Add(attempt);
                }
            }
            else
            {
                var runHistory = parent["RunHistory"];
                foreach (var runHistoryNode in runHistory.GetElementsByTagName("Time"))
                {
                    var indexedTime = ParseXmlOld(runHistoryNode as XmlElement);
                    maxAttemptIndex = Math.Max(maxAttemptIndex, indexedTime.Index);
                    var attempt = new Attempt(indexedTime.Index, indexedTime.Time, null, null, null);
                    run.AttemptHistory.Add(attempt);
                }
            }
        }

        public IRun Create(IComparisonGeneratorsFactory factory)
        {
            var document = new XmlDocument();
            document.Load(Stream);

            var run = new Run(factory);
            var parent = document["Run"];
            var version = ParseAttributeVersion(parent);

            if (version >= new Version(1, 6))
            {
                var metadata = parent["Metadata"];
                run.Metadata.RunID = metadata["Run"].GetAttribute("id");
                run.Metadata.PlatformName = metadata["Platform"].InnerText;
                run.Metadata.UsesEmulator = bool.Parse(metadata["Platform"].GetAttribute("usesEmulator"));
                run.Metadata.RegionName = metadata["Region"].InnerText;
                foreach (var variableNode in metadata["Variables"].ChildNodes.OfType<XmlElement>())
                {
                    run.Metadata.VariableValueNames.Add(variableNode.GetAttribute("name"), variableNode.InnerText);
                }
            }

            run.GameIcon = GetImageFromElement(parent["GameIcon"]);
            run.GameName = ParseString(parent["GameName"]);
            run.CategoryName = ParseString(parent["CategoryName"]);
            run.Offset = ParseTimeSpan(parent["Offset"]);
            run.AttemptCount = ParseInt(parent["AttemptCount"]);
            run.BestDeathCount = ParseInt(parent["BestDeathCount"], -1);

            int maxAttemptIndex;
            ParseAttemptHistory(version, parent, run, out maxAttemptIndex);

            var segmentsNode = parent["Segments"];

            Segment currentParent = null;
            foreach (var segmentNode in segmentsNode.GetElementsByTagName("Segment"))
            {
                var segmentElement = segmentNode as XmlElement;

                Segment split;
                var splitName = ParseString(segmentElement["Name"]);
                if( splitName.StartsWith( "-" ) ) {
                    if( currentParent == null ) {
                        currentParent = new Segment("");
                    }
                    split = new Segment( splitName );
                    split.Parent = currentParent;
                } else if( currentParent != null ) {
                    split = currentParent;
                    split.Name = splitName;
                    currentParent = null;
                } else {
                    split = new Segment( splitName );
                }
                split.Icon = GetImageFromElement(segmentElement["Icon"]);

                if (version >= new Version(1, 3))
                {
                    var splitTimes = segmentElement["SplitTimes"];
                    foreach (var comparisonNode in splitTimes.GetElementsByTagName("SplitTime"))
                    {
                        var comparisonElement = comparisonNode as XmlElement;
                        var comparisonName = comparisonElement.GetAttribute("name");
                        if (comparisonElement.InnerText.Length > 0)
                        {
                            split.Comparisons[comparisonName] = version >= new Version(1, 4, 1) ? Time.FromXml(comparisonElement) : Time.ParseText(comparisonElement.InnerText);
                        }
                        if (!run.CustomComparisons.Contains(comparisonName))
                            run.CustomComparisons.Add(comparisonName);
                    }
                }
                else
                {
                    var pbSplit = segmentElement["PersonalBestSplitTime"];
                    if (pbSplit.InnerText.Length > 0)
                    {
                        split.Comparisons[Run.PersonalBestComparisonName] = version >= new Version(1, 4, 1) ? Time.FromXml(pbSplit) : Time.ParseText(pbSplit.InnerText);
                    }
                }

                var goldSplit = segmentElement["BestSegmentTime"];
                if (goldSplit.InnerText.Length > 0)
                {
                    split.BestSegmentTime = version >= new Version(1, 4, 1) ? Time.FromXml(goldSplit) : Time.ParseText(goldSplit.InnerText);
                }

                var bestDeaths = segmentElement["BestDeathCount"];
                if( bestDeaths != null && bestDeaths.InnerText.Length > 0 ) {
                    split.BestDeathCount = int.Parse( bestDeaths.InnerText );
                }
                var pbDeaths = segmentElement["PersonalBestDeathCount"];
                if( pbDeaths != null && pbDeaths.InnerText.Length > 0 ) {
                    split.PersonalBestDeathCount = int.Parse(pbDeaths.InnerText);
                }

                var history = segmentElement["SegmentHistory"];
                foreach (var historyNode in history.GetElementsByTagName("Time"))
                {
                    var node = historyNode as XmlElement;
                    IIndexedTime indexedTime;
                    if (version >= new Version(1, 4, 1)) { 
                        indexedTime = ParseXml(node);
                    } else {
                        indexedTime = ParseXmlOld(node);
                    }
                    if( indexedTime.Index <= maxAttemptIndex && !split.SegmentHistory.ContainsKey(indexedTime.Index)) { 
                        split.SegmentHistory.Add(indexedTime.Index, indexedTime.Time);
                    }
                }

                run.Add(split);
            }

            if( currentParent != null ) {
                run.Add( currentParent );
            }

            if (version >= new Version(1, 4, 2))
            {
                var newXmlDoc = new XmlDocument();
                newXmlDoc.InnerXml = parent["AutoSplitterSettings"].OuterXml;
                run.AutoSplitterSettings = newXmlDoc.FirstChild as XmlElement;
                run.AutoSplitterSettings.Attributes.Append(ToAttribute(newXmlDoc, "gameName", run.GameName));
            }

            if (!string.IsNullOrEmpty(FilePath))
                run.FilePath = FilePath;

            var ongoingNode = parent["OngoingRun"];
            if( ongoingNode != null )
            {
                run.FrozenRun = OngoingRun.ParseXml( ongoingNode );
            }

            return run;
        }
    }
}
