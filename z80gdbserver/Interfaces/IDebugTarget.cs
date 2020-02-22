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

namespace z80gdbserver.Interfaces
{
	public interface IDebugTarget
	{
		void ClearBreakpoints();
		void DoRun();
		void DoStop();
		void AddBreakpoint(Breakpoint.BreakpointType type, uint addr);
		void RemoveBreakpoint(Breakpoint.BreakpointType type, uint addr);
		void ExecCycle();

		void WRMEM(uint addr, byte[] data);
		byte[] RDMEM(uint addr,uint len);

		void WRREG(int reg, uint val);
		uint RDREG(int reg);
		/// <summary>
		/// Optional error logging, leave null if not needed
		/// </summary>
		Action<string> LogError {get;}

		/// <summary>
		/// Optional exception logging, leave null if not needed
		/// </summary>
		Action<Exception> LogException {get;}

		/// <summary>
		/// Optional logging, leave null if not needed
		/// </summary>

		Action<string> Log {get;}

		event EventHandler BreakpointHandler;
	}
}

