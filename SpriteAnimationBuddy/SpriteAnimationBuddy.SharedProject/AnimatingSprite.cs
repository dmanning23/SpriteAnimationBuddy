using FilenameBuddy;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using SQLite.Net.Attributes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Xml;
using Vector2Extensions;
using XmlBuddy;

namespace SpriteAnimationBuddyLib
{
	/// <summary>
	/// A sprite sheet with flipbook-style animations.
	/// </summary>
	public class AnimatingSprite
	{
		#region Fields

		/// <summary>
		/// The dimensions of a single frame of animation.
		/// </summary>
		private Point _frameDimensions = Point.Zero;

		private Vector2 _sourceOffset = Vector2.Zero;

		/// <summary>
		/// The animation currently playing back on this sprite.
		/// </summary>
		private Animation _currentAnimation = null;

		/// <summary>
		/// The current frame in the current animation.
		/// </summary>
		private int _currentFrame = 0;

		/// <summary>
		/// The elapsed time since the last frame switch.
		/// </summary>
		private float _elapsedTime = 0.0f;

		#endregion //Fields

		#region Properties

		[PrimaryKey, AutoIncrement]
		public int? Id { get; set; }

		[MaxLength(128)]
		public string TextureName
		{
			get
			{
				return TextureFilename.GetRelFilename();
			}
			set
			{
				TextureFilename = new Filename(value);
			}
		}

		/// <summary>
		/// The number of frames in a row in this sprite.
		/// </summary>
		public int FramesPerRow { get; set; }

		public int FrameDimensionsX
		{
			get
			{
				return _frameDimensions.X;
			}
			set
			{
				_frameDimensions.X = value;
			}
		}
		public int FrameDimensionsY
		{
			get
			{
				return _frameDimensions.Y;
			}
			set
			{
				_frameDimensions.Y = value;
			}
		}

		public float SourceOffsetX
		{
			get
			{
				return _sourceOffset.X;
			}
			set
			{
				_sourceOffset.X = value;
			}
		}
		public float SourceOffsetY
		{
			get
			{
				return _sourceOffset.Y;
			}
			set
			{
				_sourceOffset.Y = value;
			}
		}

		[Ignore]
		public string Name
		{
			get
			{
				return TextureFilename.GetFileNoExt();
			}
		}

		/// <summary>
		/// The content path and name of the texture for this spell animation.
		/// </summary>
		[Ignore]
		public Filename TextureFilename { get; set; }

		/// <summary>
		/// The texture for this spell animation.
		/// </summary>
		[Ignore]
		public Texture2D Texture { get; set; }

		/// <summary>
		/// The width of a single frame of animation.
		/// </summary>
		[Ignore]
		public Point FrameDimensions
		{
			get
			{
				return _frameDimensions;
			}
			set
			{
				_frameDimensions = value;
			}
		}

		/// <summary>
		/// The offset of this sprite from the position it's drawn at.
		/// </summary>
		[Ignore]
		public Vector2 SourceOffset
		{
			get
			{
				return _sourceOffset;
			}
			set
			{
				_sourceOffset = value;
			}
		}

		/// <summary>
		/// The animations defined for this sprite.
		/// </summary>
		[Ignore]
		private List<Animation> Animations { get; set; }

		/// <summary>
		/// Enumerate the animations on this animated sprite.
		/// </summary>
		/// <param name="animationName">The name of the animation.</param>
		/// <returns>The animation if found; null otherwise.</returns>
		[Ignore]
		public Animation this[string animationName]
		{
			get
			{
				if (String.IsNullOrEmpty(animationName))
				{
					return null;
				}
				foreach (Animation animation in Animations)
				{
					if (String.Compare(animation.Name, animationName, StringComparison.OrdinalIgnoreCase) == 0)
					{
						return animation;
					}
				}
				return null;
			}
		}

		/// <summary>
		/// The source rectangle of the current frame of animation.
		/// </summary>
		[Ignore]
		public Rectangle SourceRectangle { get; private set; }

		#endregion //Properties

		#region Methods

		/// <summary>
		/// default constructor
		/// </summary>
		public AnimatingSprite()
		{
			Animations = new List<Animation>();
		}

		public AnimatingSprite(ContentManager content, string textureName, Point frameDimensions, Vector2 sourceOffset) :this()
		{
			//check for bad frame dimensions when building this way
			if (frameDimensions.X <= 0 || frameDimensions.Y <= 0)
			{
				throw new Exception($"Frame dimension of {{{frameDimensions.X}, {{{frameDimensions.Y}}} is invalid.");
			}

			//get the texture name
			TextureName = textureName;

			//get the source offset
			SourceOffset = sourceOffset;

			//grab the frame dimensions
			FrameDimensions = frameDimensions;

			//load the texture
			Texture = content.Load<Texture2D>(TextureFilename.GetRelPathFileNoExt());

			//set the frames per row
			FramesPerRow = Texture.Width / FrameDimensions.X;
		}

		/// <summary>
		/// copy constructor
		/// </summary>
		/// <param name="obj"></param>
		public AnimatingSprite(AnimatingSprite obj)
				: this()
		{
			Id = obj.Id;
			_frameDimensions = obj._frameDimensions;
			_currentAnimation = obj._currentAnimation;
			_currentFrame = obj._currentFrame;
			_elapsedTime = obj._elapsedTime;

			Id = obj.Id;
			TextureName = obj.TextureName;
			Texture = obj.Texture;

			FramesPerRow = obj.FramesPerRow;
			SourceOffset = obj.SourceOffset;
			Animations.AddRange(obj.Animations);
			SourceRectangle = obj.SourceRectangle;
		}

		/// <summary>
		/// Add the animation to the list, checking for name collisions.
		/// </summary>
		/// <returns>True if the animation was added to the list.</returns>
		public bool AddAnimation(Animation animation)
		{
			if ((animation != null) && (this[animation.Name] == null))
			{
				//set the foreign key of the animation
				if (Id.HasValue)
				{
					animation.AnimatingSpriteId = Id.Value;
				}

				//add the animation to the list
				Animations.Add(animation);

				//If there is currently not an animation playing, set it to the new one.
				if (null == _currentAnimation)
				{
					_currentAnimation = animation;
				}

				return true;
			}

			return false;
		}

		/// <summary>
		/// Play the given animation on the sprite.
		/// </summary>
		/// <remarks>The given animation may be null, to clear any animation.</remarks>
		public void PlayAnimation(Animation animation)
		{
			// start the new animation, ignoring redundant Plays
			if (animation != _currentAnimation)
			{
				_currentAnimation = animation;
				ResetAnimation();
			}
		}

		/// <summary>
		/// Play an animation given by index.
		/// </summary>
		public void PlayAnimation(int index)
		{
			// check the parameter
			if ((index < 0) || (index >= Animations.Count))
			{
				throw new ArgumentOutOfRangeException("index");
			}

			PlayAnimation(this.Animations[index]);
		}

		/// <summary>
		/// Play an animation given by name.
		/// </summary>
		public void PlayAnimation(string name)
		{
			// check the parameter
			if (String.IsNullOrEmpty(name))
			{
				throw new ArgumentNullException("name");
			}

			PlayAnimation(this[name]);
		}

		/// <summary>
		/// Play a given animation name, with the given direction suffix.
		/// </summary>
		/// <example>
		/// For example, passing "Walk" and "South" will play the animation
		/// named "WalkSouth".
		/// </example>
		public void PlayAnimation(string name, string direction)
		{
			// check the parameter
			if (String.IsNullOrEmpty(name))
			{
				throw new ArgumentNullException("name");
			}

			PlayAnimation(name + direction);
		}

		/// <summary>
		/// Reset the animation back to its starting position.
		/// </summary>
		public void ResetAnimation()
		{
			_elapsedTime = 0f;
			if (_currentAnimation != null)
			{
				_currentFrame = _currentAnimation.StartingFrame;

				// calculate the source rectangle by updating the animation
				UpdateAnimation(0f);
			}
		}

		/// <summary>
		/// Advance the current animation to the final sprite.
		/// </summary>
		public void AdvanceToEnd()
		{
			if (_currentAnimation != null)
			{
				_currentFrame = _currentAnimation.EndingFrame;

				// calculate the source rectangle by updating the animation
				UpdateAnimation(0f);
			}
		}

		/// <summary>
		/// Stop any animation playing on the sprite.
		/// </summary>
		public void StopAnimation()
		{
			_currentAnimation = null;
		}

		/// <summary>
		/// Returns true if playback on the current animation is complete, or if
		/// there is no animation at all.
		/// </summary>
		public bool IsPlaybackComplete
		{
			get
			{
				return ((_currentAnimation == null) ||
					(!_currentAnimation.IsLoop &&
					(_currentFrame > _currentAnimation.EndingFrame)));
			}
		}

		/// <summary>
		/// Update the current animation.
		/// </summary>
		public void UpdateAnimation(float elapsedSeconds)
		{
			if (IsPlaybackComplete)
			{
				return;
			}

			// loop the animation if needed
			if (_currentAnimation.IsLoop && (_currentFrame > _currentAnimation.EndingFrame))
			{
				_currentFrame = _currentAnimation.StartingFrame;
			}

			// update the source rectangle
			int column = _currentFrame / FramesPerRow;
			SourceRectangle = new Rectangle(
				(_currentFrame - (column * FramesPerRow)) * _frameDimensions.X,
				column * _frameDimensions.Y,
				_frameDimensions.X, _frameDimensions.Y);

			// update the elapsed time
			_elapsedTime += elapsedSeconds;

			// advance to the next frame if ready
			while (_elapsedTime >= _currentAnimation.Interval)
			{
				_currentFrame++;
				_elapsedTime -= _currentAnimation.Interval;
			}
		}

		/// <summary>
		/// Draw the sprite at the given position.
		/// </summary>
		/// <param name="spriteBatch">The SpriteBatch object used to draw.</param>
		/// <param name="position">The position of the sprite on-screen.</param>
		/// <param name="layerDepth">The depth at which the sprite is drawn.</param>
		/// <param name="color">the color to draw the sprite</param>
		/// <param name="spriteEffect">The sprite-effect applied.</param>
		public void Draw(SpriteBatch spriteBatch,
			Vector2 position,
			Color color,
			float layerDepth = 0,
			SpriteEffects spriteEffect = SpriteEffects.None)
		{
			// check the parameters
			Debug.Assert(null != spriteBatch);

			if (Texture != null)
			{
				spriteBatch.Draw(Texture,
					position,
					SourceRectangle,
					color,
					0f,
					SourceOffset,
					1f,
					spriteEffect,
					MathHelper.Clamp(layerDepth, 0f, 1f));
			}
		}

		/// <summary>
		/// Draw the sprite at the specified rectangle
		/// </summary>
		/// <param name="spriteBatch"></param>
		/// <param name="targetRect"></param>
		/// <param name="layerDepth"></param>
		/// <param name="color"></param>
		/// <param name="spriteEffect"></param>
		public void Draw(SpriteBatch spriteBatch,
			Rectangle targetRect,
			Color color,
			float layerDepth = 0,
			SpriteEffects spriteEffect = SpriteEffects.None)
		{
			// check the parameters
			Debug.Assert(null != spriteBatch);

			if (Texture != null)
			{
				spriteBatch.Draw(Texture,
					targetRect,
					SourceRectangle,
					color,
					0f,
					SourceOffset,
					spriteEffect,
					MathHelper.Clamp(layerDepth, 0f, 1f));
			}
		}

		public void ParseXml(XmlNode xmlNode)
		{
			string name = xmlNode.Name;
			string value = xmlNode.InnerText;

			switch (name)
			{
				case "AssetName":
					{
						//AssetName = value;
					}
					break;
				case "TextureName":
					{
						var sb = new StringBuilder();
						sb.Append(@"Textures\");
						sb.Append(value);
						sb.Append(@".png");
						TextureFilename = new Filename(sb.ToString());
					}
					break;
				case "FrameDimensions":
					{
						FrameDimensions = value.ToPoint();
					}
					break;
				case "FramesPerRow":
					{
						FramesPerRow = Convert.ToInt32(value);
					}
					break;
				case "SourceOffset":
					{
						SourceOffset = value.ToVector2();
					}
					break;
				case "Animations":
					{
						XmlFileBuddy.ReadChildNodes(xmlNode, ParseAnimations);
					}
					break;
			}
		}

		private void ParseAnimations(XmlNode xmlNode)
		{
			var animation = new Animation();
			XmlFileBuddy.ReadChildNodes(xmlNode, animation.ParseXmlNode);
			Animations.Add(animation);
		}

		public void LoadContent(ContentManager content)
		{
			Texture = content.Load<Texture2D>(TextureFilename.GetRelPathFileNoExt());
		}

		#endregion Methods
	}
}
