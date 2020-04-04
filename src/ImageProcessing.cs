using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace RD_AAOW
	{
	/// <summary>
	/// Класс отвечает за сравнение двух изображений с возвратом результата
	/// </summary>
	static public class ImageComparer
		{
		/// <summary>
		/// Сравнивает два изображения и возвращает степень их совпадения
		/// </summary>
		/// <param name="ControlSample">Контрольное изображение</param>
		/// <param name="CreatedImage">Изображение для сравнения</param>
		/// <returns>Степень совпадения в процентах</returns>
		static public double Compare (Bitmap ControlSample, Bitmap CreatedImage)
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
		}

	/// <summary>
	/// Класс формирует изображение из текста с использованием указанного шрифта
	/// </summary>
	public class ImageCreator
		{
		// Переменные
		private Bitmap image = null;	// Формируемое изображение
		private Brush whiteBrush = new SolidBrush (Color.FromArgb (255, 255, 255));	// Белая кисть
		private Brush blackBrush = new SolidBrush (Color.FromArgb (0, 0, 0));		// Чёрная кисть

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

			// Обрезка изображения
			ImageLoader il = new ImageLoader (image);
			image.Dispose ();
			image = il.GetBlackZone ();

			// Очистка памяти
			il.Dispose ();
			g.Dispose ();
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
		private Bitmap image = null;									// Загруженное изображение
		private ImageLoaderStatuses status = ImageLoaderStatuses.Ok;	// Статус инициализации класса (по умолчанию = Ok)

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
				System.Drawing.Imaging.PixelFormat.Format1bppIndexed);	// Теперь загружается сразу монохромным
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
						{
						goto ll;
						}
					}
				}
ll:
			if (x == image.Width)
				{
				return new Rectangle ();		// Эта ситуация возможна лишь при полностью белом рисунке
				}
			// На этом месте произойдёт её полное отсеивание
			else
				{
				r.X = x;
				}

			// Правая
			for (x = image.Width - 1; x >= 0; x--)
				{
				for (y = image.Height - 1; y >= 0; y--)	// Это позволит ускорить прогон уже обрезанного изображения (в нём есть границы)
					{
					if (image.GetPixel (x, y).R < 128)
						{
						goto lr;
						}
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
						{
						goto lt;
						}
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
						{
						goto lb;
						}
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
		/// <returns>Изображение, обрезанное до границ контрастного объекта</returns>
		public Bitmap GetBlackZone ()
			{
			// Контроль
			if (status != ImageLoaderStatuses.Ok)
				return null;

			// Получение границ рисунка и отсечение пустых полей
			Rectangle borders = GetBorder ();
			if (borders == Rectangle.Empty)
				return null;

			// Возврат
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
