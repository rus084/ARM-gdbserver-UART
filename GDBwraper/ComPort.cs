using common;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Text;
using System.Threading;

namespace GDBwraper
{
    class ComPort
    {
        public enum cmd_type: byte {
            ping = 1,
            rdmem = 2,
            wrmem = 3,
            stop = 4,
            cont = 5,
            readReg = 6,
            setBkpt = 7,
            clrBkpt = 8,
            step = 9,
        };

        public EventHandler BreakpointHandler;

        void ExceptionOccured()
        {
            BreakpointHandler?.Invoke(this,new EventArgs());
        }

        byte[] makeMsg(byte[] data)
        {
            int len = data.Length + 2 + 2;
            for (int i = 0; i < data.Length; i++)
                if (data[i] == 0xAA)
                    len++;

            byte[] msg = new byte[len];

            int putpos = 0;
            msg[putpos++] = 0xAA;
            msg[putpos++] = 0xFF;

            for (int i = 0; i < data.Length; i++)
            {
                if (data[i] == 0xAA)
                {
                    msg[putpos++] = 0xAA;
                    msg[putpos++] = 0xAA;
                }
                else
                {
                    msg[putpos++] = data[i];
                }
            }

            //msg[putpos++] = CRC8.calc_arr(data, data.Length, 0, 0);

            msg[putpos++] = 0xAA;
            msg[putpos++] = 0x00;
            return msg;
        }


        byte[] buffer;
        int pos;


        void AsyncReader()
        {
            while (true)
            {
                lock (PortLocker)
                {
                    int byteRecieved = port.BytesToRead;
                    byte[] tmp = new byte[byteRecieved];
                    port.Read(tmp, 0, byteRecieved);

                    for (int i = 0; i < byteRecieved; i++)
                        bytehandler(tmp[i]);
                }


                Thread.Sleep(50);
            }

        }

        int state;

        byte[] bytehandler(byte b)
        {
            Console.Write(b.ToString("X2"));
            switch (state)
            {
                default:
                case 0:
                    pos = 0;
                    if (b == 0xAA)
                        state = 1;
                    break;
                case 1:
                    if (b == 0xFF)
                        state = 3;
                    else if (b==0xA5)
                    {
                        Console.WriteLine("");
                        ExceptionOccured();
                        state = 0;
                    }
                    else if (b == 0xAA)
                    {
                        break;
                    }
                    else
                    {
                        state = 0;
                    }
                    break;
                case 3:
                    if (b == 0xAA)
                        state = 4;
                    else
                        buffer[pos++] = b;
                    break;
                case 4:
                    if (b == 0)
                    {
                        Console.WriteLine("");
                        byte[] data = new byte[pos];
                        for (int i = 0; i < pos; i++)
                            data[i] = buffer[i];
                        state = 0;
                        return data;
                    }
                    else if (b == 0xAA)
                    {
                        buffer[pos++] = b;
                        state = 3;
                    }
                    else if (b == 0xA5)
                    {
                        Console.WriteLine("");
                        ExceptionOccured();
                        state = 3;
                    }
                    else
                        state = 0;
                    break;
            }
            if (pos >= 255)
                state = 0;
            return null;
        }

        // алгоритм байтстаффинга:
        // старт пакета - 0xAA 0xFF
        // конец пакета - 0xAA 0x00
        // замена : 0xAA -> 0xAA 0xAA
        // AA A5 - BKPT
        public byte[] excange (byte[] data)
        {

            byte[] msg = makeMsg(data);

            int tryCnt = 7;

            lock (PortLocker)
            {
                while (tryCnt != 0)
                {
                    byte[] tmp = new byte[256];

                    int byteRecieved = port.BytesToRead;
                    port.Read(tmp, 0, byteRecieved);

                    for (int i = 0; i < byteRecieved; i++)
                    {
                        bytehandler(tmp[i]);
                    }

                    port.Write(msg, 0, msg.Length);
                    Console.WriteLine("");
                    Console.WriteLine("Out:" + utils.ByteToHex(data));
                    //Thread.Sleep(1);

                    try
                    {
                        while (true)
                        {
                            byte[] ret = null;


                            byte inp = (byte)port.ReadByte();
                            ret = bytehandler(inp);

                            //int cnt = port.Read(tmp, 0, 256);
                            //for (int i = 0; i < cnt; i++)
                            //{
                            //    if (ret == null ) ret = bytehandler(tmp[i]);
                            //    else bytehandler(tmp[i]);
                            //}

                            if (ret != null)
                                return ret;
                        }
                    }
                    catch (Exception e)
                    {

                    }
                    Console.WriteLine("retransmission to target:" + utils.ByteToHex(data));
                    tryCnt--;
                }
            }

            
            throw new TimeoutException();
        }

        SerialPort port;
        Thread AsyncReadThread;
        static object PortLocker = new object();
        public ComPort()
        {
            port = new SerialPort();
            port.PortName = SetPortName();
            port.BaudRate = 115200;
            port.Parity = Parity.None;
            port.DataBits = 8;
            port.StopBits = StopBits.One;
            port.Handshake = Handshake.None;
            port.ReadTimeout = 5;
            port.Open();
            buffer = new byte[256];
            pos = 0;
            AsyncReadThread = new Thread(new ThreadStart(AsyncReader));
            AsyncReadThread.Start(); // запускаем поток
        }



        static string SetPortName()
        {
            int PortNameI = 0;
            string PortNumS;
            string[] ports;
        beg:
            do
            {
                Console.WriteLine("Avaliable Ports:");
                ports = SerialPort.GetPortNames();
                for (int i = 0; i < ports.Length; i++)
                {
                    Console.WriteLine("   {0}: {1}", i, ports[i]);
                }

                Console.Write("Enter COM port number: ");
                PortNumS = Console.ReadLine();

            } while (!Int32.TryParse(PortNumS, out PortNameI));

            if (PortNameI >= ports.Length) goto beg;

            return ports[PortNameI];

        }




    }
}
