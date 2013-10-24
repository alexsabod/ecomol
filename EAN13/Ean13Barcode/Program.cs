#region Using directives

using System;
using System.Collections.Generic;
using System.Windows.Forms;

#endregion

namespace Ean13Barcode
{
	static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main( )
		{
			Application.EnableVisualStyles( );
		//	Application.EnableRTLMirroring( );
			Application.Run( new frmEan13( ) );
		}
       
	}
    public static class CallBackMy
    {
        public delegate void callbackEvent(string name,string ves);
        public static callbackEvent callbackEventHandler;
    }
}