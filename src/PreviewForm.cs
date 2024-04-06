using System;
using System.Drawing;
using System.Windows.Forms;

namespace RD_AAOW
	{
	/// <summary>
	/// Форма предпросмотра шрифта
	/// </summary>
	public partial class PreviewForm: Form
		{
		/// <summary>
		/// Конструктор. Запускает форму просмотра шрифта
		/// </summary>
		/// <param name="Preview">Изображение для отображения</param>
		/// <param name="Caption">Подпись окна просмотра</param>
		public PreviewForm (Bitmap Preview, string Caption)
			{
			// Инициализация
			InitializeComponent ();
			ViewBox.BackgroundImage = Preview;

			this.CancelButton = this.AcceptButton = FClose;
			FClose.Text = RDLocale.GetDefaultText (RDLDefaultTexts.Button_Close);

			// Заголовок окна
			this.Text = Caption;
			this.ShowDialog ();
			}

		// Закрытие формы
		private void FClose_Click (object sender, EventArgs e)
			{
			this.Close ();
			}
		}
	}
