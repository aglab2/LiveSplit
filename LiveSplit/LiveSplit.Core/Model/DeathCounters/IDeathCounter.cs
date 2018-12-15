using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiveSplit.Model.DeathCounters
{
    public interface IDeathCounter {
        int UpdateDeathDelta();
    }
}
