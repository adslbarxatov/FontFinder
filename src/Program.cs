using System;
using System.Windows.Forms;

namespace FontFinder
	{
	/// <summary>
	/// Класс описывает главную функцию программы
	/// </summary>
	public static class Program
		{
		/// <summary>
		/// Главная точка входа для приложения
		/// </summary>
		[STAThread]
		public static void Main ()
			{
			Application.EnableVisualStyles ();
			Application.SetCompatibleTextRenderingDefault (false);
			Application.Run (new MainForm ());
			}
		}
	}
