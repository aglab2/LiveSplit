using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System;
using LiveSplit.ComponentUtil;
using System.Threading;

namespace LiveSplit.Model.DeathCounters
{
    public class SM64EmulatorDeathCounter : IDeathCounter
    {
        enum State
        {
            INVALIDATED,
            RUNNING,
        };

        private Process process_ = null;
        private State state_ = State.INVALIDATED;

        private IntPtr ptrSaveBuffer_;
        private IntPtr ptrLives_;
        // private IntPtr ptrUnk0_;
        private IntPtr ptrUnk1_;
        private IntPtr ptrLevelScriptStack_;
        private IntPtr ptrCurrDemoInput_;
        private IntPtr ptrMarioStatesAction_;

        private int numLives_ = 0;
        private bool inMenu_ = false;
        private ulong ramPtrBase_ = 0;

        private Timer timer_;
        private Mutex mutex_ = new Mutex();
        private int delta_ = 0;

        public SM64EmulatorDeathCounter()
        {
            timer_ = new System.Threading.Timer(Update, null, 1, Timeout.Infinite);
        }

        ~SM64EmulatorDeathCounter()
        {
            timer_.Change(Timeout.Infinite, Timeout.Infinite);
            WaitHandle handle = new AutoResetEvent(false);
            timer_.Dispose(handle);
            handle.WaitOne();
            mutex_.Dispose();
        }

        public void Reset()
        {
            mutex_.WaitOne();
            delta_ = 0;
            mutex_.ReleaseMutex();
        }

        static readonly string[] ProcessNames = {
            "project64", "project64d",
            "mupen64-rerecording",
            "mupen64-pucrash",
            "mupen64_lua",
            "mupen64-wiivc",
            "mupen64-RTZ",
            "mupen64-rerecording-v2-reset",
            "mupen64-rrv8-avisplit",
            "mupen64-rerecording-v2-reset",
            "mupen64",
            "retroarch" };

        private Process FindEmulatorProcess()
        {
            foreach (string name in ProcessNames)
            {
                Process process = Process.GetProcessesByName(name).Where(p => !p.HasExited).FirstOrDefault();
                if (process != null)
                    return process;
            }
            return null;
        }

        private void Scan()
        {
            state_ = State.INVALIDATED;
            try
            {
                if (!(process_ is object) || process_.HasExited)
                {
                    process_ = FindEmulatorProcess();
                    numLives_ = 0;
                    inMenu_ = false;
                    Reset();
                }

                if (!(process_ is object))
                {
                    return;
                }

                List<long> romPtrBaseSuggestions = new List<long>();
                List<long> ramPtrBaseSuggestions = new List<long>();

                var name = process_.ProcessName.ToLower();
                int offset = 0;

                if (name.Contains("project64") || name.Contains("wine-preloader"))
                {
                    DeepPointer[] ramPtrBaseSuggestionsDPtrs = { new DeepPointer("Project64.exe", 0xD6A1C),     //1.6
                        new DeepPointer("RSP 1.7.dll", 0x4C054), new DeepPointer("RSP 1.7.dll", 0x44B5C),        //2.3.2; 2.4 
                    };

                    DeepPointer[] romPtrBaseSuggestionsDPtrs = { new DeepPointer("Project64.exe", 0xD6A2C),     //1.6
                        new DeepPointer("RSP 1.7.dll", 0x4C050), new DeepPointer("RSP 1.7.dll", 0x44B58)        //2.3.2; 2.4
                    };

                    // Time to generate some addesses for magic check
                    foreach (DeepPointer romSuggestionPtr in romPtrBaseSuggestionsDPtrs)
                    {
                        int ptr = -1;
                        try
                        {
                            ptr = romSuggestionPtr.Deref<int>(process_);
                            romPtrBaseSuggestions.Add(ptr);
                        }
                        catch (Exception)
                        {
                            continue;
                        }
                    }

                    foreach (DeepPointer ramSuggestionPtr in ramPtrBaseSuggestionsDPtrs)
                    {
                        int ptr = -1;
                        try
                        {
                            ptr = ramSuggestionPtr.Deref<int>(process_);
                            ramPtrBaseSuggestions.Add(ptr);
                        }
                        catch (Exception)
                        {
                            continue;
                        }
                    }
                }

                if (name.Contains("mupen64"))
                {
                    if (name == "mupen64")
                    {
                        // Current mupen releases
                        {
                            ramPtrBaseSuggestions.Add(0x00505CB0); // 1.0.9
                            ramPtrBaseSuggestions.Add(0x00505D80); // 1.0.9.1
                            ramPtrBaseSuggestions.Add(0x0050B110); // 1.0.10
                        }
                    }
                    else
                    {
                        // Legacy mupen versions
                        Dictionary<string, int> mupenRAMSuggestions = new Dictionary<string, int>
                    {
                        { "mupen64-rerecording", 0x008EBA80 },
                        { "mupen64-pucrash", 0x00912300 },
                        { "mupen64_lua", 0x00888F60 },
                        { "mupen64-wiivc", 0x00901920 },
                        { "mupen64-RTZ", 0x00901920 },
                        { "mupen64-rrv8-avisplit", 0x008ECBB0 },
                        { "mupen64-rerecording-v2-reset", 0x008ECA90 },
                    };
                        ramPtrBaseSuggestions.Add(mupenRAMSuggestions[name]);
                    }

                    offset = 0x20;
                }

                if (name.Contains("retroarch"))
                {
                    ramPtrBaseSuggestions.Add(0x80000000);
                    romPtrBaseSuggestions.Add(0x90000000);
                    offset = 0x40;
                }

                MagicManager mm = new MagicManager(process_, romPtrBaseSuggestions.ToArray(), ramPtrBaseSuggestions.ToArray(), offset);
                ramPtrBase_ = mm.ramPtrBase;
                ptrSaveBuffer_ = new IntPtr((long)(ramPtrBase_ + 0x207736));
                ptrLives_ = new IntPtr((long)(ramPtrBase_ + 0x33B21E));
                // ptrUnk0_ = new IntPtr((long)(ramPtrBase_ + 0x38BE28));
                ptrLevelScriptStack_ = new IntPtr((long)(ramPtrBase_ + 0x38B8B0));
                ptrUnk1_ = new IntPtr((long)(ramPtrBase_ + 0x38BDBC));
                ptrCurrDemoInput_ = new IntPtr((long)(ramPtrBase_ + 0x32D5F0));
                ptrMarioStatesAction_ = new IntPtr((long)(ramPtrBase_ + 0x33B17C));

                state_ = State.RUNNING;
            }
            catch (Exception)
            {
                state_ = State.INVALIDATED;
            }
        }

        private int GetDeathsCount()
        {
            try
            {
                int ret = 0;
                {
                    process_.ReadValue(ptrSaveBuffer_, out short val);
                    if (val != 17473)
                    {
                        inMenu_ = true;
                        return 0;
                    }
                }

                process_.ReadValue(ptrLives_, out byte num);
                if (num == numLives_ - 1 && !inMenu_)
                {
                    ret++;
                }
                numLives_ = num;

                // process_.ReadValue(ptrUnk0_, out uint num2);
                // process_.ReadValue(new IntPtr((long)(ramPtrBase_ + (num2 & 0x7FFFFFFF))), out uint num3);
                process_.ReadValue(ptrLevelScriptStack_, out uint levelScriptStack);

                bool flag = levelScriptStack == 0x8038BDA8u;
                {
                    process_.ReadValue(ptrUnk1_, out uint num5);
                    if (num5 == 0)
                    {
                        flag = (inMenu_ = true);
                    }
                }

                if (!flag)
                {
                    process_.ReadValue(ptrCurrDemoInput_, out uint num5);
                    flag = num5 != 0;
                }

                if (flag && !inMenu_)
                {
                    process_.ReadValue(ptrMarioStatesAction_, out uint num5);
                    if (num5 != 4903)
                    {
                        ret++;
                    }
                }

                inMenu_ = flag;
                return ret;
            }
            catch (Exception)
            {
                state_ = State.INVALIDATED;
                return 0;
            }
        }

        public void Update(object state)
        {
            if (state_ == State.INVALIDATED || process_.HasExited)
            {
                Scan();
            }

            if (state_ == State.RUNNING)
            {
                int cnt = GetDeathsCount();
                if (0 != cnt)
                {
                    mutex_.WaitOne();
                    delta_ += cnt;
                    mutex_.ReleaseMutex();
                }
            }

            timer_.Change(state_ == State.RUNNING ? 30 : 1000, Timeout.Infinite);
        }

        public int UpdateDeathDelta()
        {
            int cnt;
            {
                mutex_.WaitOne();
                cnt = delta_;
                delta_ = 0;
                mutex_.ReleaseMutex();
            }
            return cnt;
        }
    }
}
