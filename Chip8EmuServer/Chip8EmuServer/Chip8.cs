using Chip8EmuServer.structs;
using Microsoft.AspNetCore.Authentication;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Xml;
using System.Net.WebSockets;
using Microsoft.Extensions.Configuration.Ini;
using System;
using System.Reflection.Emit;

namespace Chip8EmuServer
{
    public class Chip8
    {
        #region "Memory"
        byte[] ram = new byte[4096];
        #endregion

        #region "Registers"
        byte[] v = new byte[16];

        ushort i = 0;

        byte delay = 0;

        byte sound = 0;

        ushort pc = 0x200;

        byte sp;

        ushort[] stack = new ushort[16];
        #endregion

        #region "External hardware"
        public Display Display { get; set; }
        public Keyboard Keyboard { get; set; }
        public Speakers Speakers { get; set; }
        #endregion

        public Chip8(byte[] program, Display display, Keyboard keyboard, Speakers speakers)
        {
            byte[] sprites = {
                0xF0, 0x90, 0x90, 0x90, 0xF0,
                0x20, 0x60, 0x20, 0x20, 0x70,
                0xF0, 0x10, 0xF0, 0x80, 0xF0,
                0xF0, 0x10, 0xF0, 0x10, 0xF0,
                0x90, 0x90, 0xF0, 0x10, 0x10,
                0xF0, 0x80, 0xF0, 0x10, 0xF0,
                0xF0, 0x80, 0xF0, 0x90, 0xF0,
                0xF0, 0x10, 0x20, 0x40, 0x40,
                0xF0, 0x90, 0xF0, 0x90, 0xF0,
                0xF0, 0x90, 0xF0, 0x10, 0xF0,
                0xF0, 0x90, 0xF0, 0x90, 0x90,
                0xE0, 0x90, 0xE0, 0x90, 0xE0,
                0xF0, 0x80, 0x80, 0x80, 0xF0,
                0xE0, 0x90, 0x90, 0x90, 0xE0,
                0xF0, 0x80, 0xF0, 0x80, 0xF0,
                0xF0, 0x80, 0xF0, 0x80, 0x80
            };
            for (int i = 0; i < sprites.Length; i++)
            {
                ram[i] = sprites[i];
            }

            for (int i = 0; i < program.Length; i++)
            {
                ram[i + pc] = program[i];
            }

            Display = display;
            Keyboard = keyboard;
            Speakers = speakers;
        }

        public void Run(CancellationToken ct)
        {
            double tickRate = 1 / 60;
            Stopwatch sw = Stopwatch.StartNew();
            double totalTime = 0;

            while (true)
            {
                double deltaTime = sw.Elapsed.TotalSeconds;
                totalTime += deltaTime;
                sw.Restart();

                while (totalTime >= tickRate)
                {
                    if (ct.IsCancellationRequested) return;
                    Instruction instruction = ReadInstruction();
                    DelayTimer();
                    Speakers.PlaySound = SoundTimer() > 0;

                    switch (instruction.OpCode)
                    {
                        case 0x0:
                            switch(instruction.Nnn)
                            {
                                case 0x00E0:
                                    Array.Fill(Display.PixelArray, false);
                                    break;
                                case 0x00EE:
                                    pc = stack[sp--];
                                    break;
                            }
                            break;
                        case 0x1:
                            pc = instruction.Nnn;
                            break;
                        case 0x2:
                            stack[++sp] = pc;
                            pc = instruction.Nnn;
                            break;
                        case 0x3:
                            if (v[instruction.X] == instruction.Kk) pc += 2;
                            break;
                        case 0x4:
                            if (v[instruction.X] != instruction.Kk) pc += 2;
                            break;
                        case 0x5:
                            if (v[instruction.X] == v[instruction.Y]) pc += 2;
                            break;
                        case 0x6:
                            v[instruction.X] = instruction.Kk;
                            break;
                        case 0x7:
                            v[instruction.X] += instruction.Kk;
                            break;
                        case 0x8:
                            switch(instruction.N)
                            {
                                case 0x0:
                                    v[instruction.X] = v[instruction.Y];
                                    break;
                                case 0x1:
                                    v[instruction.X] |= v[instruction.Y];
                                    break;
                                case 0x2:
                                    v[instruction.X] &= v[instruction.Y];
                                    break;
                                case 0x3:
                                    v[instruction.X] ^= v[instruction.Y];
                                    break;
                                case 0x4:
                                    ushort result = (ushort)(v[instruction.X] + v[instruction.Y]);
                                    if (result > 255) v[0xF] = 1;
                                    else v[0xF] = 0;
                                    v[instruction.X] = (byte)result;
                                    break;
                                case 0x5:
                                    if (v[instruction.X] > v[instruction.Y]) v[0xF] = 1;
                                    else v[0xF] = 0;
                                    v[instruction.X] -= v[instruction.Y];
                                    break;
                                case 0x6:
                                    if ((v[instruction.X] & 0x1) == 1) v[0xF] = 1;
                                    else v[0xF] = 0;
                                    v[instruction.X] /= 2;
                                    break;
                                case 0x7:
                                    if (v[instruction.Y] > v[instruction.X]) v[0xF] = 1;
                                    else v[0xF] = 0;
                                    v[instruction.X] = (byte)(v[instruction.Y] - v[instruction.X]);
                                    break;
                                case 0xE:
                                    if (((v[instruction.X] & 0x80) >> 7) == 1) v[0xF] = 1;
                                    else v[0xF] = 0;
                                    v[instruction.X] *= 2;
                                    break;
                            }
                            break;
                        case 0x9:
                            if (v[instruction.X] != v[instruction.Y]) pc += 2;
                            break;
                        case 0xA:
                            i = instruction.Nnn;
                            break;
                        case 0xB:
                            pc = (ushort)(instruction.Nnn + v[0]);
                            break;
                        case 0xC:
                            Random random = new Random();
                            v[instruction.X] = (byte)(random.Next(256) & instruction.Kk);
                            break;
                        case 0xD:
                            byte[] sprite = new byte[instruction.N];
                            for (int i = 0; i < instruction.N; i++)
                            {
                                sprite[i] = ram[this.i + i];
                            }
                            v[0xF] = Display.InsertSprite(instruction.X, instruction.Y, sprite);
                            break;
                        case 0xE:
                            switch(instruction.Kk)
                            {
                                case 0x9E:
                                    if (Keyboard.IsPressed(v[instruction.X])) pc += 2;
                                    break;
                                case 0xA1:
                                    if (!Keyboard.IsPressed(v[instruction.X])) pc += 2;
                                    break;
                            }
                            break;
                        case 0xF:
                            switch(instruction.Kk)
                            {
                                case 0x7:
                                    v[instruction.X] = delay;
                                    break;
                                case 0xA:
                                    v[instruction.X] = (byte)Keyboard.GetNextPress();
                                    break;
                                case 0x15:
                                    delay = v[instruction.X];
                                    break;
                                case 0x18:
                                    sound = v[instruction.X];
                                    break;
                                case 0x1E:
                                    i += v[instruction.X];
                                    break;
                                case 0x29:
                                    i = (ushort)(v[instruction.X] * 5);
                                    break;
                                case 0x33:
                                    ram[i] = (byte)(v[instruction.X] / 100);
                                    ram[i+1] = (byte)(v[instruction.X] % 100 / 10);
                                    ram[i+2] = (byte)(v[instruction.X] % 10 / 1);
                                    break;
                                case 0x55:
                                    for(int i = 0; i <= instruction.X; i++)
                                    {
                                        ram[this.i + i] = v[i];
                                    }
                                    break;
                                case 0x65:
                                    for(int i = 0; i <= instruction.X; i++)
                                    {
                                        v[i] = ram[this.i + i];
                                    }
                                    break;
                            }
                            break;
                    }
                    Console.WriteLine($"Opcode: {instruction.OpCode.ToString("X4")}, nnn: {instruction.Nnn.ToString("X4")}, n: {instruction.N.ToString("X4")}, kk: {instruction.Kk.ToString("X4")}");
                    totalTime -= tickRate;
                }
            }
        }
        

        private byte DelayTimer()
        {
            if (delay > 0) delay -= 1;
            return delay;
        }

        private byte SoundTimer()
        {
            if (sound > 0) sound -= 1;
            return sound;
        }

        private Instruction ReadInstruction()
        {
            byte mostSig = ram[pc];
            byte leastSig = ram[pc + 1];
            pc += 2;
            return new Instruction(mostSig, leastSig);
        }

        
    }
}
