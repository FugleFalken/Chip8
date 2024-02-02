using Microsoft.AspNetCore.SignalR;

namespace Chip8EmuServer
{
    public class Keyboard
    {
        #region "Locks"
        private readonly object keysLock = new object();
        private readonly object latestPressLock = new object();
        #endregion

        #region "Fields"
        private Dictionary<int, bool> keys;
        private (int key, DateTime timeStamp) latestPress;
        #endregion

        public Dictionary<int, bool> Keys 
        {
            get
            {
                lock (keysLock)
                {
                    return keys;
                }
            }
            private set
            {
                lock (keysLock)
                {
                    keys = value;
                }
            }
        }
        public (int key, DateTime timeStamp) LatestPress
        {
            get
            {
                lock (latestPressLock)
                {
                    return latestPress;
                }
            }
            private set
            {
                lock (latestPressLock)
                {
                    latestPress = value;
                }
            }
        }

        public Keyboard()
        {
            Keys = new Dictionary<int, bool>();
            for (int i = 0x0; i <= 0xF; i++)
            {
                Keys.Add(i, false);
            }
        }

        public bool KeyAction((int key, bool isPressed) keyAction)
        {
            if (keyAction.isPressed)
            {
                LatestPress = (keyAction.key, DateTime.Now);
            }
            return Keys[keyAction.key] = keyAction.isPressed;
        }

        public bool IsPressed(int key)
        {
            return Keys[key];
        }

        public int GetNextPress()
        {
            DateTime callTime = DateTime.Now;
            while (true)
            {
                if (LatestPress.timeStamp >= callTime) return LatestPress.key;
            }
        }
    }
}
