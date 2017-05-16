using SQLite.Net.Attributes;
using SQLiteNetExtensions.Attributes;
using System;
using System.Xml;

namespace SpriteAnimationBuddyLib
{
	/// <summary>
	/// An animation description for an AnimatingSprite object.
	/// </summary>
	public class Animation
	{
		#region Fields

		[PrimaryKey, AutoIncrement]
		public int? Id { get; set; }

		[ForeignKey(typeof(AnimatingSprite))]
		public int AnimatingSpriteId { get; set; }

		/// <summary>
		/// The name of this animation
		/// </summary>
		[MaxLength(64)]
		public string Name { get; set; }

		/// <summary>
		/// The first frame of the animation.
		/// </summary>
		public int StartingFrame { get; set; }

		/// <summary>
		/// The last frame of the animation.
		/// </summary>
		public int EndingFrame { get; set; }

		/// <summary>
		/// The interval between frames of the animation.
		/// </summary>
		public int Interval { get; set; }

		/// <summary>
		/// If true, the animation loops.
		/// </summary>
		public bool IsLoop { get; set; }

		#endregion //Fields

		#region Methods

		/// <summary>
		/// Creates a new Animation object.
		/// </summary>
		public Animation() { }

		/// <summary>
		/// Creates a new Animation object by full specification.
		/// </summary>
		public Animation(int? id, string name, int startingFrame, int endingFrame, int interval, bool isLoop)
		{
			Id = id;
			Name = name;
			StartingFrame = startingFrame;
			EndingFrame = endingFrame;
			Interval = interval;
			IsLoop = isLoop;
		}

		/// <summary>
		/// copy constructor
		/// </summary>
		public Animation(Animation obj)
			: this(obj.Id, obj.Name, obj.StartingFrame, obj.EndingFrame, obj.Interval, obj.IsLoop)
		{
		}

		public void ParseXmlNode(XmlNode xmlNode)
		{
			string name = xmlNode.Name;
			string value = xmlNode.InnerText;

			switch (name)
			{
				case "Name":
					{
						Name = value;
					}
					break;
				case "StartingFrame":
					{
						StartingFrame = Convert.ToInt32(value);
					}
					break;

				case "EndingFrame":
					{
						EndingFrame = Convert.ToInt32(value);
					}
					break;
				case "Interval":
					{
						Interval = Convert.ToInt32(value);
					}
					break;
				case "IsLoop":
					{
						IsLoop = Convert.ToBoolean(value);
					}
					break;
				default:
					{
						throw new ArgumentException(@"bad xml node name in animaion: " + name);
					}
			}
		}

		public void WriteXmlNode(XmlNode xmlNode)
		{
			//TODO WriteXml:
			throw new NotImplementedException();
		}

		#endregion //Methods
	}
}
