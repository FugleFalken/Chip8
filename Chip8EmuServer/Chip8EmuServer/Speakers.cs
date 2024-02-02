namespace Chip8EmuServer
{
    public class Speakers
    {
        #region "Locks"
        private readonly object playSoundLock = new object();
        #endregion

        #region "Fields"
        private bool playSound;
        #endregion
        public bool PlaySound 
        {
            get
            {
                lock(playSoundLock)
                {
                    return playSound;
                }
            }
            set
            {
                lock(playSoundLock)
                {
                    playSound = value;
                }
            }
        }
    }
}
