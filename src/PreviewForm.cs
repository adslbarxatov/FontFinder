using System.Drawing;
using System.Windows.Forms;

namespace FontFinder
	{
	/// <summary>
	/// Форма предпросмотра шрифта
	/// </summary>
	public partial class PreviewForm:Form
		{
		/// <summary>
		/// Конструктор. Запускает форму просмотра шрифта
		/// </summary>
		/// <param name="Preview">Изображение для отображения</param>
		/// <param name="Caption">Подпись окна просмотра</param>
		public PreviewForm (Bitmap Preview, string Caption)
			{
			InitializeComponent ();
			ViewBox.BackgroundImage = new Bitmap (Preview, ViewBox.Width, (int)((double)Preview.Height *
				((double)ViewBox.Width / (double)Preview.Width)));

			// Заголовок окна
			this.Text = Caption;
			this.ShowDialog ();
			}

		// Закрытие формы
		private void FClose_Click (object sender, System.EventArgs e)
			{
			this.Close ();
			}

		// Изменение размера формы
		private void PreviewForm_Resize (object sender, System.EventArgs e)
			{
			FClose.Top = this.Height - 62;
			FClose.Left = (this.Width - FClose.Width) / 2;

			ViewBox.Width = this.Width - 30;
			ViewBox.Height = this.Height - 80;
			}
		}
	}
