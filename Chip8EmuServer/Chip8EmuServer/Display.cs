using System.Drawing;

namespace Chip8EmuServer
{
    public class Display
    {
        #region "Locks"
        private readonly object pixelArrayLock = new object();
        #endregion

        #region "Fields"
        private bool[] pixelArray;
        #endregion

        public int DisplayWidth { get; private set; }
        public int DisplayHeight { get; private set; }
        public bool[] PixelArray
        {
            get
            {
                lock (pixelArrayLock)
                {
                    return pixelArray;
                }
            }
            private set
            {
                lock(pixelArrayLock)
                {
                    pixelArray = value;
                }
            }
        }


        public Display()
        {
            DisplayWidth = 64;
            DisplayHeight = 32;
            PixelArray = new bool[DisplayWidth * DisplayHeight];
        }

        public byte InsertSprite(int x, int y, byte[] sprite)
        {
            int spriteWidth = 8;
            bool pixelWasErased = false;

            for(int i = 0; i < sprite.Length; i++)
            {
                if (y + i > DisplayHeight) y = 0;
                for(int j = 0; j < spriteWidth; j++)
                {
                    if (x + j > DisplayWidth) x = 0;
                    int pixelPos = (DisplayWidth * y + x) + (DisplayWidth * i + j);
                    bool spriteBit = (sprite[i] & (0x80 >> j)) != 0;
                    PixelArray[pixelPos] ^= spriteBit;

                    if (PixelArray[pixelPos] && spriteBit) pixelWasErased = true;
                }
            }

            return Convert.ToByte(pixelWasErased ? 1 : 0);
        }
    }
}
