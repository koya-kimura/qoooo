namespace qoooo.Midi
{
    public static class ApcMiniMk2Layout
    {
        public static bool TryGetGridPosition(int note, out int row, out int col)
        {
            if (!ApcMiniMk2Constants.IsGridNote(note))
            {
                row = -1;
                col = -1;
                return false;
            }

            row = ApcMiniMk2Constants.GridSize - 1 - note / ApcMiniMk2Constants.GridSize;
            col = note % ApcMiniMk2Constants.GridSize;
            return true;
        }

        public static bool TryGetNote(int row, int col, out int note)
        {
            if (row < 0 || row >= ApcMiniMk2Constants.GridSize || col < 0 || col >= ApcMiniMk2Constants.GridSize)
            {
                note = -1;
                return false;
            }

            note = (ApcMiniMk2Constants.GridSize - 1 - row) * ApcMiniMk2Constants.GridSize + col;
            return true;
        }

        public static bool IsValidCell(int page, int row, int col)
            => page >= 0 && page < ApcMiniMk2Constants.PageCount
                && row >= 0 && row < ApcMiniMk2Constants.GridSize
                && col >= 0 && col < ApcMiniMk2Constants.GridSize;

        public static int GetCellKey(int page, int row, int col)
            => page * ApcMiniMk2Constants.GridSize * ApcMiniMk2Constants.GridSize
                + row * ApcMiniMk2Constants.GridSize + col;
    }
}
