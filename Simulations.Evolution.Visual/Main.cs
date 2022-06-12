using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Simulations.Evolution.Visual
{
	public partial class Main : Form
	{
		private Thread renderThread;

		private int fps;
		private int ticks;
		private System.Timers.Timer fpsTimer;

		private Map map;

		public Main()
		{
			InitializeComponent();

			Start();
		}

		private void Start()
		{
			#region FPS Timer
			fpsTimer = new System.Timers.Timer(1000);
			fpsTimer.Elapsed += (_1, _2) =>
			{
				if (IsDisposed || Disposing)
					return;

				try
				{
					Action callback = () =>
					{
						Text = "FPS: " + fps + "; Ticks: " + ticks;
						fps = 0;
						ticks = 0;
					};
					if (this.InvokeRequired)
						this.Invoke(callback);
					else
						callback();					
				}
				catch (Exception e)
				{
					MessageBox.Show(e.Message);
				}
			};
			fpsTimer.Start();
			#endregion

			map = new Map(razorPainterControl.Width, razorPainterControl.Height);

			renderThread = new Thread(() =>
			{
				while (true)
					Render();
			});
			renderThread.Start();

			gameTimer2.Interval = 15;
			gameTimer2.Tick += (_1, _2) => Update();
			gameTimer2.Start();
		}

		private new void Update()
		{
			map.NextTick();
			ticks++;
		}

		private void Render()
		{
			// do lock to avoid resize/repaint race in control
			// where are BMP and GFX recreates
			// better practice is Monitor.TryEnter() pattern, but here we do it simpler
			lock (razorPainterControl.RazorLock)
			{
				razorPainterControl.RazorGFX.Clear(Color.LightGray);


				for (int i = 0; i < map.Foods.Count; i++)
				{
					Food food = map.Foods[i];
					razorPainterControl.RazorGFX.FillEllipse(Brushes.Green, food.GetRectangle());
				}
				for (int i = 0; i < map.Entities.Count; i++)
				{
					Entity entity = map.Entities[i];
					razorPainterControl.RazorGFX.FillEllipse(Brushes.Red, entity.GetRectangle());
				}


				razorPainterControl.RazorPaint();
			}
			fps++;
		}

		private void Main_FormClosing(object sender, FormClosingEventArgs e)
		{
			renderThread.Abort();
			fpsTimer?.Dispose();
		}
	}

	public class Entity : IRectangular
	{
		private const float massiveAdvantage = 0.4f;

		private readonly static Random rand = new Random();
		private readonly Map map;

		private float x;
		private float y;
		private int food;

		public Entity(float radius, float x, float y, Map map)
		{
			Radius = radius;
			this.map = map;
			X = x;
			Y = y;
						
			// TODO: возвращение в дом (на край карты)
			// Ищем еду, если одну нашли, то возвращаемся в дом. по дороге, если нашли еще, побираем и также возвращаемся в дом.

			// При этом убегаем от больших. Большие не могут кушать челиков в домике
		}

		public float Radius { get; set; }
		public float X {
			get => x;
			set
			{
				if (value < 0)
					x = 0;
				else if (value > map.Width)
					x = map.Width;
				else
					x = value;
			}
		}
		public float Y {
			get => y;
			set
			{
				if (value < 0)
					y = 0;
				else if (value > map.Height)
					y = map.Height;
				else
					y = value;
			}
		}
		public float Speed => 100 / (2 * Radius * Radius);
		public float Vision { get; set; } = 150;

		public RectangleF GetRectangle()
		{
			return new RectangleF(new PointF(X - Radius, Y - Radius), new SizeF(2 * Radius, 2 * Radius));
		}

		public void Do()
		{
			if (FindFood(out var foodCoords))
			{
				if (CanEat(foodCoords))
				{
					Eat(foodCoords);
				}
				else
				{
					GoTo(foodCoords);
				}
			}
			//else if (FindEnemy(out var enemyCoords))
			//{
			//	RunAwayFrom(enemyCoords);
			//}
			else
			{
				RandomMove();
			}
		}

		private bool FindEnemy(out (float X, float Y) coords)
		{
			coords = (-1, -1);
			var enemies = map.Entities
				.Where(e => e != this
					&& e.Radius >= (Radius * (1f - massiveAdvantage)))
				.Select(e => new
				{
					e.X,
					e.Y,
					D = Utils.GetDistance(X, Y, e.X, e.Y)
				})
				.Where(x => x.D <= Vision);

			if (enemies.Any())
			{
				var minD = enemies.Min(f => f.D);
				var nearlyEnemy = enemies.First(f => f.D == minD);
				coords = (nearlyEnemy.X, nearlyEnemy.Y);
				return true;
			}

			return false;
		}

		private void Eat((float X, float Y) coords)
		{
			map.Eat(coords);
			food++;
		}

		private bool CanEat((float X, float Y) coords)
		{
			float actionLength = Radius;
			return Utils.GetDistance(X, Y, coords.X, coords.Y) <= actionLength;
		}

		private bool FindFood(out (float X, float Y) coords)
		{
			coords = (-1, -1);
			var foods = map.Foods
				.Select(f => new
				{
					f.X,
					f.Y,
					D = Utils.GetDistance(X, Y, f.X, f.Y)
				})
				.Union(map.Entities
					.Where(e => e != this
						&& e.Radius <= (Radius * (1f - massiveAdvantage)))
					.Select(e => new
					{
						e.X,
						e.Y,
						D = Utils.GetDistance(X, Y, e.X, e.Y)
					})
				)
				.Where(x => x.D <= Vision);

			if (foods.Any())
			{
				var minD = foods.Min(f => f.D);
				var nearlyFood = foods.First(f => f.D == minD);
				coords = (nearlyFood.X, nearlyFood.Y);
				return true;
			}

			return false;
		}

		private void RandomMove()
		{
			X += GetRandomSign() * Speed;
			Y += GetRandomSign() * Speed;

			int GetRandomSign()
			{
				if (rand.Next(0, 2) == 0)
					return 1;
				else
					return -1;
			}
		}

		private void GoTo((float X, float Y) coords)
		{
			var (dx, dy) = GetDeltasToTarget(coords);
			X -= dx;
			Y -= dy;
		}

		private void RunAwayFrom((float X, float Y) coords)
		{
			var (dx, dy) = GetDeltasToTarget(coords);
			X += dx;
			Y += dy;
		}

		private (float Dx, float Dy) GetDeltasToTarget((float X, float Y) targetCoords)
		{
			var dx = X - targetCoords.X;
			var dy = Y - targetCoords.Y;

			return (
				Math.Sign(dx) * GetEqualOrLessAbs(Speed, dx),
				Math.Sign(dy) * GetEqualOrLessAbs(Speed, dy)
			);

			// v должно быть меньше или равно |e|
			float GetEqualOrLessAbs(float v, float e)
			{
				if (e < 0)
					e = -e;

				if (v < e)
					return v;
				else
					return e;
			}
		}
	}

	public class Map
	{
		private const int chanceRangeToFoodSpawn = 60;
		private static Random rand = new Random();
		private int foodCount;

		public Map(int width, int height, int entitiesCount = 100, int foodCount = 100)
		{
			Width = width;
			Height = height;

			Entities = new List<Entity>(entitiesCount);
			//Entities.Add(new Entity(10, 100, 100, this));
			for (int i = 0; i < entitiesCount; i++)
			{
				Entities.Add(new Entity(rand.Next(5, 10), rand.Next(0, Width), rand.Next(0, Height), this));
			}

			Foods = new List<Food>(foodCount);
			//Foods.Add(new Food(10, 10));
			for (int i = 0; i < foodCount; i++)
			{
				Foods.Add(new Food(rand.Next(0, Width), rand.Next(0, Height)));
			}
			foodCount = Foods.Count;
		}

		public int Width { get; }
		public int Height { get; }

		public List<Entity> Entities { get; private set; }
		public List<Food> Foods { get; private set; }

		public void NextTick()
		{
			for (int i = 0; i < Entities.Count; i++)
			{
				Entities[i].Do();
			}

			if (Foods.Count > foodCount)
			{
				Foods.Add(new Food(rand.Next(0, Width), rand.Next(0, Height)));
				foodCount++;
			}
		}

		internal void Eat((float X, float Y) coords)
		{
			var _ = EatFood(coords) || EatEntity(coords);
		}

		private bool EatFood((float X, float Y) coords)
		{
			var eatenFood = Foods.FirstOrDefault(f => f.X == coords.X && f.Y == coords.Y);
			if (!(eatenFood is null))
			{
				return Foods.Remove(eatenFood);
			}
			return false;
		}

		private bool EatEntity((float X, float Y) coords)
		{
			var eatenEntity = Entities.FirstOrDefault(f => f.X == coords.X && f.Y == coords.Y);
			if (!(eatenEntity is null))
			{
				return Entities.Remove(eatenEntity);
			}
			return false;
		}
	}

	public class Food : IRectangular
	{
		public Food(float x, float y)
		{
			X = x;
			Y = y;
		}

		public float X { get; set; }
		public float Y { get; set; }
		public float Radius => 4;

		public RectangleF GetRectangle()
		{
			return new RectangleF(new PointF(X - Radius, Y - Radius), new SizeF(2 * Radius, 2 * Radius));
		}

		public override string ToString()
		{
			return $"({X}; {Y})";
		}
	}

	public interface IRectangular
	{
		RectangleF GetRectangle();
	}

	public static class Utils
	{
		public static float GetDistance(float x1, float y1, float x2, float y2)
		{
			var dx = x1 - x2;
			var dy = y1 - y2;
			return (float)Math.Sqrt(dx * dx + dy * dy);
		}
	}
}
