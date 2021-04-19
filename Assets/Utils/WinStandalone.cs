using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;


namespace BoardGames.Utils
{
	/// <summary>
	/// Original source: <a href="https://gist.github.com/oktomus/7bdf92b3ccee221c3f19f6e9f75720c8"/> <para/>
	/// <a href="https://improve.dk/minimizing-and-maximizing-windows/"/>
	/// </summary>
	internal static class WinStandalone
	{
		public static void Maximize()
		{
#if UNITY_EDITOR || !UNITY_STANDALONE_WIN
			return;
#endif
			var wf = new WindowFinder();
			// Maxime all windows that contains "APP_NAME" in their title.
			wf.FindWindows(0, null, new Regex(APP_NAME), new Regex(APP_NAME), new WindowFinder.FoundWindowCallback(MaximizeWindow));
		}


		private static string APP_NAME;
#if !UNITY_EDITOR && UNITY_STANDALONE_WIN
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
#endif
		private static void Init()
		{
			APP_NAME = Application.productName;
			Task.Run(Maximize);
		}


		/// <summary>
		/// The ShowWindowAsync method alters the windows show state through the nCmdShow parameter.<br/>
		/// The nCmdShow parameter can have any of the SW values.<br/>
		/// See <a href="http://msdn.microsoft.com/library/en-us/winui/winui/windowsuserinterface/windowing/windows/windowreference/windowfunctions/showwindowasync.asp"/> for full documentation.
		///</summary>
		[DllImport("user32.dll")]
		private static extern bool ShowWindowAsync(int hWnd, int nCmdShow);


		/// <summary>
		/// An enumeration containing all the possible SW values.
		/// </summary>
		private enum SW : int
		{
			HIDE = 0,
			SHOWNORMAL = 1,
			SHOWMINIMIZED = 2,
			SHOWMAXIMIZED = 3,
			SHOWNOACTIVATE = 4,
			SHOW = 5,
			MINIMIZE = 6,
			SHOWMINNOACTIVE = 7,
			SHOWNA = 8,
			RESTORE = 9,
			SHOWDEFAULT = 10
		}


		private static bool MaximizeWindow(int handle)
		{
			// Maximize the window.
			ShowWindowAsync(handle, (int)SW.SHOWMAXIMIZED);
			return true;
		}



		/// <summary>
		/// A class used for finding windows based upon their class, title, process and parent window handle.
		/// </summary>
		private class WindowFinder
		{
			// Win32 constants.
			const int WM_GETTEXT = 0x000D;
			const int WM_GETTEXTLENGTH = 0x000E;

			#region Win32 functions that have all been used in previous blogs.
			[DllImport("User32.Dll")]
			private static extern void GetClassName(int hWnd, StringBuilder s, int nMaxCount);

			[DllImport("User32.dll")]
			private static extern int GetWindowText(int hWnd, StringBuilder text, int count);

			[DllImport("User32.dll")]
			private static extern Int32 SendMessage(int hWnd, int Msg, int wParam, StringBuilder lParam);

			[DllImport("User32.dll")]
			private static extern Int32 SendMessage(int hWnd, int Msg, int wParam, int lParam);

			[DllImport("user32")]
			private static extern int GetWindowThreadProcessId(int hWnd, out int lpdwProcessId);

			/// EnumChildWindows works just like EnumWindows, except we can provide a parameter that specifies the parent
			/// window handle. If this is NULL or zero, it works just like EnumWindows. Otherwise it'll only return windows
			/// whose parent window handle matches the hWndParent parameter.
			[DllImport("user32.Dll")]
			private static extern Boolean EnumChildWindows(int hWndParent, PChildCallBack lpEnumFunc, int lParam);
			#endregion

			/// <summary>
			/// The PChildCallBack delegate that we used with EnumWindows.
			///</summary>
			private delegate bool PChildCallBack(int hWnd, int lParam);

			/// <summary>
			/// This is an event that is run each time a window was found that matches the search criterias.<br/>
			/// The boolean return value of the delegate matches the functionality of the PChildCallBack delegate function.
			///</summary>
			private event FoundWindowCallback foundWindow;
			public delegate bool FoundWindowCallback(int hWnd);

			// Members that'll hold the search criterias while searching.
			private int parentHandle;
			private Regex className;
			private Regex windowText;
			private Regex process;


			/// <summary>
			/// The main search function of the WindowFinder class. The parentHandle parameter is optional, taking in a zero if omitted.<br/>
			/// The className can be null as well, in this case the class name will not be searched.<br/>
			/// For the window text we can input a Regex object that will be matched to the window text, unless it's null.<br/>
			/// The process parameter can be null as well, otherwise it'll match on the process name (Internet Explorer = "iexplore").<br/>
			/// Finally we take the FoundWindowCallback function that'll be called each time a suitable window has been found.
			///</summary>
			public void FindWindows(int parentHandle, Regex className, Regex windowText, Regex process, FoundWindowCallback fwc)
			{
				this.parentHandle = parentHandle;
				this.className = className;
				this.windowText = windowText;
				this.process = process;

				// Add the FounWindowCallback to the foundWindow event.
				foundWindow = fwc;

				// Invoke the EnumChildWindows function.
				EnumChildWindows(parentHandle, new PChildCallBack(EnumChildWindowsCallback), 0);
			}


			/// <summary>
			/// This function gets called each time a window is found by the EnumChildWindows function.<br/> 
			/// The found windows here are NOT the final found windows as the only filtering done by EnumChildWindows is on the parent window handle.
			/// </summary>
			private bool EnumChildWindowsCallback(int handle, int lParam)
			{
				// If a class name was provided, check to see if it matches the window.
				if (className != null)
				{
					StringBuilder sbClass = new StringBuilder(256);
					GetClassName(handle, sbClass, sbClass.Capacity);

					// If it does not match, return true so we can continue on with the next window.
					if (!className.IsMatch(sbClass.ToString()))
						return true;
				}

				// If a window text was provided, check to see if it matches the window.
				if (windowText != null)
				{
					int txtLength = SendMessage(handle, WM_GETTEXTLENGTH, 0, 0);
					StringBuilder sbText = new StringBuilder(txtLength + 1);
					SendMessage(handle, WM_GETTEXT, sbText.Capacity, sbText);

					// If it does not match, return true so we can continue on with the next window.
					if (!windowText.IsMatch(sbText.ToString()))
						return true;
				}

				// If a process name was provided, check to see if it matches the window.
				if (process != null)
				{
					int processID;
					GetWindowThreadProcessId(handle, out processID);

					// Now that we have the process ID, we can use the built in .NET function to obtain a process object.
					System.Diagnostics.Process p = System.Diagnostics.Process.GetProcessById(processID);

					// If it does not match, return true so we can continue on with the next window.
					if (!process.IsMatch(p.ProcessName))
						return true;
				}

				// If we get to this point, the window is a match. Now invoke the foundWindow event and based upon
				// the return value, whether we should continue to search for windows.
				return foundWindow(handle);
			}
		}
	}



	internal static class MinimumWindowSize
	{
#if !UNITY_EDITOR && UNITY_STANDALONE_WIN
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
#endif
		private static void Init()
		{
			Set(1280, 720);
			Application.quitting += () => Reset();
		}


		// This code works exclusively with standalone build.
		// Executing GetActiveWindow in unity editor returns editor window.
		private const int DefaultValue = -1;

		// Identifier of MINMAXINFO message
		private const uint WM_GETMINMAXINFO = 0x0024;

		// SetWindowLongPtr argument : Sets a new address for the window procedure.
		private const int GWLP_WNDPROC = -4;

		private static int width;
		private static int height;
		private static bool enabled;

		// Reference to current window
		private static HandleRef hMainWindow;

		// Reference to unity WindowsProcedure handler
		private static IntPtr unityWndProcHandler;

		// Reference to custom WindowsProcedure handler
		private static IntPtr customWndProcHandler;

		// Delegate signature for WindowsProcedure
		private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

		// Instance of delegate
		private static WndProcDelegate procDelegate;

		[StructLayout(LayoutKind.Sequential)]
		private struct Minmaxinfo
		{
			public Point ptReserved;
			public Point ptMaxSize;
			public Point ptMaxPosition;
			public Point ptMinTrackSize;
			public Point ptMaxTrackSize;
		}

		private struct Point
		{
			public int x;
			public int y;
		}


		public static void Set(int minWidth, int minHeight)
		{
#if UNITY_EDITOR || !UNITY_STANDALONE_WIN
			return;
#endif
			if (minWidth < 0 || minHeight < 0) throw new ArgumentOutOfRangeException("Any component of min size cannot be less than 0");

			width = minWidth;
			height = minHeight;

			if (enabled) return;

			// Get reference
			hMainWindow = new HandleRef(null, GetActiveWindow());
			procDelegate = WndProc;
			// Generate handler
			customWndProcHandler = Marshal.GetFunctionPointerForDelegate(procDelegate);
			// Replace unity mesages handler with custom
			unityWndProcHandler = SetWindowLongPtr(hMainWindow, GWLP_WNDPROC, customWndProcHandler);

			enabled = true;
		}


		public static void Reset()
		{
#if UNITY_EDITOR || !UNITY_STANDALONE_WIN
			return;
#endif
			if (!enabled) return;
			// Replace custom message handler with unity handler
			SetWindowLongPtr(hMainWindow, GWLP_WNDPROC, unityWndProcHandler);
			hMainWindow = new HandleRef(null, IntPtr.Zero);
			unityWndProcHandler = IntPtr.Zero;
			customWndProcHandler = IntPtr.Zero;
			procDelegate = null;

			width = 0;
			height = 0;

			enabled = false;
		}


		private static IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
		{
			// All messages except WM_GETMINMAXINFO will send to unity handler
			if (msg != WM_GETMINMAXINFO) return CallWindowProc(unityWndProcHandler, hWnd, msg, wParam, lParam);

			// Intercept and change MINMAXINFO message
			var x = (Minmaxinfo)Marshal.PtrToStructure(lParam, typeof(Minmaxinfo));
			x.ptMinTrackSize = new Point { x = width, y = height };
			Marshal.StructureToPtr(x, lParam, false);

			// Send changed message
			return DefWindowProc(hWnd, msg, wParam, lParam);
		}

		[DllImport("user32.dll")]
		private static extern IntPtr GetActiveWindow();

		[DllImport("user32.dll", EntryPoint = "CallWindowProcA")]
		private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint wMsg, IntPtr wParam,
			IntPtr lParam);

		[DllImport("user32.dll", EntryPoint = "DefWindowProcA")]
		private static extern IntPtr DefWindowProc(IntPtr hWnd, uint wMsg, IntPtr wParam, IntPtr lParam);

		private static IntPtr SetWindowLongPtr(HandleRef hWnd, int nIndex, IntPtr dwNewLong)
		{
			if (IntPtr.Size == 8) return SetWindowLongPtr64(hWnd, nIndex, dwNewLong);
			return new IntPtr(SetWindowLong32(hWnd, nIndex, dwNewLong.ToInt32()));
		}

		[DllImport("user32.dll", EntryPoint = "SetWindowLong")]
		private static extern int SetWindowLong32(HandleRef hWnd, int nIndex, int dwNewLong);

		[DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
		private static extern IntPtr SetWindowLongPtr64(HandleRef hWnd, int nIndex, IntPtr dwNewLong);
	}
}