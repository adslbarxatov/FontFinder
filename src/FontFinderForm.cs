﻿using System;
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

		// Текст на нём
		private string imageText = "";

		// Найденные шрифты
		private List<FontFamily> foundFF = new List<FontFamily> ();

		// Оценки степени их соответствия исходному изображению
		private List<double> foundFFMatch = new List<double> ();

		// Стиль шрифта для поиска
		private FontStyle searchFontStyle = FontStyle.Regular;
		private SkipListProcessor slp = new SkipListProcessor ();

		// Порог срабатывания правила приостановки поиска
		private double searchPauseFactor = 90.0;

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

			LanguageCombo.Items.AddRange (Localization.LanguagesNames);
			try
				{
				LanguageCombo.SelectedIndex = (int)Localization.CurrentLanguage;
				}
			catch
				{
				LanguageCombo.SelectedIndex = 0;
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
						RDGenerics.LocalizedMessageBox (RDMessageTypes.Warning, "FileNotFound");
						break;

					case ImageLoaderStatuses.FileIsNotAnImage:
						RDGenerics.LocalizedMessageBox (RDMessageTypes.Warning, "FileIsNotAnImage");
						break;
					}

				return;
				}

			// Получение изображения и контроль
			if (image != null)
				image.Dispose ();
			image = il.GetBlackArea ();
			il.Dispose ();

			if (image == null)  // Не удалось найти границы
				{
				RDGenerics.LocalizedMessageBox (RDMessageTypes.Warning, "CannotFindText");
				return;
				}

			// Обработка
			if ((image.Width > LoadedPicture.Width) || (image.Height > LoadedPicture.Height))
				RDGenerics.LocalizedMessageBox (RDMessageTypes.Information, "LargePicture");

			if (LoadedPicture.BackgroundImage != null)
				LoadedPicture.BackgroundImage.Dispose ();
			LoadedPicture.BackgroundImage = (Bitmap)image.Clone ();

			// Активация контролов
			StartSearch.Enabled = true;
			}

		// Запуск поиска
		private void StartSearch_Click (object sender, EventArgs e)
			{
			if (LoadedPicText.Text == "")
				{
				RDGenerics.LocalizedMessageBox (RDMessageTypes.Warning, "SpecifyTextFromImage");
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
			HardWorkExecutor hwe = new HardWorkExecutor (Search, null, " ", false, true);
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
				if (createdImage == null)
					continue;
				// Здесь шрифты не пропускаем, т.к. есть шрифты, где лишь некоторые символы дают такой результат

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
				if (PauseSearch.Checked && (res >= searchPauseFactor))
					{
					PreviewForm prf = new PreviewForm (createdImage, slp.ExistentFonts[i].Name +
						", " + resultStyle.ToString ());

					if (RDGenerics.LocalizedMessageBox (RDMessageTypes.Question, "FinishSearch",
						LzDefaultTextValues.Button_Yes, LzDefaultTextValues.Button_No) == RDMessageButtons.ButtonOne)
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

				// Возврат прогресса
				if (maxRes < res)
					maxRes = res;

				string msg = string.Format (Localization.GetText ("ProcessingMessage"), i, slp.ExistentFonts.Length,
					slp.ExistentFonts[i].Name);
				if (resultStyle != searchFontStyle)
					msg += string.Format (Localization.GetText ("ProcessingStyle"), resultStyle.ToString ());
				msg += string.Format (Localization.GetText ("SkippingFontsCountAndPercentage"),
					slp.SkippingFontsCount, maxRes.ToString ("F2"));

				((BackgroundWorker)sender).ReportProgress ((int)(HardWorkExecutor.ProgressBarSize *
					i / slp.ExistentFonts.Length), msg);

				// Завершение работы, если получено требование от диалога
				if (((BackgroundWorker)sender).CancellationPending || e.Cancel)
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
				RDGenerics.LocalizedMessageBox (RDMessageTypes.Warning, "EmptyTextField");
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
			Localization.CurrentLanguage = (SupportedLanguages)LanguageCombo.SelectedIndex;

			// Локализация
			OpenImage.Filter = Localization.GetText ("OpenImageFilter") +
				" (*.bmp, *.gif, *.jpe, *.jpeg, *.jpg, *.jfif, *.png)|" +
				"*.bmp;*.gif;*.jpe;*.jpeg;*.jpg;*.jfif;*.png";
			OpenImage.Title = Localization.GetText ("OpenImageTitle");

			SelectImage.Text = Localization.GetText ("SelectImageText");
			Label02.Text = Localization.GetText ("Label02Text");
			Label03.Text = Localization.GetText ("Label03Text");
			CBold.Text = Localization.GetText ("CBoldText");
			CItalic.Text = Localization.GetText ("CItalicText");
			CUnder.Text = Localization.GetText ("CUnderText");
			CStrike.Text = Localization.GetText ("CStrikeText");
			PauseSearch.Text = Localization.GetText ("PauseSearchText");
			StartSearch.Text = Localization.GetText ("StartSearchText");
			Label05.Text = Localization.GetText ("Label05Text");
			BExit.Text = Localization.GetDefaultText (LzDefaultTextValues.Button_Exit);
			Label06.Text = Localization.GetText ("Label06Text");
			BSkipping.Text = Localization.GetText ("BSkippingText");
			}

		// Работа с пропущенными шрифтами
		private void BSkipping_Click (object sender, EventArgs e)
			{
			slp.EditList (LoadedPicText.Text);
			}
		}
	}
