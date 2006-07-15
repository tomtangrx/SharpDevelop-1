﻿// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="David Srbecký" email="dsrbecky@gmail.com"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

using Debugger.Wrappers.CorDebug;

namespace Debugger
{	
	public class Exception: RemotingObjectBase
	{
		NDebugger         debugger;
		Thread            thread;
		ICorDebugValue    corValue;
		Value             runtimeValue;
		ObjectValueClass  runtimeValueException;
		ExceptionType     exceptionType;
		SourcecodeSegment location;
		DateTime          creationTime;
		string            callstack;
		string            type;
		string            message;
		
		public NDebugger Debugger {
			get {
				return debugger;
			}
		}
		
		internal Exception(Thread thread)
		{
			creationTime = DateTime.Now;
			this.debugger = thread.Debugger;
			this.thread = thread;
			corValue = thread.CorThread.CurrentException;
			exceptionType = thread.CurrentExceptionType;
			runtimeValue = new Variable(debugger,
			                            "$exception",
			                            Variable.Flags.Default,
			                            new IExpirable[] {debugger.PauseSession},
			                            delegate { return corValue; } ).Value;
			runtimeValueException = ((ObjectValue)runtimeValue).GetClass("System.Exception");
			message = runtimeValueException.SubVariables["_message"].Value.AsString;
			
			if (thread.LastFunctionWithLoadedSymbols != null) {
				location = thread.LastFunctionWithLoadedSymbols.NextStatement;
			}
			
			callstack = "";
			int callstackItems = 0;
			foreach(Function function in thread.Callstack) {
				if (callstackItems >= 100) {
					callstack += "...\n";
					break;
				}
				
				SourcecodeSegment loc = function.NextStatement;
				callstack += function.Name + "()";
				if (loc != null) {
					callstack += " - " + loc.SourceFullFilename + ":" + loc.StartLine + "," + loc.StartColumn;
				}
				callstack += "\n";
				callstackItems++;
			}
			
			type = runtimeValue.Type;
		}
		
		public string Type {
			get {
				return type;
			}
		}
		
		public string Message {
			get {
				return message;
			}
		}
		
		public string Callstack {
			get {
				return callstack;
			}
		}

		public ExceptionType ExceptionType{
			get {
				return exceptionType;
			}
		}

		public SourcecodeSegment Location {
			get {
				return location;
			}
		}

		public DateTime CreationTime {
			get {
				return creationTime;
			}
		}
	}
}
