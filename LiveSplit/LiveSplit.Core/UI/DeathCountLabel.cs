using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace LiveSplit.UI
{
    public sealed class DeathCountLabel : SimpleLabel {

        public Image DeathIcon { get; set; }
        
        public override void Draw(Graphics g)
        {
            if( string.IsNullOrEmpty( Text ) ) {
                return;
            }
            if( DeathIcon == null ) {
                DrawMonospaced( g );
                return;
            }

            var iconProportions = DeathIcon.Width * 1.0 / DeathIcon.Height;
            var iconHeight = (float)( Height * 0.5 );
            var iconWidth = (float)( iconProportions * iconHeight );

            var advance = iconWidth + 6;
            if( Width < advance ) {
                return;
            }
            Width -= advance;
            DrawMonospaced( g );
            Width += advance;
            if( ActualWidth + advance < Width ) {
                g.DrawImage(DeathIcon, X + Width - iconWidth, Y + ( Height - iconHeight) / 2, iconWidth, iconHeight);
                ActualWidth += advance;
            }
        }
    }
}
