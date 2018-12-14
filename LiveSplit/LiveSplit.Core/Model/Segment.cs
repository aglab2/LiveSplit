using LiveSplit.Model.Comparisons;
using System;
using System.Drawing;

namespace LiveSplit.Model
{
    [Serializable]
    public class Segment : ISegment
    {
        public Image Icon { get; set; }
        public string Name { get; set; }
        public Time PersonalBestSplitTime
        {
            get { return Comparisons[Run.PersonalBestComparisonName]; }
            set { Comparisons[Run.PersonalBestComparisonName] = value; }
        }
        public IComparisons Comparisons { get; set; }
        public Time BestSegmentTime { get; set; }
        public Time SplitTime { get; set; }
        public int DeathCount { get; set; }
        public int BestDeathCount { get; set; }
        public int PersonalBestDeathCount { get; set; }
        public SegmentHistory SegmentHistory { get; set;}
        
        public Segment( string name, Time pbSplitTime = default(Time),
            Time bestSegmentTime = default(Time), Image icon = null,
            Time splitTime = default(Time), int deathCount = -1, int bestDeathCount = -1, int pbDeathCount = -1)
        {
            Comparisons = new CompositeComparisons();
            Name = name;
            PersonalBestSplitTime = pbSplitTime;
            BestSegmentTime = bestSegmentTime;
            SplitTime = splitTime;
            DeathCount = deathCount;
            BestDeathCount = bestDeathCount;
            PersonalBestDeathCount = pbDeathCount;
            Icon = null;
            SegmentHistory = new SegmentHistory();
        }

        public Segment Clone()
        {
            var newSegmentHistory = SegmentHistory.Clone();

            return new Segment(Name)
            {
                BestSegmentTime = BestSegmentTime,
                SplitTime = SplitTime,
                Icon = Icon,
                DeathCount = DeathCount,
                BestDeathCount = BestDeathCount,
                PersonalBestDeathCount = PersonalBestDeathCount,
                SegmentHistory = newSegmentHistory,
                Comparisons = (IComparisons)Comparisons.Clone()
            };
        }

        object ICloneable.Clone() => Clone();
    }
}
