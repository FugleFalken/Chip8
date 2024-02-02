using System.Net.WebSockets;
using System.Runtime.Versioning;
using System.Text;
using System.Text.Json;

namespace Chip8EmuServer
{
    public class ConnHandler
    {
        private readonly object missingPartsLock = new();

        private Thread chip8Thread;
        private CancellationTokenSource cts;
        private int[]? missingParts;

        public WebSocket WebSocket { get; private set; }
        public Display Display { get; private set; } = new();
        public Keyboard Keyboard { get; private set; } = new();
        public Speakers Speakers { get; private set; } = new();
        public SendPackage? PrevSendPack { get; private set; } = null;
        public Chip8? Chip8 { get; private set; }
        public ProgramAssembler? ProgramAssembler { get; private set; } = null;
        public int[]? MissingParts
        {
            get
            {
                lock (missingPartsLock)
                {
                    return missingParts;
                }
            }
            private set
            {
                lock (missingPartsLock)
                {
                    missingParts = value;
                }
            }
        }
        public ConnHandler(WebSocket ws)
        {
            WebSocket = ws;
        }

        public async Task Listen()
        {
            byte[] buffer = new byte[1024 * 4];
            while (WebSocket.State == WebSocketState.Open)
            {
                WebSocketReceiveResult result = await WebSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    string json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    Console.WriteLine(json);

                    ReceivePackage rp = JsonSerializer.Deserialize<ReceivePackage>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });


                    if (rp.Program == null)
                    {
                        Keyboard.KeyAction(rp.KeyAction.Value);
                    }
                    else
                    {
                        if (chip8Thread != null && chip8Thread.IsAlive)
                        {
                            cts.Cancel();
                            chip8Thread.Join();
                        }

                        if (ProgramAssembler == null || rp.Program.ProgramId != ProgramAssembler.ProgramId)
                        {
                            ProgramAssembler = new ProgramAssembler(rp.Program.ProgramId, rp.Program.Whole);
                        }

                        ProgramAssembler.AddPart(rp.Program.Part, rp.Program.Bytes);
                        if(rp.Program.Final)
                        {
                            int[] missingParts = ProgramAssembler.GetMissingParts();
                            MissingParts = missingParts;
                            if (missingParts.Length == 0)
                            {
                                Console.WriteLine(JsonSerializer.Serialize(ProgramAssembler.AssembleProgram().Select(b => Convert.ToInt32(b)).ToArray()));
                                Console.WriteLine(ProgramAssembler.AssembleProgram().Length);
                                cts = new CancellationTokenSource();
                                Chip8 = new(ProgramAssembler.AssembleProgram(), Display, Keyboard, Speakers);
                                ProgramAssembler = null;
                                chip8Thread = new Thread(() => Chip8.Run(cts.Token));
                                chip8Thread.Start();
                            }
                        }
                    }
                }
            }
        }

        public async Task Send()
        {
            while (WebSocket.State == WebSocketState.Open)
            {
                SendPackage sendPackage = new(Display.PixelArray, Speakers.PlaySound, MissingParts);
                if ((PrevSendPack != null && !sendPackage.Equals(PrevSendPack)) || MissingParts != null)
                {
                    MissingParts = null;
                    PrevSendPack = sendPackage;
                    string apJson = JsonSerializer.Serialize(sendPackage);
                    byte[] bytes = Encoding.UTF8.GetBytes(apJson);
                    ArraySegment<byte> arraySegment = new ArraySegment<byte>(bytes, 0, bytes.Length);
                    await WebSocket.SendAsync(arraySegment, WebSocketMessageType.Text, true, CancellationToken.None);
                }
            }
        }
    }
}
