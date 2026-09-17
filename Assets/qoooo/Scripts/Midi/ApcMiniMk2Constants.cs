namespace qoooo.Midi
{
    /// <summary>APC mini mk2 Communication Protocol v1.0 に基づく input 定数。</summary>
    public static class ApcMiniMk2Constants
    {
        public const int MidiPort = 0;
        public const int MidiChannel = 0; // MIDI Channel 1 は Minis では 0

        public const int GridSize = 8;
        public const int GridNoteFirst = 0;
        public const int GridNoteLast = 63;

        public const int TrackButtonNoteFirst = 100;
        public const int TrackButtonNoteLast = 107;
        public const int SceneLaunchNoteFirst = 112;
        public const int SceneLaunchNoteLast = 119;
        public const int ShiftNote = 122;

        public const int FaderCcFirst = 48;
        public const int FaderCcLast = 55;
        public const int MasterFaderCc = 56;
        public const int FaderCount = 9;
        public const int FaderButtonCount = 9;
        public const int PageCount = 8;

        public static bool IsGridNote(int note) => note >= GridNoteFirst && note <= GridNoteLast;
        public static bool IsTrackButtonNote(int note) => note >= TrackButtonNoteFirst && note <= TrackButtonNoteLast;
        public static bool IsSceneLaunchNote(int note) => note >= SceneLaunchNoteFirst && note <= SceneLaunchNoteLast;
        public static bool IsFaderCc(int control) => control >= FaderCcFirst && control <= MasterFaderCc;

        public static bool TryGetFaderIndex(int control, out int index)
        {
            if (!IsFaderCc(control))
            {
                index = -1;
                return false;
            }

            index = control - FaderCcFirst;
            return true;
        }

        public static bool TryGetFaderButtonIndex(int note, out int index)
        {
            if (IsTrackButtonNote(note))
            {
                index = note - TrackButtonNoteFirst;
                return true;
            }

            if (note == ShiftNote)
            {
                index = FaderButtonCount - 1;
                return true;
            }

            index = -1;
            return false;
        }
    }
}
