using System.ComponentModel;
using System.Windows.Forms;

namespace FontFinder
	{
	/// <summary>
	/// Класс предоставляет интерфейс выполнения и визуализации прогресса длительных вычислений
	/// </summary>
	public partial class HardWorkExecutor:Form
		{
		// Переменные
		private bool allowClose = false;

		/// <summary>
		/// Возвращает объект-обвязку исполняемого процесса
		/// </summary>
		public BackgroundWorker Worker
			{
			get
				{
				return bw;
				}
			}
		private BackgroundWorker bw = new BackgroundWorker ();

		/// <summary>
		/// Возвращает флаг, указывающий, было ли прервано выполнение
		/// </summary>
		public bool Cancelled
			{
			get
				{
				return cancelled;
				}
			}
		private bool cancelled = false;

		/// <summary>
		/// Конструктор. Выполняет настройку и запуск процесса
		/// </summary>
		/// <param name="HardWorkProcess">Процесс, выполняющий вычисления</param>
		public HardWorkExecutor (DoWorkEventHandler HardWorkProcess)
			{
			// Инициализация и локализация
			InitializeComponent ();

			// Настройка BackgroundWorker
			bw.WorkerReportsProgress = true;		// Разрешает возвраты изнутри процесса
			bw.WorkerSupportsCancellation = true;	// Разрешает завершение процесса

			if (HardWorkProcess != null)
				{
				bw.DoWork += HardWorkProcess;
				}
			else
				{
				bw.DoWork += DoWork;
				}
			bw.ProgressChanged += ProgressChanged;
			bw.RunWorkerCompleted += RunWorkerCompleted;

			// Запуск
			this.ShowDialog ();
			}

		// Метод запускает выполнение процесса
		private void HardWorkExecutor_Shown (object sender, System.EventArgs e)
			{
			bw.RunWorkerAsync ();
			}

		// Метод обрабатывает изменение состояния процесса
		private void ProgressChanged (object sender, ProgressChangedEventArgs e)
			{
			// Обновление ProgressBar
			MainProgress.Value = e.ProgressPercentage;
			MessageLabel.Text = e.UserState.ToString ();
			}

		// Метод обрабатывает завершение процесса
		private void RunWorkerCompleted (object sender, RunWorkerCompletedEventArgs e)
			{
			// Завершение работы исполнителя
			cancelled = e.Cancelled;
			bw.Dispose ();

			// Закрытие окна
			allowClose = true;
			this.Close ();
			}

		// Кнопка инициирует остановку процесса
		private void AbortButton_Click (object sender, System.EventArgs e)
			{
			bw.CancelAsync ();
			}

		// Образец метода, выполняющего длительные вычисления
		private void DoWork (object sender, DoWorkEventArgs e)
			{
			// Собственно, выполняемый процесс
			for (int i = 0; i < 100; i++)
				{
				System.Threading.Thread.Sleep (50);
				((BackgroundWorker)sender).ReportProgress (i);	// Возврат прогресса

				// Завершение работы, если получено требование от диалога
				if (((BackgroundWorker)sender).CancellationPending)
					{
					e.Cancel = true;
					return;
					}
				}

			// Завершено
			e.Result = null;
			}

		// Закрытие формы
		private void HardWorkExecutor_FormClosing (object sender, FormClosingEventArgs e)
			{
			e.Cancel = !allowClose;
			}
		}
	}
