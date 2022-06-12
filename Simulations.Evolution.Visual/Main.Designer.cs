namespace Simulations.Evolution.Visual
{
	partial class Main
	{
		/// <summary>
		/// Обязательная переменная конструктора.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Освободить все используемые ресурсы.
		/// </summary>
		/// <param name="disposing">истинно, если управляемый ресурс должен быть удален; иначе ложно.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Код, автоматически созданный конструктором форм Windows

		/// <summary>
		/// Требуемый метод для поддержки конструктора — не изменяйте 
		/// содержимое этого метода с помощью редактора кода.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			this.razorPainterControl = new RazorGDIPainter.RazorPainterControl();
			this.gameTimer2 = new System.Windows.Forms.Timer(this.components);
			this.SuspendLayout();
			// 
			// razorPainterControl
			// 
			this.razorPainterControl.Dock = System.Windows.Forms.DockStyle.Fill;
			this.razorPainterControl.Location = new System.Drawing.Point(0, 0);
			this.razorPainterControl.MinimumSize = new System.Drawing.Size(1, 1);
			this.razorPainterControl.Name = "razorPainterControl";
			this.razorPainterControl.Size = new System.Drawing.Size(694, 450);
			this.razorPainterControl.TabIndex = 0;
			// 
			// Main
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(694, 450);
			this.Controls.Add(this.razorPainterControl);
			this.Name = "Main";
			this.Text = "Main";
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Main_FormClosing);
			this.ResumeLayout(false);

		}

		#endregion

		private RazorGDIPainter.RazorPainterControl razorPainterControl;
		private System.Windows.Forms.Timer gameTimer2;
	}
}

