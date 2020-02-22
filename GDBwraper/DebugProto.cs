using System;
using System.Collections.Generic;
using System.Text;
using z80gdbserver;
using z80gdbserver.Interfaces;

namespace GDBwraper
{
    class DebugProto: IDebugTarget
    {
		public event EventHandler BreakpointHandler;

		public void ClearBreakpoints()
		{

		}
		public void DoRun()
		{
			byte[] packet = new byte[1];
			packet[0] = (byte)ComPort.cmd_type.cont;
			try
			{
				port.excange(packet);
			}
			catch (Exception e)
			{
			}
		}
		public void DoStop()
		{
			byte[] packet = new byte[1];
			packet[0] = (byte)ComPort.cmd_type.stop;
			try
			{
				port.excange(packet);
			}
			catch (Exception e)
			{
			}
		}
		public void AddBreakpoint(Breakpoint.BreakpointType type, uint addr)
		{
			byte[] packet = new byte[5];
			packet[0] = (byte)ComPort.cmd_type.setBkpt;
			packet[1] = (byte)(addr >> 0);
			packet[2] = (byte)(addr >> 8);
			packet[3] = (byte)(addr >> 16);
			packet[4] = (byte)(addr >> 24);
			try
			{
				port.excange(packet);
			}
			catch (Exception e)
			{

			}
		}
		public void RemoveBreakpoint(Breakpoint.BreakpointType type, uint addr)
		{
			byte[] packet = new byte[5];
			packet[0] = (byte)ComPort.cmd_type.clrBkpt;
			packet[1] = (byte)(addr >> 0);
			packet[2] = (byte)(addr >> 8);
			packet[3] = (byte)(addr >> 16);
			packet[4] = (byte)(addr >> 24);
			try
			{
				port.excange(packet);
			}
			catch (Exception e)
			{

			}
		}

		public void ExecCycle()
		{
			byte[] packet = new byte[1];
			packet[0] = (byte)ComPort.cmd_type.step;
			try
			{
				port.excange(packet);
			}
			catch (Exception e)
			{

			}
		}

		public void WRMEM(uint addr, byte[] data)
		{
			byte[] packet = new byte[data.Length + 5];
			packet[0] = (byte)ComPort.cmd_type.wrmem;
			packet[1] = (byte)(addr >> 0);
			packet[2] = (byte)(addr >> 8);
			packet[3] = (byte)(addr >> 16);
			packet[4] = (byte)(addr >> 24);
			for (int i = 0; i < data.Length; i++)
				packet[5 + i] = data[i];
			try
			{
				port.excange(packet);
			}
			catch (Exception e)
			{

			}

		}
		public byte[] RDMEM(uint addr, uint len)
		{
			byte[] packet = new byte[6];
			packet[0] = (byte)ComPort.cmd_type.rdmem;
			packet[1] = (byte)(addr >> 0);
			packet[2] = (byte)(addr >> 8);
			packet[3] = (byte)(addr >> 16);
			packet[4] = (byte)(addr >> 24);
			packet[5] = (byte)(len >> 0);
			try
			{
				return port.excange(packet);
			}
			catch (Exception e)
			{
				byte[] ret = new byte[0];
				return ret;
			}
		}

		public void WRREG(int reg, uint val)
		{

		}
		public uint RDREG(int reg)
		{
			byte[] packet = new byte[2];
			packet[0] = (byte)ComPort.cmd_type.readReg;
			packet[1] = (byte)(reg);
			try
			{
				byte[] ans = port.excange(packet);
				return (uint)(ans[0] + (ans[1] << 8) + (ans[2] << 16) + (ans[3] << 24));
			}
			catch (Exception e)
			{

			}
			return (uint)0xdeadbeef;
		}

		//}

		/// <summary>
		/// Optional error logging, leave null if not needed
		/// </summary>
		public Action<string> LogError { get; }

		/// <summary>
		/// Optional exception logging, leave null if not needed
		/// </summary>
		public Action<Exception> LogException { get; }

		/// <summary>
		/// Optional logging, leave null if not needed
		/// </summary>

		public Action<string> Log { get; }

		void BreakpointWrapper(object sender, EventArgs e)
		{
			BreakpointHandler?.Invoke(this,e);
		}

		ComPort port;
        public DebugProto()
        {
            port = new ComPort();
			port.BreakpointHandler += BreakpointWrapper;

		}

        public bool ping()
        {
			byte[] packet = new byte[1];
			packet[0] = (byte)ComPort.cmd_type.ping;
			try
			{
				port.excange(packet);
			}
			catch (Exception e)
			{
				return false;
			}
			

            return true;
        }
        
    }
}
