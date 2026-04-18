using System;
using System.Collections.Generic;
using System.ComponentModel;
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
		private List<string> fontFilesPaths = [];

		private string newSkippingFontsListFile = ProgramDescription.AssemblyMainName + "." +
			ProgramDescription.SkipFileExtension;

		private string sampleText;
		private bool changed = false;

		/// <summary>
		/// Возвращает список шрифтов операционной системы
		/// </summary>
		public FontFamily[] ExistingFonts
			{
			get
				{
				if (DontUseSystemFonts)
					return [];

				return existingFonts;
				}
			}
		private FontFamily[] existingFonts;

		/// <summary>
		/// Возвращает список шрифтов из файлов, кэшированных из указанных директорий
		/// </summary>
		public FontFamily[] FontsFromFiles
			{
			get
				{
				if (DontUseFontsFromFiles)
					return [];

				// Подготовленный список
				if (fontsFromFilesCached)
					return fontsFromFiles.ToArray ();

				// Кэширование
				RDInterface.RunWork (CacheFontFiles, null, " ", RDRunWorkFlags.CaptionInTheMiddle);
				return fontsFromFiles.ToArray ();
				}
			}
		private List<FontFamily> fontsFromFiles = [];
		private bool fontsFromFilesCached = false;

		// Метод кэширования шрифтов
		private void CacheFontFiles (object sender, DoWorkEventArgs e)
			{
			BackgroundWorker bw = (BackgroundWorker)sender;

			// Сбор списка файлов
			bw.ReportProgress ((int)RDWorkerForm.ProgressBarSize, RDLocale.GetText ("FontFilesPathsCollection"));

			List<string> files = [];
			for (int i = 0; i < fontFilesPaths.Count; i++)
				{
				try
					{
					files.AddRange (Directory.GetFiles (fontFilesPaths[i], "*.ttf", SearchOption.AllDirectories));
					}
				catch { }

				try
					{
					files.AddRange (Directory.GetFiles (fontFilesPaths[i], "*.otf", SearchOption.AllDirectories));
					}
				catch { }
				}

			// Попытка извлечения шрифтов
			fontsFromFiles.Clear ();
			PrivateFontCollection collection = new PrivateFontCollection ();

			for (int i = 0; i < files.Count; i++)
				{
				bw.ReportProgress ((int)RDWorkerForm.ProgressBarSize * (i + 1) / files.Count,
					string.Format (RDLocale.GetText ("FontFilesCaching"), i + 1, files.Count));

				try
					{
					collection.AddFontFile (files[i]);
					}
				catch { }
				}

			// Завершено
			fontsFromFiles.AddRange (collection.Families);
			fontsFromFilesCached = true;
			e.Result = 0;
			}

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
			FileStream FS;
			try
				{
				if (RDGenerics.StartedFromMSStore)
					// Не уверен, что это на что-то влияет
					FS = new FileStream (newSkippingFontsListFile, FileMode.Open);
				else
					FS = new FileStream (RDGenerics.AppStartupPath + newSkippingFontsListFile, FileMode.Open);
				}
			catch
				{
				FillingRequired.Checked = true;
				return;
				}
			StreamReader SR = new StreamReader (FS, RDGenerics.GetEncoding (RDEncodings.Unicode16));

			// Названия шрифтов
			string s;
			while (!string.IsNullOrWhiteSpace (s = SR.ReadLine ()))
				skippingFonts.Add (s);

			// Пути к файлам шрифтов
			while (!string.IsNullOrWhiteSpace (s = SR.ReadLine ()))
				fontFilesPaths.Add (s);

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
			BClear.Text = RDLocale.GetDefaultText (RDLDefaultTexts.Button_Clear);
			BExtended.Text = RDLocale.GetText ("BExtended");

			FillingRequired.Text = RDLocale.GetText ("FillingRequiredText");
			ExistentLabel.Text = string.Format (RDLocale.GetText ("ExistentLabelText"),
				ExistentFontsListBox.Items.Count);

			// Запуск
			SkippingFontsListBox.DataSource = null;
			SkippingFontsListBox.DataSource = skippingFonts;
			SkippingLabel.Text = string.Format (RDLocale.GetText ("SkippingLabelText"),
				SkippingFontsListBox.Items.Count);

			DirectoriesListBox.DataSource = null;
			DirectoriesListBox.DataSource = fontFilesPaths;
			FilesLabel.Text = RDLocale.GetText ("FilesLabel");
			BRemovePath.Enabled = (fontFilesPaths.Count > 0);

			UseSystemFontsFlag.Text = RDLocale.GetText ("UseSystemFontsFlag");
			UseSystemFontsFlag.Checked = !DontUseSystemFonts;

			UseFileFontsFlag.Text = RDLocale.GetText ("UseFileFontsFlag");
			UseFileFontsFlag.Checked = !DontUseFontsFromFiles;

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
			DontUseSystemFonts = !UseSystemFontsFlag.Checked;
			DontUseFontsFromFiles = !UseFileFontsFlag.Checked;
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
			if (RDInterface.LocalizedMessageBox (RDMessageFlags.Warning | RDMessageFlags.CenterText,
				"ClearSkippingFonts", RDLDefaultTexts.Button_YesNoFocus, RDLDefaultTexts.Button_No) !=
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
			FileStream FS;
			try
				{
				if (RDGenerics.StartedFromMSStore)
					FS = new FileStream (newSkippingFontsListFile, FileMode.Create);
				else
					FS = new FileStream (RDGenerics.AppStartupPath + newSkippingFontsListFile, FileMode.Create);
				}
			catch
				{
				return;
				}
			StreamWriter SW = new StreamWriter (FS, RDGenerics.GetEncoding (RDEncodings.Unicode16));

			for (int i = 0; i < skippingFonts.Count; i++)
				SW.WriteLine (skippingFonts[i]);

			SW.WriteLine ();

			for (int i = 0; i < fontFilesPaths.Count; i++)
				SW.WriteLine (fontFilesPaths[i]);

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
			FontFamily fontFamily;
			try
				{
				fontFamily = new FontFamily (SkippingFontsListBox.Items[SkippingFontsListBox.SelectedIndex].ToString ());
				}
			catch
				{
				RDInterface.LocalizedMessageBox (RDMessageFlags.Warning | RDMessageFlags.CenterText,
					"WrongFont");
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

		// Дополнительная фильтрация шрифтов
		private void BExtended_Click (object sender, EventArgs e)
			{
			// Запрос образца
			string letters = RDInterface.LocalizedMessageBox ("ExtendedFilterRequest", false, 2);

			if (string.IsNullOrWhiteSpace (letters) || (letters.Length < 2) || (letters[0] == letters[1]))
				{
				RDInterface.LocalizedMessageBox (RDMessageFlags.Warning | RDMessageFlags.CenterText, "ExtendedFilterError");
				return;
				}
			efChars[0] = letters[0].ToString ();
			efChars[1] = letters[1].ToString ();

			// Прогон
			int oldCount = skippingFonts.Count;
			RDInterface.RunWork (ExtendedFiltration, null, " ", RDRunWorkFlags.AllowOperationAbort);

			// Завершено
			RDInterface.MessageBox (RDMessageFlags.Success | RDMessageFlags.CenterText,
				string.Format (RDLocale.GetText ("ExtendedFilterResult"), skippingFonts.Count - oldCount));

			// Обновление списков
			SkippingFontsListBox.DataSource = null;
			SkippingFontsListBox.DataSource = skippingFonts;
			SkippingLabel.Text = string.Format (RDLocale.GetText ("SkippingLabelText"),
				SkippingFontsListBox.Items.Count);
			}
		private string[] efChars = new string[2];

		// Метод прогона
		private void ExtendedFiltration (object sender, DoWorkEventArgs e)
			{
			BackgroundWorker bw = ((BackgroundWorker)sender);
			int length = existingFonts.Length;
			FontStyle style = FontStyle.Regular;
			List<string> protectedNames = [];

			// Создание контрольных образцов, используемых в качестве подмены
			// в неподдерживаемых шрифтах
			Bitmap ba;
			FontFamily family = new FontFamily (GenericFontFamilies.SansSerif);
			_ = ImageProcessor.CreateBitmapFromFont (efChars[1], family, 40, style, false, false, out ba);

			byte[] arial = ImageProcessor.MakeArray (ba);
			Size arialSize = ba.Size;
			protectedNames.Add (family.Name);

			ba.Dispose ();
			family.Dispose ();

			family = new FontFamily (GenericFontFamilies.Serif);
			_ = ImageProcessor.CreateBitmapFromFont (efChars[1], family, 40, style, false, false, out ba);

			byte[] times = ImageProcessor.MakeArray (ba);
			Size timesSize = ba.Size;
			protectedNames.Add (family.Name);

			ba.Dispose ();
			family.Dispose ();

			family = new FontFamily (GenericFontFamilies.Monospace);
			_ = ImageProcessor.CreateBitmapFromFont (efChars[1], family, 40, style, false, false, out ba);

			byte[] courier = ImageProcessor.MakeArray (ba);
			Size courierSize = ba.Size;
			protectedNames.Add (family.Name);

			ba.Dispose ();
			family.Dispose ();

			// Прогон
			for (int i = 0; i < length; i++)
				{
				// Контроль
				string name = existingFonts[i].Name;
				if (skippingFonts.Contains (name))
					continue;

				// Завершение работы, если получено требование от диалога
				if (bw.CancellationPending)
					{
					e.Cancel = true;
					return;
					}

				// Сравнение
				Bitmap b1;
				_ = ImageProcessor.CreateBitmapFromFont (efChars[0], existingFonts[i], 40, style, false, false, out b1);
				if (b1 == null)
					continue;

				Bitmap b2;
				_ = ImageProcessor.CreateBitmapFromFont (efChars[1], existingFonts[i], 40, style, false, false, out b2);
				if (b2 == null)
					{
					b2.Dispose ();
					continue;
					}

				byte[] s1 = ImageProcessor.MakeArray (b1);
				double res = 0, ares = 0, tres = 0, cres = 0;
				try
					{
					res = ImageProcessor.Compare (s1, b1.Size, b2);
					ares = ImageProcessor.Compare (arial, arialSize, b2);
					tres = ImageProcessor.Compare (times, timesSize, b2);
					cres = ImageProcessor.Compare (courier, courierSize, b2);
					}
				catch { }

				b1.Dispose ();
				b2.Dispose ();

				// Добавление
				if ((res > 99) || (ares > 99) || (tres > 99) || (cres > 99))
					{
					if (!skippingFonts.Contains (name) && !protectedNames.Contains (name))
						{
						skippingFonts.Add (name);
						changed = true;
						}
					}

				// Возврат прогресса
				string msg = string.Format (RDLocale.GetText ("ProcessingMessage"), i, length, name);
				msg += string.Format (RDLocale.GetText ("SkippingFontsCountAndPercentage"),
					skippingFonts.Count, res.ToString ("F2"));

				bw.ReportProgress ((int)(RDWorkerForm.ProgressBarSize * i / length), msg);
				}

			// Завершено
			e.Result = 0;
			}

		// Добавление директории с внешними шрифтами
		private void BAddPath_Click (object sender, EventArgs e)
			{
			if (FBDialog.ShowDialog () != DialogResult.OK)
				return;

			string s = FBDialog.SelectedPath;
			if (!s.EndsWith ('\\'))
				s += "\\";

			fontFilesPaths.Add (s);
			DirectoriesListBox.DataSource = null;
			DirectoriesListBox.DataSource = fontFilesPaths;

			BRemovePath.Enabled = (fontFilesPaths.Count > 0);

			fontsFromFilesCached = false;
			changed = true;
			}

		// Удаление директории с внешними шрифтами
		private void BRemovePath_Click (object sender, EventArgs e)
			{
			if (DirectoriesListBox.SelectedIndex < 0)
				return;

			fontFilesPaths.RemoveAt (DirectoriesListBox.SelectedIndex);
			DirectoriesListBox.DataSource = null;
			DirectoriesListBox.DataSource = fontFilesPaths;

			BRemovePath.Enabled = (fontFilesPaths.Count > 0);

			fontsFromFilesCached = false;
			changed = true;
			}

		/// <summary>
		/// Возвращает или задаёт флаг пропуска системных шрифтов
		/// </summary>
		public static bool DontUseSystemFonts
			{
			get
				{
				return RDGenerics.GetSettings (dontUseSystemFontsPar, false);
				}
			set
				{
				RDGenerics.SetSettings (dontUseSystemFontsPar, value);
				}
			}
		private const string dontUseSystemFontsPar = "DontUseSystemFonts";

		/// <summary>
		/// Возвращает или задаёт флаг пропуска файловых шрифтов
		/// </summary>
		public static bool DontUseFontsFromFiles
			{
			get
				{
				return RDGenerics.GetSettings (dontUseFontsFromFilesPar, false);
				}
			set
				{
				RDGenerics.SetSettings (dontUseFontsFromFilesPar, value);
				}
			}
		private const string dontUseFontsFromFilesPar = "DontUseFontsFromFiles";
		}
	}
