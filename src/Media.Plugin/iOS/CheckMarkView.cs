using System;
using CoreGraphics;
using UIKit;

namespace Plugin.Media
{
	public class CheckMarkView : UIView
	{
		private bool _checked = false;
		private CheckMarkStyle _checkMarkStyle = CheckMarkStyle.OpenCircle;

		public CheckMarkView()
		{
			Opaque = false;
		}

		public bool Checked
		{
			get
			{
				return _checked;
			}
			set
			{
				_checked = value;
				SetNeedsDisplay();
			}
		}

		public CheckMarkStyle CheckMarkStyle
		{
			get
			{
				return _checkMarkStyle;
			}
			set
			{
				_checkMarkStyle = value;
				SetNeedsDisplay();
			}
		}

		public override void Draw(CGRect rect)
		{
			if (Checked)
				DrawRectChecked(rect);
			else if (CheckMarkStyle == CheckMarkStyle.OpenCircle)
				DrawRectOpenCircle(rect);
			else if (CheckMarkStyle == CheckMarkStyle.GrayedOut)
				DrawRectGrayedOut(rect);
		}


		private void DrawRectChecked(CGRect rect)
		{
			var context = UIGraphics.GetCurrentContext();

			var checkmarkBlue2 = UIColor.FromRGBA(0.078f, 0.435f, 0.875f, 1f);

			// Shadow Declarations
			var shadow2 = UIColor.Brown;
			var shadow2Offset = new CGSize(0.1, -0.1);
			nfloat shadow2BlurRadius = 2.5f;

			var frame = Bounds;

			// Subframes
			var group = new CGRect(frame.GetMinX() + 3, frame.GetMinY() + 3, frame.Width - 6, frame.Height - 6);


			// CheckedOval Drawing
			var checkedOvalPath = UIBezierPath.FromOval(new CGRect(group.GetMinX() + Math.Floor(group.Width * 0.00000 + 0.5), group.GetMinY() + Math.Floor(group.Height * 0.00000 + 0.5), Math.Floor(group.Width * 1.00000 + 0.5) - Math.Floor(group.Width * 0.00000 + 0.5), Math.Floor(group.Height * 1.00000 + 0.5) - Math.Floor(group.Height * 0.00000f + 0.5f)));
			context.SaveState();
			context.SetShadow(shadow2Offset, shadow2BlurRadius, shadow2.CGColor);
			checkmarkBlue2.SetFill();
			checkedOvalPath.Fill();
			context.RestoreState();

			UIColor.White.SetStroke();
			checkedOvalPath.LineWidth = 1;
			checkedOvalPath.Stroke();


			// Bezier Drawing
			var bezierPath = new UIBezierPath();
			bezierPath.MoveTo(new CGPoint(group.GetMinX() + 0.27083f * group.Width, group.GetMinY() + 0.54167f * group.Height));
			bezierPath.AddLineTo(new CGPoint(group.GetMinX() + 0.41667f * group.Width, group.GetMinY() + 0.68750f * group.Height));
			bezierPath.AddLineTo(new CGPoint(group.GetMinX() + 0.75000f * group.Width, group.GetMinY() + 0.35417f * group.Height));
			bezierPath.LineCapStyle = CGLineCap.Square;

			UIColor.White.SetStroke();
			bezierPath.LineWidth = 1.3f;
			bezierPath.Stroke();
		}

		private void DrawRectGrayedOut(CGRect rect)
		{
			var context = UIGraphics.GetCurrentContext();

			var grayTranslucent = UIColor.FromRGBA(1, 1, 1, 0.6f);

			// Shadow Declarations
			var shadow2 = UIColor.Black;
			var shadow2Offset = new CGSize(0.1, -0.1);
			nfloat shadow2BlurRadius = 2.5f;

			var frame = Bounds;

			// Subframes
			var group = new CGRect(frame.GetMinX() + 3, frame.GetMinY() + 3, frame.Width - 6, frame.Height - 6);

			// UncheckedOval Drawing
			var uncheckedOvalPath = UIBezierPath.FromOval(new CGRect(group.GetMinX() + Math.Floor(group.Width * 0.00000 + 0.5), group.GetMinY() + Math.Floor(group.Height * 0.00000 + 0.5), Math.Floor(group.Width * 1.00000 + 0.5) - Math.Floor(group.Width * 0.00000 + 0.5), Math.Floor(group.Height * 1.00000 + 0.5) - Math.Floor(group.Height * 0.00000 + 0.5)));
			context.SaveState();
			context.SetShadow(shadow2Offset, shadow2BlurRadius, shadow2.CGColor);
			grayTranslucent.SetFill();
			uncheckedOvalPath.Fill();
			context.RestoreState();
			UIColor.White.SetStroke();
			uncheckedOvalPath.LineWidth = 1f;
			uncheckedOvalPath.Stroke();


			// Bezier Drawing
			var bezierPath = new UIBezierPath();
			bezierPath.MoveTo(new CGPoint(group.GetMinX() + 0.27083 * group.Width, group.GetMinY() + 0.54167 * group.Height));
			bezierPath.AddLineTo(new CGPoint(group.GetMinX() + 0.41667 * group.Width, group.GetMinY() + 0.68750 * group.Height));
			bezierPath.AddLineTo(new CGPoint(group.GetMinX() + 0.75000 * group.Width, group.GetMinY() + 0.35417 * group.Height));
			bezierPath.LineCapStyle = CGLineCap.Square;
			UIColor.White.SetStroke();
			bezierPath.LineWidth = 1.3f;
			bezierPath.Stroke();
		}

		private void DrawRectOpenCircle(CGRect rect)
		{
			var context = UIGraphics.GetCurrentContext();

			// Shadow Declarations
			var shadow = UIColor.Black;
			var shadowOffset = new CGSize(0.1, -0.1);
			nfloat shadowBlurRadius = 0.5f;
			var shadow2 = UIColor.Black;
			var shadow2Offset = new CGSize(0.1, -0.1);
			nfloat shadow2BlurRadius = 2.5f;

			var frame = Bounds;

			// Subframes
			var group = new CGRect(frame.GetMinX() + 3, frame.GetMinY() + 3, frame.Width - 6, frame.Height - 6);


			// EmptyOval Drawing
			var emptyOvalPath = UIBezierPath.FromOval(new CGRect(group.GetMinX() + Math.Floor(group.Width * 0.00000 + 0.5), group.GetMinY() + Math.Floor(group.Height * 0.00000 + 0.5), Math.Floor(group.Width * 1.00000 + 0.5) - Math.Floor(group.Width * 0.00000 + 0.5), Math.Floor(group.Height * 1.00000 + 0.5) - Math.Floor(group.Height * 0.00000 + 0.5)));
			context.SaveState();
			context.SetShadow(shadow2Offset, shadow2BlurRadius, shadow2.CGColor);
			context.RestoreState();
			context.SaveState();
			context.SetShadow(shadowOffset, shadowBlurRadius, shadow.CGColor);
			UIColor.White.SetStroke();
			emptyOvalPath.LineWidth = 1;
			emptyOvalPath.Stroke();
			context.RestoreState();
		}
	}

	public enum CheckMarkStyle
	{
		OpenCircle,
		GrayedOut
	}

	//enum SSCheckMarkStyle
	//{
	//	OpenCircle,
	//	GrayedOut
	//}

	//class SSCheckMark : UIView
	//{

	//	private bool checkedBool = false;
	//	// choose whether you like open or grayed out non-selected items
	//	private SSCheckMarkStyle checkMarkStyleReal = SSCheckMarkStyle.GrayedOut;

	//	public bool Checked
	//	{
	//		get => checkedBool;
	//		set
	//		{
	//			checkedBool = value;
	//			SetNeedsDisplay();
	//		}
	//	}

	//	public SSCheckMarkStyle CheckMarkStyle
	//	{
	//		get => checkMarkStyleReal;
	//		set
	//		{
	//			checkMarkStyleReal = value;
	//			SetNeedsDisplay();
	//		}
	//	}

	//	public override void Draw(CGRect rect)
	//	{
	//		base.Draw(rect);

	//		if (Checked)
	//		{
	//			this.DrawRectChecked(rect);
	//		}
	//		else
	//		{
	//			if (CheckMarkStyle == SSCheckMarkStyle.OpenCircle)
	//			{
	//				DrawRectOpenCircle(rect);
	//			}
	//			else if (CheckMarkStyle == SSCheckMarkStyle.GrayedOut)
	//			{
	//				DrawRectGrayedOut(rect);
	//			}
	//		}
	//	}

	//	private void DrawRectChecked(CGRect rect)
	//	{
	//		var context = UIGraphics.GetCurrentContext();
	//		var checkmarkBlue2 = UIColor.FromRGB((nfloat)0.078, (nfloat)0.435, (nfloat)0.875);
	//		var shadow2 = UIColor.Black;
	//		var shadow2Offset = new CGSize(width: 0.1, height: -0.1);
	//		var shadow2BlurRadius = 2.5;
	//		var frame = this.Bounds;
	//		var group = new CGRect(frame.Left + 3, frame.Bottom + 3, width: frame.Width - 6, height: frame.Height - 6);

	//		var checkedOvalPath = UIBezierPath.FromOval(new CGRect(group.Left + Math.Floor(group.Width * 0.00000 + 0.5), group.Bottom + Math.Floor(group.Height * 0.00000 + 0.5), Math.Floor(group.Width * 1.00000 + 0.5) - Math.Floor(group.Width * 0.00000 + 0.5), height: Math.Floor(group.Height * 1.00000 + 0.5) - Math.Floor(group.Height * 0.00000 + 0.5)));

	//		context.SaveState();

	//		context.SetShadow(shadow2Offset, (nfloat)shadow2BlurRadius, shadow2.CGColor);
	//		checkmarkBlue2.SetFill();
	//		checkedOvalPath.Fill();
	//		context.RestoreState();

	//		UIColor.White.SetStroke();
	//		checkedOvalPath.LineWidth = 1;
	//		checkedOvalPath.Stroke();

	//		var bezierPath = new UIBezierPath();
	//		bezierPath.MoveTo(new CGPoint(group.Left + 0.27083 * group.Width, group.Top + 0.54167 * group.Height));
	//		bezierPath.AddLineTo(new CGPoint(group.Left + 0.41667 * group.Width, group.Bottom + 0.68750 * group.Height));
	//		bezierPath.AddLineTo(new CGPoint(group.Left + 0.75000 * group.Width, group.Bottom + 0.35417 * group.Height));
	//		bezierPath.LineCapStyle = CGLineCap.Square;
	//		UIColor.White.SetStroke();
	//		bezierPath.LineWidth = (nfloat)1.3;
	//		bezierPath.Stroke();
	//	}

	//	void DrawRectGrayedOut(CGRect rect)
	//	{
	//		var context = UIGraphics.GetCurrentContext();
	//		var grayTranslucent = UIColor.FromRGBA(0xFF, 0xFF, 0xFF, 0x99);
	//		var shadow2 = UIColor.Black;
	//		var shadow2Offset = new CGSize(0.1, -0.1);
	//		var shadow2BlurRadius = 2.5;
	//		var frame = Bounds;
	//		var group = new CGRect(frame.Left + 3, y: frame.Bottom + 3, width: frame.Width - 6, height: frame.Height - 6);

	//		var uncheckedOvalPath = UIBezierPath.FromOval(new CGRect(group.Bottom + Math.Floor(group.Width * 0.00000 + 0.5), group.Bottom + Math.Floor(group.Height * 0.00000 + 0.5), Math.Floor(group.Width * 1.00000 + 0.5) - Math.Floor(group.Width * 0.00000 + 0.5), Math.Floor(group.Height * 1.00000 + 0.5) - Math.Floor(group.Height * 0.00000 + 0.5)));

	//		context.SaveState();
	//		context.SetShadow(shadow2Offset, (nfloat)shadow2BlurRadius, shadow2.CGColor);
	//		grayTranslucent.SetFill();
	//		uncheckedOvalPath.Fill();
	//		context.RestoreState();

	//		UIColor.White.SetStroke();
	//		uncheckedOvalPath.LineWidth = 1;
	//		uncheckedOvalPath.Stroke();

	//		var bezierPath = new UIBezierPath();
	//		bezierPath.MoveTo(new CGPoint(group.Left + 0.27083 * group.Width, group.Bottom + 0.54167 * group.Height));
	//		bezierPath.AddLineTo(new CGPoint(group.Left + 0.41667 * group.Width, group.Bottom + 0.68750 * group.Height));
	//		bezierPath.AddLineTo(new CGPoint(group.Left + 0.75000 * group.Width, group.Bottom + 0.35417 * group.Height));
	//		bezierPath.LineCapStyle = CGLineCap.Square;
	//		UIColor.White.SetStroke();
	//		bezierPath.LineWidth = (nfloat)1.3;
	//		bezierPath.Stroke();
	//	}

	//	void DrawRectOpenCircle(CGRect rect)
	//	{
	//		var context = UIGraphics.GetCurrentContext();

	//		var shadow = UIColor.Black;

	//		var shadowOffset = new CGSize(width: 0.1, height: -0.1);

	//		var shadowBlurRadius = 0.5;

	//		var shadow2 = UIColor.Black;

	//		var shadow2Offset = new CGSize(width: 0.1, height: -0.1);

	//		var shadow2BlurRadius = 2.5;

	//		var frame = Bounds;

	//		var group = new CGRect(x: frame.Left + 3, y: frame.Bottom + 3, width: frame.Width - 6, height: frame.Height - 6);

	//		var emptyOvalPath = UIBezierPath.FromOval(new CGRect((nfloat)(group.Left + Math.Floor(group.Width * 0.00000 + 0.5)), (nfloat)(group.Bottom + Math.Floor(group.Height * 0.00000 + 0.5)), (nfloat)(Math.Floor(group.Width * 1.00000 + 0.5) - Math.Floor(group.Width * 0.00000 + 0.5)), (nfloat)(Math.Floor(group.Height * 1.00000 + 0.5) - Math.Floor(group.Height * 0.00000 + 0.5))));

	//		context.SaveState();
	//		context.SetShadow(shadow2Offset, (nfloat)shadow2BlurRadius, shadow2.CGColor);
	//		context.RestoreState();

	//		context.SaveState();
	//		context.SetShadow(shadowOffset, (nfloat)shadowBlurRadius, shadow.CGColor);
	//		UIColor.White.SetStroke();
	//		emptyOvalPath.LineWidth = 1;
	//		emptyOvalPath.Stroke();
	//		context.RestoreState();
	//	}
	//}
}
