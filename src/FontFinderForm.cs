using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace RD_AAOW
	{
	/// <summary>
	/// Главная форма программы
	/// </summary>
	public partial class MainForm: Form
		{
		// Исходное изображение
		private Bitmap image = null;

		// Интерфейсные параметры, передаваемые в поисковый процесс
		private string imageText = "";
		private double searchPauseFactor = 90.0;
		private bool pauseSearch = false;

		// Найденные шрифты
		private List<FontFamily> foundFF = new List<FontFamily> ();

		// Оценки степени их соответствия исходному изображению
		private List<double> foundFFMatch = new List<double> ();

		// Стиль шрифта для поиска
		private FontStyle searchFontStyle = FontStyle.Regular;
		private SkipListProcessor slp = new SkipListProcessor ();

		// Максимальная длина строки для сравнения
		private const uint MaxSearchStringLength = 50;

		// Максимальное количество отображаемых результатов
		private const uint MaxResultsCount = 100;

		// Минимальный порог прерывания поиска
		private const uint MinValidationLimit = 50;

		// Максимальный порог прерывания поиска
		private const uint MaxValidationLimit = 99;

		/// <summary>
		/// Конструктор. Создаёт главную форму программы
		/// </summary>
		public MainForm ()
			{
			// Первичная настройка
			InitializeComponent ();

			// Настройка контролов
			this.Text = ProgramDescription.AssemblyTitle;
			RDGenerics.LoadWindowDimensions (this);

			LoadedPicText.MaxLength = (int)MaxSearchStringLength;
			SearchPauseFactor.Minimum = MinValidationLimit;
			SearchPauseFactor.Maximum = MaxValidationLimit;

			LanguageCombo.Items.AddRange (RDLocale.LanguagesNames);
			try
				{
				LanguageCombo.SelectedIndex = (int)RDLocale.CurrentLanguage;
				}
			catch
				{
				LanguageCombo.SelectedIndex = 0;
				}

			// Контроль прав
			if (!RDGenerics.IsStartupPathAccessible)
				{
				BSkipping.Enabled = false;
				this.Text += RDLocale.GetDefaultText (RDLDefaultTexts.Message_LimitedFunctionality);
				}
			}

		// Выбор изображения
		private void SelectImage_Click (object sender, EventArgs e)
			{
			// Отключение кнопки поиска (на всякий случай)
			StartSearch.Enabled = false;

			// Запуск диалога
			OpenImage.FileName = "";
			OpenImage.ShowDialog ();
			}

		// Изображение выбрано
		private void OpenImage_FileOk (object sender, CancelEventArgs e)
			{
			// Проверка изображения
			ImageLoader il = new ImageLoader (OpenImage.FileName);

			if (il.InitStatus != ImageLoaderStatuses.Ok)
				{
				switch (il.InitStatus)
					{
					case ImageLoaderStatuses.FileNotFound:
						RDGenerics.MessageBox (RDMessageTypes.Warning_Left,
							string.Format (RDLocale.GetDefaultText (RDLDefaultTexts.Message_LoadFailure_Fmt),
							OpenImage.FileName));
						break;

					case ImageLoaderStatuses.FileIsNotAnImage:
						RDGenerics.LocalizedMessageBox (RDMessageTypes.Warning_Center, "FileIsNotAnImage");
						break;
					}

				return;
				}

			// Получение изображения и контроль
			if (image != null)
				image.Dispose ();
			image = il.GetBlackArea ((uint)LoadedPicture.Width, (uint)LoadedPicture.Height);
			il.Dispose ();

			if (image == null)  // Не удалось найти границы
				{
				RDGenerics.LocalizedMessageBox (RDMessageTypes.Warning_Left, "CannotFindText");
				return;
				}

			// Обработка
			if (LoadedPicture.BackgroundImage != null)
				LoadedPicture.BackgroundImage.Dispose ();
			LoadedPicture.BackgroundImage = (Bitmap)image.Clone ();

			// Активация контролов
			StartSearch.Enabled = true;
			}

		// Запуск поиска
		private void StartSearch_Click (object sender, EventArgs e)
			{
			if (string.IsNullOrWhiteSpace (LoadedPicText.Text))
				{
				RDGenerics.LocalizedMessageBox (RDMessageTypes.Warning_Center, "SpecifyTextFromImage");
				return;
				}

			// Блокировка окна
			Label02.Enabled = Label03.Enabled = Label05.Enabled = Label06.Enabled =
				SelectImage.Enabled = LoadedPicture.Visible = LoadedPicText.Enabled =
				CBold.Enabled = CItalic.Enabled = CUnder.Enabled = CStrike.Enabled =
				PauseSearch.Enabled = SearchPauseFactor.Enabled = ViewBox.Visible =
				StartSearch.Enabled = LanguageCombo.Enabled = BExit.Enabled = BSkipping.Enabled = false;

			// Считывание параметров поиска
			imageText = LoadedPicText.Text;
			searchPauseFactor = (double)SearchPauseFactor.Value;
			pauseSearch = PauseSearch.Checked;

			// Настройка стиля поиска
			CBold_CheckedChanged (null, null);

			// Сброс всех результатов
			ResultsList.Items.Clear ();
			ResultsList.Enabled = false;
			foundFF.Clear ();
			foundFFMatch.Clear ();

			// Запуск потока поиска и ожидание его завершения
			RDGenerics.RunWork (Search, null, null, RDRunWorkFlags.AllowOperationAbort);
			slp.FinishFilling ();

			// Деблокировка окна
			Label02.Enabled = Label03.Enabled = Label05.Enabled = Label06.Enabled =
				SelectImage.Enabled = LoadedPicture.Visible = LoadedPicText.Enabled =
				CBold.Enabled = CItalic.Enabled = CUnder.Enabled = CStrike.Enabled =
				PauseSearch.Enabled = ViewBox.Visible = StartSearch.Enabled =
				LanguageCombo.Enabled = BExit.Enabled = BSkipping.Enabled = true;
			SearchPauseFactor.Enabled = PauseSearch.Checked;

			// Выгрузка результатов
			for (int i = 0; i < ((MaxResultsCount > foundFF.Count) ? foundFF.Count : (int)MaxResultsCount); i++)
				ResultsList.Items.Add (foundFF[i].Name + " (" + foundFFMatch[i].ToString ("F2") + "%)");
			ResultsList.Enabled = true;
			}

		// Поток поиска шрифта
		private void Search (object sender, DoWorkEventArgs e)
			{
			// Переменные
			Bitmap createdImage;
			BackgroundWorker bw = ((BackgroundWorker)sender);

			// Поиск
			double maxRes = 0;
			for (int i = 0; i < slp.ExistentFonts.Length; i++)
				{
				// Проверка на пропуск
				if (slp.FontMustBeSkipped (slp.ExistentFonts[i].Name))
					continue;

				// Создание изображения с выбранным шрифтом
				FontStyle resultStyle = ImageProcessor.CreateBitmapFromFont (imageText, slp.ExistentFonts[i],
					image.Height, searchFontStyle, CUnder.Checked, CStrike.Checked, out createdImage);

				// Отображение прогресса
				string msg = string.Format (RDLocale.GetText ("ProcessingMessage"), i, slp.ExistentFonts.Length,
					slp.ExistentFonts[i].Name);
				if (resultStyle != searchFontStyle)
					msg += string.Format (RDLocale.GetText ("ProcessingStyle"), resultStyle.ToString ());
				msg += string.Format (RDLocale.GetText ("SkippingFontsCountAndPercentage"),
					slp.SkippingFontsCount, maxRes.ToString ("F2"));

				bw.ReportProgress ((int)(HardWorkExecutor.ProgressBarSize * i / slp.ExistentFonts.Length), msg);

				// Защита
				if (createdImage == null)
					continue;

				// Сравнение
				double res = 0;
				try
					{
					res = ImageProcessor.Compare (image, createdImage);
					// Иногда имеют место сбои обращения к изображению
					}
				catch { }

				// Отсечение совпадающих результатов
				// (исходим из предположения, что разные шрифты не могут давать одинаковый результат сравнения)
				if (foundFFMatch.Contains (res))
					{
					if (slp.FillingIsRequired)
						slp.AddSkippingFont (slp.ExistentFonts[i].Name);
					continue;
					}

				// Запрос на прерывание поиска
				if (pauseSearch && (res >= searchPauseFactor))
					{
					PreviewForm prf = new PreviewForm (createdImage, slp.ExistentFonts[i].Name +
						", " + resultStyle.ToString ());

					if (RDGenerics.LocalizedMessageBox (RDMessageTypes.Question_Center, "FinishSearch",
						RDLDefaultTexts.Button_Yes, RDLDefaultTexts.Button_No) == RDMessageButtons.ButtonOne)
						{
						e.Cancel = true;
						}
					}

				// Запись результата сравнения с сортировкой
				int j = 0;
				for (; j < foundFF.Count; j++)
					{
					if (foundFFMatch[j] < res)
						break;
					}

				foundFF.Insert (j, slp.ExistentFonts[i]);
				foundFFMatch.Insert (j, res);

				// Обрезка списка результатов снизу
				if (foundFF.Count > MaxResultsCount)
					{
					foundFF.RemoveAt ((int)MaxResultsCount);
					foundFFMatch.RemoveAt ((int)MaxResultsCount);
					}

				// Очистка памяти
				createdImage.Dispose ();

				// Обновление максимума
				if (maxRes < res)
					maxRes = res;

				// Завершение работы, если получено требование от диалога
				if (bw.CancellationPending || e.Cancel)
					{
					e.Cancel = true;
					return;
					}
				}
			}

		// Выход из программы
		private void BExit_Click (object sender, EventArgs e)
			{
			this.Close ();
			}

		private void MainForm_FormClosing (object sender, FormClosingEventArgs e)
			{
			// Сохранение
			RDGenerics.SaveWindowDimensions (this);
			slp.SaveList ();
			}

		// Справочные сведения
		private void Q5_Click (object sender, EventArgs e)
			{
			RDGenerics.ShowAbout (false);
			}

		// Выбор пункта для предпросмотра
		private void ResultsList_SelectedIndexChanged (object sender, EventArgs e)
			{
			if (ResultsList.SelectedIndex < 0)
				return;

			// Проверка на наличие текста
			if (LoadedPicText.Text == "")
				{
				RDGenerics.LocalizedMessageBox (RDMessageTypes.Warning_Center, "EmptyTextField");
				return;
				}

			// Запуск просмотра
			if (ViewBox.BackgroundImage != null)
				ViewBox.BackgroundImage.Dispose ();

			Bitmap createdImage;
			ImageProcessor.CreateBitmapFromFont (LoadedPicText.Text, foundFF[ResultsList.SelectedIndex],
				ViewBox.Height, searchFontStyle, CUnder.Checked, CStrike.Checked, out createdImage);
			if (createdImage == null)
				return;

			ViewBox.BackgroundImage = createdImage;
			}

		// Установка или снятие галочки
		private void PauseSearch_CheckedChanged (object sender, EventArgs e)
			{
			SearchPauseFactor.Enabled = PauseSearch.Checked;
			}

		// Настройка стиля поиска
		private void CBold_CheckedChanged (object sender, EventArgs e)
			{
			searchFontStyle = FontStyle.Regular;
			if (CBold.Checked)
				searchFontStyle |= FontStyle.Bold;
			if (CItalic.Checked)
				searchFontStyle |= FontStyle.Italic;
			if (CUnder.Checked)
				searchFontStyle |= FontStyle.Underline;
			if (CStrike.Checked)
				searchFontStyle |= FontStyle.Strikeout;
			}

		// Локализация формы
		private void LanguageCombo_SelectedIndexChanged (object sender, EventArgs e)
			{
			// Сохранение языка
			RDLocale.CurrentLanguage = (RDLanguages)LanguageCombo.SelectedIndex;

			// Локализация
			OpenImage.Filter = RDLocale.GetText ("OpenImageFilter") +
				" (*.bmp, *.gif, *.jpe, *.jpeg, *.jpg, *.jfif, *.png)|" +
				"*.bmp;*.gif;*.jpe;*.jpeg;*.jpg;*.jfif;*.png";
			OpenImage.Title = RDLocale.GetText ("OpenImageTitle");

			SelectImage.Text = RDLocale.GetText ("SelectImageText");
			Label02.Text = RDLocale.GetText ("Label02Text");
			Label03.Text = RDLocale.GetText ("Label03Text");
			CBold.Text = RDLocale.GetText ("CBoldText");
			CItalic.Text = RDLocale.GetText ("CItalicText");
			CUnder.Text = RDLocale.GetText ("CUnderText");
			CStrike.Text = RDLocale.GetText ("CStrikeText");
			PauseSearch.Text = RDLocale.GetText ("PauseSearchText");
			StartSearch.Text = RDLocale.GetText ("StartSearchText");
			Label05.Text = RDLocale.GetText ("Label05Text");
			BExit.Text = RDLocale.GetDefaultText (RDLDefaultTexts.Button_Exit);
			Label06.Text = RDLocale.GetText ("Label06Text");
			BSkipping.Text = RDLocale.GetText ("BSkippingText");
			Q5.Text = RDLocale.GetDefaultText (RDLDefaultTexts.Control_AppAbout);
			}

		// Работа с пропущенными шрифтами
		private void BSkipping_Click (object sender, EventArgs e)
			{
			slp.EditList (LoadedPicText.Text);
			}
		}
	}
