using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Text;
using System.Windows.Forms;

namespace RD_AAOW
	{
	/// <summary>
	/// Главная форма программы
	/// </summary>
	public partial class MainForm:Form
		{
		// Переменные
		private Bitmap image = null;									// Исходное изображение
		private string imageText = "";									// Текст на нём
		private ImageCreator ic;										// Оператор, формирующий образцы шрифтов
		private List<FontFamily> foundFF = new List<FontFamily> ();		// Найденные шрифты
		private List<double> foundFFMatch = new List<double> ();		// Оценки степени их соответствия исходному изображению
		private FontStyle searchFontStyle = FontStyle.Regular;			// Стиль шрифта для поиска
		private FontStyle[] otherStyles = { FontStyle.Regular,			// Стили, замещающие текущий в случае его недопустимости
											  FontStyle.Bold ,
											  FontStyle.Italic,
											  FontStyle.Bold | FontStyle.Italic };
		private SupportedLanguages al = Localization.CurrentLanguage;
		private SkipListProcessor slp = new SkipListProcessor ();

		private double searchPauseFactor = 90.0;		// Порог срабатывания правила приостановки поиска

		// Ограничительные константы
		private const uint MaxSearchStringLength = 50;	// Максимальная длина строки для сравнения
		private const uint MaxResultsCount = 100;		// Максимальное количество отображаемых результатов
		private const uint MinValidationLimit = 50;		// Минимальный порог прерывания поиска
		private const uint MaxValidationLimit = 99;		// Максимальный порог прерывания поиска

		/// <summary>
		/// Конструктор. Создаёт главную форму программы
		/// </summary>
		public MainForm ()
			{
			// Первичная настройка
			InitializeComponent ();

			// Настройка контролов
			this.Text = ProgramDescription.AssemblyTitle;

			LoadedPicText.MaxLength = (int)MaxSearchStringLength;
			SearchPauseFactor.Minimum = MinValidationLimit;
			SearchPauseFactor.Maximum = MaxValidationLimit;

			for (int i = 0; i < Localization.AvailableLanguages; i++)
				LanguageCombo.Items.Add (((SupportedLanguages)i).ToString ());
			try
				{
				LanguageCombo.SelectedIndex = (int)al;
				}
			catch
				{
				LanguageCombo.SelectedIndex = 0;
				}
			}

		// Выбор изображения
		private void SelectImage_Click (object sender, System.EventArgs e)
			{
			// Отключение кнопки поиска (на всякий случай)
			StartSearch.Enabled = false;

			// Запуск диалога
			OpenImage.FileName = "";
			OpenImage.ShowDialog ();
			}

		// Изображение выбрано
		private void OpenImage_FileOk (object sender, System.ComponentModel.CancelEventArgs e)
			{
			// Проверка изображения
			ImageLoader il = new ImageLoader (OpenImage.FileName);

			if (il.InitStatus != ImageLoaderStatuses.Ok)
				{
				switch (il.InitStatus)
					{
					case ImageLoaderStatuses.FileNotFound:
						MessageBox.Show (Localization.GetText ("FileNotFound", al),
							ProgramDescription.AssemblyTitle, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
						break;

					case ImageLoaderStatuses.FileIsNotAnImage:
						MessageBox.Show (Localization.GetText ("FileIsNotAnImage", al),
							ProgramDescription.AssemblyTitle, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
						break;
					}

				return;
				}

			// Сохранение
			if (image != null)
				image.Dispose ();
			image = il.GetBlackZone ();
			il.Dispose ();

			if ((image.Width > LoadedPicture.Width) || (image.Height > LoadedPicture.Height))
				MessageBox.Show (Localization.GetText ("LargePicture", al),
					ProgramDescription.AssemblyTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);

			if (LoadedPicture.BackgroundImage != null)
				LoadedPicture.BackgroundImage.Dispose ();
			LoadedPicture.BackgroundImage = (Bitmap)image.Clone ();

			// Активация контролов
			StartSearch.Enabled = true;
			}

		// Запуск поиска
		private void StartSearch_Click (object sender, System.EventArgs e)
			{
			if (LoadedPicText.Text == "")
				{
				MessageBox.Show (Localization.GetText ("SpecifyTextFromImage", al),
					ProgramDescription.AssemblyTitle, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
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

			// Настройка стиля поиска
			CBold_CheckedChanged (null, null);

			// Сброс всех результатов
			ResultsList.Items.Clear ();
			ResultsList.Enabled = false;
			foundFF.Clear ();
			foundFFMatch.Clear ();

			// Запуск потока поиска и ожидание его завершения
			HardWorkExecutor hwe = new HardWorkExecutor (Search);
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
			// Получение списка шрифтов системы
			InstalledFontCollection ifc = new InstalledFontCollection ();
			FontFamily[] ff = ifc.Families;
			ifc.Dispose ();

			// Поиск
			for (int i = 0; i < ff.Length; i++)
				{
				// Проверка на пропуск
				if (slp.FontMustBeSkipped (ff[i].Name))
					continue;

				// Создание изображения с выбранным шрифтом
				FontStyle resultStyle = CreateBitmapFromFont (imageText, ff[i], image.Height, searchFontStyle);
				if ((ic == null) || (ic.CreatedImage == null))
					continue;	// Здесь шрифты не пропускаем, т.к. есть шрифты, где лишь некоторые символы дают такой результат

				// Сравнение
				double res = 0;
				try
					{
					res = ImageComparer.Compare (image, ic.CreatedImage);	// Иногда имеют место сбои обращения к изображению
					}
				catch
					{
					}

				// Отсечение совпадающих результатов
				// (исходим из предположения, что разные шрифты не могут давать одинаковый результат сравнения)
				if (foundFFMatch.Contains (res))
					{
					if (slp.FillingIsRequired)
						slp.AddSkippingFont (ff[i].Name);
					continue;
					}

				// Запрос на прерывание поиска
				if (PauseSearch.Checked && (res >= searchPauseFactor))
					{
					PreviewForm prf = new PreviewForm (ic.CreatedImage, ff[i].Name + ", " + resultStyle.ToString ());
					if (MessageBox.Show (Localization.GetText ("FinishSearch", al),
						ProgramDescription.AssemblyTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
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

				foundFF.Insert (j, ff[i]);
				foundFFMatch.Insert (j, res);

				// Обрезка списка результатов снизу
				if (foundFF.Count > MaxResultsCount)
					{
					foundFF.RemoveAt ((int)MaxResultsCount);
					foundFFMatch.RemoveAt ((int)MaxResultsCount);
					}

				// Очистка памяти
				ic.Dispose ();

				// Возврат прогресса
				string msg = string.Format (Localization.GetText ("ProcessingMessage", al), i, ff.Length, ff[i].Name);
				if (resultStyle != searchFontStyle)
					msg += string.Format (Localization.GetText ("ProcessingStyle", al), resultStyle.ToString ());
				msg += string.Format (Localization.GetText ("SkippingFontsCount", al), slp.SkippingFontsCount);

				((BackgroundWorker)sender).ReportProgress ((int)(HardWorkExecutor.ProgressBarSize *
					(double)i / (double)ff.Length), msg);

				// Завершение работы, если получено требование от диалога
				if (((BackgroundWorker)sender).CancellationPending || e.Cancel)
					{
					e.Cancel = true;
					return;
					}
				}
			}

		// Метод формирует изображение шрифта
		private FontStyle CreateBitmapFromFont (string Text, FontFamily Font, float Size, FontStyle Style)
			{
			Font font = null;

			// Обработка указанного стиля (если возможно)
			try
				{
				if (Font.IsStyleAvailable (Style))
					{
					font = new Font (Font, Size, Style);
					ic = new ImageCreator (Text, font);
					font.Dispose ();
					return Style;
					}
				}
			catch
				{
				if (ic != null)
					ic.Dispose ();
				if (font != null)
					font.Dispose ();
				}

			// Если не получается, выбрать другой стиль
			for (int t = 0; t < otherStyles.Length; t++)
				{
				FontStyle otherFontStyle = otherStyles[t];
				if (CUnder.Checked)
					otherFontStyle |= FontStyle.Underline;
				if (CStrike.Checked)
					otherFontStyle |= FontStyle.Strikeout;

				try
					{
					if (Font.IsStyleAvailable (otherFontStyle))
						{
						font = new Font (Font, Size, otherFontStyle);
						ic = new ImageCreator (Text, font);
						font.Dispose ();
						return otherFontStyle;
						}
					}
				catch
					{
					if (ic != null)
						ic.Dispose ();
					if (font != null)
						font.Dispose ();
					}
				}

			// Иначе - непонятно, что делать
			if (ic != null)
				ic.Dispose ();
			return Style;
			}

		// Выход из программы
		private void BExit_Click (object sender, System.EventArgs e)
			{
			this.Close ();
			}

		private void MainForm_FormClosing (object sender, FormClosingEventArgs e)
			{
			// Сохранение списка
			slp.SaveList ();
			}

		// Справочные сведения
		private void Q5_Click (object sender, System.EventArgs e)
			{
			// Общая
			ProgramDescription.ShowAbout ();

			// Справка
			MessageBox.Show (string.Format (Localization.GetText ("HelpText", al), MaxSearchStringLength,
				MinValidationLimit, MaxValidationLimit, MaxResultsCount), ProgramDescription.AssemblyTitle,
				MessageBoxButtons.OK, MessageBoxIcon.Information);

			// Видео
			if (MessageBox.Show (Localization.GetText ("ShowVideo", al), ProgramDescription.AssemblyTitle,
				MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
				ProgramDescription.ShowVideoManual ();
			}

		// Выбор пункта для предпросмотра
		private void ResultsList_SelectedIndexChanged (object sender, System.EventArgs e)
			{
			if (ResultsList.SelectedIndex < 0)
				return;

			// Проверка на наличие текста
			if (LoadedPicText.Text == "")
				{
				MessageBox.Show (Localization.GetText ("EmptyTextField", al),
					 ProgramDescription.AssemblyTitle, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return;
				}

			// Запуск просмотра
			if (ViewBox.BackgroundImage != null)
				ViewBox.BackgroundImage.Dispose ();

			FontStyle resultStyle = CreateBitmapFromFont (LoadedPicText.Text, foundFF[ResultsList.SelectedIndex],
				ViewBox.Height, searchFontStyle);
			if (ic.CreatedImage == null)
				return;

			ViewBox.BackgroundImage = (Image)ic.CreatedImage.Clone ();
			ic.Dispose ();
			}

		// Установка или снятие галочки
		private void PauseSearch_CheckedChanged (object sender, System.EventArgs e)
			{
			SearchPauseFactor.Enabled = PauseSearch.Checked;
			}

		// Настройка стиля поиска
		private void CBold_CheckedChanged (object sender, System.EventArgs e)
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
		private void LanguageCombo_SelectedIndexChanged (object sender, System.EventArgs e)
			{
			// Сохранение языка
			Localization.CurrentLanguage = al = (SupportedLanguages)LanguageCombo.SelectedIndex;

			// Локализация
			OpenImage.Filter = Localization.GetText ("OpenImageFilter", al) + " (*.bmp, *.gif, *.jpe, *.jpeg, *.jpg, *.jfif, *.png)|" +
				"*.bmp;*.gif;*.jpe;*.jpeg;*.jpg;*.jfif;*.png";
			OpenImage.Title = Localization.GetText ("OpenImageTitle", al);

			SelectImage.Text = Localization.GetText ("SelectImageText", al);
			Label02.Text = Localization.GetText ("Label02Text", al);
			Label03.Text = Localization.GetText ("Label03Text", al);
			CBold.Text = Localization.GetText ("CBoldText", al);
			CItalic.Text = Localization.GetText ("CItalicText", al);
			CUnder.Text = Localization.GetText ("CUnderText", al);
			CStrike.Text = Localization.GetText ("CStrikeText", al);
			PauseSearch.Text = Localization.GetText ("PauseSearchText", al);
			StartSearch.Text = Localization.GetText ("StartSearchText", al);
			Label05.Text = Localization.GetText ("Label05Text", al);
			BExit.Text = Localization.GetText ("BExitText", al);
			Label06.Text = Localization.GetText ("Label06Text", al);
			BSkipping.Text = Localization.GetText ("BSkippingText", al);
			}

		// Работа с пропущенными шрифтами
		private void BSkipping_Click (object sender, System.EventArgs e)
			{
			slp.EditList (al);
			}
		}
	}
