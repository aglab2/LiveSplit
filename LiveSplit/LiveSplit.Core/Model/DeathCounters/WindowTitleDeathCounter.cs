using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace LiveSplit.Model.DeathCounters
{
    public class WindowTitleDeathCounter : IDeathCounter {

        private int prevDeathCount = -1;
        private IntPtr prevActiveWindow = IntPtr.Zero;

        public void Reset()
        {
            prevDeathCount = -1;
            prevActiveWindow = IntPtr.Zero;
        }

        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();
        public int UpdateDeathDelta()
        {
            IntPtr activeWindow = GetForegroundWindow();
            var newDeathCount = GetWindowDeathCount( activeWindow );
            if( newDeathCount == -1 ) {
                return 0;
            }
            if( prevDeathCount == -1 || activeWindow != prevActiveWindow ) {
                prevDeathCount = newDeathCount;
                prevActiveWindow = activeWindow;
                return 0;
            }

            var delta = Math.Max( newDeathCount - prevDeathCount, 0 );
            prevDeathCount = newDeathCount;
            return delta;
        }



        [DllImport("user32.dll")]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        // Return a death count value associated with the given window.
        // -1 is returned if no death count is found.
        static private StringBuilder charBuffer = new StringBuilder(512);
        static public int GetWindowDeathCount( IntPtr handle )
        {
            charBuffer.Clear();
            if (GetWindowText(handle, charBuffer, 512) > 0) {
                return ParseWindowTitle(charBuffer.ToString());
            }
            return -1;
        }

        static private string deathSuffix = "eath";
        static private int ParseWindowTitle(string titleStr)
        {
            var deathSuffixPos = titleStr.IndexOf(deathSuffix);
            if (deathSuffixPos == -1) {
                return -1;
            }
            deathSuffixPos += deathSuffix.Length;
            if (deathSuffixPos >= titleStr.Length) {
                return -1;
            }
            while (!Char.IsDigit(titleStr[deathSuffixPos]) && titleStr[deathSuffixPos] != '[') {
                deathSuffixPos++;
                if (deathSuffixPos >= titleStr.Length) {
                    return -1;
                }
            }

            if (titleStr[deathSuffixPos] == '[') {
                var bracketEndPos = titleStr.IndexOf(']', deathSuffixPos + 1);
                if (bracketEndPos == -1) {
                    return ParseStringPrefix(titleStr, deathSuffixPos + 1);
                }
                bracketEndPos = SkipWhitespace(titleStr, bracketEndPos + 1);
                if (bracketEndPos >= titleStr.Length) {
                    return ParseStringPrefix(titleStr, deathSuffixPos + 1);
                }
                if (titleStr[bracketEndPos] != ':') {
                    return ParseStringPrefix(titleStr, deathSuffixPos + 1);
                } else {
                    return ParseStringPrefix(titleStr, bracketEndPos + 1);
                }
            } else {
                return ParseStringPrefix(titleStr, deathSuffixPos);
            }
        }

        static private int SkipWhitespace(string str, int startPos)
        {
            while (startPos < str.Length && Char.IsWhiteSpace(str[startPos])) {
                startPos++;
            }
            return startPos;
        }

        static private int ParseStringPrefix(string str, int startPos)
        {
            startPos = SkipWhitespace(str, startPos);
            var currentPos = startPos;
            while (currentPos < str.Length && Char.IsDigit(str[currentPos])) {
                currentPos++;
            }
            var substr = str.Substring(startPos, currentPos - startPos);
            int parseResult;
            var success = int.TryParse(substr, out parseResult);
            return success ? parseResult : -1;
        }
    }
}

