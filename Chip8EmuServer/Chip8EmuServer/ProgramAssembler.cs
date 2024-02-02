namespace Chip8EmuServer
{
    public class ProgramAssembler
    {
        public Dictionary<int, byte[]> ScrambledProgram { get; set; }
        public int TotalParts { get; set; }
        public int ProgramId { get; set; }

        public ProgramAssembler(int programId, int totalParts)
        {
            TotalParts = totalParts;
            ProgramId = programId;
            ScrambledProgram = new Dictionary<int, byte[]>();
        }

        public void AddPart(int index, byte[] data)
        {
            if(!ScrambledProgram.ContainsKey(index))
            {
                ScrambledProgram.Add(index, data);
            }
        }


        public byte[] AssembleProgram()
        {
            return ScrambledProgram.OrderBy(part => part.Key).SelectMany(part => part.Value).ToArray();
        }

        public int[] GetMissingParts()
        {
            int[] missingParts = new int[TotalParts - ScrambledProgram.Count()];
            int missingPartsPointer = 0;
            if(missingParts.Length != 0)
            {
                for(int i = 1; i <= TotalParts; i++)
                {
                    if (!ScrambledProgram.ContainsKey(i))
                    {
                        missingParts[missingPartsPointer++] = i;
                    }
                }
            }
            return missingParts;
        }
    }
}
