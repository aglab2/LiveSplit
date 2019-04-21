using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace LiveSplit.Model.RunSavers
{
    public class SerializeRunSaver : IRunSaver
    {
        public void Save(IRun run, Stream stream, OngoingRun ongoingRun)
        {
            var obj = (IRun)run.Clone();
            foreach (var segment in obj)
            {
                segment.SplitTime = default(Time);
                segment.DeathCount = -1;
            }

            var formatter = new BinaryFormatter();
            formatter.Serialize(stream, obj);
        }
    }
}
