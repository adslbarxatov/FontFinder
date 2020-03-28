﻿using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace ESHQSetupStub
	{
	/// <summary>
	/// Класс предоставляет интерфейс визуализации прогресса установки/удаления программы
	/// </summary>
	public partial class HardWorkExecutor:Form
		{
		// Переменные
		private bool allowClose = false;						// Запрет выхода из формы до окончания работы
		private Bitmap progress, frameGreenGrey, frameBack;		// Объекты-отрисовщики
		private Graphics g, gp;
		private int currentXOffset = 0;
		private int currentPercentage = 0;

		/// <summary>
		/// Длина шкалы прогресса
		/// </summary>
		public const uint ProgressBarSize = 1000;

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
		/// Возвращает результат установки/удаления
		/// </summary>
		public int ExecutionResult
			{
			get
				{
				return executionResult;
				}
			}
		private int executionResult = 0;

		/// <summary>
		/// Возвращает результат выполняемых операций
		/// </summary>
		public string Result
			{
			get
				{
				return result;
				}
			}
		private string result = "";

		// Инициализация ProgressBar
		private void InitializeProgressBar ()
			{
			// Настройка контролов
			InitializeComponent ();
			this.BackColor = ProgramDescription.MasterBackColor;
			StateLabel.ForeColor = AbortButton.ForeColor = ProgramDescription.MasterTextColor;
			AbortButton.BackColor = ProgramDescription.MasterButtonColor;

			// Инициализация
			progress = new Bitmap (this.Width - 20, 30);
			g = Graphics.FromHwnd (this.Handle);
			gp = Graphics.FromImage (progress);

			// Формирование стрелок
			Point[] frame = new Point[] {
					new Point (0, 0),
					new Point (this.Width / 4, 0),
					new Point (this.Width / 4 + progress .Height / 2, progress .Height / 2),
					new Point (this.Width / 4, progress .Height),
					new Point (0, progress .Height),
					new Point (progress .Height / 2, progress .Height / 2)
					};

			// Подготовка дескрипторов
			SolidBrush green = new SolidBrush (Color.FromArgb (0, 160, 80)),
				grey = new SolidBrush (Color.FromArgb (160, 160, 160)),
				back = new SolidBrush (this.BackColor);

			frameGreenGrey = new Bitmap (10 * this.Width / 4, progress.Height);
			frameBack = new Bitmap (10 * this.Width / 4, progress.Height);
			Graphics g1 = Graphics.FromImage (frameGreenGrey),
				g2 = Graphics.FromImage (frameBack);

			// Сборка
			for (int i = 0; i < 8; i++)
				{
				for (int j = 0; j < frame.Length; j++)
					{
					frame[j].X += this.Width / 4;
					}

				g1.FillPolygon ((i % 2 == 0) ? green : grey, frame);
				g2.FillPolygon (back, frame);
				}

			// Объём
			for (int i = 0; i < frameGreenGrey.Height; i++)
				{
				Pen p = new Pen (Color.FromArgb (200 - (int)(200.0 * Math.Sin (Math.PI * (double)i /
					(double)frameGreenGrey.Height)), this.BackColor));
				g1.DrawLine (p, 0, i, frameGreenGrey.Width, i);
				p.Dispose ();
				}

			// Освобождение ресурсов
			g1.Dispose ();
			g2.Dispose ();
			green.Dispose ();
			grey.Dispose ();
			back.Dispose ();

			// Запуск таймера
			DrawingTimer.Interval = 1;
			DrawingTimer.Enabled = true;
			}

		/// <summary>
		/// Конструктор. Выполняет указанное действие
		/// </summary>
		/// <param name="HardWorkProcess">Выполняемый процесс</param>
		public HardWorkExecutor (DoWorkEventHandler HardWorkProcess)
			{
			// Настройка BackgroundWorker
			bw.WorkerReportsProgress = true;		// Разрешает возвраты изнутри процесса
			bw.WorkerSupportsCancellation = true;	// Разрешает завершение процесса

			bw.DoWork += ((HardWorkProcess != null) ? HardWorkProcess : DoWork);
			bw.RunWorkerCompleted += RunWorkerCompleted;
			bw.ProgressChanged += ProgressChanged;

			// Донастройка окна
			InitializeProgressBar ();
			currentPercentage = (int)ProgressBarSize;

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
			currentPercentage = e.ProgressPercentage;

			StateLabel.Text = (string)e.UserState;
			}

		// Метод обрабатывает завершение процесса
		private void RunWorkerCompleted (object sender, RunWorkerCompletedEventArgs e)
			{
			// Завершение работы исполнителя
			try
				{
				executionResult = int.Parse (e.Result.ToString ());
				result = e.Result.ToString ();
				}
			catch
				{
				executionResult = -100;
				}
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
			for (int i = 0; i < ProgressBarSize; i++)
				{
				System.Threading.Thread.Sleep (500);
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
			DrawingTimer.Enabled = false;

			if (g != null)
				g.Dispose ();
			if (gp != null)
				gp.Dispose ();
			if (progress != null)
				progress.Dispose ();
			if (frameGreenGrey != null)
				frameGreenGrey.Dispose ();
			if (frameBack != null)
				frameBack.Dispose ();
			}

		// Отрисовка прогресс-бара
		private void DrawingTimer_Tick (object sender, System.EventArgs e)
			{
			// Отрисовка текущей позиции
			gp.DrawImage (frameGreenGrey, currentXOffset, 0);
			gp.DrawImage (frameBack, -9 * this.Width / 4, 0);
			gp.DrawImage (frameBack, currentPercentage * (progress.Width - progress.Height / 2) / ProgressBarSize -
				this.Width / 4, 0);

			g.DrawImage (progress, 10, StateLabel.Top + StateLabel.Height + 10);

			// Смещение
			if (currentXOffset++ >= -2 * this.Width / 4)
				currentXOffset = -4 * this.Width / 4;
			}
		}
	}
