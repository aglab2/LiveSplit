﻿using LiveSplit.Model.Input;
using System;

namespace LiveSplit.Model
{
    public interface ITimerModel
    {
        LiveSplitState CurrentState { get; set; }

        event EventHandler OnSplit;
        event EventHandler OnUndoSplit;
        event EventHandler OnSkipSplit;
        event EventHandler OnStart;
        event EventHandlerT<TimerPhase> OnReset;
        event EventHandler OnPause;
        event EventHandler OnUndoAllPauses;
        event EventHandler OnResume;
        event EventHandler OnScrollUp;
        event EventHandler OnScrollDown;
        event EventHandler OnSwitchComparisonPrevious;
        event EventHandler OnSwitchComparisonNext;
        
        void Start();
        void LoadFrozenRun();
        void InitializeGameTime();
        void Split();
        void SkipSplit();
        void UndoSplit();
        void AddDeaths( int addCount );
        void Reset();
        void Reset(bool updateSplits);
        void Pause();
        void UndoAllPauses();
        void ScrollUp();
        void ScrollDown();
        void SwitchComparisonPrevious();
        void SwitchComparisonNext();
    }
}
