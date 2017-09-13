using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using AssetsLibrary;
using CoreGraphics;
using Foundation;
using Plugin.Media.Abstractions;
using UIKit;

namespace Plugin.Media
{

	public class ELCImagePickerViewController : UINavigationController
	{
		private int _maxImagesCount;

		private readonly StoreCameraMediaOptions options;

		readonly TaskCompletionSource<List<MediaFile>> _TaskCompletionSource = new TaskCompletionSource<List<MediaFile>>();
		public Task<List<MediaFile>> Completion
		{
			get
			{
				return _TaskCompletionSource.Task;
			}
		}

		public static ELCImagePickerViewController NewInstance(StoreCameraMediaOptions options = null, MultiPickerCustomisations customisations = null)
		{
			customisations = customisations ?? new MultiPickerCustomisations();

			var albumPicker = new ELCAlbumPickerController();
			var picker = new ELCImagePickerViewController(albumPicker, options);
			albumPicker.Parent = picker;
			albumPicker.PathToOverlay = customisations.PathToOverlay;
			picker._maxImagesCount = customisations.MaximumImagesCount;
			picker.NavigationBar.BarStyle = GetUIBarStyle(customisations.BarStyle);
			return picker;
		}

		private static UIBarStyle GetUIBarStyle(MultiPickerBarStyle barStyle)
		{
			if (barStyle == MultiPickerBarStyle.Black)
				return UIBarStyle.Black;

			if (barStyle == MultiPickerBarStyle.BlackOpaque)
				return UIBarStyle.BlackOpaque;

			if (barStyle == MultiPickerBarStyle.BlackTranslucent)
				return UIBarStyle.BlackTranslucent;

			return UIBarStyle.Default;
		}

		ELCImagePickerViewController(UIViewController rootController, StoreCameraMediaOptions options) : base(rootController)
		{
			this.options = options ?? new StoreCameraMediaOptions();
	}

		void SelectedAssets(List<ALAsset> assets)
		{
			var results = new List<MediaFile>();
			foreach (var asset in assets)
			{
				var obj = asset.AssetType;
				if (obj == default(ALAssetType))
					continue;

				var rep = asset.DefaultRepresentation;
				if (rep != null)
				{
					var mediaFile = GetPictureMediaFile(asset);
					if (mediaFile != null)
					{
						results.Add(mediaFile);
					}
				}
			}

			_TaskCompletionSource.TrySetResult(results);
		}

		void CancelledPicker()
		{
			_TaskCompletionSource.TrySetCanceled();
		}

		bool ShouldSelectAsset(ALAsset asset, int previousCount)
		{
			var shouldSelect = _maxImagesCount <= 0 || previousCount < _maxImagesCount;
			if (!shouldSelect)
			{
				string title = string.Format("Only {0} photos please!", _maxImagesCount);
				string message = string.Format("You can only send {0} photos at a time.", _maxImagesCount);
				var alert = new UIAlertView(title, message, null, null, "Okay");
				alert.Show();
			}
			return shouldSelect;
		}


		private MediaFile GetPictureMediaFile(ALAsset asset)
		{
			var rep = asset.DefaultRepresentation;
			if (rep == null)
				return null;

			var cgImage = rep.GetFullScreenImage();


			var path = MediaPickerDelegate.GetOutputPath(MediaImplementation.TypeImage,
				options.Directory ?? "temp",
				options.Name);
			
			var image = new UIImage(cgImage, 1.0f, UIImageOrientation.Up);

			var percent = 1.0f;
			if (options.PhotoSize != PhotoSize.Full)
			{
				try
				{
					switch (options.PhotoSize)
					{
						case PhotoSize.Large:
							percent = .75f;
							break;
						case PhotoSize.Medium:
							percent = .5f;
							break;
						case PhotoSize.Small:
							percent = .25f;
							break;
						case PhotoSize.Custom:
							percent = (float)options.CustomPhotoSize / 100f;
							break;
					}

					if (options.PhotoSize == PhotoSize.MaxWidthHeight && options.MaxWidthHeight.HasValue)
					{
						var max = Math.Max(image.CGImage.Width, image.CGImage.Height);
						if (max > options.MaxWidthHeight.Value)
						{
							percent = (float)options.MaxWidthHeight.Value / (float)max;
						}
					}

					if (percent < 1.0f)
					{
						//begin resizing image
						image = image.ResizeImageWithAspectRatio(percent);
					}

				}
				catch (Exception ex)
				{
					Console.WriteLine($"Unable to compress image: {ex}");
				}
			}


			NSDictionary meta = null;
			try
			{
				meta = PhotoLibraryAccess.GetPhotoLibraryMetadata(asset.AssetUrl);

			}
			catch (Exception ex)
			{
				Console.WriteLine($"Unable to get metadata: {ex}");
			}

			//iOS quality is 0.0-1.0
			var quality = (options.CompressionQuality / 100f);
			var savedImage = false;
			if (meta != null)
				savedImage = MediaPickerDelegate.SaveImageWithMetadata(image, quality, meta, path);

			if (!savedImage)
				image.AsJPEG(quality).Save(path, true);


			string aPath = null;
			//try to get the album path's url
			var url = asset.AssetUrl;
			aPath = url?.AbsoluteString;

			return new MediaFile(path, () => File.OpenRead(path), albumPath: aPath);
		}

		public class ELCAlbumPickerController : UITableViewController
		{
			static readonly NSObject _Dispatcher = new NSObject();
			readonly List<ALAssetsGroup> AssetGroups = new List<ALAssetsGroup>();

			public string PathToOverlay { get; set; }

			ALAssetsLibrary Library;

			WeakReference _Parent;
			public ELCImagePickerViewController Parent
			{
				get
				{
					return _Parent == null ? null : _Parent.Target as ELCImagePickerViewController;
				}
				set
				{
					_Parent = new WeakReference(value);
				}
			}

			public ELCAlbumPickerController()
			{
			}

			public override void ViewDidLoad()
			{
				base.ViewDidLoad();

				NavigationItem.Title = "Loading...";
				var cancelButton = new UIBarButtonItem(UIBarButtonSystemItem.Cancel/*, cancelImagePicker*/);
				cancelButton.Clicked += CancelClicked;
				NavigationItem.RightBarButtonItem = cancelButton;

				AssetGroups.Clear();

				Library = new ALAssetsLibrary();
				Library.Enumerate(ALAssetsGroupType.All, GroupsEnumerator, GroupsEnumeratorFailed);
			}

			public override void ViewDidDisappear(bool animated)
			{
				base.ViewDidDisappear(animated);
				if (IsMovingFromParentViewController || IsBeingDismissed)
				{
					NavigationItem.RightBarButtonItem.Clicked -= CancelClicked;
				}
			}

			void CancelClicked(object sender = null, EventArgs e = null)
			{
				var parent = Parent;
				if (parent != null)
				{
					parent.CancelledPicker();
				}
			}

			void GroupsEnumeratorFailed(NSError error)
			{
				Console.WriteLine("Enumerator failed!");
			}

			void GroupsEnumerator(ALAssetsGroup agroup, ref bool stop)
			{
				if (agroup == null)
				{
					return;
				}

				// added fix for camera albums order
				if (agroup.Name.ToString().ToLower() == "camera roll" && agroup.Type == ALAssetsGroupType.SavedPhotos)
				{
					AssetGroups.Insert(0, agroup);
				}
				else
				{
					AssetGroups.Add(agroup);
				}

				_Dispatcher.BeginInvokeOnMainThread(ReloadTableView);
			}

			void ReloadTableView()
			{
				TableView.ReloadData();
				NavigationItem.Title = "Select an Album";
			}

			public override nint NumberOfSections(UITableView tableView)
			{
				return 1;
			}

			public override nint RowsInSection(UITableView tableView, nint section)
			{
				return AssetGroups.Count;
			}

			public override UITableViewCell GetCell(UITableView tableView, NSIndexPath indexPath)
			{
				const string cellIdentifier = "Cell";

				var cell = tableView.DequeueReusableCell(cellIdentifier);
				if (cell == null)
				{
					cell = new UITableViewCell(UITableViewCellStyle.Default, cellIdentifier);
				}

				// Get count
				var g = AssetGroups[indexPath.Row];
				g.SetAssetsFilter(ALAssetsFilter.AllPhotos);
				var gCount = g.Count;
				cell.TextLabel.Text = string.Format("{0} ({1})", g.Name, gCount);
				try
				{
					cell.ImageView.Image = new UIImage(g.PosterImage);
				}
				catch (Exception e)
				{
					Console.WriteLine("Failed to set thumbnail {0}", e);
				}
				cell.Accessory = UITableViewCellAccessory.DisclosureIndicator;

				return cell;
			}

			public override void RowSelected(UITableView tableView, NSIndexPath indexPath)
			{
				var assetGroup = AssetGroups[indexPath.Row];
				assetGroup.SetAssetsFilter(ALAssetsFilter.AllPhotos);
				var picker = new ELCAssetTablePicker(assetGroup);
				picker.Parent = Parent;
				picker.PathToOverlay = PathToOverlay;
				NavigationController.PushViewController(picker, true);
			}

			public override nfloat GetHeightForRow(UITableView tableView, NSIndexPath indexPath)
			{
				return 57;
			}
		}


		class ELCAssetTablePicker : UITableViewController
		{
			static readonly NSObject _Dispatcher = new NSObject();

			int Columns = 4;
			public bool SingleSelection { get; set; }
			public bool ImmediateReturn { get; set; }
			public string PathToOverlay { get; set; }
			readonly ALAssetsGroup AssetGroup;

			readonly List<ELCAsset> ElcAssets = new List<ELCAsset>();

			WeakReference _Parent;
			public ELCImagePickerViewController Parent
			{
				get
				{
					return _Parent == null ? null : _Parent.Target as ELCImagePickerViewController;
				}
				set
				{
					_Parent = new WeakReference(value);
				}
			}

			public ELCAssetTablePicker(ALAssetsGroup assetGroup)
			{
				AssetGroup = assetGroup;
			}

			public override void ViewDidLoad()
			{
				TableView.SeparatorStyle = UITableViewCellSeparatorStyle.None;
				TableView.AllowsSelection = false;

				if (ImmediateReturn)
				{

				}
				else
				{
					var doneButtonItem = new UIBarButtonItem(UIBarButtonSystemItem.Done);
					doneButtonItem.Clicked += DoneClicked;
					NavigationItem.RightBarButtonItem = doneButtonItem;
					NavigationItem.Title = "Loading...";
				}

				Task.Run((Action)PreparePhotos);
			}

			public override void ViewWillAppear(bool animated)
			{
				base.ViewWillAppear(animated);
				Columns = (int)(View.Bounds.Size.Width / 80f);
			}

			public override void ViewDidDisappear(bool animated)
			{
				base.ViewDidDisappear(animated);
				if (IsMovingFromParentViewController || IsBeingDismissed)
				{
					NavigationItem.RightBarButtonItem.Clicked -= DoneClicked;
				}
			}

			public override void DidRotate(UIInterfaceOrientation fromInterfaceOrientation)
			{
				base.DidRotate(fromInterfaceOrientation);
				Columns = (int)(View.Bounds.Size.Width / 80f);
				TableView.ReloadData();
			}

			void PreparePhotos()
			{
				AssetGroup.Enumerate(PhotoEnumerator);

				_Dispatcher.BeginInvokeOnMainThread(() => {
					TableView.ReloadData();
					// scroll to bottom
					nint section = NumberOfSections(TableView) - 1;
					nint row = TableView.NumberOfRowsInSection(section) - 1;
					if (section >= 0 && row >= 0)
					{
						var ip = NSIndexPath.FromRowSection(row, section);
						TableView.ScrollToRow(ip, UITableViewScrollPosition.Bottom, false);
					}
					NavigationItem.Title = SingleSelection ? "Pick Photo" : "Pick Photos";
				});
			}

			void PhotoEnumerator(ALAsset result, nint index, ref bool stop)
			{
				if (result == null)
				{
					return;
				}

				ELCAsset elcAsset = new ELCAsset(this, result);

				bool isAssetFiltered = false;
				/*if (self.assetPickerFilterDelegate &&
                    [self.assetPickerFilterDelegate respondsToSelector:@selector(assetTablePicker:isAssetFilteredOut:)])
                {
                    isAssetFiltered = [self.assetPickerFilterDelegate assetTablePicker:self isAssetFilteredOut:(ELCAsset*)elcAsset];
                }*/

				if (result.DefaultRepresentation == null)
					isAssetFiltered = true;

				if (!isAssetFiltered)
				{
					ElcAssets.Add(elcAsset);
				}
			}

			void DoneClicked(object sender = null, EventArgs e = null)
			{
				var selected = new List<ALAsset>();

				foreach (var asset in ElcAssets)
				{
					if (asset.Selected)
					{
						selected.Add(asset.Asset);
					}
				}

				var parent = Parent;
				if (parent != null)
				{
					parent.SelectedAssets(selected);
				}
			}

			bool ShouldSelectAsset(ELCAsset asset)
			{
				int selectionCount = TotalSelectedAssets;
				bool shouldSelect = true;

				var parent = Parent;
				if (parent != null)
				{
					shouldSelect = parent.ShouldSelectAsset(asset.Asset, selectionCount);
				}

				return shouldSelect;
			}

			void AssetSelected(ELCAsset asset, bool selected)
			{
				TotalSelectedAssets += (selected) ? 1 : -1;

				if (SingleSelection)
				{
					foreach (var elcAsset in ElcAssets)
					{
						if (asset != elcAsset)
						{
							elcAsset.Selected = false;
						}
					}
				}
				if (ImmediateReturn)
				{
					var parent = Parent;
					var obj = new List<ALAsset>(1);
					obj.Add(asset.Asset);
					parent.SelectedAssets(obj);
				}
			}

			public override nint NumberOfSections(UITableView tableView)
			{
				return 1;
			}

			public override nint RowsInSection(UITableView tableView, nint section)
			{
				if (Columns <= 0)
					return 4;
				int numRows = (int)Math.Ceiling((float)ElcAssets.Count / Columns);
				return numRows;
			}

			List<ELCAsset> AssetsForIndexPath(NSIndexPath path)
			{
				int index = path.Row * Columns;
				int length = Math.Min(Columns, ElcAssets.Count - index);
				return ElcAssets.GetRange(index, length);
			}

			public override UITableViewCell GetCell(UITableView tableView, NSIndexPath indexPath)
			{
				const string cellIdentifier = "Cell";

				var cell = TableView.DequeueReusableCell(cellIdentifier) as ELCAssetCell;
				if (cell == null)
				{
					cell = new ELCAssetCell(UITableViewCellStyle.Default, cellIdentifier, PathToOverlay);
				}
				cell.SetAssets(AssetsForIndexPath(indexPath), Columns);
				return cell;
			}

			public override nfloat GetHeightForRow(UITableView tableView, NSIndexPath indexPath)
			{
				return 79;
			}

			public int TotalSelectedAssets;

			public class ELCAsset
			{
				public readonly ALAsset Asset;
				readonly WeakReference _Parent;

				bool _Selected;

				public ELCAsset(ELCAssetTablePicker parent, ALAsset asset)
				{
					_Parent = new WeakReference(parent);
					Asset = asset;
				}

				public void ToggleSelected()
				{
					Selected = !Selected;
				}

				public bool Selected
				{
					get
					{
						return _Selected;
					}

					set
					{
						var parent = _Parent.Target as ELCAssetTablePicker;
						if (value && parent != null && !parent.ShouldSelectAsset(this))
						{
							return;
						}

						_Selected = value;

						if (parent != null)
						{
							parent.AssetSelected(this, value);
						}
					}
				}
			}

			class ELCAssetCell : UITableViewCell
			{
				List<ELCAsset> RowAssets;
				int Columns;
				readonly List<UIImageView> ImageViewArray = new List<UIImageView>();
				readonly List<UIImageView> OverlayViewArray = new List<UIImageView>();

				private string _pathToOverlay;

				public ELCAssetCell(UITableViewCellStyle style, string reuseIdentifier, string pathToOverlay) : base(style, reuseIdentifier)
				{
					UITapGestureRecognizer tapRecognizer = new UITapGestureRecognizer(CellTapped);
					AddGestureRecognizer(tapRecognizer);
					_pathToOverlay = pathToOverlay;
				}

				private UIImage GetOverlayImage()
				{
					if (!string.IsNullOrEmpty(_pathToOverlay))
					{
						try
						{
							return new UIImage(_pathToOverlay);
						}
						catch
						{
							// Failed to load custom overlay image
						}
					}

					return UIImage.FromBundle("Overlay.png");
				}

				public void SetAssets(List<ELCAsset> assets, int columns)
				{
					RowAssets = assets;
					Columns = columns;

					foreach (var view in ImageViewArray)
					{
						view.RemoveFromSuperview();
					}
					foreach (var view in OverlayViewArray)
					{
						view.RemoveFromSuperview();
					}

					UIImage overlayImage = null;
					for (int i = 0; i < RowAssets.Count; i++)
					{
						var asset = RowAssets[i];

						if (i < ImageViewArray.Count)
						{
							var imageView = ImageViewArray[i];
							imageView.Image = new UIImage(asset.Asset.Thumbnail);
						}
						else
						{
							var imageView = new UIImageView(new UIImage(asset.Asset.Thumbnail));
							ImageViewArray.Add(imageView);
						}

						if (i < OverlayViewArray.Count)
						{
							var overlayView = OverlayViewArray[i];
							overlayView.Hidden = !asset.Selected;
						}
						else
						{
							if (overlayImage == null)
							{
								overlayImage = GetOverlayImage();
							}
							var overlayView = new UIImageView(overlayImage);
							OverlayViewArray.Add(overlayView);
							overlayView.Hidden = !asset.Selected;
						}
					}
				}

				void CellTapped(UITapGestureRecognizer tapRecognizer)
				{
					CGPoint point = tapRecognizer.LocationInView(this);
					var totalWidth = Columns * 75 + (Columns - 1) * 4;
					var startX = (Bounds.Size.Width - totalWidth) / 2;

					var frame = new RectangleF((float)startX, 2, 75, 75);
					for (int i = 0; i < RowAssets.Count; ++i)
					{
						if (frame.Contains((float)point.X, (float)point.Y))
						{
							ELCAsset asset = RowAssets[i];
							asset.Selected = !asset.Selected;
							var overlayView = OverlayViewArray[i];
							overlayView.Hidden = !asset.Selected;
							break;
						}
						var x = frame.X + frame.Width + 4;
						frame = new RectangleF(x, frame.Y, frame.Width, frame.Height);
					}
				}

				public override void LayoutSubviews()
				{
					var totalWidth = Columns * 75 + (Columns - 1) * 4;
					var startX = (Bounds.Size.Width - totalWidth) / 2;

					var frame = new RectangleF((float)startX, 2, 75, 75);

					int i = 0;
					foreach (var imageView in ImageViewArray)
					{
						imageView.Frame = frame;
						AddSubview(imageView);

						var overlayView = OverlayViewArray[i++];
						overlayView.Frame = frame;
						AddSubview(overlayView);

						var x = frame.X + frame.Width + 4;
						frame = new RectangleF(x, frame.Y, frame.Width, frame.Height);
					}
				}
			}
		}
	}
}