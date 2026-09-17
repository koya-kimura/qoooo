using qoooo.Parameters;
using UnityEngine;

namespace qoooo.Midi
{
    /// <summary>現在PageのgridとScene page buttonをParameter状態から描画する。</summary>
    public sealed class ApcMiniMk2LedRenderer : MonoBehaviour
    {
        private const byte Off=0;
        // APC mini mk2 velocity palette: 22=#005900, 29=#00FF55, 3=#FFFFFF.
        // 暗所のVJ操作でもPadの状態を判別しやすい明度を選ぶ。
        private const byte ToggleDim=22;
        private const byte ToggleBright=29;
        private const byte RadioDim=62;
        private const byte RadioBright=60;
        private const byte OneshotDim=6;
        private const byte OneshotBright=5;
        private const byte MomentaryDim=47;
        private const byte MomentaryBright=45;
        private const byte SequenceDim=54;
        private const byte SequenceBright=52;
        private const byte SequenceInactive=55;
        private const byte CurrentPageBright=3;
        [SerializeField] private RtMidiOutput _output;
        [SerializeField] private ApcMiniMk2ParameterController _controller;
        [SerializeField] private MidiParameterRegistry _registry;
        [SerializeField, Range(1f,60f)] private float _refreshRate=15f;
        private float _nextRefresh;
        private readonly byte[] _lastGrid=new byte[64];
        private readonly bool[] _lastTrackButtons=new bool[ApcMiniMk2Constants.FaderButtonCount];
        private readonly bool[] _lastSceneButtons=new bool[ApcMiniMk2Constants.PageCount];
        private bool _singleLedsInvalidated=true;
        private int _lastPage=-1;
        private int _lastConnectionGeneration=-1;
        private void OnEnable() { InvalidateAll(); }
        private void OnDisable() { Clear(); }
        private void OnDestroy() { Clear(); }
        private void LateUpdate()
        {
            if(Time.unscaledTime<_nextRefresh)return; _nextRefresh=Time.unscaledTime+1f/_refreshRate; Draw();
        }
        public void InvalidateAll()
        {
            for(var i=0;i<_lastGrid.Length;i++)_lastGrid[i]=byte.MaxValue;
            _singleLedsInvalidated=true;
            _lastPage=-1;
        }
        public void Clear()
        {
            if(_output==null)return;
            for(var row=0;row<8;row++)for(var col=0;col<8;col++)if(ApcMiniMk2Layout.TryGetNote(row,col,out var note))_output.SendLedOff(note);
            for(var page=0;page<8;page++)_output.SendSingleLed(ApcMiniMk2Constants.SceneLaunchNoteFirst+page,false);
            for(var index=0;index<8;index++)_output.SendSingleLed(ApcMiniMk2Constants.TrackButtonNoteFirst+index,false);
            _output.SendSingleLed(ApcMiniMk2Constants.ShiftNote,false);
        }
        private void Draw()
        {
            if(_output==null||_controller==null||_registry==null)return;
            if (!_output.IsConnected) return;
            if (_lastConnectionGeneration != _output.ConnectionGeneration)
            {
                _lastConnectionGeneration = _output.ConnectionGeneration;
                InvalidateAll();
            }
            var page=_controller.CurrentPage;
            var mapping=_controller.ExportMapping();
            for(var row=0;row<8;row++)
            for(var col=0;col<8;col++)
            {
                var index=row*8+col; var color=GetColor(mapping,page,row,col);
                if(_lastGrid[index]==color && _lastPage==page)continue;
                _lastGrid[index]=color;
                if(ApcMiniMk2Layout.TryGetNote(row,col,out var note)) _output.SendNoteOn(note,color);
            }
            for(var value=0;value<8;value++)
            {
                var isCurrent=value==page;
                if(_singleLedsInvalidated || _lastSceneButtons[value]!=isCurrent || _lastPage!=page)_output.SendSingleLed(ApcMiniMk2Constants.SceneLaunchNoteFirst+value,isCurrent);
                _lastSceneButtons[value]=isCurrent;
            }
            for(var index=0;index<ApcMiniMk2Constants.FaderButtonCount;index++)
            {
                var active=_controller.GetFaderMode(index)!=FaderButtonMode.Normal;
                if(!_singleLedsInvalidated && _lastTrackButtons[index]==active)continue;
                var note=index<8?ApcMiniMk2Constants.TrackButtonNoteFirst+index:ApcMiniMk2Constants.ShiftNote;
                _output.SendSingleLed(note,active); _lastTrackButtons[index]=active;
            }
            if(_lastPage!=page)
            {
                _lastPage=page;
            }
            _singleLedsInvalidated=false;
        }
        private byte GetColor(ApcMidiMappingData mapping,int page,int row,int col)
        {
            var key=ApcMiniMk2Layout.GetCellKey(page,row,col);
            foreach(var binding in mapping.Toggles)
                if(Matches(binding?.Button,key) && _registry.TryGet(binding.ParameterId,out var parameter) && parameter is BoolParameter toggle)return toggle.Value?ToggleBright:ToggleDim;
            foreach(var binding in mapping.Radios)
                if(binding?.OptionButtons != null && _registry.TryGet(binding.ParameterId,out var parameter) && parameter is RadioParameter radio)
                    for(var index=0;index<binding.OptionButtons.Count;index++)if(Matches(binding.OptionButtons[index],key))return radio.Value==index?RadioBright:RadioDim;
            foreach(var binding in mapping.Oneshots)
                if(Matches(binding?.Button,key) && _registry.TryGet(binding.ParameterId,out var parameter) && parameter is OneshotParameter oneshot)return oneshot.Value?OneshotBright:OneshotDim;
            foreach(var binding in mapping.Momentaries)
                if(Matches(binding?.Button,key) && _registry.TryGet(binding.ParameterId,out var parameter) && parameter is MomentaryParameter momentary)return momentary.WasTriggeredThisFrame?MomentaryBright:MomentaryDim;
            foreach(var binding in mapping.States)
                if(Matches(binding?.Button,key) && _registry.TryGet(binding.ParameterId,out var parameter) && parameter is StateParameter state)return StateColor(state.Value);
            foreach(var binding in mapping.Sequences)
                if(binding?.StepButtons != null && _registry.TryGet(binding.ParameterId,out var parameter) && parameter is SequenceParameter sequence)
                    for(var index=0;index<binding.StepButtons.Count;index++)if(Matches(binding.StepButtons[index],key))
                        return sequence.ActiveStep==index ? SequenceBright : (sequence.Steps[index] ? SequenceDim : SequenceInactive);
            return Off;
        }
        private static bool Matches(ApcButtonSlot? slot,int key) => slot.HasValue && slot.Value.IsValid && slot.Value.CellKey==key;
        private static bool Matches(ApcButtonSlot slot,int key) => slot.IsValid && slot.CellKey==key;
        private static byte StateColor(int state)
        {
            // yellow -> green -> blue -> orange -> red -> purple
            return (byte)(state % 6 switch { 0 => 13, 1 => 29, 2 => 45, 3 => 60, 4 => 5, _ => 53 });
        }
    }
}
