namespace Chip8EmuServer
{
    public class SendPackage
    {
        public bool[] PixelArray { get; private set; }
        public bool PlaySound { get; private set; }
        public int[]? MissingParts { get; set; }

        public SendPackage(bool[] pixelArray, bool playSound, int[]? missingParts)
        {
            PixelArray = pixelArray;
            PlaySound = playSound;
            MissingParts = missingParts;
        }

        public override bool Equals(object? obj)
        {
            return obj is SendPackage package &&
                   EqualityComparer<bool[]>.Default.Equals(PixelArray, package.PixelArray) &&
                   PlaySound == package.PlaySound;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(PixelArray, PlaySound);
        }
    }
}
