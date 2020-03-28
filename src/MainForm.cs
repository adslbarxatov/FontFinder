using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Text;
using System.Windows.Forms;

namespace FontFinder
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

		private double searchPauseFactor = 90.0;		// Порог срабатывания правила приостановки поиска

		// Ограничительные константы
		private const uint MaxSearchStringLength = 50;	// Максимальная длина строки для сравнения
		private const uint MaxResultsCount = 100;		// Максимальное количество отображаемых результатов
		private const uint MinValidationLimit = 50;		// Минимальный порог прерывания поиска
		private const uint MaxValidationLimit = 99;		// Максимальный порог прерывания поиска

		private const string picturesExtensions = "(*.bmp, *.gif, *.jpe, *.jpeg, *.jpg, *.jfif, *.png)|" +
			"*.bmp;*.gif;*.jpe;*.jpeg;*.jpg;*.jfif;*.png";

		/// <summary>
		/// Конструктор. Создаёт главную форму программы
		/// </summary>
		public MainForm ()
			{
			// Первичная настройка
			InitializeComponent ();
			RU_CheckedChanged (null, null);

			// Настройка контролов
			this.Text = ProgramDescription.AssemblyTitle;

			LoadedPicText.MaxLength = (int)MaxSearchStringLength;
			SearchPauseFactor.Minimum = MinValidationLimit;
			SearchPauseFactor.Maximum = MaxValidationLimit;
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
						MessageBox.Show (RU.Checked ? "Указанный файл не найден или недоступен" : "Specified file is unavailable for opening",
							ProgramDescription.AssemblyTitle, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
						break;

					case ImageLoaderStatuses.FileIsNotAnImage:
						MessageBox.Show (RU.Checked ? "Указанный файл не является допустимым изображением" : "Specified file is not a valid image",
							ProgramDescription.AssemblyTitle, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
						break;
					}

				return;
				}

			// Сохранение
			if (image != null)
				{
				image.Dispose ();
				}
			image = il.GetBlackZone ();
			il.Dispose ();

			if (LoadedPicture.BackgroundImage != null)
				{
				LoadedPicture.BackgroundImage.Dispose ();
				}
			LoadedPicture.BackgroundImage = (Bitmap)image.Clone ();

			// Активация контролов
			StartSearch.Enabled = true;
			}

		// Запуск поиска
		private void StartSearch_Click (object sender, System.EventArgs e)
			{
			if (LoadedPicText.Text == "")
				{
				MessageBox.Show (RU.Checked ? "Укажите текст с загруженного изображения" : "Specify text from the sample image",
					ProgramDescription.AssemblyTitle, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return;
				}

			// Блокировка окна
			Label02.Enabled = Label03.Enabled = Label05.Enabled =
				SelectImage.Enabled = LoadedPicture.Visible = LoadedPicText.Enabled =
				CBold.Enabled = CItalic.Enabled = CUnder.Enabled = CStrike.Enabled =
				PauseSearch.Enabled = SearchPauseFactor.Enabled =
				StartSearch.Enabled = RU.Enabled = EN.Enabled = BExit.Enabled = false;

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
			ESHQSetupStub.HardWorkExecutor hwe = new ESHQSetupStub.HardWorkExecutor (Search);

			// Деблокировка окна
			Label02.Enabled = Label03.Enabled = Label05.Enabled =
				SelectImage.Enabled = LoadedPicture.Visible = LoadedPicText.Enabled =
				CBold.Enabled = CItalic.Enabled = CUnder.Enabled = CStrike.Enabled =
				PauseSearch.Enabled =
				StartSearch.Enabled = RU.Enabled = EN.Enabled = BExit.Enabled = true;
			SearchPauseFactor.Enabled = PauseSearch.Checked;

			// Выгрузка результатов
			for (int i = 0; i < ((MaxResultsCount > foundFF.Count) ? foundFF.Count : (int)MaxResultsCount); i++)
				{
				ResultsList.Items.Add (foundFF[i].Name + " (" + foundFFMatch[i].ToString ("F2") + "%)");
				}
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
				// Создание изображения с выбранным шрифтом
				FontStyle resultStyle = CreateBitmapFromFont (imageText, ff[i], image.Height, searchFontStyle);

				// Сравнение
				double res = 0;
				try
					{
					res = ImageComparer.Compare (image, ic.CreatedImage);	// Иногда имеют место сбои обращения к изображению
					}
				catch
					{
					}

				// Запрос на прерывание поиска
				if (PauseSearch.Checked && (res >= searchPauseFactor))
					{
					PreviewForm prf = new PreviewForm (ic.CreatedImage, ff[i].Name + ", " + resultStyle.ToString ());
					if (MessageBox.Show (RU.Checked ? "Завершить поиск?" : "Finish the search?",
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
						{
						break;
						}
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
				string msg = (RU.Checked ? "Обрабатывается " : "Processing ") + i.ToString () +
					(RU.Checked ? " из " : " from ") + ff.Length.ToString () + ":\n" + ff[i].Name;
				if (resultStyle != searchFontStyle)
					{
					msg += (RU.Checked ? " со стилем " : " with style ") + resultStyle.ToString ();
					}
				((BackgroundWorker)sender).ReportProgress ((int)(ESHQSetupStub.HardWorkExecutor.ProgressBarSize *
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

			// Обработка указанного стиля
			try
				{
				font = new Font (Font, Size, Style);
				ic = new ImageCreator (Text, font);
				font.Dispose ();
				return Style;
				}
			catch
				{
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
					font = new Font (Font, Size, otherFontStyle);
					ic = new ImageCreator (Text, font);
					font.Dispose ();
					return otherFontStyle;
					}
				catch
					{
					ic.Dispose ();
					if (font != null)
						font.Dispose ();
					}
				}

			// Иначе - непонятно, что делать
			ic.Dispose ();
			return Style;
			}

		// Выход из программы
		private void BExit_Click (object sender, System.EventArgs e)
			{
			this.Close ();
			}

		// Справочные сведения
		private void Q1_Click (object sender, System.EventArgs e)
			{
			MessageBox.Show (RU.Checked ?
				"Рекомендуется использование контрастных изображений, на которых тёмный текст расположен " +
				"на светлом фоне, и кроме него ничего нет. Текст должен быть ровным (без траекторий).\n\n" +
				"Обрезанное до границ текста изображение будет отображено в поле ниже кнопки выбора картинки"
				:
				"It is recommended to use contrast pictures with dark text and bright background (without other " +
				"elements). Text must be flat (without trajectories).\n\n" +
				"Cropped image will be shown in the field below the image selection button",
				ProgramDescription.AssemblyTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
			}

		private void Q2_Click (object sender, System.EventArgs e)
			{
			MessageBox.Show (RU.Checked ?
				"Для подбора шрифта необходимо указать текст, изображённый на картинке, с учётом регистра." +
				" В данной версии программы текст не может быть длиннее " + MaxSearchStringLength.ToString () +
				" символов. Одновременно он не может отсутствовать"
				:
				"It is necessary to specify the text from the picture (it's case sensitive). This version of " +
				"application allows non-empty text strings no longer than " + MaxSearchStringLength.ToString () + " characters",
				ProgramDescription.AssemblyTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
			}

		private void Q3_Click (object sender, System.EventArgs e)
			{
			MessageBox.Show (RU.Checked ?
				"В некоторых случаях параметры шрифта существенно влияют на его отображение. " +
				"Укажите эти параметры, если поиск не даёт желаемых результатов"
				:
				"In some cases these parameters may significantly change results of search. " +
				"Specify them if you haven't received needed fonts",
				ProgramDescription.AssemblyTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
			}

		private void Q4_Click (object sender, System.EventArgs e)
			{
			MessageBox.Show (RU.Checked ?
				"Если вы хотите, чтобы поиск был прерван при обнаружении совпадения с некоторым порогом, можно установить " +
				"соответствующий флажок. Порогом срабатывания этого правила служит число от " + MinValidationLimit.ToString () +
				" до " + MaxValidationLimit.ToString () + " процентов.\n\n" +
				"Нажатие кнопки «Поиск» запустит перебор шрифтов на предмет совпадения с тем, который использован в загруженном " +
				"изображении"
				:
				"If you want to pause search when you get the specified percentage of similarity (between " +
				MinValidationLimit.ToString () + " and " + MaxValidationLimit.ToString () + ").\n\n" +
				"'Search' button starts fonts matching with sample image",
				ProgramDescription.AssemblyTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
			}

		private void Q5_Click (object sender, System.EventArgs e)
			{
			MessageBox.Show (RU.Checked ?
				"По окончании поиска в списке будет отображено не более " + MaxResultsCount.ToString () +
				" наиболее подходящих шрифтов и степени их совпадения с оригиналом в процентах. Щелчок по любому " +
				"результату позволяет отобразить заданный ранее текст с использованием выбранного шрифта и стиля.\n\n" +
				"Поиск может занимать значительное время. Используйте кнопку «×», чтобы остановить поиск и просмотреть " +
				"уже имеющиеся результаты"
				:
				"After search complete you'll get a list of most matching fonts (no more than " + MaxResultsCount.ToString () + "). " +
				"By clicking you can see specified text in selected font and style.\n\n" +
				"Search may be too long. Use '×' button to finish it immediately and view current results",
				ProgramDescription.AssemblyTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
			}

		// Выбор пункта для предпросмотра
		private void ResultsList_SelectedIndexChanged (object sender, System.EventArgs e)
			{
			if (ResultsList.SelectedIndex < 0)
				return;

			// Проверка на наличие текста
			if (LoadedPicText.Text == "")
				{
				MessageBox.Show ("Поле текста не должно быть пустым. Укажите текст для просмотра шрифта",
					 ProgramDescription.AssemblyTitle, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return;
				}

			// Запуск просмотра
			if (ViewBox.BackgroundImage != null)
				ViewBox.BackgroundImage.Dispose ();

			FontStyle resultStyle = CreateBitmapFromFont (LoadedPicText.Text, foundFF[ResultsList.SelectedIndex],
				ViewBox.Height, searchFontStyle);
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
		private void RU_CheckedChanged (object sender, System.EventArgs e)
			{
			if (RU.Checked)
				{
				OpenImage.Filter = "Изображения " + picturesExtensions;
				OpenImage.Title = "Выберите изображение";

				SelectImage.Text = "1. Укажите &изображение с образцом шрифта";
				Label02.Text = "2. Укажите текст, содержащийся на изображении:";
				Label03.Text = "3. Укажите предполагаемый стиль и требуемый стоп-фактор:";
				CBold.Text = "&Жирный";
				CItalic.Text = "&Курсив";
				CUnder.Text = "&Подчёркнутый";
				CStrike.Text = "&Зачёркнутый";
				PauseSearch.Text = "П&риостанавливать поиск, если найдено           %-ное совпадение";
				StartSearch.Text = "4. &Начните поиск";
				Label05.Text = "5. Результаты (по степени совпадения с образцом):";
				BExit.Text = "В&ыход";
				BAbout.Text = "&О программе";
				}
			else
				{
				OpenImage.Filter = "Pictures " + picturesExtensions;
				OpenImage.Title = "Select an image";

				SelectImage.Text = "1. Select font's &picture";
				Label02.Text = "2. Specify the text from the picture:";
				Label03.Text = "3. Specify expected font style and stop-factor:";
				CBold.Text = "&Bold";
				CItalic.Text = "&Italic";
				CUnder.Text = "&Underlined";
				CStrike.Text = "Striked &out";
				PauseSearch.Text = "P&ause search if found                                        % match";
				StartSearch.Text = "4. Begin &search";
				Label05.Text = "5. Results (in descending order of similarity's degree):";
				BExit.Text = "E&xit";
				BAbout.Text = "Abo&ut";
				}
			}

		// Отображение справки
		private void BAbout_Click (object sender, System.EventArgs e)
			{
			ProgramDescription.ShowAbout ();

			if (MessageBox.Show (RU.Checked ? "Показать видеоруководство пользователя на нашем YouTube-канале?" :
				"Do you want to view video-manual on our YouTube channel?", ProgramDescription.AssemblyTitle,
				MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
				{
				ProgramDescription.ShowVideoManual ();
				}
			}
		}
	}
