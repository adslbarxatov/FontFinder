using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace RD_AAOW
	{
	/// <summary>
	/// Класс отвечает за обработку изображений
	/// </summary>
	public static class ImageProcessor
		{
		// Стили, замещающие текущий в случае его недоступности
		private static FontStyle[] otherStyles = {
			FontStyle.Regular,
			FontStyle.Bold ,
			FontStyle.Italic,
			FontStyle.Bold | FontStyle.Italic
			};

		/// <summary>
		/// Сравнивает два изображения и возвращает степень их совпадения
		/// </summary>
		/// <param name="ControlSample">Контрольное изображение</param>
		/// <param name="CreatedImage">Изображение для сравнения</param>
		/// <returns>Степень совпадения в процентах</returns>
		public static double Compare (Bitmap ControlSample, Bitmap CreatedImage)
			{
			// Переменные
			double res = 0.0;

			// Контроль
			if ((ControlSample == null) || (CreatedImage == null))
				return res;

			if (ControlSample.Width * ControlSample.Height * CreatedImage.Width * CreatedImage.Height == 0)
				return res;

			// Сравнение с учётом масштаба
			for (int x = 0; x < ControlSample.Width; x++)
				{
				for (int y = 0; y < ControlSample.Height; y++)
					{
					if (Math.Abs (ControlSample.GetPixel (x, y).R -
						CreatedImage.GetPixel (CreatedImage.Width * x / ControlSample.Width,
						CreatedImage.Height * y / ControlSample.Height).R) < 128)
						{
						res += 1.0;
						}
					}
				}

			// Результат
			return 100.0 * res / (ControlSample.Width * ControlSample.Height);
			}

		/// <summary>
		/// Метод формирует изображение шрифта
		/// </summary>
		/// <param name="Font">Используемый шрифт</param>
		/// <param name="ResultImage">Изображение шрифта</param>
		/// <param name="Size">Кегль шрифта</param>
		/// <param name="Strikeout">Флаг зачёркивания</param>
		/// <param name="Style">Стиль используемого шрифта</param>
		/// <param name="Text">Текст для формирования изображения</param>
		/// <param name="Underline">Флаг подчёркивания</param>
		/// <returns>Возвращает стиль, который удалось применить к указанному шрифту</returns>
		public static FontStyle CreateBitmapFromFont (string Text, FontFamily Font, float Size, FontStyle Style,
			bool Underline, bool Strikeout, out Bitmap ResultImage)
			{
			Font font = null;
			ImageCreator ic = null;

			// Обработка указанного стиля (если возможно)
			try
				{
				if (Font.IsStyleAvailable (Style))
					{
					font = new Font (Font, Size, Style);
					ic = new ImageCreator (Text, font);
					font.Dispose ();

					if (ic.IsImageCreated)
						ResultImage = (Bitmap)ic.CreatedImage.Clone ();
					else
						ResultImage = null;

					ic.Dispose ();
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
				if (Underline)
					otherFontStyle |= FontStyle.Underline;
				if (Strikeout)
					otherFontStyle |= FontStyle.Strikeout;

				try
					{
					if (Font.IsStyleAvailable (otherFontStyle))
						{
						font = new Font (Font, Size, otherFontStyle);
						ic = new ImageCreator (Text, font);
						font.Dispose ();

						if (ic.IsImageCreated)
							ResultImage = (Bitmap)ic.CreatedImage.Clone ();
						else
							ResultImage = null;

						ic.Dispose ();
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

			ResultImage = null;
			return Style;
			}
		}

	/// <summary>
	/// Класс формирует изображение из текста с использованием указанного шрифта
	/// </summary>
	public class ImageCreator
		{
		// Формируемое изображение
		private Bitmap image = null;

		// Белая кисть
		private Brush whiteBrush = new SolidBrush (Color.FromArgb (255, 255, 255));

		// Чёрная кисть
		private Brush blackBrush = new SolidBrush (Color.FromArgb (0, 0, 0));

		/// <summary>
		/// Возвращает сформированное изображение
		/// </summary>
		public Bitmap CreatedImage
			{
			get
				{
				return image;
				}
			}

		/// <summary>
		/// Возвращает флаг успешности формирования изображения
		/// </summary>
		public bool IsImageCreated
			{
			get
				{
				return (image != null);
				}
			}

		/// <summary>
		/// Конструктор. Создаёт изображение из текста с заданным шрифтом
		/// </summary>
		/// <param name="Text">Текст для формирования изображения</param>
		/// <param name="UsedFont">Шрифт для формирования изображения</param>
		public ImageCreator (string Text, Font UsedFont)
			{
			// Формирование заготовки изображения
			Bitmap b = new Bitmap (10, 10);
			Graphics g = Graphics.FromImage (b);

			// Здесь есть утечки памяти, т.к. объекты b и g не очищаются при возникновении
			// исключений в строке ниже
			SizeF sz = g.MeasureString (Text, UsedFont);
			g.Dispose ();
			b.Dispose ();

			// Получение дескриптора для отрисовки текста
			image = new Bitmap ((int)(sz.Width * 1.2f), (int)(sz.Height * 1.2f));
			g = Graphics.FromImage (image);

			// Заливка изображения
			g.FillRectangle (whiteBrush, 0, 0, image.Width, image.Height);

			// Отрисовка текста
			g.DrawString (Text, UsedFont, blackBrush, (image.Width - sz.Width) / 2, (image.Height - sz.Height) / 2);
			g.Dispose ();

			// Обрезка изображения
			ImageLoader il = new ImageLoader (image);
			image.Dispose ();
			image = il.GetBlackArea ();

			// Очистка памяти
			il.Dispose ();
			}

		/// <summary>
		/// Метод отвечает за освобождение занятых классом ресурсов
		/// </summary>
		public void Dispose ()
			{
			if (image != null)
				image.Dispose ();
			whiteBrush.Dispose ();
			blackBrush.Dispose ();
			}
		}

	/// <summary>
	/// Состояния инициализации класса
	/// </summary>
	public enum ImageLoaderStatuses
		{
		/// <summary>
		/// Инициализация успешно завершена
		/// </summary>
		Ok = 0,

		/// <summary>
		/// Файл не найден или недоступен
		/// </summary>
		FileNotFound = -1,

		/// <summary>
		/// Файл не является известным файлом изображения
		/// </summary>
		FileIsNotAnImage = -2
		};

	/// <summary>
	/// Класс выполняет загрузку и монохромирование изображения
	/// </summary>
	public class ImageLoader
		{
		// Переменные
		private Bitmap image = null;                                    // Загруженное изображение
		private ImageLoaderStatuses status = ImageLoaderStatuses.Ok;    // Статус инициализации класса

		/// <summary>
		/// Конструктор. Создаёт объект-изображение из файла
		/// </summary>
		/// <param name="Path">Путь к файлу изображения</param>
		public ImageLoader (string Path)
			{
			FileStream FS = null;

			// Попытка открытия файла
			try
				{
				FS = new FileStream (Path, FileMode.Open);
				}
			catch
				{
				status = ImageLoaderStatuses.FileNotFound;
				return;
				}

			// Попытка инициализации изображения
			try
				{
				image = (Bitmap)Image.FromStream (FS);
				}
			catch
				{
				status = ImageLoaderStatuses.FileIsNotAnImage;
				return;
				}

			// Устранение ссылочной зависимости загруженного изображения от файла
			Image image2 = (Image)image.Clone (new Rectangle (0, 0, image.Width, image.Height),
				PixelFormat.Format1bppIndexed); // Теперь загружается сразу монохромным
			FS.Close ();

			image.Dispose ();
			image = (Bitmap)image2.Clone ();
			image2.Dispose ();
			}

		/// <summary>
		/// Конструктор. Создаёт объект-изображение из существующего изображения
		/// </summary>
		/// <param name="CreatedImage">Ранее созданное изображение</param>
		public ImageLoader (Image CreatedImage)
			{
			if (CreatedImage != null)
				image = (Bitmap)CreatedImage.Clone ();
			}

		/// <summary>
		/// Возвращает статус инициализации класса
		/// </summary>
		public ImageLoaderStatuses InitStatus
			{
			get
				{
				return status;
				}
			}

		// Определяет границы чёрного объекта на рисунке
		private Rectangle GetBorder ()
			{
			// Контроль
			if (status != ImageLoaderStatuses.Ok)
				return new Rectangle ();

			Rectangle r = new Rectangle (0, 0, 1, 1);
			int x, y;

			// Поиск границ
			// Левая
			for (x = 0; x < image.Width; x++)
				{
				for (y = 0; y < image.Height; y++)
					{
					if (image.GetPixel (x, y).R < 128)
						goto ll;
					}
				}
			ll:

			// Эта ситуация возможна лишь при полностью белом рисунке
			if (x == image.Width)
				return new Rectangle ();

			// На этом месте произойдёт её полное отсеивание
			else
				r.X = x;

			// Правая
			// Это позволит ускорить прогон уже обрезанного изображения (в нём есть границы)
			for (x = image.Width - 1; x >= 0; x--)
				{
				for (y = image.Height - 1; y >= 0; y--)
					{
					if (image.GetPixel (x, y).R < 128)
						goto lr;
					}
				}

			lr:
			r.Width = x + 1 - r.X;

			// Верхняя
			for (y = 0; y < image.Height; y++)
				{
				for (x = 0; x < image.Width; x++)
					{
					if (image.GetPixel (x, y).R < 128)
						goto lt;
					}
				}
			lt:
			r.Y = y;

			// Нижняя
			for (y = image.Height - 1; y >= 0; y--)
				{
				for (x = image.Width - 1; x >= 0; x--)
					{
					if (image.GetPixel (x, y).R < 128)
						goto lb;
					}
				}
			lb:
			r.Height = y + 1 - r.Y;

			// Готово
			return r;
			}

		/// <summary>
		/// Возвращает часть исходного изображения, содержащую контрастный объект
		/// </summary>
		/// <returns>Изображение, обрезанное до границ контрастного объекта,
		/// или null, если границы найти не удалось</returns>
		public Bitmap GetBlackArea ()
			{
			return GetBlackArea (0, 0);
			}

		/// <summary>
		/// Возвращает часть исходного изображения, содержащую контрастный объект
		/// </summary>
		/// <returns>Изображение, обрезанное до границ контрастного объекта,
		/// или null, если границы найти не удалось.
		/// Если параметры Width и Height не равны нулю, исходное изображение
		/// будет изменено таким образом, чтобы вписаться в оба значения</returns>
		public Bitmap GetBlackArea (uint Width, uint Height)
			{
			// Контроль
			if (status != ImageLoaderStatuses.Ok)
				return null;

			// Получение границ рисунка и отсечение пустых полей
			Rectangle borders = GetBorder ();
			if (borders == Rectangle.Empty)
				return null;

			// Обрезка по контрастному объекту
			Bitmap image2 = image.Clone (borders, PixelFormat.Format1bppIndexed);
			image.Dispose ();

			// Подгонка размера
			if (Width * Height != 0)
				{
				int w = image2.Width;
				int h = image2.Height;
				int origW = (int)Width;
				int origH = (int)Height;
				double dw = 1.0;
				double dh = 1.0;
				bool changed = false;

				// Расчёт масштабов
				if (origH < image2.Height)
					{
					dh = (double)image2.Height / (double)origH;
					changed = true;
					}

				if (origW < image2.Width)
					{
					dw = (double)image2.Width / (double)origW;
					changed = true;
					}

				// Изменение размера
				if (changed)
					{
					double d = Math.Max (dw, dh);

					Bitmap image3 = new Bitmap (image2, (int)(w / d), (int)(h / d));
					image2.Dispose ();

					image2 = (Bitmap)image3.Clone ();
					image3.Dispose ();
					}
				}

			// Повторная монохромизация
			image = (Bitmap)image2.Clone ();
			image2.Dispose ();

			borders = new Rectangle (0, 0, image.Width, image.Height);
			return image.Clone (borders, PixelFormat.Format1bppIndexed);
			}

		/// <summary>
		/// Возвращает загруженное изображение после всех выполненных преобразований (если они были)
		/// </summary>
		public Bitmap LoadedImage
			{
			get
				{
				return image;
				}
			}

		/// <summary>
		/// Метод отвечает за освобождение занятых классом ресурсов
		/// </summary>
		public void Dispose ()
			{
			if (image != null)
				image.Dispose ();
			}
		}
	}
