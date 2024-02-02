namespace Chip8EmuServer
{
    public class ReceivePackage
    {
        public Chip8Program? Program { get; set; }
        public (int key, bool isPressed)?  KeyAction { get; set; }



    }
}
