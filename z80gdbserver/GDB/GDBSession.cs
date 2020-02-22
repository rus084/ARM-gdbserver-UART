/*
 * Copyright 2011 Alexander Tsidaev
 * 
 * This file is part of z80gdbserver.
 * z80gdbserver is free software: you can redistribute it and/or modify it under the
 * terms of the GNU General Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 * 
 * z80gdbserver is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY;
 * without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
 * See the GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License along with z80gdbserver. 
 * If not, see http://www.gnu.org/licenses/.
 */
using System;
using System.Linq;

using z80gdbserver.Interfaces;

namespace z80gdbserver.Gdb
{
	public class GDBSession
	{
		public static class StandartAnswers
		{
			public const string Empty = "";
			public const string OK = "OK";
			public const string Error = "E00";
			public const string Breakpoint = "T05";
			public const string HaltedReason = "S05";//"T05thread:00;";
			public const string Interrupt = "T02";
		}

		private readonly IDebugTarget _target;

		public GDBSession(IDebugTarget target)
		{
			_target = target;
		}

		#region Register stuff

		// GDB regs order:
		// R0 - R12 , SP , LR , PC , xPSR

		public static int RegistersCount { get { return 17; } }


		public string GetRegisterAsHex(int reg)
		{
			uint result = _target.RDREG(reg);
			return ((byte)result).ToLowEndianHexString().ToLower() + ((byte)(result>>8)).ToLowEndianHexString().ToLower() +
				((byte)(result >> 16)).ToLowEndianHexString().ToLower() + ((byte)(result >> 24)).ToLowEndianHexString().ToLower();
			
		}

		public bool SetRegister(int reg, string hexValue)
		{
			uint val = 0;
			if (hexValue.Length == 4)
				val = Convert.ToUInt32(hexValue.Substring(0, 2), 16) | (Convert.ToUInt32(hexValue.Substring(2, 2), 16) << 8) |
					(Convert.ToUInt32(hexValue.Substring(4, 2), 16) << 16) | (Convert.ToUInt32(hexValue.Substring(6, 2), 16) << 24);

			_target.WRREG(reg, val);

			return true;
		}

		#endregion

		public static string FormatResponse(string response,bool ack)
		{
			return ((ack)?"+$":"$") + response + "#" + GDBPacket.CalculateCRC(response);
		}

		public string ParseRequest(GDBPacket packet, out bool isSignal)
		{
			var result = StandartAnswers.Empty;
			isSignal = false;

			try
			{
				switch (packet.CommandName)
				{
					case '\0': // Command is empty ("+" in 99.99% cases)
						return null;
					case 'q':
						result = GeneralQueryResponse(packet); break; // handleQueryPacket(data);
					case 'Q':
						result = GeneralQueryResponse(packet); break;
					case '?':
						result = GetTargetHaltedReason(packet); break; //handleException();
					case '!': // extended connection
						break;
					case 'g': // read registers
						result = ReadRegisters(packet); break;
					case 'G': // write registers
						result = WriteRegisters(packet); break;
					case 'm': // read memory
						result = ReadMemory(packet); break;
					case 'M': // write memory
						result = WriteMemory(packet); break;
					case 'X': // write memory binary
							  // Not implemented yet, client shoul use M instead
							  //result = StandartAnswers.OK;
						break;
					case 'p': // get single register
						result = GetRegister(packet); break;
					case 'P': // set single register
						result = SetRegister(packet); break;
					case 'v': // some requests, mainly vCont
						result = ExecutionRequest(packet); break; // packet = gdbCreateMsgPacket("");
					case 's': //stepi
						_target.ExecCycle();
						result = "T05";
						break;
					case 'z': // remove bp
						result = RemoveBreakpoint(packet);
						break;
					case 'Z': // insert bp
						result = SetBreakpoint(packet);
						break;
					case 'k': // Kill the target
						break;
					case 'H': // set thread
						result = StandartAnswers.OK; // we do not have threads, so ignoring this command is OK
						break;
					case 'c': // continue
						_target.DoRun();
						result = null;
						break;
					case 'D': // Detach from client
						_target.DoRun();
						result = StandartAnswers.OK;
						break;
					default:
						// ctrl+c is SIGINT
						if (packet.GetBytes()[0] == 0x03)
						{
							_target.DoStop();
							result = StandartAnswers.Interrupt;
							isSignal = true;
						}
						else
							return null;
						break;

				}
			}
			catch (Exception ex)
			{
				_target.LogException?.Invoke(ex);
				result = GetErrorAnswer(Errno.EPERM);
			}

			if (result == null)
				return "+";
			else
				return FormatResponse(result,!isSignal);
		}

		private static string GetErrorAnswer(Errno errno)
		{
			return string.Format("E{0:D2}", (int)errno);
		}

		private string GeneralQueryResponse(GDBPacket packet)
		{
			string command = packet.GetCommandParameters()[0];
			if (command.StartsWith("Supported"))
				return "PacketSize=4000";
			if (command.StartsWith("C"))
				return StandartAnswers.Empty;
			if (command.StartsWith("Attached"))
				return "1";
			if (command.StartsWith("TStatus"))
				return StandartAnswers.Empty;
			if (command.StartsWith("Offset"))
				return StandartAnswers.Error;
			return StandartAnswers.OK;
		}

		private string GetTargetHaltedReason(GDBPacket packet)
		{
			return StandartAnswers.HaltedReason;
		}

		private string ReadRegisters(GDBPacket packet)
		{
			string[] values = new string[17];
			for (int i = 0; i < values.Length; i++)
				values[i] = GetRegisterAsHex(i);
			return String.Join("", values);
		}

		private string WriteRegisters(GDBPacket packet)
		{
			/*
			var regsData = packet.GetCommandParameters()[0];
			for (int i = 0, pos = 0; i < RegistersCount; i++)
			{
				int currentRegisterLength = 8;
				SetRegister(i, regsData.Substring(pos, currentRegisterLength));
				pos += currentRegisterLength;
			}
			*/
			return StandartAnswers.OK;
		}

		private string GetRegister(GDBPacket packet)
		{
			return GetRegisterAsHex(Convert.ToInt32(packet.GetCommandParameters()[0], 16));
		}

		private string SetRegister(GDBPacket packet)
		{
			var parameters = packet.GetCommandParameters()[0].Split(new char[] { '=' });
			if (SetRegister(Convert.ToInt32(parameters[0], 16), parameters[1]))
				return StandartAnswers.OK;
			else
				return StandartAnswers.Error;
		}

		private string ReadMemory(GDBPacket packet)
		{
			var parameters = packet.GetCommandParameters();
			if (parameters.Length < 2)
			{
				return GetErrorAnswer(Errno.EPERM);
			}
			var arg1 = Convert.ToUInt32(parameters[0], 16);
			var arg2 = Convert.ToUInt32(parameters[1], 16);
			if (arg1 > uint.MaxValue || arg2 > uint.MaxValue)
			{
				return GetErrorAnswer(Errno.EPERM);
			}
			var addr = (uint)arg1;
			var length = (uint)arg2;
			var result = string.Empty;
			for (uint i = 0; i < length;)
			{
				uint remain = length - i;
				if (remain > 64)
					remain = 64;

				byte[] get = _target.RDMEM((uint)(addr + i),(uint) remain);

				for (int j = 0; j < get.Length; j++)
				{
					var hex = get[j].ToLowEndianHexString().ToLower();
					result += hex;
				}


				if ((get.Length == 0) && (i == 0))
					return GetErrorAnswer(Errno.EFAULT);
				else if (get.Length != remain)  return result;




				i += remain;
			}
			return result;
		}

		private string WriteMemory(GDBPacket packet)
		{
			var parameters = packet.GetCommandParameters();
			if (parameters.Length < 3)
			{
				return GetErrorAnswer(Errno.ENOENT);
			}
			var arg1 = Convert.ToUInt32(parameters[0], 16);
			var arg2 = Convert.ToUInt32(parameters[1], 16);
			if (arg1 > uint.MaxValue || arg2 > uint.MaxValue)
			{
				return GetErrorAnswer(Errno.ENOENT);
			}
			uint addr = (uint)arg1;
			uint length = (uint)arg2;

			byte[] data = new byte[length];
			for (uint i = 0; i < length; i++)
			{
				var hex = parameters[2].Substring((int)i * 2, 2);
				var value = Convert.ToByte(hex, 16);
				data[i] = value;
			}

			for (uint i=0;i<length;)
			{
				uint remain = length - i;
				if (remain > 64)
					remain = 64;

				byte[] toSend = new byte[remain];
				for (int j=0;j<remain;j++)
				{
					toSend[j] = data[i + j];
				}
				_target.WRMEM((uint)(addr + i), toSend);
				i += remain;
			}


			return StandartAnswers.OK;
		}

		private string ExecutionRequest(GDBPacket packet)
		{
			string command = packet.GetCommandParameters()[0];
			if (command.StartsWith("Cont?"))
				return "";
			if (command.StartsWith("Cont"))
			{

			}
			return StandartAnswers.Empty;
		}

		private string SetBreakpoint(GDBPacket packet)
		{
			string[] parameters = packet.GetCommandParameters();
			Breakpoint.BreakpointType type = Breakpoint.GetBreakpointType(int.Parse(parameters[0]));
			uint addr = Convert.ToUInt32(parameters[1], 16);

			_target.AddBreakpoint(type, addr);

			return StandartAnswers.OK;
		}

		private string RemoveBreakpoint(GDBPacket packet)
		{
			string[] parameters = packet.GetCommandParameters();
			Breakpoint.BreakpointType type = Breakpoint.GetBreakpointType(int.Parse(parameters[0]));
			uint addr = Convert.ToUInt32(parameters[1], 16);

			_target.RemoveBreakpoint(type, addr);

			return StandartAnswers.OK;
		}
	}
}

