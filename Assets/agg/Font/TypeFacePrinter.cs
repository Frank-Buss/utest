﻿using MatterHackers.Agg.Image;
using MatterHackers.Agg.VertexSource;
using MatterHackers.VectorMath;

//----------------------------------------------------------------------------
// Anti-Grain Geometry - Version 2.4
//
// C# port by: Lars Brubaker
//                  larsbrubaker@gmail.com
// Copyright (C) 2007-2011
//
// Permission to copy, use, modify, sell and distribute this software
// is granted provided this copyright notice appears in all copies.
// This software is provided "as is" without express or implied
// warranty, and with no claim as to its suitability for any purpose.
//
//----------------------------------------------------------------------------
//
// Class StringPrinter.cs
//
// Class to output the vertex source of a string as a run of glyphs.
//----------------------------------------------------------------------------
using System;
using System.Collections.Generic;

namespace MatterHackers.Agg.Font
{
	public enum Justification { Left, Center, Right };

	public enum Baseline { BoundsTop, BoundsCenter, TextCenter, Text, BoundsBottom };

	public class TypeFacePrinter : VertexSourceLegacySupport
	{
		private String text = "";

		private Vector2 totalSizeCache;

		public Justification Justification { get; set; }

		public Baseline Baseline { get; set; }

		public bool DrawFromHintedCache { get; set; }

		StyledTypeFace typeFaceStyle;
		public StyledTypeFace TypeFaceStyle
		{
			get { return typeFaceStyle; }
			
			set
			{
				if (value != typeFaceStyle)
				{
					typeFaceStyle = value;
					totalSizeCache = new Vector2();
				}
			}
		}

		public String Text
		{
			get
			{
				return text;
			}
			set
			{
				if (text != value)
				{
					totalSizeCache.x = 0;
					text = value;
				}
			}
		}

		public Vector2 Origin { get; set; }

		public TypeFacePrinter(String text = "", double pointSize = 12, Vector2 origin = new Vector2(), Justification justification = Justification.Left, Baseline baseline = Baseline.Text)
			: this(text, new StyledTypeFace(LiberationSansFont.Instance, pointSize), origin, justification, baseline)
		{
		}

		public TypeFacePrinter(String text, StyledTypeFace typeFaceStyle, Vector2 origin = new Vector2(), Justification justification = Justification.Left, Baseline baseline = Baseline.Text)
		{
			this.TypeFaceStyle = typeFaceStyle;
			this.text = text;
			this.Justification = justification;
			this.Origin = origin;
			this.Baseline = baseline;
		}

		public TypeFacePrinter(String text, TypeFacePrinter copyPropertiesFrom)
			: this(text, copyPropertiesFrom.TypeFaceStyle, copyPropertiesFrom.Origin, copyPropertiesFrom.Justification, copyPropertiesFrom.Baseline)
		{
		}

		public RectangleDouble LocalBounds
		{
			get
			{
				Vector2 size = GetSize();
				RectangleDouble bounds;

				switch (Justification)
				{
					case Justification.Left:
						bounds = new RectangleDouble(0, TypeFaceStyle.DescentInPixels, size.x, size.y + TypeFaceStyle.DescentInPixels);
						break;

					case Justification.Center:
						bounds = new RectangleDouble(-size.x / 2, TypeFaceStyle.DescentInPixels, size.x / 2, size.y + TypeFaceStyle.DescentInPixels);
						break;

					case Justification.Right:
						bounds = new RectangleDouble(-size.x, TypeFaceStyle.DescentInPixels, 0, size.y + TypeFaceStyle.DescentInPixels);
						break;

					default:
						throw new NotImplementedException();
				}

				switch (Baseline)
				{
					case Font.Baseline.BoundsCenter:
						bounds.Offset(0, -TypeFaceStyle.AscentInPixels / 2);
						break;

					default:
						break;
				}

				bounds.Offset(Origin);
				return bounds;
			}
		}

		public void Render(Graphics2D graphics2D, RGBA_Bytes color, IVertexSourceProxy vertexSourceToApply)
		{
			vertexSourceToApply.VertexSource = this;
			rewind(0);
			if (DrawFromHintedCache)
			{
				// TODO: make this work
				graphics2D.Render(vertexSourceToApply, color);
			}
			else
			{
				graphics2D.Render(vertexSourceToApply, color);
			}
		}

		public void Render(Graphics2D graphics2D, RGBA_Bytes color)
		{
			if (DrawFromHintedCache)
			{
				RenderFromCache(graphics2D, color);
			}
			else
			{
				rewind(0);
				graphics2D.Render(this, color);
			}
		}

		private void RenderFromCache(Graphics2D graphics2D, RGBA_Bytes color)
		{
			if (text != null && text.Length > 0)
			{
				Vector2 currentOffset = Vector2.Zero;

				currentOffset = GetBaseline(currentOffset);
				currentOffset.y += Origin.y;

				string[] lines = text.Split('\n');
				foreach (string line in lines)
				{
					currentOffset = GetXPositionForLineBasedOnJustification(currentOffset, line);
					currentOffset.x += Origin.x;

					for (int currentChar = 0; currentChar < line.Length; currentChar++)
					{
						ImageBuffer currentGlyphImage = TypeFaceStyle.GetImageForCharacter(line[currentChar], 0, 0, color);

						if (currentGlyphImage != null)
						{
							graphics2D.Render(currentGlyphImage, currentOffset);
						}

						// get the advance for the next character
						currentOffset.x += TypeFaceStyle.GetAdvanceForCharacter(line, currentChar);
					}

					// before we go onto the next line we need to move down a line
					currentOffset.x = 0;
					currentOffset.y -= TypeFaceStyle.EmSizeInPixels;
				}
			}
		}

		public override IEnumerable<VertexData> Vertices()
		{
			if (text != null && text.Length > 0)
			{
				Vector2 currentOffset = new Vector2(0, 0);

				currentOffset = GetBaseline(currentOffset);

				string[] lines = text.Split('\n');
				foreach (string line in lines)
				{
					currentOffset = GetXPositionForLineBasedOnJustification(currentOffset, line);

					for (int currentChar = 0; currentChar < line.Length; currentChar++)
					{
						IVertexSource currentGlyph = TypeFaceStyle.GetGlyphForCharacter(line[currentChar]);

						if (currentGlyph != null)
						{
							foreach (VertexData vertexData in currentGlyph.Vertices())
							{
								if (vertexData.command != ShapePath.FlagsAndCommand.CommandStop)
								{
									VertexData offsetVertex = new VertexData(vertexData.command, vertexData.position + currentOffset + Origin);
									yield return offsetVertex;
								}
							}
						}

						// get the advance for the next character
						currentOffset.x += TypeFaceStyle.GetAdvanceForCharacter(line, currentChar);
					}

					// before we go onto the next line we need to move down a line
					currentOffset.x = 0;
					currentOffset.y -= TypeFaceStyle.EmSizeInPixels;
				}
			}

			VertexData endVertex = new VertexData(ShapePath.FlagsAndCommand.CommandStop, Vector2.Zero);
			yield return endVertex;
		}

		private Vector2 GetXPositionForLineBasedOnJustification(Vector2 currentOffset, string line)
		{
			Vector2 size = GetSize(line);
			switch (Justification)
			{
				case Justification.Left:
					currentOffset.x = 0;
					break;

				case Justification.Center:
					currentOffset.x = -size.x / 2;
					break;

				case Justification.Right:
					currentOffset.x = -size.x;
					break;

				default:
					throw new NotImplementedException();
			}
			return currentOffset;
		}

		private Vector2 GetBaseline(Vector2 currentOffset)
		{
			switch (Baseline)
			{
				case Baseline.Text:
					currentOffset.y = 0;
					break;

				case Baseline.BoundsTop:
					currentOffset.y = -TypeFaceStyle.AscentInPixels;
					break;

				case Baseline.BoundsCenter:
					currentOffset.y = -TypeFaceStyle.AscentInPixels / 2;
					break;

				default:
					throw new NotImplementedException();
			}
			return currentOffset;
		}

		public Vector2 GetSize(string text = null)
		{
			if (text == null)
			{
				text = this.text;
			}

			if (text != this.text)
			{
				Vector2 calculatedSize;
				GetSize(0, Math.Max(0, text.Length - 1), out calculatedSize, text);
				return calculatedSize;
			}

			if (totalSizeCache.x == 0)
			{
				Vector2 calculatedSize;
				GetSize(0, Math.Max(0, text.Length - 1), out calculatedSize, text);
				totalSizeCache = calculatedSize;
			}

			return totalSizeCache;
		}

		public void GetSize(int characterToMeasureStartIndexInclusive, int characterToMeasureEndIndexInclusive, out Vector2 offset, string text = null)
		{
			if (text == null)
			{
				text = this.text;
			}

			offset.x = 0;
			offset.y = TypeFaceStyle.EmSizeInPixels;

			double currentLineX = 0;

			for (int i = characterToMeasureStartIndexInclusive; i < characterToMeasureEndIndexInclusive; i++)
			{
				if (text[i] == '\n')
				{
					if (i + 1 < characterToMeasureEndIndexInclusive && (text[i + 1] == '\n') && text[i] != text[i + 1])
					{
						i++;
					}
					currentLineX = 0;
					offset.y += TypeFaceStyle.EmSizeInPixels;
				}
				else
				{
					currentLineX += TypeFaceStyle.GetAdvanceForCharacter(text, i);

					if (currentLineX > offset.x)
					{
						offset.x = currentLineX;
					}
				}
			}

			if (text.Length > characterToMeasureEndIndexInclusive)
			{
				if (text[characterToMeasureEndIndexInclusive] == '\n')
				{
					currentLineX = 0;
					offset.y += TypeFaceStyle.EmSizeInPixels;
				}
				else
				{
					offset.x += TypeFaceStyle.GetAdvanceForCharacter(text, characterToMeasureEndIndexInclusive);
				}
			}
		}

		public int NumLines()
		{
			int characterToMeasureStartIndexInclusive = 0;
			int characterToMeasureEndIndexInclusive = text.Length - 1;
			return NumLines(characterToMeasureStartIndexInclusive, characterToMeasureEndIndexInclusive);
		}

		public int NumLines(int characterToMeasureStartIndexInclusive, int characterToMeasureEndIndexInclusive)
		{
			int numLines = 1;

			characterToMeasureStartIndexInclusive = Math.Max(0, Math.Min(characterToMeasureStartIndexInclusive, text.Length - 1));
			characterToMeasureEndIndexInclusive = Math.Max(0, Math.Min(characterToMeasureEndIndexInclusive, text.Length - 1));
			for (int i = characterToMeasureStartIndexInclusive; i < characterToMeasureEndIndexInclusive; i++)
			{
				if (text[i] == '\n')
				{
					if (i + 1 < characterToMeasureEndIndexInclusive && (text[i + 1] == '\n') && text[i] != text[i + 1])
					{
						i++;
					}
					numLines++;
				}
			}

			return numLines;
		}

		public void GetOffset(int characterToMeasureStartIndexInclusive, int characterToMeasureEndIndexInclusive, out Vector2 offset)
		{
			offset = Vector2.Zero;

			characterToMeasureEndIndexInclusive = Math.Min(text.Length - 1, characterToMeasureEndIndexInclusive);

			for (int index = characterToMeasureStartIndexInclusive; index <= characterToMeasureEndIndexInclusive; index++)
			{
				if (text[index] == '\n')
				{
					offset.x = 0;
					offset.y -= TypeFaceStyle.EmSizeInPixels;
				}
				else
				{
					offset.x += TypeFaceStyle.GetAdvanceForCharacter(text, index);
				}
			}
		}

		// this will return the position to the left of the requested character.
		public Vector2 GetOffsetLeftOfCharacterIndex(int characterIndex)
		{
			Vector2 offset;
			GetOffset(0, characterIndex - 1, out offset);
			return offset;
		}

		// If the Text is "TEXT" and the position is less than half the distance to the center
		// of "T" the return value will be 0 if it is between the center of 'T' and the center of 'E'
		// it will be 1 and so on.
		public int GetCharacterIndexToStartBefore(Vector2 position)
		{
			int clostestIndex = -1;
			double clostestXDistSquared = double.MaxValue;
			double clostestYDistSquared = double.MaxValue;
			Vector2 offset = new Vector2(0, TypeFaceStyle.EmSizeInPixels * NumLines());
			int characterToMeasureStartIndexInclusive = 0;
			int characterToMeasureEndIndexInclusive = text.Length - 1;
			if (text.Length > 0)
			{
				characterToMeasureStartIndexInclusive = Math.Max(0, Math.Min(characterToMeasureStartIndexInclusive, text.Length - 1));
				characterToMeasureEndIndexInclusive = Math.Max(0, Math.Min(characterToMeasureEndIndexInclusive, text.Length - 1));
				for (int i = characterToMeasureStartIndexInclusive; i <= characterToMeasureEndIndexInclusive; i++)
				{
					CheckForBetterClickPosition(ref position, ref clostestIndex, ref clostestXDistSquared, ref clostestYDistSquared, ref offset, i);

					if (text[i] == '\r')
					{
						throw new Exception("All \\r's should have been converted to \\n's.");
					}

					if (text[i] == '\n')
					{
						offset.x = 0;
						offset.y -= TypeFaceStyle.EmSizeInPixels;
					}
					else
					{
						Vector2 nextSize;
						GetOffset(i, i, out nextSize);

						offset.x += nextSize.x;
					}
				}

				CheckForBetterClickPosition(ref position, ref clostestIndex, ref clostestXDistSquared, ref clostestYDistSquared, ref offset, characterToMeasureEndIndexInclusive + 1);
			}

			return clostestIndex;
		}

		private static void CheckForBetterClickPosition(ref Vector2 position, ref int clostestIndex, ref double clostestXDistSquared, ref double clostestYDistSquared, ref Vector2 offset, int i)
		{
			Vector2 delta = position - offset;
			double deltaYLengthSquared = delta.y * delta.y;
			if (deltaYLengthSquared < clostestYDistSquared)
			{
				clostestYDistSquared = deltaYLengthSquared;
				clostestXDistSquared = delta.x * delta.x;
				clostestIndex = i;
			}
			else if (deltaYLengthSquared == clostestYDistSquared)
			{
				double deltaXLengthSquared = delta.x * delta.x;
				if (deltaXLengthSquared < clostestXDistSquared)
				{
					clostestXDistSquared = deltaXLengthSquared;
					clostestIndex = i;
				}
			}
		}
	}
}