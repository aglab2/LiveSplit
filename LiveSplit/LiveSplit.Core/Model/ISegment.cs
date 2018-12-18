using LiveSplit.Model.Comparisons;
using System;
using System.Drawing;

namespace LiveSplit.Model
{
    public interface ISegment
    {
        Image Icon { get; set; }
        string Name { get; set; }
        Time PersonalBestSplitTime { get; set; }
        IComparisons Comparisons { get; set; }
        Time BestSegmentTime { get; set; }
        Time SplitTime { get; set; }
        int DeathCount { get; set; }
        int BestDeathCount { get; set; }
        int PersonalBestDeathCount { get; set; }
        SegmentHistory SegmentHistory { get; set; }
        ISegment Parent { get; set; }

        ISegment CopySegment();
    }
}
