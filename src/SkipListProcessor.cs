using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace RD_AAOW
	{
	/// <summary>
	/// Форма настройки пропускаемых шрифтов
	/// </summary>
	public partial class SkipListProcessor:Form
		{
		// Переменные и константы
		private List<string> skippingFonts = new List<string> ();
		private const string skippingFontsListFile = ProgramDescription.AssemblyMainName + ".skp";
		private SupportedLanguages al;
		private string sampleText = "SaMpLe 123";
		private FontFamily[] existentFonts;

		/// <summary>
		/// Возвращает список шрифтов операционной системы
		/// </summary>
		public FontFamily[] ExistentFonts
			{
			get
				{
				return existentFonts;
				}
			}

		/// <summary>
		/// Конструктор. Загружает данные о пропускаемых шрифтах
		/// </summary>
		public SkipListProcessor ()
			{
			// Инициализация
			InitializeComponent ();

			// Получение списка шрифтов системы
			InstalledFontCollection ifc = new InstalledFontCollection ();
			existentFonts = ifc.Families;
			ifc.Dispose ();

			ExistentFontsListBox.DataSource = existentFonts;
			ExistentFontsListBox.DisplayMember = ExistentFontsListBox.ValueMember = "Name";

			// Загрузка файла
			FileStream FS = null;
			try
				{
				FS = new FileStream (skippingFontsListFile, FileMode.Open);
				}
			catch
				{
				FillingRequired.Checked = true;
				return;
				}
			StreamReader SR = new StreamReader (FS, Encoding.Unicode);

			while (!SR.EndOfStream)
				{
				skippingFonts.Add (SR.ReadLine ());
				}

			// Завершено
			SR.Close ();
			FS.Close ();
			}

		/// <summary>
		/// Метод открывает окно ручного управления списком
		/// </summary>
		/// <param name="SampleText">Образец текста для предпросмотра шрифтов</param>
		/// <param name="InterfaceLanguage">Язык интерфейса программы</param>
		public void EditList (SupportedLanguages InterfaceLanguage, string SampleText)
			{
			// Инициализация
			if ((SampleText != null) && (SampleText != ""))
				sampleText = SampleText;
			al = InterfaceLanguage;

			// Настройка
			this.Text = Localization.GetText ("SkipListProcessorCaption", al);
			BExit.Text = Localization.GetText ("BExitText", al);
			FillingRequired.Text = Localization.GetText ("FillingRequiredText", al);
			ExistentLabel.Text = string.Format (Localization.GetText ("ExistentLabelText", al), ExistentFontsListBox.Items.Count);

			// Запуск
			SkippingFontsListBox.DataSource = null;
			SkippingFontsListBox.DataSource = skippingFonts;
			SkippingLabel.Text = string.Format (Localization.GetText ("SkippingLabelText", al), SkippingFontsListBox.Items.Count);

			this.ShowDialog ();
			}

		// Добавление шрифта
		private void BAdd_Click (object sender, EventArgs e)
			{
			if (ExistentFontsListBox.SelectedIndex < 0)
				return;

			string s = ExistentFontsListBox.SelectedValue.ToString ();

			if (!skippingFonts.Contains (s))
				skippingFonts.Add (s);
			skippingFonts.Sort ();

			SkippingFontsListBox.DataSource = null;
			SkippingFontsListBox.DataSource = skippingFonts;
			SkippingLabel.Text = string.Format (Localization.GetText ("SkippingLabelText", al), SkippingFontsListBox.Items.Count);
			}

		// Выход
		private void BExit_Click (object sender, EventArgs e)
			{
			this.Close ();
			}

		// Удаление шрифта
		private void BRemove_Click (object sender, EventArgs e)
			{
			if (SkippingFontsListBox.SelectedIndex < 0)
				return;

			skippingFonts.RemoveAt (SkippingFontsListBox.SelectedIndex);

			SkippingFontsListBox.DataSource = null;
			SkippingFontsListBox.DataSource = skippingFonts;
			SkippingLabel.Text = string.Format (Localization.GetText ("SkippingLabelText", al), SkippingFontsListBox.Items.Count);
			}

		// Очистка списка шрифтов
		private void BClear_Click (object sender, EventArgs e)
			{
			if (MessageBox.Show (Localization.GetText ("ClearSkippingFonts", al), ProgramDescription.AssemblyTitle,
				MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) == DialogResult.Yes)
				{
				skippingFonts.Clear ();

				SkippingFontsListBox.DataSource = null;
				SkippingFontsListBox.DataSource = skippingFonts;
				SkippingLabel.Text = string.Format (Localization.GetText ("SkippingLabelText", al), SkippingFontsListBox.Items.Count);
				}
			}

		/// <summary>
		/// Метод сохраняет текущий список пропускаемых шрифтов
		/// </summary>
		public void SaveList ()
			{
			// Загрузка файла
			FileStream FS = null;
			try
				{
				FS = new FileStream (skippingFontsListFile, FileMode.Create);
				}
			catch
				{
				return;
				}
			StreamWriter SW = new StreamWriter (FS, Encoding.Unicode);

			for (int i = 0; i < skippingFonts.Count; i++)
				SW.WriteLine (skippingFonts[i]);

			// Завершено
			SW.Close ();
			FS.Close ();
			}

		/// <summary>
		/// Метод проверяет, требуется ли пропустить указанный шрифт
		/// </summary>
		/// <param name="FontName">Название шрифта</param>
		/// <returns>Возвращает true, если шрифт следует пропустить</returns>
		public bool FontMustBeSkipped (string FontName)
			{
			return skippingFonts.Contains (FontName);
			}

		/// <summary>
		/// Метод добавляет шрифт в список пропускаемых
		/// </summary>
		/// <param name="FontName">Название шрифта</param>
		public void AddSkippingFont (string FontName)
			{
			if (FillingRequired.Checked && !skippingFonts.Contains (FontName))
				skippingFonts.Add (FontName);
			}

		/// <summary>
		/// Флаг указывает на необходимость повторного заполнения списка
		/// </summary>
		public bool FillingIsRequired
			{
			get
				{
				return FillingRequired.Checked;
				}
			}

		/// <summary>
		/// Метод снимает необходимость заполнения списка
		/// </summary>
		public void FinishFilling ()
			{
			if (FillingRequired.Checked)
				{
				skippingFonts.Sort ();
				FillingRequired.Checked = false;
				}
			}

		/// <summary>
		/// Возвращает количество пропускаемых шрифтов
		/// </summary>
		public uint SkippingFontsCount
			{
			get
				{
				return (uint)skippingFonts.Count;
				}
			}

		// Просмотр шрифта
		private void SkippingFontsListBox_DoubleClick (object sender, EventArgs e)
			{
			// Контроль
			if (SkippingFontsListBox.SelectedIndex < 0)
				return;

			// Формирование и отображение изображения
			Bitmap createdImage;
			FontFamily fontFamily = null;
			try
				{
				fontFamily = new FontFamily (SkippingFontsListBox.Items[SkippingFontsListBox.SelectedIndex].ToString ());
				}
			catch
				{
				MessageBox.Show (Localization.GetText ("WrongFont", al), ProgramDescription.AssemblyTitle,
					MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return;
				}

			ImageProcessor.CreateBitmapFromFont (sampleText, fontFamily, 150, FontStyle.Regular,
				false, false, out createdImage);
			if (createdImage == null)
				return;

			PreviewForm pf = new PreviewForm (createdImage, fontFamily.Name);

			// Завершение
			pf.Dispose ();
			createdImage.Dispose ();
			}

		private void ExistentFontsListBox_DoubleClick (object sender, EventArgs e)
			{
			// Контроль
			if (ExistentFontsListBox.SelectedIndex < 0)
				return;

			// Формирование и отображение изображения
			Bitmap createdImage;
			FontFamily ff = (FontFamily)ExistentFontsListBox.Items[ExistentFontsListBox.SelectedIndex];

			ImageProcessor.CreateBitmapFromFont (sampleText, ff, 150, FontStyle.Regular,
				false, false, out createdImage);
			if (createdImage == null)
				return;

			PreviewForm pf = new PreviewForm (createdImage, ff.Name);

			// Завершение
			pf.Dispose ();
			createdImage.Dispose ();
			}
		}
	}
