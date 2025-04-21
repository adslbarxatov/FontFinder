using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Windows.Forms;

namespace RD_AAOW
	{
	/// <summary>
	/// Форма настройки пропускаемых шрифтов
	/// </summary>
	public partial class SkipListProcessor: Form
		{
		// Переменные и константы
		private List<string> skippingFonts = [];
		private string newSkippingFontsListFile = RDGenerics.AppStartupPath +
			ProgramDescription.AssemblyMainName + "." + ProgramDescription.SkipFileExtension;

		private string sampleText;
		private bool changed = false;

		/// <summary>
		/// Возвращает список шрифтов операционной системы
		/// </summary>
		public FontFamily[] ExistingFonts
			{
			get
				{
				return existingFonts;
				}
			}
		private FontFamily[] existingFonts;

		/// <summary>
		/// Конструктор. Загружает данные о пропускаемых шрифтах
		/// </summary>
		public SkipListProcessor ()
			{
			// Инициализация
			InitializeComponent ();
			RDGenerics.LoadWindowDimensions (this);

			// Получение списка шрифтов системы
			InstalledFontCollection ifc = new InstalledFontCollection ();
			existingFonts = ifc.Families;
			ifc.Dispose ();

			ExistentFontsListBox.DataSource = existingFonts;
			ExistentFontsListBox.DisplayMember = ExistentFontsListBox.ValueMember = "Name";

			// Загрузка файла
			/*FileStream FS = null;*/
			FileStream FS;
			try
				{
				FS = new FileStream (newSkippingFontsListFile, FileMode.Open);
				}
			catch
				{
				FillingRequired.Checked = true;
				return;
				}
			StreamReader SR = new StreamReader (FS, RDGenerics.GetEncoding (RDEncodings.Unicode16));

			while (!SR.EndOfStream)
				skippingFonts.Add (SR.ReadLine ());

			// Завершено
			SR.Close ();
			FS.Close ();
			}

		/// <summary>
		/// Метод открывает окно ручного управления списком
		/// </summary>
		/// <param name="SampleText">Образец текста для предпросмотра шрифтов</param>
		public void EditList (string SampleText)
			{
			// Инициализация
			if (!string.IsNullOrWhiteSpace (SampleText))
				sampleText = SampleText;
			else
				sampleText = RDLocale.GetText ("SampleText");

			// Настройка
			this.Text = RDLocale.GetText ("SkipListProcessorCaption");
			BExit.Text = RDLocale.GetDefaultText (RDLDefaultTexts.Button_Close);
			BClear.Text = RDLocale.GetText ("BClear");

			FillingRequired.Text = RDLocale.GetText ("FillingRequiredText");
			ExistentLabel.Text = string.Format (RDLocale.GetText ("ExistentLabelText"),
				ExistentFontsListBox.Items.Count);

			// Запуск
			SkippingFontsListBox.DataSource = null;
			SkippingFontsListBox.DataSource = skippingFonts;
			SkippingLabel.Text = string.Format (RDLocale.GetText ("SkippingLabelText"),
				SkippingFontsListBox.Items.Count);

			this.ShowDialog ();
			}

		// Добавление шрифта
		private void BAdd_Click (object sender, EventArgs e)
			{
			// Контроль
			if (ExistentFontsListBox.SelectedIndex < 0)
				return;

			// Добавление
			string s = ExistentFontsListBox.SelectedValue.ToString ();

			if (!skippingFonts.Contains (s))
				skippingFonts.Add (s);
			skippingFonts.Sort ();

			// Передача в списки
			SkippingFontsListBox.DataSource = null;
			SkippingFontsListBox.DataSource = skippingFonts;
			SkippingLabel.Text = string.Format (RDLocale.GetText ("SkippingLabelText"),
				SkippingFontsListBox.Items.Count);

			changed = true;
			}

		// Выход
		private void BExit_Click (object sender, EventArgs e)
			{
			this.Close ();
			}

		private void SkipListProcessor_FormClosing (object sender, FormClosingEventArgs e)
			{
			RDGenerics.SaveWindowDimensions (this);
			}

		// Удаление шрифта
		private void BRemove_Click (object sender, EventArgs e)
			{
			// Контроль
			if (SkippingFontsListBox.SelectedIndex < 0)
				return;

			// Удаление
			skippingFonts.RemoveAt (SkippingFontsListBox.SelectedIndex);

			// Обновление списков
			SkippingFontsListBox.DataSource = null;
			SkippingFontsListBox.DataSource = skippingFonts;
			SkippingLabel.Text = string.Format (RDLocale.GetText ("SkippingLabelText"),
				SkippingFontsListBox.Items.Count);

			changed = true;
			}

		// Очистка списка шрифтов
		private void BClear_Click (object sender, EventArgs e)
			{
			// Контроль
			if (RDInterface.LocalizedMessageBox (RDMessageTypes.Warning_Center, "ClearSkippingFonts",
				RDLDefaultTexts.Button_YesNoFocus, RDLDefaultTexts.Button_No) !=
				RDMessageButtons.ButtonOne)
				return;

			// Сброс
			skippingFonts.Clear ();

			// Обновление
			SkippingFontsListBox.DataSource = null;
			SkippingFontsListBox.DataSource = skippingFonts;
			SkippingLabel.Text = string.Format (RDLocale.GetText ("SkippingLabelText"),
				SkippingFontsListBox.Items.Count);

			changed = true;
			}

		/// <summary>
		/// Метод сохраняет текущий список пропускаемых шрифтов
		/// </summary>
		public void SaveList ()
			{
			// Контроль
			if (!changed)
				return;

			// Загрузка файла
			/*FileStream FS = null;*/
			FileStream FS;
			try
				{
				FS = new FileStream (newSkippingFontsListFile, FileMode.Create);
				}
			catch
				{
				return;
				}
			StreamWriter SW = new StreamWriter (FS, RDGenerics.GetEncoding (RDEncodings.Unicode16));

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
				{
				skippingFonts.Add (FontName);
				changed = true;
				}
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
				changed = true;
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
			/*FontFamily fontFamily = null;*/
			FontFamily fontFamily;
			try
				{
				fontFamily = new FontFamily (SkippingFontsListBox.Items[SkippingFontsListBox.SelectedIndex].ToString ());
				}
			catch
				{
				RDInterface.LocalizedMessageBox (RDMessageTypes.Warning_Center, "WrongFont");
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

		// Клавиатурное управление
		private void ExistentFontsListBox_KeyDown (object sender, KeyEventArgs e)
			{
			switch (e.KeyCode)
				{
				case Keys.Right:
				case Keys.Insert:
					BAdd_Click (null, null);
					break;

				case Keys.Space:
				case Keys.Return:
					ExistentFontsListBox_DoubleClick (null, null);
					break;
				}
			}

		private void SkippingFontsListBox_KeyDown (object sender, KeyEventArgs e)
			{
			switch (e.KeyCode)
				{
				case Keys.Left:
				case Keys.Delete:
					BRemove_Click (null, null);
					break;

				case Keys.Space:
				case Keys.Return:
					ExistentFontsListBox_DoubleClick (null, null);
					break;
				}
			}
		}
	}
