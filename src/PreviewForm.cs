using System.Drawing;
using System.Windows.Forms;

namespace FontFinder
	{
	/// <summary>
	/// Форма предпросмотра шрифта
	/// </summary>
	public partial class PreviewForm:Form
		{
		// Переменные
		private string viewText = "";									// Текст-образец
		private FontFamily viewFontFamily = null;						// Шрифт
		private FontStyle viewFontStyle = FontStyle.Regular;			// Стиль отображения шрифта
		private bool understrike = false, strikeout = false;			// Флаги подчёркивания и зачёркивания
		private FontStyle[] otherStyles = { FontStyle.Regular,			// Стили, замещающие текущий в случае его недопустимости
											  FontStyle.Bold ,
											  FontStyle.Italic,
											  FontStyle.Bold | FontStyle.Italic };

		/// <summary>
		/// Конструктор. Запускает форму просмотра шрифта
		/// </summary>
		/// <param name="Strikeout">Указывает, является ли текст, используемый для просмотра, перечёркнутым</param>
		/// <param name="Understrike">Указывает, является ли текст, используемый для просмотра, подчёркнутым</param>
		/// <param name="ViewFontFamily">Имя шрифта для просмотра</param>
		/// <param name="ViewFontStyle">Стиль шрифта</param>
		/// <param name="ViewText">Текст-образец</param>
		public PreviewForm (string ViewText, FontFamily ViewFontFamily, FontStyle ViewFontStyle, bool Understrike, bool Strikeout)
			{
			InitializeComponent ();

			viewText = ViewText;
			viewFontFamily = ViewFontFamily;
			viewFontStyle = ViewFontStyle;
			understrike = Understrike;
			strikeout = Strikeout;

			this.ShowDialog ();
			}

		// Загрузка формы
		private void PreviewForm_Load (object sender, System.EventArgs e)
			{
			ImageCreator ic = null;
			int t = 0;

			// Формирование изображения
retry:
			try
				{
				ic = new ImageCreator (viewText, new Font (viewFontFamily, ViewBox.Height, viewFontStyle));
				}
			catch
				{
				// Если не получается, выбрать другой стиль
				if (t < otherStyles.Length)
					{
					viewFontStyle = otherStyles[t++];
					if (understrike)
						{
						viewFontStyle |= FontStyle.Underline;
						}
					if (strikeout)
						{
						viewFontStyle |= FontStyle.Strikeout;
						}
					goto retry;
					}
				}

			// Отображение
			ViewBox.BackgroundImage = new Bitmap (ic.CreatedImage, ViewBox.Width, (int)((double)ic.CreatedImage.Height *
				((double)ViewBox.Width / (double)ic.CreatedImage.Width)));

			// Заголовок окна
			this.Text = /*"Просмотр шрифта " +*/ viewFontFamily.Name + /*", стиль "*/", " + viewFontStyle.ToString ();
			}

		// Закрытие формы
		private void FClose_Click (object sender, System.EventArgs e)
			{
			this.Close ();
			}
		}
	}
