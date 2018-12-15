using System.Drawing;

namespace LiveSplit.Options.SettingsFactories
{
    public interface ISettingsFactory
    {
        ISettings Create( Bitmap defaultDeathIcon );
    }
}
