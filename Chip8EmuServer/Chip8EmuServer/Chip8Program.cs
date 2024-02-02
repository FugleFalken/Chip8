namespace Chip8EmuServer
{
    public class Chip8Program
    {
        public int ProgramId { get; set; }
        public int[]? IntArray { get; set; }
        public byte[] Bytes { get => IntArray?.Select(integer => Convert.ToByte(integer)).ToArray(); }
        public int Part { get; set; }
        public int Whole { get; set; }
        public bool Final { get; set; }
    }
}
